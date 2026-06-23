# Completed Features

Nhật ký các task/chức năng đã hoàn thành của dự án EMS.
Mỗi khi xong một chức năng, thêm một mục theo định dạng bên dưới.

---

## 2026-06-12 — Event CRUD API
**Tóm tắt:** API quản lý sự kiện đầy đủ CRUD + luồng duyệt/từ chối, lưu trên Firestore. Có cách ly tenant (mọi thao tác kiểm `tenantId`), phân quyền (sửa/xóa: organizer hoặc admin/manager; duyệt/từ chối: chỉ admin/manager), validate `EndTime > StartTime`, chuẩn hóa thời gian về UTC.
**Endpoint:** `GET /api/events`, `GET /api/events/{id}`, `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}`, `POST /api/events/{id}/approve`, `POST /api/events/{id}/reject`.
**File chính:**
- `src/EMS.Core/Entities/Event.cs`, `src/EMS.Core/Entities/Enums/EventStatus.cs`
- `src/EMS.Core/Interfaces/Services/IEventService.cs`
- `src/EMS.Infrastructure/Services/FirestoreEventService.cs`
- `src/EMS.WebAPI/Controllers/EventsController.cs`, `src/EMS.WebAPI/Program.cs` (DI)
- `src/EMS.Shared/DTOs/Events/` (Create/Update/Reject/Response DTO)

**Trạng thái:** branch `Event-CRUD-API`, build thành công (0 lỗi).

## 2026-06-17 — Setup MVC Layout & Auth Mock
**Tóm tắt:** Thiết lập giao diện cho cổng sinh viên EMS.Mvc. Nâng cấp Layout chính (Glassmorphic navbar, footer, Bootstrap Icons), tạo trang Đăng nhập và Đăng ký giả lập (mock auth) hỗ trợ lưu session qua Cookie để hiển thị trạng thái động trên thanh điều hướng, cập nhật trang chủ Index với các mock event cards trực quan.
**File chính:**
- `src/EMS.Mvc/Controllers/AuthController.cs`
- `src/EMS.Mvc/Views/Auth/Login.cshtml`
- `src/EMS.Mvc/Views/Auth/Register.cshtml`
- `src/EMS.Mvc/Views/Shared/_Layout.cshtml`
- `src/EMS.Mvc/Views/Home/Index.cshtml`
- `src/EMS.Mvc/wwwroot/css/site.css`

**Trạng thái:** Hoàn thành, build thành công (0 lỗi).

## 2026-06-19 — Registration API (đăng ký / huỷ / waitlist + duyệt/từ chối)
**Tóm tắt:** API đăng ký tham gia sự kiện. `register`: chỉ cho event đã `Approved`, chặn đăng ký trùng, tự xếp `Waitlisted` khi số chỗ active ≥ `Capacity` (Capacity ≤ 0 = không giới hạn), ngược lại `Pending`. `cancel`: chủ đăng ký hoặc organizer/admin/manager huỷ, tự đẩy người waitlist sớm nhất lên `Pending` khi giải phóng chỗ. `approve/reject`: chỉ organizer của event hoặc admin/manager. Có cách ly tenant ở mọi thao tác, chuẩn hoá thời gian UTC.
**Endpoint:** `GET /api/registrations/me`, `GET /api/registrations/event/{eventId}`, `GET /api/registrations/{id}`, `POST /api/registrations`, `POST /api/registrations/{id}/cancel`, `POST /api/registrations/{id}/approve`, `POST /api/registrations/{id}/reject`.
**File chính:**
- `src/EMS.Core/Entities/Registration.cs`, `src/EMS.Core/Entities/Enums/RegistrationStatus.cs`
- `src/EMS.Core/Interfaces/Services/IRegistrationService.cs`
- `src/EMS.Infrastructure/Services/FirestoreRegistrationService.cs`
- `src/EMS.WebAPI/Controllers/RegistrationsController.cs`, `src/EMS.WebAPI/Program.cs` (DI)
- `src/EMS.Shared/DTOs/Registrations/` (Create/Reject/Response DTO)

**Trạng thái:** branch `Event-CRUD-API`, build thành công (0 lỗi).

## 2026-06-19 — Check-in API + Hangfire + Email reminder job
**Tóm tắt:** (1) **Check-in**: `generate` cho user tự sinh mã check-in cho đăng ký đã `Confirmed` (hết hạn khi event kết thúc); `validate` cho organizer/admin quét mã & đánh dấu tham dự (kiểm quyền trong service để atomic, chặn mã hết hạn/đã check-in/chưa confirmed). (2) **Hangfire**: cấu hình `Hangfire.MemoryStorage` (Firestore không có SQL) + Server + Dashboard `/hangfire`. (3) **Email reminder job**: `IEmailService`/`SmtpEmailService` (no-op an toàn khi chưa cấu hình SMTP) + `EventReminderJob` chạy mỗi giờ, quét cross-tenant event `Approved` bắt đầu trong 24h, gửi mail cho người `Confirmed` chưa nhắc, đặt cờ `reminderSent`.
**Endpoint:** `POST /api/checkin/generate`, `POST /api/checkin/validate`; Dashboard `GET /hangfire`.
**File chính:**
- `src/EMS.Core/Entities/Registration.cs` (thêm field check-in/reminder), `src/EMS.Core/Interfaces/Services/IEmailService.cs`, `IRegistrationService.cs`
- `src/EMS.Infrastructure/Services/FirestoreRegistrationService.cs`, `SmtpEmailService.cs`, `src/EMS.Infrastructure/Jobs/EventReminderJob.cs`
- `src/EMS.WebAPI/Controllers/CheckInController.cs`, `src/EMS.WebAPI/Program.cs` (Hangfire + DI + recurring job), `appsettings.json` (section `Email`)
- `src/EMS.Shared/DTOs/CheckIns/` (Generate/Validate/Response DTO)

**Trạng thái:** build thành công (0 lỗi).

## 2026-06-21 — Event Listing (Cards) & Detail Page
**Tóm tắt:** Xây dựng trang danh sách sự kiện có lọc tìm kiếm từ khóa và trang chi tiết sự kiện cho Cổng sinh viên (EMS.Mvc). Kết nối trực tiếp cơ sở dữ liệu Firestore, tự động phân giải Tenant ID từ subdomain của request thông qua TenantMiddleware, và áp dụng thiết kế Glassmorphic đồng bộ.
**File chính:**
- `src/EMS.Mvc/Program.cs` (Cấu hình DI, Middleware)
- `src/EMS.Mvc/Middlewares/TenantMiddleware.cs` (Middleware phân giải Tenant)
- `src/EMS.Mvc/Controllers/EventsController.cs` (Controller xử lý sự kiện)
- `src/EMS.Mvc/Views/Events/Index.cshtml` (Giao diện danh sách & Tìm kiếm)
- `src/EMS.Mvc/Views/Events/Detail.cshtml` (Giao diện chi tiết sự kiện)
- `src/EMS.Mvc/Controllers/HomeController.cs` & `src/EMS.Mvc/Views/Home/Index.cshtml` (Tích hợp sự kiện động lên trang chủ)
- `src/EMS.Mvc/Views/Shared/_Layout.cshtml` (Hiển thị TenantName động)

**Trạng thái:** Hoàn thành, build thành công (0 lỗi).

## 2026-06-23 — Student MyEvents & Registration Flow
**Tóm tắt:** Xây dựng tính năng đăng ký tham gia sự kiện và hủy đăng ký cho sinh viên trên cổng EMS.Mvc. Triển khai cấu trúc dữ liệu Registration cùng service lưu trữ Firestore (FirestoreRegistrationService) & bộ nhớ đệm giả lập (DevInMemoryRegistrationService). Thêm trang quản lý sự kiện cá nhân "Sự kiện của tôi" (MyEvents.cshtml) hỗ trợ phân loại theo trạng thái (Vé hoạt động / Lịch sử & Đã hủy).
**File chính:**
- `src/EMS.Core/Entities/Enums/RegistrationStatus.cs` (Enum trạng thái đăng ký)
- `src/EMS.Core/Entities/Registration.cs` (Entity đăng ký sự kiện)
- `src/EMS.Core/Interfaces/Services/IRegistrationService.cs` (Interface service quản lý đăng ký)
- `src/EMS.Infrastructure/Services/FirestoreRegistrationService.cs` (Triển khai Firestore)
- `src/EMS.Mvc/Services/DevInMemoryRegistrationService.cs` (Triển khai in-memory dev)
- `src/EMS.Mvc/Controllers/EventsController.cs` (Thêm các action Register, Cancel, MyEvents)
- `src/EMS.Mvc/Views/Events/Detail.cshtml` (Form đăng ký & hủy trực tiếp)
- `src/EMS.Mvc/Views/Events/MyEvents.cshtml` (Giao diện danh sách sự kiện cá nhân)
- `src/EMS.Mvc/Views/Shared/_Layout.cshtml` (Menu Sự kiện của tôi trên navbar)

**Trạng thái:** Hoàn thành, build thành công (0 lỗi).

---

<!--
## YYYY-MM-DD — Tên chức năng
**Tóm tắt:** mô tả ngắn chức năng làm gì.
**File chính:** đường dẫn các file liên quan.
**Trạng thái:** đã commit / branch.
-->
