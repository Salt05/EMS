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
| 8 | `EMS-13` | Registration APIs | 2026-06-19 | API đăng ký/huỷ/waitlist tham gia sự kiện và duyệt/từ chối đăng ký, isolation theo Tenant, tự động đẩy waitlist khi có chỗ trống. |
| 9 | `EMS-14` | Check-in API & Background Jobs | 2026-06-19 | API sinh/validate mã check-in, thiết lập Hangfire (in-memory) và job gửi email nhắc lịch cho người tham dự trước 24h. |

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

### 8. Registration APIs (Task EMS-13)
*   **Mô tả**: API quản lý đăng ký tham gia sự kiện kèm luồng duyệt và danh sách chờ (waitlist).
*   **Chi tiết thực hiện**:
    *   Tạo `Registration` entity, enum `RegistrationStatus` (Pending/Confirmed/Waitlisted/Cancelled/Rejected) và các DTO (Create/Reject/Response).
    *   `FirestoreRegistrationService` lưu trên collection `registrations`, isolation theo `tenantId` ở mọi thao tác.
    *   **Register**: chỉ cho sự kiện đã `Approved`, chặn đăng ký trùng; tự xếp `Waitlisted` khi số đăng ký active ≥ `Capacity` (Capacity ≤ 0 = không giới hạn), ngược lại `Pending`.
    *   **Cancel/Reject**: khi giải phóng một chỗ, tự động đẩy người waitlist sớm nhất (theo `RegisteredAt`) lên `Pending`.
    *   **Approve/Reject**: chỉ organizer của sự kiện hoặc admin/manager mới được thao tác.
    *   Endpoint: `POST /api/registrations`, `POST /api/registrations/{id}/cancel|approve|reject`, `GET /api/registrations/me|event/{eventId}|{id}`.
*   **Các file quan trọng**:
    *   `src/EMS.Core/Entities/Registration.cs`, `src/EMS.Core/Entities/Enums/RegistrationStatus.cs`
    *   `src/EMS.Infrastructure/Services/FirestoreRegistrationService.cs`
    *   `src/EMS.WebAPI/Controllers/RegistrationsController.cs`

---

### 9. Check-in API & Background Jobs (Task EMS-14)
*   **Mô tả**: Sinh/validate mã check-in tại sự kiện và hạ tầng tác vụ nền gửi email nhắc lịch.
*   **Chi tiết thực hiện**:
    *   Mở rộng `Registration` thêm các trường check-in (`CheckInCode`, `CheckInCodeExpiresAt`, `CheckedIn`, `CheckedInAt`) và cờ nhắc lịch (`ReminderSent`, `ReminderSentAt`).
    *   **Generate** (`POST /api/checkin/generate`): người dùng tự sinh mã check-in cho đăng ký đã `Confirmed`; mã hết hạn khi sự kiện kết thúc.
    *   **Validate** (`POST /api/checkin/validate`): organizer/admin quét mã, đánh dấu đã tham dự; kiểm quyền **bên trong service** để thao tác atomic (tránh side-effect trước khi check quyền), chặn mã hết hạn / đã check-in / chưa confirmed.
    *   **Hangfire**: cấu hình `Hangfire.MemoryStorage` (project dùng Firestore, không có SQL), bật Hangfire Server + Dashboard tại `/hangfire`.
    *   **Email reminder job**: `IEmailService`/`SmtpEmailService` (đọc section `Email`, **no-op an toàn** khi chưa cấu hình SMTP) và `EventReminderJob` chạy mỗi giờ (`Cron.Hourly`), quét cross-tenant các sự kiện `Approved` bắt đầu trong 24h tới, gửi email cho người `Confirmed` chưa được nhắc và đặt cờ `reminderSent`.
*   **Các file quan trọng**:
    *   `src/EMS.Core/Interfaces/Services/IEmailService.cs`
    *   `src/EMS.Infrastructure/Services/SmtpEmailService.cs`
    *   `src/EMS.Infrastructure/Jobs/EventReminderJob.cs`
    *   `src/EMS.WebAPI/Controllers/CheckInController.cs`
    *   `src/EMS.WebAPI/Program.cs` (Hangfire setup + recurring job), `src/EMS.WebAPI/appsettings.json` (section `Email`)
