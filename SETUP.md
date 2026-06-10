# 🚀 Hướng dẫn Setup Development Environment

## 📋 Yêu cầu

- .NET 6.0 SDK trở lên
- Node.js 16+ (cho Firebase Emulator)
- Visual Studio 2022 (hoặc VS Code)
- Git

## 1️⃣ Clone Repository

```bash
git clone https://github.com/Salt05/EMS.git
cd EMS
```

## 2️⃣ Cấu hình Firebase

### Option A: Dùng Firebase Emulator (Recommended - Development)

#### Setup Firebase Emulator

```bash
# Cài Firebase CLI
npm install -g firebase-tools

# Login Firebase
firebase login

# Khởi động emulator
cd firebase-emulator
firebase emulators:start

# Lưu ý: Các port mặc định
# - Firestore: localhost:8080
# - Auth: localhost:9099
# - Emulator UI: localhost:4000
```

Tạo file `.env.local` trong `src/EMS.WebAPI/`:

```env
FIRESTORE_EMULATOR_HOST=localhost:8080
FIREBASE_AUTH_EMULATOR_HOST=localhost:9099
FIREBASE_PROJECT_ID=demo-project
FIREBASE_API_KEY=demo-api-key
```

### Option B: Dùng Firebase Console (Production/Staging)

1. Tạo project tại https://console.firebase.google.com/
2. Download `serviceAccountKey.json` từ Project Settings → Service Accounts
3. Lưu vào `src/EMS.WebAPI/serviceAccountKey.json`
4. Cập nhật `appsettings.json`:

```json
{
  "Firebase": {
	"ProjectId": "your-firebase-project",
	"ApiKey": "your-api-key",
	"AuthDomain": "your-project.firebaseapp.com"
  }
}
```

## 3️⃣ Cấu hình appsettings.json

Sửa file `src/EMS.WebAPI/appsettings.Development.json`:

```json
{
  "Firebase": {
	"ProjectId": "demo-project",
	"ApiKey": "AIzaSyDxKKKKKKKKKKKKKKKKKKKKKKKK",
	"AuthDomain": "demo-project.firebaseapp.com",
	"DatabaseURL": "https://demo-project.firebaseio.com",
	"StorageBucket": "demo-project.appspot.com",
	"MessagingSenderId": "123456789",
	"AppId": "1:123456789:web:abcdef123456"
  },
  "Jwt": {
	"SecretKey": "your-super-secret-key-must-be-at-least-32-characters-long-for-hs256",
	"Issuer": "ems",
	"Audience": "ems-users",
	"ExpirationMinutes": 60
  },
  "AllowedOrigins": [
	"http://localhost:3000",
	"http://localhost:4200",
	"http://localhost:5173",
	"http://*.localhost:*"
  ],
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning",
	  "EMS": "Debug"
	}
  }
}
```

## 4️⃣ Build & Run

```bash
# Build solution
dotnet build

# Run WebAPI
cd src/EMS.WebAPI
dotnet run

# API sẽ chạy tại: https://localhost:7xxx
# Swagger UI: https://localhost:7xxx/swagger/index.html
```

## 5️⃣ Setup Firestore Collections

### A. Tạo Tenant

Truy cập Firestore → Tạo collection `tenants`:

```
Document ID: tenant-1
{
  "id": "tenant-1",
  "name": "Test Tenant",
  "subdomain": "test",
  "email": "admin@test.com",
  "phoneNumber": "",
  "address": "",
  "createdAt": 2024-01-01T00:00:00Z,
  "isActive": true
}
```

### B. Tạo Roles

Collection `roles`:

```
Document ID: role-employee
{
  "id": "role-employee",
  "type": 3,
  "name": "Employee",
  "description": "Employee role",
  "tenantId": "tenant-1",
  "createdAt": 2024-01-01T00:00:00Z
}
```

## 6️⃣ Test API

### Với Postman

#### 1. Register

```http
POST /api/auth/register
Host: test.localhost:7xxx
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "Password123!",
  "fullName": "Test User",
  "mssv": "SV001"
}
```

Response:
```json
{
  "message": "Registration successful",
  "userId": "user-uuid"
}
```

#### 2. Login

```http
POST /api/auth/login
Host: test.localhost:7xxx
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "Password123!"
}
```

Response:
```json
{
  "userId": "user-uuid",
  "email": "user@test.com",
  "fullName": "Test User",
  "accessToken": "eyJhbGc...",
  "expiresIn": 3600,
  "roles": ["employee"]
}
```

#### 3. Sử dụng Token

```http
GET /api/protected-endpoint
Host: test.localhost:7xxx
Authorization: Bearer eyJhbGc...
```

## 7️⃣ Cấu hình localhost

### Windows (Notepad Admin)

Sửa `C:\Windows\System32\drivers\etc\hosts`:

```
127.0.0.1  localhost
127.0.0.1  ems.com
127.0.0.1  test.ems.com
127.0.0.1  tenant1.ems.com
127.0.0.1  tenant2.ems.com
```

### Mac/Linux

```bash
sudo nano /etc/hosts

# Thêm:
127.0.0.1  ems.com test.ems.com tenant1.ems.com tenant2.ems.com
```

## 8️⃣ Logs & Debugging

Logs được lưu trong thư mục `logs/`:

```bash
# Xem logs real-time
tail -f logs/ems-*.txt

# Hoặc dùng Serilog Console output
```

## ❌ Troubleshooting

### "Tenant not found"

Kiểm tra:
1. Subdomain trong URL đúng không? (`test.localhost:5000`)
2. Collection `tenants` có document không?
3. Subdomain phải lowercase

```bash
# Debug in Firestore Emulator UI
# localhost:4000
```

### "Jwt:SecretKey not configured"

Kiểm tra `appsettings.json` có `Jwt:SecretKey` với độ dài ≥ 32 ký tự

### "Firebase initialization failed"

- Nếu dùng emulator: Kiểm tra `FIRESTORE_EMULATOR_HOST` environment variable
- Nếu dùng production: Kiểm tra `serviceAccountKey.json` đúng vị trí

### Port conflict

```bash
# Tìm process chạy trên port
# Windows
netstat -ano | findstr :5000

# Mac/Linux
lsof -i :5000

# Kill process
taskkill /PID <PID> /F
```

## 📚 Tài liệu bổ sung

- [Firebase Emulator Suite](https://firebase.google.com/docs/emulator-suite)
- [ASP.NET Core JWT](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer)
- [Cloud Firestore .NET Client](https://googleapis.dev/nodejs/firestore/latest/)

---

**Happy Coding! 🎉**
