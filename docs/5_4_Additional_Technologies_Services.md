# 5. MÔ TẢ CHI TIẾT ỨNG DỤNG

## 5.4 CÁC CÔNG NGHỆ NÂNG CAO VÀ DỊCH VỤ TÍCH HỢP

> **Lưu ý theo yêu cầu đề cương:** Dự án **EMS** triển khai kiến trúc hệ thống hiện đại, ngoài mô hình 3 lớp cơ bản (Models - Controllers - Views), hệ thống còn tích hợp các công nghệ nâng cao như Cơ sở dữ liệu Cloud NoSQL, Tác vụ chạy ngầm tự động (Hangfire), Trí tuệ nhân tạo (Gemini AI), Cổng thanh toán trực tuyến (VNPay) và các dịch vụ kết xuất dữ liệu báo cáo chuyên dụng.

---

### 5.4.1 Cơ sở dữ liệu NoSQL Cloud & Xác thực (Google Firebase)

* **Google Firebase Firestore (Cơ sở dữ liệu NoSQL Cloud):**
  * **Vai trò:** Thay thế cho cơ sở dữ liệu quan hệ SQL truyền thống. Hệ thống sử dụng Firestore lưu trữ dữ liệu dưới dạng các tập hợp (Collections) và tài liệu (Documents), cho phép mở rộng linh hoạt, tốc độ truy vấn cao và cách ly dữ liệu giữa các Tenant.
  * **Các Lớp dịch vụ xử lý chính (`EMS.Infrastructure/Services`):**
    * `FirestoreEventService.cs`: Quản lý lưu trữ và truy vấn dữ liệu sự kiện.
    * `FirestoreRegistrationService.cs`: Quản lý vé và điểm danh.
    * `FirestoreUserService.cs`: Quản lý tài khoản người dùng.
    * `FirestoreTenantService.cs`: Quản lý đa tổ chức Multi-tenant.
* **Firebase Authentication & JWT Token (Xác thực & Bảo mật):**
  * **Vai trò:** Xác thực danh tính người dùng đa nền tảng, quản lý token bảo mật JWT (JSON Web Token) cho phép đăng nhập một lần (Single Sign-On).
  * **Lớp dịch vụ xử lý:** `FirebaseAuthService.cs`.
* **Firebase Storage (Lưu trữ tệp đa phương tiện):**
  * **Vai trò:** Lưu trữ ảnh banner sự kiện, logo thương hiệu trường học và tài liệu minh chứng.

---

### 5.4.2 Tác vụ chạy ngầm tự động & Gửi Email (Hangfire & SMTP Email)

* **Hangfire Framework (Lập lịch tác vụ ngầm - Background Jobs):**
  * **Vai trò:** Chạy các tác vụ nền định kỳ mà không làm ảnh hưởng đến tốc độ phản hồi trang web người dùng.
  * **Lớp tác vụ chính (`EventReminderJob.cs`):** Tự động chạy mỗi giờ một lần để quét toàn bộ sự kiện sắp diễn ra trong vòng 24 giờ và 1 giờ tới, sau đó tự động gửi Email nhắc nhở tham dự cho các sinh viên đã có vé.
* **SmtpEmailService (Dịch vụ gửi Email thông báo):**
  * **Vai trò:** Tự động gửi Email thông báo tới sinh viên trong các trường hợp: xác nhận đăng ký vé thành công, báo duyệt vé từ danh sách chờ (Waitlist) và email nhắc lịch sự kiện.

---

### 5.4.3 Trí tuệ nhân tạo Trợ lý AI (Google Gemini AI)

* **Google Gemini AI API Integration (`GeminiAiService.cs`):**
  * **Vai trò:** Tích hợp mô hình ngôn ngữ lớn (LLM) của Google để cung cấp 2 tính năng thông minh:
    1. **Trợ lý AI tư vấn sinh viên (Chatbot):** Tự động trả lời thắc mắc của sinh viên về sự kiện, thời gian, địa điểm và hướng dẫn đăng ký.
    2. **Hỗ trợ Ban tổ chức tạo nội dung (Content Generator):** Gợi ý và tự động soạn thảo văn bản mô tả sự kiện cho Ban tổ chức khi tạo sự kiện mới.

---

### 5.4.4 Thanh toán trực tuyến (Cổng thanh toán VNPay)

* **VNPay Payment Gateway (`VnPayService.cs`):**
  * **Vai trò:** Tích hợp cổng thanh toán trực tuyến an toàn theo chuẩn ngân hàng cho các sự kiện có thu phí tham dự.
  * **Quy trình:** Sinh URL thanh toán mã hóa SHA256 ➔ Chuyển hướng người dùng sang ứng dụng Ngân hàng/VNPAY ➔ Xử lý phản hồi bảo mật (IPN Callback) để tự động kích hoạt vé sau khi thanh toán thành công.

---

### 5.4.5 Các thư viện kết xuất báo cáo & Đồng bộ lịch (Export Libraries)

| STT | Tên thư viện     | Công dụng & Chức năng trong hệ thống                                                                                                     | File xử lý trong mã nguồn |
| :-: | :------------------ | :--------------------------------------------------------------------------------------------------------------------------------------------- | :---------------------------- |
|  1  | **ClosedXML** | Xuất danh sách sinh viên đăng ký vé ra file Excel (`.xlsx`).                                                                          | `ReportsController.cs`      |
|  2  | **QuestPDF**  | Thiết kế và kết xuất danh sách điểm danh thực tế ra file PDF (`.pdf`) chuẩn in ấn.                                               | `ReportsController.cs`      |
|  3  | **ICal.NET**  | Sinh file định dạng iCalendar (`.ics`) giúp sinh viên thêm sự kiện vào Google Calendar / Outlook chỉ với 1-click.                 | `CalendarService.cs`        |
|  4  | **Chart.js**  | Trực quan hóa dữ liệu thống kê số liệu đăng ký/điểm danh dưới dạng biểu đồ tròn và biểu đồ cột trên Dashboard Admin. | `Dashboard.razor`           |
