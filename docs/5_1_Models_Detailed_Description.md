# 5. MÔ TẢ CHI TIẾT ỨNG DỤNG

## 5.1 MODELS (Mô hình dữ liệu các thực thể)

Dưới đây là phần trình bày chi tiết các lớp đối tượng trong mô hình dữ liệu (`EMS.Core/Entities`) dưới dạng **Bảng thuộc tính**, giúp bài báo cáo trực quan, chuyên nghiệp và dễ theo dõi.

---

### 5.1.2 Lớp `Event.cs` (Mô hình Sự kiện)

* **Mục đích:** Đóng gói toàn bộ dữ liệu sự kiện, bao gồm thông tin mô tả, thời gian tổ chức, địa điểm, sức chứa, giá vé, trạng thái phê duyệt của Admin và mã check-in điểm danh.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã định danh duy nhất của sự kiện (GUID). |
| 2 | `TenantId` | `string` | **FK** | Mã trường/tổ chức sở hữu sự kiện (Đa tổ chức Multi-tenant). |
| 3 | `Title` | `string` | | Tên/Tiêu đề sự kiện. |
| 4 | `Description` | `string` | | Nội dung mô tả chi tiết về sự kiện. |
| 5 | `Location` | `string` | | Địa điểm tổ chức sự kiện. |
| 6 | `VenueId` | `string?` | **FK** | Mã địa điểm cố định (nếu thuộc danh mục Venues). |
| 7 | `StartTime` | `DateTime` | | Thời gian bắt đầu sự kiện. |
| 8 | `EndTime` | `DateTime` | | Thời gian kết thúc sự kiện. |
| 9 | `Capacity` | `int` | | Sức chứa tối đa / Số lượng vé phát ra. |
| 10 | `ImageUrl` | `string?` | | Đường dẫn ảnh banner trình chiếu của sự kiện. |
| 11 | `OrganizerId` | `string` | **FK** | Mã tài khoản Ban tổ chức khởi tạo sự kiện. |
| 12 | `Status` | `EventStatus` | | Trạng thái duyệt (`Pending`, `Approved`, `Rejected`, `Cancelled`). |
| 13 | `ApprovedById` | `string?` | **FK** | Mã Admin phê duyệt sự kiện. |
| 14 | `ApprovedAt` | `DateTime?` | | Thời điểm sự kiện được phê duyệt. |
| 15 | `RejectionReason`| `string?` | | Lý do từ chối sự kiện (nếu Admin rejected). |
| 16 | `CheckInCode` | `string?` | | Mã điểm danh ngẫu nhiên 6 ký tự. |
| 17 | `Price` | `decimal` | | Giá vé tham gia (0 nếu là sự kiện miễn phí). |
| 18 | `Scope` | `EventScope` | | Phạm vi công khai (`Public`) hay nội bộ (`Internal`). |
| 19 | `CreatedAt` | `DateTime` | | Thời điểm khởi tạo bản ghi sự kiện. |

* **Phương thức chính:** `ToFirestoreDocument()` (Đóng gói thuộc tính sang định dạng Document Firestore) và thuộc tính kiểm tra vé miễn phí `IsFree` (`Price <= 0`).

---

### 5.1.3 Lớp `Registration.cs` (Mô hình Đăng ký Tham gia & Vé)

* **Mục đích:** Quản lý thông tin đăng ký vé của sinh viên, trạng thái xếp hàng chờ (Waitlist), thông tin điểm danh thực tế và dữ liệu thanh toán VNPay.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã đăng ký / Mã vé duy nhất. |
| 2 | `TenantId` | `string` | **FK** | Mã trường/tổ chức quản lý vé. |
| 3 | `EventId` | `string` | **FK** | Mã sự kiện được đăng ký tham gia. |
| 4 | `UserId` | `string` | **FK** | Mã sinh viên/người dùng đăng ký vé. |
| 5 | `StudentName` | `string` | | Họ và tên sinh viên tham gia. |
| 6 | `StudentEmail` | `string` | | Email nhận thông báo và vé điện tử. |
| 7 | `Status` | `RegistrationStatus`| | Trạng thái vé (`Pending`, `Confirmed`, `Waitlisted`, `Cancelled`). |
| 8 | `RegisteredAt` | `DateTime` | | Thời điểm sinh viên bấm đăng ký vé. |
| 9 | `CheckInCode` | `string?` | | Mã check-in 6 số riêng cho từng lượt đăng ký. |
| 10 | `CheckedIn` | `bool` | | Cờ xác nhận sinh viên đã có mặt tại sự kiện chưa (`true/false`). |
| 11 | `CheckedInAt` | `DateTime?` | | Thời điểm Ban tổ chức quét mã check-in thực tế. |
| 12 | `ReminderSent` | `bool` | | Cờ đánh dấu đã gửi email nhắc nhở tự động (Hangfire). |
| 13 | `IsPaid` | `bool` | | Cờ xác nhận đã hoàn tất thanh toán VNPay chưa. |
| 14 | `PaymentTransactionId`|`string?`| | Mã giao dịch thanh toán qua cổng VNPay. |
| 15 | `PlatformFee` | `decimal` | | Khấu trừ phí dịch vụ hệ thống. |

---

### 5.1.4 Lớp `User.cs` (Mô hình Người dùng)

* **Mục đích:** Quản lý hồ sơ tài khoản người dùng, phân quyền (Sinh viên, Ban tổ chức, Admin), thông tin sinh viên và mã định danh xác thực Firebase Auth.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã người dùng trong hệ thống. |
| 2 | `FirebaseUid` | `string` | | Mã Uid nhận từ hệ thống Firebase Authentication. |
| 3 | `Email` | `string` | | Địa chỉ Email tài khoản. |
| 4 | `FullName` | `string` | | Họ và tên người dùng. |
| 5 | `MSSV` | `string?` | | Mã số sinh viên (nếu thuộc vai trò Sinh viên). |
| 6 | `Department` | `string?` | | Khoa / Đơn vị công tác. |
| 7 | `PhoneNumber` | `string?` | | Số điện thoại liên lạc. |
| 8 | `TenantId` | `string` | **FK** | Mã trường/tổ chức người dùng trực thuộc. |
| 9 | `RoleIds` | `List<string>` | **FK** | Danh sách vai trò phân quyền gán cho tài khoản. |
| 10 | `Status` | `UserStatus` | | Trạng thái tài khoản (`Active`, `Inactive`, `Banned`). |
| 11 | `LastLoginAt` | `DateTime?` | | Thời gian thực hiện đăng nhập gần nhất. |

---

### 5.1.5 Lớp `Tenant.cs` (Mô hình Đa tổ chức / Trường học)

* **Mục đích:** Quản lý danh sách các trường đại học/tổ chức tham gia hệ thống (Mô hình SaaS Đa tổ chức Multi-tenant), subdomain và các tham số cấu hình riêng.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã định danh duy nhất của tổ chức/trường học. |
| 2 | `Name` | `string` | | Tên đầy đủ của Trường/Tổ chức. |
| 3 | `Subdomain` | `string` | | Tên miền con nhận diện (VD: `hcmut`, `uit`). |
| 4 | `Email` | `string` | | Email liên hệ đại diện của tổ chức. |
| 5 | `PhoneNumber` | `string?` | | Số điện thoại liên hệ tổ chức. |
| 6 | `Address` | `string?` | | Địa chỉ trụ sở chính của trường/tổ chức. |
| 7 | `IsActive` | `bool` | | Trạng thái hoạt động của Tenant. |
| 8 | `PlatformFeePercentage`| `double` | | Tỷ lệ % phí dịch vụ nền tảng trích xuất. |

---

### 5.1.6 Lớp `AgendaItem.cs` (Mô hình Lịch trình Tiết mục)

* **Mục đích:** Lưu trữ thông tin chi tiết từng mốc lịch trình, tiết mục hoặc thông tin diễn giả trong quá trình diễn ra sự kiện.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã chi tiết mốc lịch trình. |
| 2 | `EventId` | `string` | **FK** | Mã sự kiện chứa lịch trình này. |
| 3 | `Title` | `string` | | Tên tiết mục / Chủ đề trình bày. |
| 4 | `SpeakerName` | `string?` | | Tên diễn giả / Khách mời phụ trách. |
| 5 | `StartTime` | `DateTime` | | Thời gian bắt đầu tiết mục. |
| 6 | `EndTime` | `DateTime` | | Thời gian kết thúc tiết mục. |
| 7 | `Description` | `string?` | | Nội dung tóm tắt của tiết mục. |

---

### 5.1.7 Lớp `RewardCategory.cs` (Mô hình Danh mục Phần thưởng)

* **Mục đích:** Định nghĩa kho quà tặng, chứng nhận hoặc điểm rèn luyện dành cho sinh viên sau khi hoàn thành tham dự sự kiện.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã phần thưởng. |
| 2 | `TenantId` | `string` | **FK** | Mã tổ chức phát hành quà tặng. |
| 3 | `Name` | `string` | | Tên phần thưởng (VD: Áo thun EMS, Giấy chứng nhận). |
| 4 | `Type` | `RewardType` | | Loại phần thưởng (`TrainingPoint` hoặc `Gift`). |
| 5 | `Description` | `string` | | Điều kiện hoặc mô tả phần quà. |
| 6 | `IsActive` | `bool` | | Trạng thái còn mở đổi quà hay không. |

---

### 5.1.8 Lớp `UserRewardRecord.cs` (Mô hình Nhật ký Đổi quà)

* **Mục đích:** Ghi nhận lịch sử sinh viên thực hiện đổi điểm lấy phần thưởng trong hệ thống.

| STT | Tên thuộc tính | Kiểu dữ liệu | Khóa | Mô tả / Ý nghĩa |
| :-: | :--- | :--- | :-: | :--- |
| 1 | `Id` | `string` | **PK** | Mã bản ghi lượt đổi quà. |
| 2 | `TenantId` | `string` | **FK** | Mã trường/tổ chức quản lý. |
| 3 | `UserId` | `string` | **FK** | Mã sinh viên thực hiện đổi quà. |
| 4 | `RewardCategoryId` | `string` | **FK** | Mã phần thưởng được chọn đổi. |
| 5 | `PointsSpent` | `int` | | Số điểm rèn luyện/tích lũy đã trừ. |
| 6 | `ClaimedAt` | `DateTime` | | Thời điểm thực hiện đổi quà thành công. |
