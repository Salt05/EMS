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
