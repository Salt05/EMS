# 5. MÔ TẢ CHI TIẾT ỨNG DỤNG

## 5.2 CONTROLLERS

Phần này thống kê và mô tả chi tiết toàn bộ các **Action Methods** thuộc tất cả các Controller trong hệ thống dưới dạng **Bảng tổng hợp**, bao gồm phân hệ Cổng sinh viên (`EMS.Mvc`) và phân hệ Backend WebAPI (`EMS.WebAPI`).

---

### 5.2.1 Phân hệ Controllers Cổng Sinh viên (`EMS.Mvc`)

#### 5.2.1.1 Lớp `HomeController.cs`
* **Mục đích:** Quản lý luồng trang chủ, tải các sự kiện nổi bật, banner động sự kiện sắp/đang diễn ra của sinh viên và thông tin bảo mật.

| STT | Tên Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | View / Partial View liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Index()` | `GET /` <br>`GET /Home/Index` | Tải danh sách sự kiện đã duyệt (`Approved`), tính toán chỗ trống khả dụng real-time, tải banner sự kiện của sinh viên đã đăng nhập. | `Views/Home/Index.cshtml`<br>`_UpcomingEventBanner.cshtml`<br>`_EventCard.cshtml` |
| 2 | `Privacy()` | `GET /Home/Privacy` | Hiển thị điều khoản dịch vụ và chính sách bảo mật thông tin người dùng. | `Views/Home/Privacy.cshtml` |

---

#### 5.2.1.2 Lớp `EventsController.cs`
* **Mục đích:** Quản lý toàn bộ luồng xem danh sách sự kiện, chi tiết sự kiện, đăng ký 1-click, hủy vé, danh sách chờ Waitlist, điểm danh check-in, lịch trình tiết mục và đổi phần thưởng.

| STT | Tên Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | View / Partial View / Kiểu trả về |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Index(searchString)` | `GET /Events`<br>`GET /Events/Index` | Lọc & tìm kiếm sự kiện theo từ khóa (tiêu đề, địa điểm), phân quyền xem công khai/nội bộ. | `Views/Events/Index.cshtml`<br>`_EventCard.cshtml` |
| 2 | `Detail(id)` | `GET /Events/Detail/{id}` | Hiển thị thông tin chi tiết sự kiện, diễn giả, sức chứa, địa điểm và danh sách lịch trình. | `Views/Events/Detail.cshtml`<br>`_AgendaTimeline.cshtml` |
| 3 | `Register(eventId)` | `POST /Events/Register` | Đăng ký vé 1-click. Tự động chuyển sang `Waitlisted` nếu hết chỗ, hoặc chuyển cổng VNPay nếu có phí. | Redirect `Detail.cshtml` hoặc `MyEvents.cshtml` |
| 4 | `Cancel(eventId)` | `POST /Events/Cancel` | Hủy vé tham dự của sinh viên. Tự động đẩy người dùng tiếp theo từ danh sách Waitlist lên chính thức. | Redirect `Detail.cshtml` hoặc `MyEvents.cshtml` |
| 5 | `MyEvents()` | `GET /Events/MyEvents` | Quản lý danh sách sự kiện cá nhân của sinh viên (phân loại: Vé đang hoạt động & Lịch sử/Đã hủy). | `Views/Events/MyEvents.cshtml` |
| 6 | `GetAgenda(eventId)` | `GET /api/events/{eventId}/agenda` | API AJAX lấy danh sách lịch trình tiết mục (Agenda) dạng JSON. | `JsonResult` (JSON Array) |
| 7 | `DownloadIcs(id)` | `GET /Events/DownloadIcs/{id}` | Sinh và xuất file lịch `.ics` để thêm nhanh sự kiện vào Google Calendar hoặc Outlook. | `FileResult` (`text/calendar`) |
| 8 | `CheckInWithCode(eventId, code)` | `POST /Events/CheckInWithCode` | Sinh viên nhập mã check-in 6 ký tự trên form để thực hiện điểm danh. | Redirect `Detail.cshtml` |
| 9 | `CheckInWithCodeAjax(...)` | `POST /Events/CheckInWithCodeAjax` | Xử lý điểm danh ngầm qua AJAX không cần tải lại trang web. | `JsonResult` (`{ success: true, ... }`) |
| 10 | `MyRewards(type, fromDate, toDate)`| `GET /Events/MyRewards` | Xem số điểm tích lũy và lọc lịch sử đổi quà thưởng của sinh viên. | `Views/Events/MyRewards.cshtml`<br>`_MyRewardsTablePartial.cshtml` |
| 11 | `GenerateQrCode(eventId)` | `POST /Events/GenerateQrCode` | Sinh mã check-in ngẫu nhiên 6 số và thời gian hết hạn dùng để hiển thị QR Code. | `JsonResult` (`{ code, expiresAt }`) |
| 12 | `CheckInStatus(eventId)` | `GET /Events/CheckInStatus` | API kiểm tra trạng thái điểm danh real-time của sinh viên đối với sự kiện. | `JsonResult` (`{ checkedIn: true/false }`) |

---

#### 5.2.1.3 Lớp `AuthController.cs`
* **Mục đích:** Quản lý quy trình đăng nhập, đăng ký tài khoản và đăng xuất cho cổng sinh viên.

| STT | Tên Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | View / Partial View / Kiểu trả về |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Login(returnUrl)` | `GET /Auth/Login` | Hiển thị giao diện form đăng nhập tài khoản. | `Views/Auth/Login.cshtml` |
| 2 | `Login(model, returnUrl)`| `POST /Auth/Login` | Xác thực thông tin tài khoản và thiết lập Cookie Session (`user_session`). | Redirect `Home/Index` hoặc `returnUrl` |
| 3 | `Register()` | `GET /Auth/Register` | Hiển thị giao diện form đăng ký tài khoản sinh viên mới. | `Views/Auth/Register.cshtml` |
| 4 | `Register(model)` | `POST /Auth/Register` | Xử lý tạo mới tài khoản sinh viên và tự động đăng nhập. | Redirect `Home/Index` |
| 5 | `Logout()` | `POST /Auth/Logout` | Đăng xuất tài khoản, xóa Cookie Session. | Redirect `Home/Index` |

---

#### 5.2.1.4 Lớp `AiChatController.cs`
* **Mục đích:** Tiếp nhận tin nhắn từ khung chat nổi Trợ lý AI (Gemini AI Chatbot) tích hợp trên giao diện web sinh viên và kết nối đến `GeminiAiService` tư vấn tự động.

| STT | Tên Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | View / Component liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `SendMessage(request)` | `POST /api/aichat/send` | Nhận câu hỏi từ sinh viên, truy vấn danh sách sự kiện hiện tại và gửi cho AI Gemini tư vấn tự động. | `wwwroot/js/ai-chatbot.js`<br>`Views/Shared/_Layout.cshtml` |

---

#### 5.2.1.5 Lớp `PaymentController.cs`
* **Mục đích:** Xử lý tích hợp cổng thanh toán trực tuyến VNPay cho các sự kiện có thu lệ phí vé.

| STT | Tên Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | View / Kiểu trả về |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Pay(registrationId)` | `GET /Payment/Pay` | Hiển thị trang xác nhận thông tin thanh toán vé. | `Views/Payment/Pay.cshtml` |
| 2 | `CreatePaymentUrl(regId)` | `POST /Payment/CreatePaymentUrl` | Sinh liên kết mã hóa thanh toán an toàn và chuyển hướng sang VNPay. | Redirect (`VnPayUrl`) |
| 3 | `VnPayCallback()` | `GET /Payment/VnPayCallback` | Tiếp nhận phản hồi từ VNPay (IPN/Callback), cập nhật trạng thái vé `IsPaid = true`. | `Views/Payment/Success.cshtml`<br>`Views/Payment/Failure.cshtml` |

---

### 5.2.2 Phân hệ Controllers Backend WebAPI (`EMS.WebAPI`)

Dưới đây là các bảng chi tiết Action Methods của từng Controller thuộc phân hệ Backend API phục vụ Dashboard Quản trị và các dịch vụ tích hợp:

#### 5.2.2.1 Lớp `EventsController.cs` (`/api/events`)
* **Mục đích:** Cung cấp API quản lý sự kiện (CRUD), quy trình phê duyệt/từ chối sự kiện của Admin.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetEvents()` | `GET /api/events` | Lấy danh sách sự kiện theo TenantId. | `tenantId`, `status` | `200 OK` (List Events) |
| 2 | `GetEvent(id)` | `GET /api/events/{id}` | Lấy thông tin chi tiết một sự kiện. | `id` (EventId) | `200 OK` (EventDetailDto) |
| 3 | `CreateEvent(dto)` | `POST /api/events` | Ban tổ chức tạo mới sự kiện. | `CreateEventDto` | `201 Created` |
| 4 | `UpdateEvent(id, dto)`| `PUT /api/events/{id}` | Cập nhật thông tin sự kiện. | `id`, `UpdateEventDto` | `200 OK` |
| 5 | `DeleteEvent(id)` | `DELETE /api/events/{id}` | Hủy hoặc xóa bản ghi sự kiện. | `id` (EventId) | `200 OK` |
| 6 | `ApproveEvent(id)` | `POST /api/events/{id}/approve` | Admin duyệt công khai sự kiện. | `id` (EventId) | `200 OK` |
| 7 | `RejectEvent(id, dto)`| `POST /api/events/{id}/reject` | Admin từ chối phê duyệt sự kiện. | `id`, `RejectEventDto` | `200 OK` |

---

#### 5.2.2.2 Lớp `RegistrationsController.cs` (`/api/registrations`)
* **Mục đích:** API quản lý đăng ký vé, danh sách người tham dự và duyệt Waitlist.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetMyRegistrations()`| `GET /api/registrations/me` | Lấy danh sách vé của người dùng đăng nhập. | Header JWT Token | `200 OK` (List Registrations) |
| 2 | `GetEventRegistrations(eventId)`| `GET /api/registrations/event/{eventId}` | BTC xem danh sách sinh viên đăng ký sự kiện. | `eventId` | `200 OK` (List Registrations) |
| 3 | `Register(dto)` | `POST /api/registrations` | API đăng ký vé tham dự sự kiện. | `CreateRegistrationDto` | `201 Created` |
| 4 | `CancelRegistration(id)`| `POST /api/registrations/{id}/cancel` | API hủy vé đã đăng ký. | `id` (RegistrationId) | `200 OK` |
| 5 | `ApproveRegistration(id)`| `POST /api/registrations/{id}/approve` | BTC duyệt vé từ danh sách Waitlist. | `id` (RegistrationId) | `200 OK` |
| 6 | `RejectRegistration(id, dto)`| `POST /api/registrations/{id}/reject` | BTC từ chối yêu cầu đăng ký vé. | `id`, `RejectRegistrationDto` | `200 OK` |

---

#### 5.2.2.3 Lớp `CheckInController.cs` (`/api/checkin`)
* **Mục đích:** API sinh mã check-in 6 số và xác minh điểm danh thực tế tại sự kiện.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GenerateCheckInCode(dto)`| `POST /api/checkin/generate` | Sinh mã điểm danh 6 ký tự ngẫu nhiên. | `GenerateCheckInCodeDto` | `200 OK` (`{ checkInCode }`) |
| 2 | `ValidateCheckInCode(dto)`| `POST /api/checkin/validate` | BTC quét/nhập mã xác nhận điểm danh. | `ValidateCheckInCodeDto` | `200 OK` (`{ success: true }`) |

---

#### 5.2.2.4 Lớp `AuthController.cs` (`/api/auth`)
* **Mục đích:** API quản lý xác thực tài khoản qua Firebase Auth & cấp phát JWT Token.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `Register(dto)` | `POST /api/auth/register` | Đăng ký tài khoản người dùng mới. | `RegisterRequestDto` | `201 Created` |
| 2 | `Login(dto)` | `POST /api/auth/login` | Đăng nhập tài khoản & nhận JWT Token. | `LoginRequestDto` | `200 OK` (`{ token, user }`) |
| 3 | `RefreshToken(dto)` | `POST /api/auth/refresh-token` | Làm mới Token truy cập hết hạn. | `RefreshTokenRequestDto`| `200 OK` (`{ token }`) |

---

#### 5.2.2.5 Lớp `RewardsController.cs` (`/api/rewards`)
* **Mục đích:** API quản lý danh mục phần thưởng & xử lý giao dịch đổi quà tích lũy.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetCategories()` | `GET /api/rewards/categories` | Lấy danh sách phần thưởng khả dụng. | `tenantId` | `200 OK` (List RewardCategory) |
| 2 | `CreateCategory(dto)` | `POST /api/rewards/categories` | Admin tạo mới phần thưởng vào kho quà. | `CreateRewardCategoryDto`| `201 Created` |
| 3 | `ClaimReward(dto)` | `POST /api/rewards/claim` | Sinh viên đổi điểm lấy phần thưởng. | `ClaimRewardDto` | `200 OK` (`{ success: true }`) |
| 4 | `GetUserRewardHistory()`| `GET /api/rewards/history` | Xem nhật ký đổi quà cá nhân. | Header JWT Token | `200 OK` (List UserRewardRecord) |

---

#### 5.2.2.6 Lớp `ReportsController.cs` (`/api/reports`)
* **Mục đích:** API kết xuất dữ liệu thống kê ra các định dạng file báo cáo.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `ExportRegistrationsExcel(eventId)`| `GET /api/reports/events/{eventId}/registrations/excel` | Xuất danh sách đăng ký ra file Excel (`.xlsx`). | `eventId` | `FileResult` (`application/vnd.openxmlformats...`) |
| 2 | `ExportAttendancePdf(eventId)` | `GET /api/reports/events/{eventId}/attendance/pdf` | Xuất danh sách điểm danh ra file PDF (`.pdf`). | `eventId` | `FileResult` (`application/pdf`) |

---

#### 5.2.2.7 Lớp `TenantsController.cs` (`/api/tenants`)
* **Mục đích:** API quản lý các trường đại học/tổ chức (Mô hình Multi-tenant).

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetTenants()` | `GET /api/tenants` | Lấy danh sách tất cả các Tenant. | None | `200 OK` (List Tenant) |
| 2 | `GetTenantBySubdomain(subdomain)`| `GET /api/tenants/{subdomain}` | Phân giải dữ liệu Tenant qua Subdomain. | `subdomain` | `200 OK` (TenantDto) |
| 3 | `CreateTenant(dto)` | `POST /api/tenants` | SuperAdmin khởi tạo Tenant mới. | `CreateTenantDto` | `201 Created` |

---

#### 5.2.2.8 Lớp `AdminUsersController.cs` (`/api/admin/users`)
* **Mục đích:** API quản trị người dùng & phân quyền hệ thống (Role Management).

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetUsers(filter)` | `GET /api/admin/users` | Danh sách & tìm kiếm người dùng. | `UserFilterDto` | `200 OK` (PagedList Users) |
| 2 | `CreateUser(dto)` | `POST /api/admin/users` | Tạo mới tài khoản người dùng/BTC. | `CreateUserDto` | `201 Created` |
| 3 | `UpdateUserRole(id, dto)`| `PUT /api/admin/users/{id}/role` | Phân quyền vai trò mới cho người dùng. | `id`, `ChangeRoleDto` | `200 OK` |
| 4 | `ToggleUserStatus(id)` | `PUT /api/admin/users/{id}/status` | Khóa hoặc mở lại tài khoản người dùng. | `id` (UserId) | `200 OK` |

---

#### 5.2.2.9 Lớp `EmailTemplatesController.cs` (`/api/admin/email-templates`)
* **Mục đích:** API quản lý mẫu Email thông báo tự động.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetTemplates()` | `GET /api/admin/email-templates` | Lấy danh sách các mẫu Email hệ thống. | None | `200 OK` (List EmailTemplate) |
| 2 | `GetTemplate(id)` | `GET /api/admin/email-templates/{id}` | Lấy nội dung mẫu Email chi tiết. | `id` | `200 OK` (EmailTemplateDto) |
| 3 | `UpdateTemplate(id, dto)`| `PUT /api/admin/email-templates/{id}` | Cập nhật nội dung mẫu Email. | `id`, `UpdateEmailTemplateDto` | `200 OK` |

---

#### 5.2.2.10 Lớp `DashboardStatsController.cs` (`/api/dashboard/stats`)
* **Mục đích:** API cung cấp số liệu thống kê cho Bảng điều khiển (Dashboard).

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `GetAdminStats()` | `GET /api/dashboard/stats/admin` | Thống kê tổng số sự kiện, lượt đăng ký & doanh thu toàn trường. | Header JWT Token | `200 OK` (AdminStatsDto) |
| 2 | `GetOrganizerStats()`| `GET /api/dashboard/stats/organizer` | Thống kê riêng các chỉ số của Ban tổ chức. | Header JWT Token | `200 OK` (OrganizerStatsDto) |

---

#### 5.2.2.11 Lớp `AiController.cs` (`/api/ai`)
* **Mục đích:** API tích hợp Trợ lý thông minh AI Gemini.

| STT | Action Method | HTTP Verb & Route | Mục đích & Chức năng xử lý | Payload / Input | Response Output |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | `Consult(request)` | `POST /api/ai/consult` | API hỏi đáp tự động với AI Gemini. | `AiConsultRequest` | `200 OK` (`{ answer }`) |
| 2 | `GenerateDescription(req)`| `POST /api/ai/generate-description` | AI gợi ý tự động viết bài mô tả sự kiện. | `GenerateDescriptionRequest` | `200 OK` (`{ generatedContent }`) |
