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

<!--
## YYYY-MM-DD — Tên chức năng
**Tóm tắt:** mô tả ngắn chức năng làm gì.
**File chính:** đường dẫn các file liên quan.
**Trạng thái:** đã commit / branch.
-->
