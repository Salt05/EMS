# 5. MÔ TẢ CHI TIẾT ỨNG DỤNG

## 5.3 VIEWS

Phần này thống kê và mô tả chi tiết danh sách các giao diện (Views/Pages/Components) trong hệ thống dưới dạng **Bảng dữ liệu chi tiết theo từng gói**, bao gồm các trang giao diện Razor Views thuộc Cổng sinh viên (`EMS.Mvc`) và các trang giao diện Blazor WebAssembly thuộc Dashboard quản trị (`EMS.BlazorWASM`).

---

### 5.3.1 Phân hệ Gói Views Cổng Sinh viên (`EMS.Mvc`)

#### 5.3.1.1 Gói `Views/Home`
* **Mục đích:** Quản lý các giao diện trang chủ, banner trình chiếu và chính sách bảo mật cho cổng sinh viên.

| STT | Tên View / Partial View | Loại View | Mục đích & Nội dung hiển thị chính | Controller liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Index.cshtml` | Full Page | Trang chủ hệ thống: Slider banner sự kiện lớn, thanh tìm kiếm nhanh và lưới 6 sự kiện mới nhất. | `HomeController` |
| 2 | `_UpcomingEventBanner.cshtml` | Partial View | Banner động đếm ngược thời gian sự kiện sắp diễn ra của sinh viên đã đăng nhập. | `HomeController` |
| 3 | `Privacy.cshtml` | Full Page | Hiển thị chi tiết chính sách bảo mật dữ liệu và điều khoản dịch vụ người dùng. | `HomeController` |

---

#### 5.3.1.2 Gói `Views/Events`
* **Mục đích:** Quản lý các giao diện danh sách sự kiện, chi tiết sự kiện, vé cá nhân, điểm danh và kho đổi quà thưởng.

| STT | Tên View / Partial View | Loại View | Mục đích & Nội dung hiển thị chính | Controller liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Index.cshtml` | Full Page | Lưới danh sách sự kiện kèm bộ lọc từ khóa, danh mục (Hội thảo, CLB, Học thuật) và phân trang. | `EventsController` |
| 2 | `Detail.cshtml` | Full Page | Chi tiết sự kiện: Banner, mô tả, địa điểm, sức chứa khả dụng, nút Đăng ký 1-click & Waitlist. | `EventsController` |
| 3 | `_AgendaTimeline.cshtml` | Partial View | Mốc thời gian lịch trình tiết mục (Agenda Timeline) và thông tin diễn giả khách mời. | `EventsController` |
| 4 | `_EventCard.cshtml` | Partial View | Thẻ thông tin thu nhỏ của sự kiện (Event Card) tái sử dụng trên các trang. | `EventsController` |
| 5 | `MyEvents.cshtml` | Full Page | Trang quản lý vé cá nhân của sinh viên (Tab: Vé đang hoạt động & Tab: Lịch sử tham dự/Hủy). | `EventsController` |
| 6 | `Checkin.cshtml` | Full Page | Trang hiển thị Mã điểm danh 6 số và mã QR Code cho sinh viên điểm danh tại cửa sự kiện. | `EventsController` |
| 7 | `MyRewards.cshtml` | Full Page | Trang quản lý tổng số điểm tích lũy và kho phần thưởng cho phép sinh viên đổi quà. | `EventsController` |
| 8 | `_MyRewardsTablePartial.cshtml` | Partial View | Bảng lịch sử các lượt đổi phần thưởng của sinh viên. | `EventsController` |

---

#### 5.3.1.3 Gói `Views/Auth`
* **Mục đích:** Quản lý các giao diện đăng nhập và đăng ký tài khoản sinh viên.

| STT | Tên View / Partial View | Loại View | Mục đích & Nội dung hiển thị chính | Controller liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Login.cshtml` | Full Page | Form đăng nhập tài khoản sinh viên/người dùng thiết kế chuẩn Glassmorphism. | `AuthController` |
| 2 | `Register.cshtml` | Full Page | Form đăng ký tài khoản sinh viên mới, hỗ trợ validate dữ liệu ngay trên Client. | `AuthController` |

---

#### 5.3.1.4 Gói `Views/Payment`
* **Mục đích:** Quản lý các giao diện xác nhận và trả về kết quả thanh toán trực tuyến VNPay.

| STT | Tên View / Partial View | Loại View | Mục đích & Nội dung hiển thị chính | Controller liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Pay.cshtml` | Full Page | Trang xác nhận thông tin vé và tổng tiền trước khi chuyển hướng sang VNPay. | `PaymentController` |
| 2 | `Success.cshtml` | Full Page | Giao diện thông báo giao dịch thanh toán vé thành công qua VNPay. | `PaymentController` |
| 3 | `Failure.cshtml` | Full Page | Giao diện thông báo giao dịch thanh toán thất bại hoặc bị người dùng hủy. | `PaymentController` |

---

#### 5.3.1.5 Gói `Views/Shared`
* **Mục đích:** Quản lý giao diện khung bố cục dùng chung (Layout) cho toàn bộ cổng sinh viên.

| STT | Tên View / Partial View | Loại View | Mục đích & Nội dung hiển thị chính | Controller liên quan |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `_Layout.cshtml` | Master Layout | Khung giao diện chung: Header, Navigation Bar, Footer và tích hợp Floating AI Chatbot. | Tất cả Controllers |
| 2 | `_ValidationScriptsPartial.cshtml`| Partial View | Thư viện Scripts validate dữ liệu Form phía Client (jQuery Validation). | Tất cả Controllers |

---

### 5.3.2 Phân hệ Gói Pages / Components Dashboard Quản trị (`EMS.BlazorWASM`)

#### 5.3.2.1 Gói `Pages/Admin` (Quản trị viên Tenant)
* **Mục đích:** Quản lý toàn bộ các giao diện nghiệp vụ dành cho Quản trị viên cấp trường/tổ chức.

| STT | Tên Page / Component | Loại Component | Mục đích & Nội dung hiển thị chính | Vai trò sử dụng |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Dashboard.razor` | Blazor Page | Bảng điều khiển KPI Cards tổng hợp số sự kiện, tỷ lệ lấp đầy vé và biểu đồ Chart.js. | Admin / Manager |
| 2 | `Events/Index.razor` | Blazor Page | Danh sách toàn bộ các sự kiện trong trường kèm công cụ lọc và tìm kiếm. | Admin / Manager |
| 3 | `Events/Create.razor` | Blazor Page | Form tạo mới sự kiện với các tùy chọn sức chứa, địa điểm, thời gian và loại vé. | Admin / Manager |
| 4 | `Events/Details.razor` | Blazor Page | Xem chi tiết toàn bộ thông tin sự kiện, danh sách đăng ký và lịch trình. | Admin / Manager |
| 5 | `PendingEvents.razor` | Blazor Page | Quy trình kiểm duyệt sự kiện: Admin bấm Phê duyệt (`Approve`) hoặc Từ chối (`Reject`). | Admin / Manager |
| 6 | `Users/UserManagement.razor` | Blazor Page | Quản lý người dùng: Phân quyền tài khoản (Student, Organizer, Admin) và Khóa/Mở tài khoản. | Admin / Manager |
| 7 | `Attendance.razor` | Blazor Page | Công cụ quét/nhập mã check-in 6 số để xác nhận điểm danh sinh viên real-time. | Admin / Organizer |
| 8 | `RewardCategories.razor` | Blazor Page | Quản lý kho phần thưởng & quà tặng tích điểm dành cho sinh viên. | Admin / Manager |
| 9 | `RewardStats.razor` | Blazor Page | Bảng thống kê số điểm tích lũy và tình hình đổi quà của sinh viên. | Admin / Manager |
| 10 | `EmailTemplates.razor` | Blazor Page | Cấu hình và chỉnh sửa nội dung các mẫu Email thông báo tự động. | Admin / Manager |
| 11 | `Reports.razor` | Blazor Page | Trung tâm kết xuất báo cáo: Xuất danh sách đăng ký ra Excel (`.xlsx`) và điểm danh ra PDF (`.pdf`). | Admin / Manager |

---

#### 5.3.2.2 Gói `Pages/Organizer` (Ban tổ chức)
* **Mục đích:** Quản lý các giao diện nghiệp vụ riêng dành cho Ban tổ chức sự kiện.

| STT | Tên Page / Component | Loại Component | Mục đích & Nội dung hiển thị chính | Vai trò sử dụng |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Dashboard.razor` | Blazor Page | Bảng điều khiển tiến độ đăng ký vé, danh sách chờ Waitlist và tỷ lệ điểm danh. | Organizer |
| 2 | `MyEvents.razor` | Blazor Page | Danh sách các sự kiện do chính Ban tổ chức đó khởi tạo và quản lý. | Organizer |
| 3 | `PendingRegistrations.razor` | Blazor Page | Duyệt thủ công sinh viên từ danh sách chờ (Waitlist) lên vé chính thức. | Organizer |
| 4 | `Registrations.razor` | Blazor Page | Xem và quản lý danh sách toàn bộ sinh viên đã đăng ký tham gia sự kiện. | Organizer |
| 5 | `Participants.razor` | Blazor Page | Quản lý danh sách sinh viên đã điểm danh thành công tại sự kiện. | Organizer |
| 6 | `Attendance.razor` | Blazor Page | Giao diện tối ưu di động giúp quét/nhập nhanh mã điểm danh 6 số tại cổng sự kiện. | Organizer |

---

#### 5.3.2.3 Gói `Pages/SuperAdmin` (Quản trị hệ thống toàn nền tảng)
* **Mục đích:** Quản lý các giao diện quản trị cấp cao cho toàn bộ hệ thống Đa tổ chức (SaaS Multi-tenant).

| STT | Tên Page / Component | Loại Component | Mục đích & Nội dung hiển thị chính | Vai trò sử dụng |
| :-: | :--- | :--- | :--- | :--- |
| 1 | `Dashboard.razor` | Blazor Page | Bảng điều khiển giám sát toàn bộ hoạt động của hệ thống SaaS đa tổ chức cross-tenant. | SuperAdmin |
| 2 | `Tenants.razor` | Blazor Page | Quản lý các trường/tổ chức: Khởi tạo Tenant mới, cài đặt Subdomain, Logo và màu sắc. | SuperAdmin |
| 3 | `Users.razor` | Blazor Page | Giám sát và quản lý toàn bộ người dùng cross-tenant trên toàn hệ thống. | SuperAdmin |
| 4 | `Events.razor` | Blazor Page | Giám sát toàn bộ sự kiện trên tất cả các trường/tổ chức. | SuperAdmin |
| 5 | `Settings.razor` | Blazor Page | Quản lý tham số hệ thống toàn cục, cấu hình API Firebase, Hangfire và SMTP Server. | SuperAdmin |
