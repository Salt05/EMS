# Phân tích Phong cách Thiết kế Giao diện (Aplot AI Style)

## 1. Phong cách chủ đạo (Core Aesthetic)

Giao diện mang đậm phong cách **Modern Glassmorphism** kết hợp với **Aurora UI** (các mảng màu gradient mềm mại chìm dưới background) và phảng phất nét **Neumorphism** hiện đại thông qua các khối nổi (floating cards) và nút bấm có chiều sâu không gian. Thiết kế tối giản, tập trung vào typography và khoảng trắng, mang lại cảm giác tương lai (futuristic) nhưng vẫn vô cùng thân thiện, khuyến khích sự tương tác.

## 2. Bảng màu (Color Palette)

Hệ thống màu sắc được tinh chỉnh hoàn hảo, tạo sự tương phản tốt nhưng không làm chói mắt.

* **Dark Mode:**
  * **Background:** Màu đen sâu (`#0D0D0D` đến `#141414`), điểm xuyết các vùng ánh sáng gradient xanh dương/tím mờ ảo ở trung tâm, tạo cảm giác không gian 3D sâu thẳm.
  * **Surface/Card:** Màu xám than trong suốt với viền cực mỏng, độ sáng thấp (VD: `rgba(255, 255, 255, 0.05)`).
  * **Text:** Trắng tinh khiết cho tiêu đề chính, xám nhạt (`#A0A0A0`) cho văn bản phụ.
* **Light Mode:**
  * **Background:** Màu trắng ngà mượt mà (`#F9FAFB`), vầng sáng gradient pastel (xanh lơ, hồng nhạt, vàng cam nhạt) lan tỏa nhẹ nhàng từ giữa màn hình.
  * **Surface/Card:** Trắng tinh (`#FFFFFF`) kèm hiệu ứng đổ bóng mờ (soft drop shadow) tạo cảm giác bề mặt nổi rõ rệt.
  * **Text:** Đen xám đậm (`#1A1A1A`) cho tiêu đề, xám vừa cho văn bản phụ.
* **Accent Color (Màu nhấn chung):** Xanh dương hoàng gia / Tím nhạt (`#4F46E5` hoặc tương tự) dùng cho nút bấm chính (Primary Button), các thẻ tag đang active.

## 3. Typography

* Sử dụng font chữ **Sans-serif** hiện đại, hình học (ví dụ: Inter, Poppins, hoặc Plus Jakarta Sans).
* Độ tương phản weight rõ ràng: Tiêu đề (Headings) dùng font weight Bold (700) đến Extra Bold, trong khi các đoạn text phụ, placeholder dùng Regular (400) hoặc Medium (500).
* Kích thước chữ lớn ở phần trung tâm (ví dụ: "HI Rownok...") tạo điểm nhấn thị giác mạnh mẽ ngay khi vừa mở trang.

## 4. UI Components & Iconography (Các thành phần Giao diện)

* **Cấu trúc bo góc (Border Radius):** Chú trọng vào kiểu dáng **Squircle** (siêu bo tròn) và hình viên thuốc (Pill-shaped). Gần như không có góc nhọn nào xuất hiện.
* **Thanh điều hướng (Sidebar):**
  * Nằm dọc bên trái, tách biệt hoàn toàn với nội dung chính.
  * Các icon tối giản, dạng line (viền) mảnh. Icon đang active được đặt trong một background vòng tròn mờ tinh tế.
* **Input Field & Khối tác vụ (Floating Task Card):**
  * Khối nhập liệu trung tâm là một cụm trôi nổi cỡ lớn, mang lại cảm giác của một thanh dock 3D có thể chạm vào.
  * Bao gồm: Banner Upgrade bên trong (nút nhấn màu xanh nổi bật), vùng nhập liệu không viền, và dải công cụ bên dưới.
  * Các nút công cụ (Attach File, Reasoning, Create Image...) thiết kế dạng hình viên thuốc với viền mỏng (`1px solid`). Nút Send là một hình tròn với icon nổi bật.
* **Tags/Chips:** Các thẻ gợi ý (Coding, Cooking, Health...) xếp hàng ngang bên dưới input, thiết kế bo tròn dạng pill. Đi kèm icon nhỏ, viền rất mảnh và đổ bóng cực nhẹ, mang lại xúc cảm thị giác (tactile feel) tuyệt vời.
* **Nhân vật / Avatar:** Sử dụng hình ảnh minh họa hoặc render 3D mềm mại, kết hợp với các bóng chat (chat bubbles) nổi bồng bềnh nhờ hiệu ứng shadow, tạo nên sự sinh động (nếu người dùng có yêu cầu)

## 5. Effects & Shadows (Hiệu ứng)

* **Inner Shadow & Drop Shadow:** Sử dụng soft-shadow với độ lan tỏa (blur) lớn thay vì bóng gắt, giúp các khối UI tách biệt khỏi nền một cách tự nhiên.
  * Ở Light mode, bóng có màu xám nhạt trong suốt.
  * Ở Dark mode, bóng gần như tiệp vào nền đen hoặc sử dụng viền sáng (glow viền) thay thế để nhấn mạnh hình khối.
* **Blur/Backdrop Filter:** Ứng dụng hiệu ứng kính mờ (frosted glass) ở các thanh panel hoặc khối nhập liệu để lộ một phần màu gradient của background phía sau.
