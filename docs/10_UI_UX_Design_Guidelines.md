# BẢN HƯỚNG DẪN THIẾT KẾ UI/UX (EMS SYSTEM)

Các quy tắc dưới đây được thiết lập nhằm mang lại trải nghiệm tinh tế, chuyên nghiệp và có độ phản hồi thị giác cao cho mọi đối tượng người dùng.

---

## PHẦN A: BẢNG SO SÁNH HAI NGÔN NGỮ THIẾT KẾ

| Đặc tính thị giác | Admin & Organizer Dashboard (Blazor WASM) | Student & Attendee Portal (ASP.NET MVC) |
| :--- | :--- | :--- |
| **Vibe & Concept** | **Futuristic Dark Slate (Công cụ, Hiện đại, Tập trung)** | **Vibrant Bento Glassmorphism (Rực rỡ, Năng động, Cảm xúc)** |
| **Tone màu chủ đạo** | Nền tối Slate (`#0F172A`), Thẻ tối (`#1E293B`), Chi tiết Indigo (`#6366F1`) | Nền sáng Slate (`#F8FAFC`), Radial Mesh Gradient loang màu Tím/Hồng nhạt |
| **Font chữ (Heading & Body)** | **Inter** (Sạch sẽ, hình học, tối ưu hiển thị bảng biểu/dữ liệu số) | **Outfit** (Bo tròn tinh tế, trẻ trung, tạo cảm giác thân thiện) |
| **Đặc tính Bento** | Bento Grid phẳng, phân chia rõ ràng bằng viền mảnh và shadow mềm | Bento Glassmorphism lơ lửng trên nền mesh gradient loang màu |
| **Tâm lý thiết kế** | Giảm mỏi mắt khi làm việc lâu, tập trung tối đa vào thông tin dữ liệu | Kích thích sự tò mò, tạo cảm giác chuyên nghiệp nhưng trẻ trung |

---

## PHẦN B: CHI TIẾT NGÔN NGỮ THIẾT KẾ ADMIN (BLAZOR WASM)

Giao diện Admin là một công cụ làm việc hiệu năng cao (Work Tool). Mọi thiết kế phải hướng đến sự rõ ràng, dễ đọc dữ liệu (Legibility) và thao tác nhanh chóng.

### 1. Palette Màu Sắc (CSS Variables)
*   **Màu nền chính (Body bg):** `#0F172A` (Deep Slate - Giúp dịu mắt).
*   **Màu thẻ & Input (Card/Input bg):** `#1E293B` (Slightly lighter slate để phân cấp thị giác).
*   **Màu tương tác (Primary / Hover):** `#6366F1` (Indigo 500) / `#4F46E5` (Indigo 600).
*   **Màu chữ chính:** `#F1F5F9` (Độ tương phản cao trên nền tối).
*   **Màu chữ phụ:** `#94A3B8` (Dành cho mô tả, nhãn phụ).
*   **Borders:** `#334155` (Viền mảnh, chìm).

### 2. Cấu trúc Layout
*   **Lưới Bento Thực Dụng (Bento Grid):** Chia các khu vực chức năng thành các thẻ (cards) riêng biệt. Khoảng cách (gap) giữa các thẻ tối thiểu là `1.25rem` (20px) để giao diện có không gian thở.
*   **Header Bar cố định:** Chiều cao tối thiểu 56px, nền `#1E293B` có border bottom mảnh để phân tách rõ ràng phần điều hướng và nội dung.

---

## PHẦN C: QUY TẮC THIẾT KẾ CHUNG (ÁP DỤNG CHO CẢ ADMIN VÀ SINH VIÊN)

Đây là những quy tắc cốt lõi bắt buộc phải tuân thủ khi xây dựng bất kỳ giao diện nào trong hệ thống EMS.

### 1. Quy tắc Bao Bọc Đa Tầng (Double-Bezel / Nested Enclosure)
Tuyệt đối không đặt các phần tử quan trọng (như ảnh, biểu đồ, cụm input phức tạp) nằm phẳng lỳ trên nền card. Hãy áp dụng quy tắc lồng ghép để tạo chiều sâu haptic (như các thiết bị phần cứng cao cấp của Apple):
*   **Outer Shell (Vỏ ngoài):** Một vùng đệm ngoài có màu nền mờ nhạt (`rgba(255,255,255,0.03)` trên nền tối hoặc `rgba(0,0,0,0.02)` trên nền sáng), viền mảnh 1px, bo góc lớn (ví dụ: `24px`).
*   **Inner Core (Nhân bên trong):** Container nội dung thực sự nằm bên trong vỏ ngoài, có khoảng đệm (padding) đồng đều từ `6px` đến `8px`.
*   **Quy luật bo góc đồng tâm (Mathematically Consistent Radius):** Bo góc của nhân bên trong phải nhỏ hơn bo góc vỏ ngoài theo công thức: 
    $$R_{inner} = R_{outer} - Padding_{outer}$$
    *(Ví dụ: Vỏ ngoài bo `24px`, padding giữa 2 lớp là `8px` $\rightarrow$ nhân bên trong phải bo đúng `16px` để tạo các đường cong song song hoàn hảo).*

### 2. Phân Cấp Bo Góc (Corner Radius Hierarchy)
Hệ thống bo góc phải được chuẩn hóa để tạo sự nhất quán trực quan:
*   **Micro-elements (`6px` - `8px`):** Áp dụng cho các badge nhỏ, dropdown menu con, checkbox hoặc tooltip.
*   **Standard Input & Button (`10px` - `12px`):** Áp dụng cho nút bấm, ô nhập liệu thông thường.
*   **Card & Component (`16px` - `20px`):** Áp dụng cho thẻ thông tin sự kiện, bảng biểu, form lớn.
*   **Bento Section (`24px` - `32px`):** Áp dụng cho các khối Bento lớn hoặc phân vùng layout lớn.

### 3. Nút Bấm & Trạng Tính Tương Tác Premium
*   **Cấu trúc Nút bấm Chính (Button-in-Button):** Nút bấm CTA (Call to action) quan trọng nên có dạng viên thuốc (`rounded-full`) hoặc bo góc lớn. Nếu nút có icon đi kèm (ví dụ: mũi tên `↗`), icon đó phải nằm trong một vòng tròn nhỏ riêng biệt ở góc phải nút, tạo sự tương tác sinh động.
*   **Phản hồi vật lý (Kinetic Physics):**
    *   Khi hover: Nút nâng nhẹ hoặc tăng độ phát sáng (glow shadow).
    *   Khi click (Active): Thu nhỏ nhẹ nút bấm (`scale-[0.98]`) để giả lập cảm giác nhấn phím cơ học.
*   **Tốc độ chuyển đổi (Transition-duration):** Sử dụng các hiệu ứng chuyển đổi mượt mà với `cubic-bezier(0.34, 1.56, 0.64, 1)` (hiệu ứng đàn hồi nhẹ) thay vì `linear` hay `ease-in-out` mặc định của CSS.

### 4. Spacing và Nhịp Điệu Không Gian (Whitespace Management)
*   **Tăng gấp đôi Whitespace:** Thiết kế của AI thường mắc lỗi nhồi nhét chữ và thông tin. Hãy tăng khoảng trống xung quanh tiêu đề và giữa các phần lớn (`py-16` đến `py-24` trên desktop).
*   **Eyebrow Tags:** Trước khi bắt đầu một thẻ tiêu đề lớn (H1/H2), luôn đi kèm một thẻ nhãn siêu nhỏ phía trên (ví dụ: chữ in hoa, khoảng cách chữ rộng, kích thước `10px`, tracking `0.15em`, ví dụ: `TỔNG QUAN SỰ KIỆN`).

### 5. Sử Dụng Icon Tinh Tế (Iconography)
*   Chỉ sử dụng các icon dạng nét mảnh (Outline/Line) với độ dày stroke đồng nhất (1.5px hoặc 1px). Không dùng xen kẽ icon nét đậm (Solid) và nét mảnh.
*   Khuyến khích sử dụng các bộ thư viện như **Remix Icon (Line)** hoặc **Phosphor Icons (Light/Regular)**.

### 6. Đáp Ứng Thiết Bị Di Động (Mobile-Responsive Overrides)
*   **Sụp lưới Bento tự động:** Tất cả Bento grid phức tạp (2 cột, 3 cột bất đối xứng) bắt buộc phải tự động chuyển về dạng 1 cột dọc (`grid-cols-1`) khi màn hình nhỏ hơn 768px.
*   **Bỏ hiệu ứng nặng:** Tự động loại bỏ hiệu ứng `backdrop-filter: blur` (kính mờ) trên các danh sách cuộn dài ở thiết bị di động để tránh gây sụt giảm khung hình (FPS drop) do GPU phải render lại liên tục.
*   **Khoảng đệm an toàn:** Tránh dùng `h-screen` (chiều cao toàn màn hình cố định) vì thanh địa chỉ của trình duyệt di động hay co giãn. Hãy dùng `min-h-[100dvh]` để thay thế.

---

## PHẦN D: KIỂM SOÁT HIỆU NĂNG GIAO DIỆN (PERFORMANCE GUARDRAILS)
*   **GPU-Safe Animations:** Chỉ tạo chuyển động cho 2 thuộc tính: `transform` (di chuyển, co giãn, xoay) và `opacity` (độ mờ). Tuyệt đối không tạo chuyển động (transition/animation) cho `width`, `height`, `top`, `left` vì chúng bắt trình duyệt phải tính toán lại layout (reflow), gây giật lag.
*   **Z-Index có hệ thống:** Không sử dụng các giá trị z-index tùy tiện (như `z-[9999]`). Hãy phân cấp rõ ràng:
    *   `z-0` đến `z-10`: Nội dung và thẻ card bình thường.
    *   `z-20`: Header bar cố định (sticky).
    *   `z-30`: Dropdown menu, popover.
    *   `z-40`: Hộp thoại Modal, overlay nền.
    *   `z-50`: Tooltip và Toast thông báo khẩn cấp.
