# EMS - Design System & UI/UX Guidelines

Tài liệu này quy định 2 ngôn ngữ thiết kế hoàn toàn khác biệt phục vụ cho 2 đối tượng người dùng của hệ thống EMS, nhằm tối ưu hóa trải nghiệm riêng biệt của họ.

---

## PHẦN A: ADMIN / ORGANIZER DASHBOARD
**Phong cách: Clean Bento (Thực dụng, Sang trọng, Rõ ràng)**

Giao diện là công cụ làm việc (Work Tool) của Ban tổ chức. Ưu tiên tối đa hóa khả năng đọc dữ liệu (Legibility), hiệu năng cao và giảm tải nhận thức (cognitive load) khi phải thao tác nhiều giờ.

### 1. Nguyên tắc "3 KHÔNG"
1. **KHÔNG Glassmorphism:** Không sử dụng nền kính mờ, đảm bảo hiệu năng và không rối mắt.
2. **KHÔNG Gradients:** Tránh dùng các mảng màu gradient lớn làm nền. Chỉ sử dụng màu đơn sắc (Solid).
3. **KHÔNG Neo-brutalism:** Không dùng viền đen dày, không đổ bóng đen đặc.

### 2. Quy tắc Thị giác
*   **Layout:** Cấu trúc lưới Bento Grid ngăn nắp, khoảng trắng rộng rãi.
*   **Background:** Nền chính xám rất nhạt (`#F7F9FC`). Các thẻ thông tin (Cards) nền Trắng tinh (`#FFFFFF`).
*   **Text:** Chữ màu xám than đậm (`#1A1F36`), tuyệt đối không dùng màu đen `#000000` hoàn toàn.
*   **Borders & Shadows:** Viền siêu mảnh 1px (`#E3E8EE`), đổ bóng cực mềm (diffused soft shadow).
*   **Typography:** Sans-serif hiện đại (Inter, Roboto) với độ tương phản cao.

---

## PHẦN B: END-USER / ATTENDEE PORTAL
**Phong cách: Bento Glassmorphism (Năng động, Rực rỡ, Tương lai)**

Giao diện dành cho người tham gia sự kiện. Mục tiêu cốt lõi: **Truyền cảm hứng, Năng động, Cá nhân hóa và Thu hút sự chú ý (Wow Factor).** 
*(Tham chiếu theo bản thiết kế màu sắc rực rỡ mà bạn đã cung cấp).*

### 1. Nguyên tắc Cốt lõi
*   **Cảm xúc làm chủ đạo:** Trải nghiệm tìm kiếm và đăng ký sự kiện cần mang lại sự hứng thú giống như đang sử dụng một ứng dụng B2C cao cấp hoặc mạng xã hội.
*   **Hiệu ứng thị giác mạnh:** Tận dụng không gian 3D, sự phân tầng màu sắc để làm nổi bật sự kiện.

### 2. Quy tắc Thị giác
*   **Background (Nền dưới cùng):** Sử dụng các hình ảnh khung cảnh, không gian sự kiện hoặc các dải màu loang (Mesh Gradients) có độ rực rỡ cao làm nền dưới cùng.
*   **Glass Cards (Thẻ kính mờ):** Các khối Bento thay vì màu trắng trơn sẽ dùng hiệu ứng kính mờ (`backdrop-filter: blur`, nền trắng/đen có độ trong suốt 20%-40%) lơ lửng trên nền background.
*   **Vibrant Gradients (Màu nổi bật):** Sử dụng các dải màu gradient chói (Tím sang Hồng, Xanh dương sang Lục lam) áp dụng vào các thẻ thống kê cá nhân (Số sự kiện sắp tham gia, Vé của tôi) hoặc nút Call-to-action (Đăng ký ngay).
*   **Borders:** Đường viền sáng bóng, trong suốt nhẹ để tạo cảm giác khối thủy tinh (Glass edges). Bo góc lớn (Large rounded corners) tạo sự thân thiện.
*   **Micro-interactions:** Cần tích hợp các hiệu ứng khi hover chuột (phóng to nhẹ, thẻ phát sáng) để tạo cảm giác giao diện "sống động".
