# Lộ trình Phát triển Chi tiết Theo từng Giai đoạn (Phase)

## Tổng quan các Phase

| Phase | Tên | Thời gian | Output chính |
|-------|-----|-----------|---------------|
| Phase 0 | Environment & Infrastructure | Tuần 1 | Solution, CI/CD, Docker |
| Phase 1 | Multi-tenant Core & Authentication | Tuần 2-3 | Tenant resolution, Firebase Auth |
| Phase 2 | Event Management | Tuần 4-5 | CRUD events, approval workflow |
| Phase 3 | Registration System | Tuần 6-7 | Register, cancel, waitlist |
| Phase 4 | Attendance & Check-in | Tuần 8 | Check-in code, attendance tracking |
| Phase 5 | Agenda & Venue Management | Tuần 9 | Agenda items, venue CRUD |
| Phase 6 | Notification System | Tuần 10 | Email templates, Hangfire jobs |
| Phase 7 | Calendar Integration (.ics) | Tuần 11 | .ics export, Google Calendar |
| Phase 8 | Dashboard & Reporting | Tuần 12-13 | Charts, Excel/PDF export |
| Phase 9 | Security Hardening | Tuần 14 | Security rules, rate limiting |
| Phase 10 | Deployment & Go-Live | Tuần 15 | Production deployment |

---

## Phase 0: Environment & Infrastructure Setup

**Thời gian:** Tuần 1 (5 ngày)

**Mục tiêu:** Thiết lập môi trường phát triển, CI/CD, cấu trúc solution chuẩn multi-tenant.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 0.1 | Tạo Solution | Solution .NET 6 với cấu trúc multi-layer |
| 0.2 | Cấu hình Logging | Serilog + Console/File sink |
| 0.3 | Cấu hình Swagger | OpenAPI documentation |
| 0.4 | Health Checks | Endpoint `/health` cho monitoring |
| 0.5 | Docker Compose | Firebase Emulator, Hangfire SQL Server |
| 0.6 | GitHub Actions | CI pipeline (build, test) |
| 0.7 | Cấu hình Firebase | Service account, SDK initialization |

### Database cần tạo

Không có database migration ở Phase 0. Chỉ cấu hình kết nối Firebase.

### API cần xây dựng

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/health` | Health check |
| GET | `/health/ready` | Readiness probe |
| GET | `/health/live` | Liveness probe |

### UI cần xây dựng

- Trang placeholder cho MVC (`Home/Index`)
- Trang placeholder cho Blazor WASM

### Tiêu chí hoàn thành (Definition of Done)

- [ ] `dotnet build` thành công
- [ ] Docker Compose chạy được Firebase Emulator
- [ ] Truy cập được Swagger UI tại `/swagger`
- [ ] GitHub Actions build thành công
- [ ] Serilog ghi log ra console

---

## Phase 1: Multi-tenant Core & Authentication

**Thời gian:** Tuần 2-3 (10 ngày)

**Mục tiêu:** Xác thực người dùng đa tenant, phân quyền, cơ chế nhận diện tenant.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 1.1 | Tenant Resolution | Middleware lấy tenant từ subdomain hoặc header |
| 1.2 | Tenant Validation | Kiểm tra tenant tồn tại trong Firestore |
| 1.3 | Firebase Auth Integration | Đăng ký, đăng nhập qua Firebase |
| 1.4 | Custom Claims | Gán tenantId và role cho user |
| 1.5 | Tenant Onboarding API | Tạo tenant mới (admin account) |
| 1.6 | JWT Middleware | Xác thực token, gán HttpContext.User |
| 1.7 | Role-based Authorization | Student, Organizer, Admin, SuperAdmin |
| 1.8 | Tenant Switcher (SuperAdmin) | Cho phép SuperAdmin chuyển đổi tenant |

### Database cần tạo (Firestore collections)

```

users/{userId}                    # Global users collection

- email: string
- tenantId: string
- role: string
- mssv: string (nếu student)
- fullName: string
- isActive: boolean
- createdAt: timestamp

tenants/{tenantId}                # Tenant configuration

- name: string
- subdomain: string
- primaryColor: string
- logoUrl: string
- settings: map
  maxRegistrationsPerStudent: number
  allowWaitlist: boolean
  defaultCheckInCodeExpiry: number
- createdAt: timestamp
- createdBy: string

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| POST | `/api/auth/register` | Đăng ký tài khoản sinh viên | None (tenant required) |
| POST | `/api/auth/login` | Đăng nhập, trả về JWT | None |
| POST | `/api/auth/logout` | Đăng xuất | Authenticated |
| GET | `/api/auth/current-user` | Lấy thông tin user hiện tại | Authenticated |
| POST | `/api/auth/forgot-password` | Quên mật khẩu | None |
| POST | `/api/auth/reset-password` | Đặt lại mật khẩu | None |
| POST | `/api/tenants` | Tạo tenant mới | SuperAdmin |
| GET | `/api/tenants` | Danh sách tenants | SuperAdmin |
| GET | `/api/tenants/{tenantId}/settings` | Lấy cấu hình tenant | Tenant Admin |
| PUT | `/api/tenants/{tenantId}/settings` | Cập nhật cấu hình tenant | Tenant Admin |

### UI cần xây dựng

**MVC (Student Portal):**
- Trang đăng nhập (`/login`)
- Trang đăng ký (`/register`)
- Trang quên mật khẩu (`/forgot-password`)
- Layout với tenant branding (dynamic CSS)

**Blazor WASM (Dashboard):**
- Trang đăng nhập
- Tenant switcher (cho SuperAdmin)

### Tiêu chí hoàn thành

- [ ] User có thể đăng ký và đăng nhập với MSSV
- [ ] JWT token chứa đúng tenantId và role
- [ ] Middleware từ chối request với tenant không hợp lệ
- [ ] SuperAdmin có thể tạo tenant mới qua API
- [ ] Tenant Admin có thể xem và sửa settings
- [ ] Integration test cho các endpoint auth

---

## Phase 2: Event Management

**Thời gian:** Tuần 4-5 (10 ngày)

**Mục tiêu:** CRUD sự kiện, phê duyệt (Admin), hiển thị danh sách cho sinh viên.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 2.1 | Create Event | Organizer tạo sự kiện, có checkbox RequireApproval |
| 2.2 | Update Event | Chỉ Organizer tạo hoặc Admin mới được sửa |
| 2.3 | Delete Event | Soft delete (chuyển status Cancelled) |
| 2.4 | Event Listing (Student) | Danh sách Approved events, lọc theo venue/thời gian |
| 2.5 | Event Detail | Chi tiết sự kiện, venue info, agenda preview |
| 2.6 | Admin Approval | Duyệt/từ chối sự kiện (Pending → Approved/Rejected) |
| 2.7 | Event Status Workflow | Pending → Approved → Ongoing → Ended → Cancelled |
| 2.8 | Event Image Upload | Upload banner lên Firebase Storage |

### Database cần tạo (Firestore collections)

```

{tenantId}/events/{eventId}

- publicId: string (GUID)
- title: string
- description: string
- imageUrl: string
- venueId: reference (to venues)
- organizerId: string (user id)
- startTime: timestamp
- endTime: timestamp
- registrationStart: timestamp
- registrationEnd: timestamp
- maxCapacity: number
- trainingPoints: number
- requireApproval: boolean
- status: string (Pending, Approved, Ongoing, Ended, Cancelled)
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

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| POST | `/api/events` | Tạo sự kiện | Organizer/Admin |
| GET | `/api/events` | Danh sách sự kiện (filter by status, venue, time) | Public |
| GET | `/api/events/{publicId}` | Chi tiết sự kiện | Public |
| PUT | `/api/events/{publicId}` | Cập nhật sự kiện | Organizer (owner) or Admin |
| DELETE | `/api/events/{publicId}` | Xóa (soft) | Organizer (owner) or Admin |
| POST | `/api/events/{publicId}/approve` | Duyệt sự kiện | Admin |
| POST | `/api/events/{publicId}/reject` | Từ chối | Admin |
| POST | `/api/events/{publicId}/image` | Upload banner | Organizer/Admin |

### UI cần xây dựng

**MVC (Student Portal):**
- Trang danh sách sự kiện (cards layout, filter sidebar)
- Trang chi tiết sự kiện

**Blazor WASM (Organizer Dashboard):**
- Trang "My Events" (danh sách sự kiện của tôi)
- Form tạo/sửa sự kiện
- Tab quản lý (registrations, waitlist, check-in)

**Blazor WASM (Admin Dashboard):**
- Trang "Pending Events" (danh sách chờ duyệt)
- Nút Approve/Reject

### Tiêu chí hoàn thành

- [ ] Organizer tạo được sự kiện với trạng thái Pending
- [ ] Admin phê duyệt → sự kiện xuất hiện trên MVC
- [ ] Sinh viên xem được danh sách và chi tiết
- [ ] Upload ảnh banner thành công lên Firebase Storage
- [ ] Lọc sự kiện theo venue và thời gian hoạt động

---

## Phase 3: Registration System

**Thời gian:** Tuần 6-7 (10 ngày)

**Mục tiêu:** Đăng ký, hủy đăng ký, waitlist, xét duyệt tùy chọn, giới hạn số lượng.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 3.1 | Register (auto-approve) | Đăng ký vào sự kiện không cần duyệt |
| 3.2 | Register (require approval) | Đăng ký → trạng thái Pending |
| 3.3 | Approve/Reject Registration | Organizer duyệt từ chối đăng ký |
| 3.4 | Cancel Registration | Sinh viên hủy trước giờ bắt đầu |
| 3.5 | Waitlist | Hết chỗ → vào hàng chờ |
| 3.6 | Auto-process Waitlist | Khi có hủy, tự động duyệt người đầu waitlist |
| 3.7 | Registration Limit | Giới hạn số sự kiện/sinh viên/tháng (theo tenant setting) |
| 3.8 | My Events Page | Sinh viên xem sự kiện đã đăng ký (pending/approved/rejected) |

### Database cần tạo (Firestore collections)

```

{tenantId}/registrations/{registrationId}

- eventId: reference
- studentId: string (user id)
- registeredAt: timestamp
- status: string (Pending, Approved, Rejected, Cancelled)
- isAttended: boolean
- checkInTime: timestamp
- cancelledAt: timestamp
- cancelReason: string

{tenantId}/waitlist/{waitlistId}

- eventId: reference
- studentId: string
- joinedAt: timestamp
- notifiedAt: timestamp
- status: string (Waiting, Promoted)

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| POST | `/api/registrations` | Đăng ký sự kiện | Student |
| DELETE | `/api/registrations/{eventId}` | Hủy đăng ký | Student (owner) |
| PUT | `/api/registrations/{registrationId}/approve` | Duyệt đăng ký | Organizer (event owner) |
| PUT | `/api/registrations/{registrationId}/reject` | Từ chối | Organizer (event owner) |
| GET | `/api/registrations/my-events` | Sự kiện của tôi | Student |
| GET | `/api/events/{eventId}/registrations` | Danh sách đăng ký (phân trang) | Organizer/Admin |
| GET | `/api/events/{eventId}/waitlist` | Danh sách waitlist | Organizer/Admin |

### Background Jobs (Hangfire)

| Job | Schedule | Mô tả |
|-----|----------|-------|
| ProcessWaitlist | Chạy ngay khi có hủy | Xử lý waitlist queue |
| UpdateEventStatus | Chạy mỗi 15 phút | Cập nhật Ongoing/Ended |

### UI cần xây dựng

**MVC (Student Portal):**
- Nút "Đăng ký" / "Hủy đăng ký" trên trang chi tiết
- Trang "My Events" (hiển thị registration status)
- Popup xác nhận trước khi đăng ký/hủy

**Blazor WASM (Organizer Dashboard):**
- Tab "Pending Approvals" (duyệt/từ chối)
- Tab "Approved Registrations" (danh sách)
- Tab "Waitlist" (xem hàng chờ)

### Tiêu chí hoàn thành

- [ ] Sinh viên đăng ký → kiểm tra capacity và limit
- [ ] Hết chỗ → vào waitlist (kiểm tra Firestore)
- [ ] Hủy đăng ký → giải phóng chỗ → process waitlist
- [ ] Organizer duyệt/từ chối đăng ký pending
- [ ] Giới hạn số đăng ký/student được áp dụng

---

## Phase 4: Attendance & Check-in

**Thời gian:** Tuần 8 (5 ngày)

**Mục tiêu:** Điểm danh bằng mã ngẫu nhiên có thời hạn.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 4.1 | Generate Check-in Code | Organizer sinh mã 6 ký tự, hết hạn sau 30 phút |
| 4.2 | Student Check-in | Nhập mã, kiểm tra hợp lệ, ghi nhận thời gian |
| 4.3 | Auto-expire Code | Mã tự động hết hạn sau expiry time |
| 4.4 | Attendance List | Organizer xem danh sách đã check-in (real-time) |
| 4.5 | Prevent Duplicate Check-in | Mỗi sinh viên chỉ check-in 1 lần/sự kiện |

### Database cần tạo (cập nhật)

```

{tenantId}/events/{eventId}

- checkInCode: string (thêm)
- checkInCodeExpiry: timestamp (thêm)
- checkInCodeGeneratedBy: string (thêm - user id)

{tenantId}/registrations/{registrationId}

- isAttended: boolean
- checkInTime: timestamp

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| POST | `/api/events/{publicId}/generate-code` | Tạo mã check-in mới | Organizer/Admin |
| POST | `/api/events/{publicId}/checkin` | Check-in với mã | Student |
| GET | `/api/events/{publicId}/attendees` | Danh sách đã check-in | Organizer/Admin |

### UI cần xây dựng

**MVC (Student Portal):**
- Ô nhập mã check-in (chỉ hiển thị nếu sự kiện Ongoing và đã đăng ký)
- Bàn phím số tự động hiển thị trên mobile
- Thông báo thành công/thất bại

**Blazor WASM (Organizer Dashboard):**
- Nút "Generate New Code"
- Hiển thị mã hiện tại và thời gian còn lại (countdown)
- Danh sách check-in tự động cập nhật (polling 5 giây)

### Tiêu chí hoàn thành

- [ ] Organizer sinh mã → lưu vào Firestore, hiển thị đúng
- [ ] Mã hết hạn sau 30 phút
- [ ] Sinh viên nhập đúng mã → cập nhật isAttended và checkInTime
- [ ] Không thể check-in nếu mã sai, hết hạn, hoặc đã check-in trước đó
- [ ] Danh sách attendee cập nhật real-time

---

## Phase 5: Agenda & Venue Management

**Thời gian:** Tuần 9 (5 ngày)

**Mục tiêu:** Quản lý lịch trình chi tiết và địa điểm.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 5.1 | Agenda CRUD | Organizer thêm/sửa/xóa agenda items |
| 5.2 | Agenda Display | Sinh viên xem timeline agenda trên trang chi tiết |
| 5.3 | Venue CRUD | Admin quản lý địa điểm (tên, địa chỉ, sức chứa, map) |
| 5.4 | Venue Selection | Chọn venue từ dropdown khi tạo/sửa sự kiện |

### Database cần tạo

```

{tenantId}/agendaItems/{agendaId}

- eventId: reference
- startTime: timestamp
- endTime: timestamp
- title: string
- description: string
- speaker: string
- materialUrl: string
- order: number

{tenantId}/venues/{venueId}        # Đã có trong Phase 2, bổ sung field

- isActive: boolean (đã có)
- ... (các field khác)

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| GET | `/api/events/{publicId}/agenda` | Lấy agenda | Public |
| POST | `/api/events/{publicId}/agenda` | Thêm agenda item | Organizer |
| PUT | `/api/agenda/{agendaId}` | Cập nhật | Organizer |
| DELETE | `/api/agenda/{agendaId}` | Xóa | Organizer |
| GET | `/api/venues` | Danh sách venues | Public |
| POST | `/api/venues` | Thêm venue | Admin |
| PUT | `/api/venues/{venueId}` | Cập nhật | Admin |
| DELETE | `/api/venues/{venueId}` | Xóa | Admin |

### UI cần xây dựng

**MVC (Student Portal):**
- Timeline agenda trên trang chi tiết sự kiện
- Link download tài liệu (nếu có)

**Blazor WASM (Organizer Dashboard):**
- Agenda management (drag-drop để sắp xếp thứ tự)
- Upload material file lên Firebase Storage

**Blazor WASM (Admin Dashboard):**
- Venue management CRUD

### Tiêu chí hoàn thành

- [ ] Organizer thêm agenda item → hiển thị đúng thứ tự
- [ ] Sinh viên xem được agenda với thời gian chi tiết
- [ ] Admin quản lý được venues
- [ ] Venue được chọn từ dropdown khi tạo/sửa event

---

## Phase 6: Notification System

**Thời gian:** Tuần 10 (5 ngày)

**Mục tiêu:** Gửi email tự động qua Hangfire.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 6.1 | Registration Confirmation | Gửi email xác nhận khi đăng ký thành công |
| 6.2 | Waitlist Promotion | Gửi email khi từ waitlist lên chính thức |
| 6.3 | Registration Rejection | Gửi email khi Organizer từ chối đăng ký |
| 6.4 | Event Reminder (1 day) | Nhắc nhở 1 ngày trước sự kiện |
| 6.5 | Event Reminder (1 hour) | Nhắc nhở 1 giờ trước sự kiện |
| 6.6 | Event Cancellation | Gửi email khi Admin hủy sự kiện |
| 6.7 | Email Templates | Template có thể tùy chỉnh theo tenant |

### Database cần tạo

```

{tenantId}/emailTemplates/{templateId}

- name: string (registration_confirmation, reminder_1day, etc.)
- subject: string
- body: string (HTML)
- isActive: boolean

{tenantId}/emailLogs/{logId}

- to: string
- templateId: reference
- status: string (Sent, Failed)
- sentAt: timestamp
- error: string

```

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| GET | `/api/email-templates` | Danh sách templates | Tenant Admin |
| PUT | `/api/email-templates/{templateId}` | Cập nhật template | Tenant Admin |

### Background Jobs (Hangfire)

| Job | Schedule | Mô tả |
|-----|----------|-------|
| SendRegistrationEmail | Enqueue khi đăng ký | Gửi xác nhận |
| SendWaitlistPromotionEmail | Enqueue khi promoted | Gửi thông báo |
| SendReminder1Day | Chạy hàng ngày lúc 8:00 | Gửi reminder cho event tomorrow |
| SendReminder1Hour | Chạy mỗi giờ | Gửi reminder cho event trong 1 giờ tới |

### UI cần xây dựng

**Blazor WASM (Tenant Admin):**
- Trang quản lý email templates (xem, chỉnh sửa HTML)
- Preview template với dữ liệu mẫu

### Tiêu chí hoàn thành

- [ ] Đăng ký thành công → email được gửi (kiểm tra mail trap)
- [ ] Waitlist promotion → email được gửi
- [ ] Reminder jobs chạy đúng lịch
- [ ] Tenant có thể tùy chỉnh email template

---

## Phase 7: Calendar Integration (.ics)

**Thời gian:** Tuần 11 (3 ngày)

**Mục tiêu:** Cho phép sinh viên tải file .ics để thêm vào Google Calendar/Outlook.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 7.1 | Export .ics | Tạo file .ics từ thông tin sự kiện |
| 7.2 | Add to Google Calendar | Nút chuyển hướng đến Google Calendar |
| 7.3 | Download .ics file | Tải file về máy |

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| GET | `/api/events/{publicId}/calendar.ics` | Trả về file .ics | Public |

### Thư viện sử dụng

- **iCal.NET** (hoặc tự implement)

### UI cần xây dựng

**MVC (Student Portal):**
- Nút "Thêm vào lịch" trên trang chi tiết sự kiện
- Dropdown: "Google Calendar" / "Download .ics"

### Tiêu chí hoàn thành

- [ ] File .ics tải về mở được bằng Google Calendar
- [ ] Thông tin sự kiện (title, time, location, description) hiển thị đúng
- [ ] Múi giờ được xử lý đúng (UTC → local)

---

## Phase 8: Dashboard & Reporting

**Thời gian:** Tuần 12-13 (10 ngày)

**Mục tiêu:** Cung cấp báo cáo thống kê và xuất danh sách Excel/PDF.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 8.1 | Admin Dashboard | Biểu đồ số events theo tháng, top organizers |
| 8.2 | Organizer Dashboard | Tỷ lệ check-in/registration, waitlist stats |
| 8.3 | Export Registrations (Excel) | Xuất danh sách đăng ký |
| 8.4 | Export Attendees (PDF) | Xuất danh sách đã điểm danh |
| 8.5 | Event Statistics | Thống kê chi tiết cho từng sự kiện |

### API cần xây dựng

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|-------|---------------|
| GET | `/api/dashboard/admin/stats` | Thống kê toàn tenant | Admin |
| GET | `/api/dashboard/organizer/stats` | Thống kê organizer | Organizer |
| GET | `/api/events/{publicId}/stats` | Thống kê event | Organizer/Admin |
| GET | `/api/events/{publicId}/export/excel` | Export Excel | Organizer/Admin |
| GET | `/api/events/{publicId}/export/pdf` | Export PDF | Organizer/Admin |

### Thư viện sử dụng

- **ClosedXML** hoặc **EPPlus** – Export Excel
- **QuestPDF** – Export PDF

### UI cần xây dựng

**Blazor WASM (Admin/Organizer Dashboard):**
- Biểu đồ (Chart.js hoặc ApexCharts)
- Nút "Export Excel", "Export PDF"
- Bảng danh sách có phân trang, tìm kiếm, filter

### Tiêu chí hoàn thành

- [ ] Admin dashboard hiển thị đúng số liệu
- [ ] Organizer xem được tỷ lệ check-in
- [ ] Export Excel mở được, đúng định dạng
- [ ] Export PDF có branding của tenant

---

## Phase 9: Security Hardening

**Thời gian:** Tuần 14 (5 ngày)

**Mục tiêu:** Áp dụng các biện pháp bảo mật.

### Danh sách tính năng

| STT | Tính năng | Mô tả |
|-----|-----------|-------|
| 9.1 | Firebase Security Rules | Hoàn thiện rules cho multi-tenant |
| 9.2 | CORS Configuration | Chỉ cho phép domain đã đăng ký |
| 9.3 | Rate Limiting | Giới hạn request cho API (tùy chọn nhẹ) |
| 9.4 | HTTPS Enforcement | Chuyển hướng HTTP → HTTPS |
| 9.5 | Security Headers | HSTS, X-Frame-Options, X-Content-Type-Options |
| 9.6 | Input Validation | Validate tất cả input (FluentValidation) |
| 9.7 | Sanitize HTML | Lọc HTML trong description (tránh XSS) |

### Cấu hình cần thêm

**Firebase Security Rules** (hoàn thiện):

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Tenant data isolation
    match /{tenantId}/{document=**} {
      allow read, write: if request.auth != null 
        && request.auth.token.tenantId == tenantId;
    }
  
    // Registration: student chỉ được đăng ký cho chính mình
    match /{tenantId}/registrations/{registrationId} {
      allow read: if request.auth.token.role in ['admin', 'organizer']
        || resource.data.studentId == request.auth.uid;
      allow write: if request.auth.token.role in ['admin', 'organizer']
        || (request.auth.token.role == 'student' 
            && request.resource.data.studentId == request.auth.uid);
    }
  }
}
```

### Tiêu chí hoàn thành

- [ ] Firebase Rules ngăn user truy cập tenant khác
- [ ] Rate limiting bảo vệ login và check-in endpoint
- [ ] Security headers có mặt trong response
- [ ] All input validated (FluentValidation)

---

## Phase 10: Deployment & Go-Live

**Thời gian:** Tuần 15 (5 ngày)

**Mục tiêu:** Triển khai lên production, monitoring, backup.

### Danh sách tính năng

| STT  | Tính năng                 | Mô tả                                    |
| ---- | --------------------------- | ------------------------------------------ |
| 10.1 | Docker Containerization     | Dockerfile cho API, MVC, Blazor            |
| 10.2 | CI/CD Pipeline              | GitHub Actions build, test, deploy         |
| 10.3 | Production Firebase Project | Tạo project riêng cho production         |
| 10.4 | Domain Configuration        | Cấu hình subdomain cho tenants           |
| 10.5 | SSL Certificate             | Let's Encrypt hoặc Cloudflare             |
| 10.6 | Monitoring                  | Health checks, Serilog, Hangfire dashboard |
| 10.7 | Backup Strategy             | Firestore export hàng ngày               |
| 10.8 | Documentation               | Deployment guide, admin manual             |

### Môi trường triển khai

| Environment | URL             | Mục đích                 |
| ----------- | --------------- | --------------------------- |
| Development | dev.ems.com     | Phát triển, test nội bộ |
| Staging     | staging.ems.com | UAT, demo cho khách hàng  |
| Production  | *.ems.com       | Live tenants                |

### CI/CD Pipeline (GitHub Actions)

```yaml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet test

  deploy-api:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'ems-api'
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: './published-api'

  deploy-frontend:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Firebase Hosting
        uses: FirebaseExtended/action-hosting-deploy@v0
        with:
          firebaseServiceAccount: ${{ secrets.FIREBASE_SERVICE_ACCOUNT }}
```
