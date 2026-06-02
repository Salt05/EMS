# Hướng dẫn Tích hợp Firebase

## 1. Giới thiệu

Firebase là nền tảng BaaS (Backend-as-a-Service) được sử dụng cho EMS thay vì SQL Server. Các service Firebase được sử dụng:

| Service | Mục đích |
|---------|----------|
| **Firebase Authentication** | Đăng nhập, đăng ký, quản lý người dùng |
| **Cloud Firestore** | Cơ sở dữ liệu NoSQL chính |
| **Firebase Storage** | Lưu trữ ảnh banner, tài liệu agenda |
| **Firebase Security Rules** | Bảo mật dữ liệu multi-tenant |

## 2. Cấu hình Dự án Firebase

### 2.1 Tạo dự án Firebase

1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Nhấn **"Create a project"**
3. Đặt tên: `EMS-Production` (hoặc `EMS-Staging`, `EMS-Dev`)
4. Tắt Google Analytics (hoặc bật nếu cần)
5. Nhấn **"Create project"**

### 2.2 Thêm ứng dụng Web (cho Blazor WASM và MVC)

```javascript
// firebase-config.js (sẽ nhúng vào layout)
const firebaseConfig = {
  apiKey: "AIzaSyD...",
  authDomain: "ems-production.firebaseapp.com",
  projectId: "ems-production",
  storageBucket: "ems-production.appspot.com",
  messagingSenderId: "123456789",
  appId: "1:123456789:web:abcdef"
};
```

**Lưu ý:** Cấu hình này chỉ dùng cho client-side (MVC, Blazor WASM). Backend .NET dùng Admin SDK.

### 2.3 Tạo Service Account cho Backend

1. Trong Firebase Console: **Project Settings → Service Accounts**
2. Chọn **".NET"** tab
3. Nhấn **"Generate new private key"**
4. Tải file JSON về và đặt tại `EMS.WebAPI/secure/firebase-service-account.json`
5. **QUAN TRỌNG:** Thêm file này vào `.gitignore`

## 3. Cài đặt Firebase SDK cho .NET

### 3.1 Packages cần cài

```bash
# Trong EMS.Infrastructure
dotnet add package FirebaseAdmin --version 2.4.0
dotnet add package Google.Cloud.Firestore --version 3.5.0
dotnet add package Google.Cloud.Storage.V1 --version 4.10.0
```

### 3.2 Khởi tạo Firebase Admin SDK

```csharp
// Program.cs của EMS.WebAPI
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var credentialPath = Path.Combine(AppContext.BaseDirectory, "secure", "firebase-service-account.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.GetApplicationDefault(),
    ProjectId = "ems-production"
});
```

## 4. Thiết kế Firestore Database

### 4.1 Cấu trúc Collection

```text
# Global collections (không có tenant prefix)
users/{userId}
  - email: string
  - tenantId: string
  - role: string
  - mssv: string
  - fullName: string
  - phoneNumber: string
  - isActive: boolean
  - createdAt: timestamp

tenants/{tenantId}
  - name: string
  - subdomain: string
  - primaryColor: string
  - logoUrl: string
  - settings: map
      maxRegistrationsPerStudent: number
      allowWaitlist: boolean
      defaultCheckInCodeExpiry: number
  - createdAt: timestamp

# Tenant-specific collections (prefix là tenantId)
{tenantId}/events/{eventId}
  - publicId: string
  - title: string
  - description: string
  - imageUrl: string
  - venueId: string (reference)
  - organizerId: string
  - startTime: timestamp
  - endTime: timestamp
  - registrationStart: timestamp
  - registrationEnd: timestamp
  - maxCapacity: number
  - trainingPoints: number
  - requireApproval: boolean
  - status: string
  - checkInCode: string
  - checkInCodeExpiry: timestamp
  - createdAt: timestamp
  - updatedAt: timestamp

{tenantId}/venues/{venueId}
  - name: string
  - address: string
  - capacity: number
  - mapUrl: string
  - isActive: boolean

{tenantId}/registrations/{registrationId}
  - eventId: reference
  - studentId: string
  - registeredAt: timestamp
  - status: string
  - isAttended: boolean
  - checkInTime: timestamp
  - cancelledAt: timestamp
  - cancelReason: string

{tenantId}/waitlist/{waitlistId}
  - eventId: reference
  - studentId: string
  - joinedAt: timestamp
  - notifiedAt: timestamp
  - status: string

{tenantId}/agendaItems/{agendaId}
  - eventId: reference
  - startTime: timestamp
  - endTime: timestamp
  - title: string
  - description: string
  - speaker: string
  - materialUrl: string
  - order: number

{tenantId}/emailTemplates/{templateId}
  - name: string
  - subject: string
  - body: string
  - isActive: boolean
```

### 4.2 Ví dụ truy vấn Firestore (Repository)

```csharp
// Infrastructure/Repositories/EventRepository.cs
public async Task<List<Event>> GetApprovedEventsAsync(string tenantId, int limit, string? startAfterId = null)
{
    var collection = _firestoreDb.Collection($"{tenantId}/events");
    var query = collection
        .WhereEqualTo("status", "Approved")
        .OrderBy("startTime")
        .Limit(limit);
  
    if (!string.IsNullOrEmpty(startAfterId))
    {
        var startAfterDoc = await collection.Document(startAfterId).GetSnapshotAsync();
        query = query.StartAfter(startAfterDoc);
    }
  
    var snapshot = await query.GetSnapshotAsync();
    return snapshot.Documents.Select(doc => doc.ConvertTo<Event>()).ToList();
}
```

## 5. Firebase Authentication Integration

### 5.1 Đăng ký user (Firebase Auth + Firestore)

```csharp
// Infrastructure/Services/AuthService.cs
public async Task<AuthResult> RegisterAsync(RegisterRequest request, string tenantId)
{
    // 1. Tạo user trong Firebase Auth
    var userRecordArgs = new UserRecordArgs()
    {
        Email = request.Email,
        Password = request.Password,
        DisplayName = request.FullName,
        PhoneNumber = request.PhoneNumber,
        Uid = request.MSSV  // Dùng MSSV làm UID
    };
  
    var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
  
    // 2. Gán custom claims (tenantId, role)
    var claims = new Dictionary<string, object>
    {
        { "tenantId", tenantId },
        { "role", "student" },
        { "mssv", request.MSSV }
    };
    await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);
  
    // 3. Lưu thông tin user vào Firestore (users collection global)
    var userDoc = new User
    {
        Id = userRecord.Uid,
        Email = request.Email,
        TenantId = tenantId,
        Role = "student",
        MSSV = request.MSSV,
        FullName = request.FullName,
        PhoneNumber = request.PhoneNumber,
        CreatedAt = Timestamp.GetCurrentTimestamp()
    };
  
    await _firestoreDb.Collection("users").Document(userRecord.Uid).SetAsync(userDoc);
  
    return new AuthResult { Success = true, UserId = userRecord.Uid };
}
```

### 5.2 Tạo JWT từ Firebase Token

```csharp
// Infrastructure/Services/JwtService.cs
public async Task<string> GenerateJwtTokenAsync(string firebaseToken)
{
    // Verify Firebase token
    var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(firebaseToken);
    var uid = decodedToken.Uid;
  
    // Lấy user từ Firestore
    var userDoc = await _firestoreDb.Collection("users").Document(uid).GetSnapshotAsync();
    var user = userDoc.ConvertTo<User>();
  
    // Tạo JWT
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, uid),
        new Claim("tenantId", user.TenantId),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("mssv", user.MSSV)
    };
  
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
  
    var token = new JwtSecurityToken(
        issuer: _issuer,
        audience: _audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);
  
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

## 6. Firebase Storage (Image Upload)

### 6.1 Upload file

```csharp
// Infrastructure/Services/StorageService.cs
public async Task<string> UploadEventImageAsync(string tenantId, string eventId, Stream fileStream, string fileName)
{
    var storageClient = StorageClient.Create();
    var bucketName = $"{_projectId}.appspot.com";
    var objectName = $"tenants/{tenantId}/events/{eventId}/{Guid.NewGuid()}_{fileName}";
  
    using var memoryStream = new MemoryStream();
    await fileStream.CopyToAsync(memoryStream);
    memoryStream.Position = 0;
  
    var result = await storageClient.UploadObjectAsync(bucketName, objectName, "image/jpeg", memoryStream);
  
    // Tạo public URL (nếu bucket là public)
    return $"https://storage.googleapis.com/{bucketName}/{objectName}";
}
```

## 7. Firestore Security Rules

### 7.1 File `firestore.rules`

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Global users collection - user chỉ được đọc chính mình
    match /users/{userId} {
      allow read: if request.auth != null && request.auth.uid == userId;
      allow write: if request.auth != null && request.auth.uid == userId;
    }
  
    // Tenants collection - chỉ admin tenant hoặc super admin
    match /tenants/{tenantId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && 
        (request.auth.token.role == 'superadmin' || 
         (request.auth.token.tenantId == tenantId && request.auth.token.role == 'admin'));
    }
  
    // Tenant data isolation
    match /{tenantId}/{document=**} {
      allow read, write: if request.auth != null 
        && request.auth.token.tenantId == tenantId;
    }
  
    // Registration validation - student chỉ được tạo registration cho chính mình
    match /{tenantId}/registrations/{registrationId} {
      allow create: if request.auth.token.role == 'student' 
        && request.resource.data.studentId == request.auth.uid;
      allow read: if request.auth.token.role in ['admin', 'organizer']
        || resource.data.studentId == request.auth.uid;
    }
  }
}
```

### 7.2 Deploy rules

```bash
firebase deploy --only firestore:rules
```

## 8. Firebase Emulator cho Development

### 8.1 Cài đặt Firebase CLI

```bash
npm install -g firebase-tools
firebase login
firebase init emulators
```

### 8.2 Chạy emulator

```bash
firebase emulators:start --project demo-test
```

### 8.3 Kết nối .NET với Emulator

```csharp
// Trong Program.cs (Development only)
if (env.IsDevelopment())
{
    Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");
    Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", "localhost:9099");
}
```

## 9. Xử lý Lỗi Firebase Thường Gặp

| Lỗi                                                            | Nguyên nhân                     | Giải pháp                                            |
| --------------------------------------------------------------- | --------------------------------- | ------------------------------------------------------ |
| `Grpc.Core.RpcException: Status(StatusCode=PermissionDenied)` | Security rules từ chối          | Kiểm lại rules, đảm bảo request.auth có tenantId |
| `FirebaseAuthException: User not found`                       | UID không tồn tại              | Kiểm tra user được tạo trước khi gán claims    |
| `FirebaseStorageException: Bucket not found`                  | Storage bucket chưa được tạo | Tạo bucket trong Firebase Console                     |
| `FirebaseException: Quota exceeded`                           | Vượt quota free tier            | Nâng cấp lên Blaze plan hoặc tối ưu query        |

## 10. Best Practices

1. **Không dùng transaction cho mọi thao tác** – Firestore transaction giới hạn 500 writes.
2. **Dùng batch writes** cho nhiều thao tác độc lập.
3. **Denormalize dữ liệu** – ví dụ lưu event title trong registration để tránh read thêm.
4. **Tạo composite indexes** cho các query thường dùng (Events theo status + startTime).
5. **Sử dụng `Asynchronous` methods** cho mọi Firestore call.
6. **Không lưu thông tin nhạy cảm** (như password) trong Firestore.
