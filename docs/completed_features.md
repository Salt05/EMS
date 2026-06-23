# 📜 Nhật Ký Hoàn Thành Tính Năng (Completed Features Log)

Tài liệu này ghi nhận chi tiết kỹ thuật các tính năng và nhiệm vụ đã hoàn thành của hệ thống quản lý sự kiện **EMS (Event Management System)**.

---

## 🏁 Danh Sách Tính Năng Đã Hoàn Thành

| STT | Mã Task | Tên Tính Năng | Ngày Hoàn Thành | Mô Tả Tóm Tắt |
|---|---|---|---|---|
| 1 | `Phase 0` | Setup Solution & Infrastructure | 2026-06-10 | Khởi tạo cấu trúc dự án (.NET 6), NuGet packages, Serilog, CORS và Swagger. |
| 2 | `P1_E1` | Firebase Setup & Emulator | 2026-06-11 | Khởi tạo Admin SDK, liên kết Firestore và cấu hình Emulator môi trường local. |
| 3 | `P1_E2` | Tenant Resolution Middleware | 2026-06-11 | Middleware tự động bóc tách subdomain của request để định danh tenant tương ứng. |
| 4 | `P1_E3` | Authentication APIs (WebAPI) | 2026-06-11 | API Register, Login, Reset mật khẩu kết hợp Firebase Auth client & tự sinh JWT token. |
| 5 | `P1_E7` | Blazor WASM Authentication UI | 2026-06-12 | Trang Đăng nhập/Đăng xuất Blazor, phân quyền router, auto-attach token & Tenant ID vào HTTP Header. |
| 6 | `EMS-10` | Event CRUD & Approval APIs | 2026-06-14 | Các API RESTful quản lý vòng đời sự kiện, tích hợp phê duyệt/từ chối từ Admin và isolation dữ liệu theo Tenant. |
| 7 | `EMS-12` | MyEvents & Event Forms (Blazor) | 2026-06-14 | Giao diện bảng danh sách sự kiện cho Organizer, form thêm mới/sửa sự kiện, xử lý lỗi cấu hình API CORS & Firestore Serialization. |
| 8 | `EMS-13` | Event Listing (Cards) & Detail Page (MVC) | 2026-06-21 | Trang danh sách sự kiện và chi tiết sự kiện cho Cổng sinh viên (MVC) tích hợp dữ liệu Firestore, lọc tìm kiếm từ khóa, tự động định danh Tenant và giao diện Glassmorphism. |
| 9 | `EMS-14` | Student MyEvents & Registration Flow | 2026-06-23 | Tính năng đăng ký, hủy tham gia sự kiện và dashboard xem các sự kiện đã đăng ký của sinh viên (MVC). |

---

## 🔍 Chi Tiết Kỹ Thuật Từng Tính Năng

### 1. Setup Solution & Infrastructure (Phase 0)
*   **Mô tả**: Thiết lập nền tảng dự án dạng Multi-tier architecture.
*   **Chi tiết thực hiện**:
    *   Tạo solution `EMS.sln` chứa các dự án thành phần: `EMS.Core`, `EMS.Infrastructure`, `EMS.WebAPI`, `EMS.Mvc`, `EMS.BlazorWASM`, `EMS.Shared`, và `EMS.Tests`.
    *   Tham chiếu chéo đúng thiết kế: `Core` độc lập; `Infrastructure` triển khai interface của `Core`; `WebAPI` / `Mvc` tham chiếu đến cả hai; `BlazorWASM` chỉ tham chiếu đến `Shared`.
    *   Cấu hình **Serilog** ghi log ra Console & File hàng ngày, mở cổng **CORS Policy** cho Blazor/MVC, tích hợp **Swagger UI** hỗ trợ Bearer Token.
*   **Các file quan trọng**: `EMS.sln`, `src/EMS.WebAPI/Program.cs`, `src/EMS.WebAPI/appsettings.json`.

---

### 2. Firebase & Firestore Integration (Phase 1 - Epic E1.1)
*   **Mô tả**: Tích hợp Firebase BaaS thay thế cho SQL Server truyền thống.
*   **Chi tiết thực hiện**:
    *   Đăng ký cấu hình SDK Firebase Admin SDK trên Backend để làm việc với Firestore DB, Firebase Auth.
    *   Cài đặt tệp key bảo mật `firebase-key.json` (được đưa vào `.gitignore` để bảo mật).
    *   Cài đặt môi trường phát triển local thông qua **Firebase Local Emulator Suite** (Auth port `9099`, Firestore port `8080`, UI port `4000`).
*   **Các file quan trọng**: `src/EMS.WebAPI/firebase-key.json`, `SETUP.md`.

---

### 3. Tenant Resolution (Phase 1 - Epic E1.2)
*   **Mô tả**: Thiết lập cơ chế phân giải Multi-tenant tự động.
*   **Chi tiết thực hiện**:
    *   Tạo `TenantMiddleware` bóc tách host để xác định subdomain (ví dụ: `test.localhost:5001` -> subdomain `test`).
    *   Tạo `TenantResolver` và `FirestoreTenantService` truy vấn collection `tenants` trong Firestore bằng subdomain nhận được để lấy `TenantId`.
    *   Lưu thông tin tenant vào `HttpContext.Items["Subdomain"]` để các controller và service khác sử dụng trong luồng request.
*   **Các file quan trọng**:
    *   `src/EMS.WebAPI/Middleware/TenantMiddleware.cs`
    *   `src/EMS.Infrastructure/Services/TenantResolver.cs`
    *   `src/EMS.Infrastructure/Services/FirestoreTenantService.cs`

---

### 4. Authentication API (Phase 1 - Epic E1.3)
*   **Mô tả**: Cung cấp các API đăng ký, đăng nhập và phân quyền cho người dùng.
*   **Chi tiết thực hiện**:
    *   Hiện thực `FirebaseAuthService` gọi REST API của Firebase để thực hiện đăng ký tài khoản và đăng nhập email/mật khẩu.
    *   Tạo `FirestoreUserService` quản lý hồ sơ người dùng trong collection `users`, thực hiện lưu thông tin và kiểm tra `tenantId` cách ly.
    *   Hiện thực `JwtService` mã hóa JWT chứa các Claims: `userId`, `email`, `fullName`, `tenantId`, và mảng danh sách `roles` của người dùng.
    *   Viết `AuthController` cung cấp 3 endpoint: `POST /api/auth/register`, `POST /api/auth/login`, và `POST /api/auth/forgot-password`.
*   **Các file quan trọng**:
    *   `src/EMS.WebAPI/Controllers/AuthController.cs`
    *   `src/EMS.Infrastructure/Services/FirebaseAuthService.cs`
    *   `src/EMS.Infrastructure/Services/FirestoreUserService.cs`
    *   `src/EMS.Infrastructure/Services/JwtService.cs`

---

### 5. Blazor WASM Authentication UI (Phase 1 - Epic E1.7)
*   **Mô tả**: Xây dựng cơ chế đăng nhập và xác thực phía Client-side cho Blazor WebAssembly.
*   **Chi tiết thực hiện**:
    *   **CustomAuthStateProvider**: Quản lý lưu trữ/đọc JWT trong `localStorage` bằng `Blazored.LocalStorage`, phân tích claims để cập nhật trạng thái đăng nhập cho toàn bộ ứng dụng Blazor.
    *   **AuthorizationMessageHandler**: DelegatingHandler tự động đính kèm `Authorization: Bearer <token>` và Header định danh Tenant `X-Tenant-ID` vào mọi HTTP request gửi lên WebAPI.
    *   **Auth UI**: 
        *   Trang `Login.razor` thiết kế layout tối giản (MinimalLayout), hỗ trợ nhập liệu và gọi service xác thực.
        *   Trang `Logout.razor` xóa token trong bộ nhớ trình duyệt và chuyển hướng về đăng nhập.
        *   Cập nhật `App.razor` sử dụng `CascadingAuthenticationState` và `AuthorizeRouteView` để bảo vệ các trang dashboard bên trong.
    *   **TenantSwitcher**: Dropdown chỉ hiển thị cho tài khoản admin/superadmin để chuyển đổi nhanh giữa các Tenant trong phiên làm việc.
*   **Các file quan trọng**:
    *   `src/EMS.BlazorWASM/Services/CustomAuthStateProvider.cs`
    *   `src/EMS.BlazorWASM/Services/AuthorizationMessageHandler.cs`
    *   `src/EMS.BlazorWASM/Pages/Login.razor`
    *   `src/EMS.BlazorWASM/Shared/TenantSwitcher.razor`
    *   `src/EMS.BlazorWASM/wwwroot/css/app.css` (Dark theme design system)

---

### 6. Event CRUD & Approval APIs (Task EMS-10)
*   **Mô tả**: Thiết lập Web API RESTful quản lý vòng đời sự kiện, tích hợp phê duyệt sự kiện và phân lập dữ liệu.
*   **Chi tiết thực hiện**:
    *   Tạo `Event` entity và DTO (CreateEventDto, UpdateEventDto, EventResponseDto...).
    *   Cài đặt `FirestoreEventService` thực hiện CRUD trên collection `events` của Firebase.
    *   Tích hợp isolation đa khách hàng: API yêu cầu gửi `tenantId`, chặn lấy/xóa dữ liệu khác tenant.
    *   Viết endpoint riêng `POST /{id}/approve` và `POST /{id}/reject` dành cho Admin/Manager.
*   **Các file quan trọng**:
    *   `src/EMS.WebAPI/Controllers/EventsController.cs`
    *   `src/EMS.Infrastructure/Services/FirestoreEventService.cs`
    *   `src/EMS.Core/Entities/Event.cs`

---

### 7. MyEvents & Event Forms UI (Task EMS-12)
*   **Mô tả**: Xây dựng giao diện Frontend Blazor WASM cho Organizer thao tác với sự kiện.
*   **Chi tiết thực hiện**:
    *   Tạo `EventServiceClient` đóng gói logic HTTP gọi lên API.
    *   Trang **MyEvents** (`/organizer/my-events`): Bảng danh sách sự kiện kèm nút Edit/Delete (có xác thực Authorization Role).
    *   Trang **Create/Edit Event**: Form nhập thông tin ứng dụng EditForm của Blazor, validate Model.
    *   **Bug Fixes ngoài lề**: 
        *   Cập nhật `ApiBaseUrl` của Frontend trùng khớp port `7296` của Backend.
        *   Sửa lỗi CORS: thêm `7115` vào `AllowedOrigins`.
        *   Cập nhật hàm `ToFirestoreDocument()` của `Event.cs` và `User.cs`: chặn đưa `DateTime.MinValue` lên Firestore để tránh lỗi serialize; sử dụng UTC theo yêu cầu của Google Cloud.
        *   Sửa lỗi fallback Localhost của `TenantResolver` khi domain không có dấu ".".
*   **Các file quan trọng**:
    *   `src/EMS.BlazorWASM/Pages/Organizer/MyEvents.razor`
    *   `src/EMS.BlazorWASM/Pages/Organizer/CreateEvent.razor`
    *   `src/EMS.BlazorWASM/Pages/Organizer/EditEvent.razor`
    *   `src/EMS.BlazorWASM/Services/EventServiceClient.cs`

---

### 8. Event Listing & Detail Page (MVC) (Task EMS-13)
*   **Mô tả**: Xây dựng giao diện Frontend ASP.NET Core MVC cho Student Portal để tìm kiếm và xem chi tiết sự kiện.
*   **Chi tiết thực hiện**:
    *   Tích hợp Firestore: Cấu hình Firebase Project trong `appsettings.json`, đăng ký các dịch vụ `FirestoreDb`, `ITenantService`, `IEventService` trong `Program.cs`.
    *   Tenant resolution: Viết `TenantMiddleware` bóc tách subdomain để tự động phân giải Tenant ID và Tenant Name cho mỗi request, có cơ chế fallback thông minh cho môi trường local.
    *   Trang chủ: Cập nhật `HomeController` và `Index.cshtml` hiển thị các sự kiện thực tế có trạng thái `Approved` từ Firestore.
    *   Trang danh sách: Tạo `EventsController` và view `Events/Index` tích hợp bộ tìm kiếm từ khóa (tên, mô tả, địa điểm) với thiết kế Glassmorphism và micro-animations.
    *   Trang chi tiết: Tạo view `Events/Detail` hiển thị toàn bộ thông tin sự kiện (thời gian, địa điểm, sức chứa, lệ phí miễn phí) với tính năng kiểm tra đăng nhập trước khi hiển thị nút đăng ký.
*   **Các file quan trọng**:
    *   `src/EMS.Mvc/Program.cs`
    *   `src/EMS.Mvc/Middlewares/TenantMiddleware.cs`
    *   `src/EMS.Mvc/Controllers/EventsController.cs`
    *   `src/EMS.Mvc/Views/Events/Index.cshtml`
    *   `src/EMS.Mvc/Views/Events/Detail.cshtml`

---

### 9. Student MyEvents & Registration Flow (Task EMS-14)
*   **Mô tả**: Thiết lập cơ chế đăng ký và hủy tham gia sự kiện của sinh viên, cùng với dashboard quản lý vé hoạt động và lịch sử đăng ký.
*   **Chi tiết thực hiện**:
    *   **Core Entity**: Tạo `RegistrationStatus` enum và `Registration` entity lưu giữ thông tin đăng ký (EventId, StudentEmail, StudentName, Status, CreatedAt, UpdatedAt) kèm Firestore mapping.
    *   **Custom Exceptions**: Tạo `BusinessRuleException` và `NotFoundException` để biểu diễn các lỗi logic nghiệp vụ.
    *   **Service Layer**: Thiết lập `IRegistrationService` và triển khai song song `FirestoreRegistrationService` (quản lý Firestore cách ly theo Tenant) và `DevInMemoryRegistrationService` (lưu trữ in-memory phục vụ debug nhanh local).
    *   **Dependency Injection**: Cấu hình đăng ký `IRegistrationService` trong cả WebAPI và MVC `Program.cs`.
    *   **MVC Controller**: Bổ sung các action `Register` (đăng ký kèm kiểm tra sức chứa và trạng thái sự kiện), `Cancel` (hủy vé), và `MyEvents` (xem danh sách vé cá nhân) vào `EventsController.cs`.
    *   **Giao diện Portal**:
        *   Cập nhật `Detail.cshtml`: Cho phép hiển thị nút Đăng ký / Hủy đăng ký hoặc trạng thái chờ/từ chối động tùy theo thông tin đăng ký của sinh viên đang đăng nhập.
        *   Tạo mới `MyEvents.cshtml`: Dashboard phân chia thành 2 tab "Vé hoạt động" (Đã xác nhận, Chờ duyệt) và "Lịch sử & Đã hủy" cực kỳ trực quan và đồng bộ phong cách Glassmorphism.
        *   Cập nhật `_Layout.cshtml`: Thêm link "Sự kiện của tôi" trên thanh menu điều hướng khi sinh viên đăng nhập.
*   **Các file quan trọng**:
    *   `src/EMS.Core/Entities/Registration.cs` & `Enums/RegistrationStatus.cs`
    *   `src/EMS.Core/Interfaces/Services/IRegistrationService.cs`
    *   `src/EMS.Infrastructure/Services/FirestoreRegistrationService.cs`
    *   `src/EMS.Mvc/Services/DevInMemoryRegistrationService.cs`
    *   `src/EMS.Mvc/Controllers/EventsController.cs`
    *   `src/EMS.Mvc/Views/Events/Detail.cshtml`
    *   `src/EMS.Mvc/Views/Events/MyEvents.cshtml`
    *   `src/EMS.Mvc/Views/Shared/_Layout.cshtml`
