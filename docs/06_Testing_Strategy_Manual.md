# Chiến lược Kiểm thử và Danh sách Test Case

## 1. Nguyên tắc kiểm thử

### 1.1 Trách nhiệm
| Vai trò | Trách nhiệm |
|---------|--------------|
| **AI Agent** | Liệt kê danh sách test case (không viết code test) |
| **Developer** | Viết unit test và integration test |
| **QA** (nếu có) | Thực hiện kiểm thử thủ công và tự động |

### 1.2 Các loại kiểm thử

| Loại | Người thực hiện | Tần suất | Mục tiêu |
|------|-----------------|----------|----------|
| Unit Test | Developer | Mỗi pull request | Kiểm tra logic từng method |
| Integration Test | Developer | Mỗi pull request | Kiểm tra tương tác giữa các service |
| API Test | Developer / QA | Hàng ngày | Kiểm tra endpoint hoạt động đúng |
| UI Test (Manual) | QA | Mỗi release | Kiểm tra giao diện và luồng người dùng |
| Load Test | Developer | Trước release | Đảm bảo hiệu năng |

### 1.3 Môi trường kiểm thử

| Môi trường | Sử dụng cho | Kết nối |
|------------|--------------|---------|
| **Local** | Development, unit test | Firebase Emulator |
| **Dev** | Integration test, API test | Firebase Emulator + Test data |
| **Staging** | UAT, demo | Firebase Project riêng (staging) |
| **Production** | Live | Firebase Production |

---

## 2. Danh sách Test Case Mẫu (Theo Feature)

### 2.1 Authentication (Phase 1)

#### TC-AUTH-001: Đăng ký sinh viên thành công
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Firebase Emulator đang chạy, chưa có user với MSSV=202312345 |
| **Steps** | POST /api/auth/register `{ "mssv": "202312345", "fullName": "Nguyen Van A", "email": "a.nguyen@university.edu", "password": "P@ssw0rd", "phoneNumber": "0912345678" }` |
| **Expected** | 201 Created, user được tạo trong Firebase Auth, Firestore users collection có document |

#### TC-AUTH-002: Đăng ký thất bại (MSSV đã tồn tại)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Đã có user MSSV=202312345 |
| **Steps** | POST /api/auth/register với MSSV=202312345 |
| **Expected** | 400 Bad Request, message "MSSV already exists" |

#### TC-AUTH-003: Đăng nhập thành công
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | User đã đăng ký |
| **Steps** | POST /api/auth/login `{ "mssv": "202312345", "password": "P@ssw0rd" }` |
| **Expected** | 200 OK, trả về JWT token, claims chứa tenantId và role |

#### TC-AUTH-004: Đăng nhập thất bại (sai mật khẩu)
| Trường | Giá trị |
|--------|---------|
| **Steps** | POST /api/auth/login với password sai |
| **Expected** | 401 Unauthorized |

#### TC-AUTH-005: Đăng nhập thất bại (tenant không tồn tại)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Gọi API với subdomain không tồn tại (invalid.ems.com) |
| **Expected** | 404 Not Found, message "Tenant not found" |

---

### 2.2 Event Management (Phase 2)

#### TC-EVENT-001: Tạo sự kiện thành công (Organizer)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | User role Organizer đã đăng nhập |
| **Steps** | POST /api/events `{ "title": "Hội thảo CNTT", "description": "...", "venueId": "venue123", "startTime": "2026-07-01T09:00:00Z", "endTime": "2026-07-01T17:00:00Z", "maxCapacity": 100, "requireApproval": false }` |
| **Expected** | 201 Created, event status = Pending |

#### TC-EVENT-002: Danh sách sự kiện (sinh viên xem)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Có ít nhất 3 sự kiện với status Approved |
| **Steps** | GET /api/events?status=Approved |
| **Expected** | 200 OK, trả về mảng events, chỉ bao gồm Approved events |

#### TC-EVENT-003: Lọc sự kiện theo venue
| Trường | Giá trị |
|--------|---------|
| **Steps** | GET /api/events?venueId=venue123 |
| **Expected** | Chỉ trả về events có venueId = venue123 |

#### TC-EVENT-004: Phê duyệt sự kiện (Admin)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Admin đăng nhập, event có status=Pending |
| **Steps** | POST /api/events/{publicId}/approve |
| **Expected** | 200 OK, event.status = Approved, sinh viên có thể thấy |

#### TC-EVENT-005: Từ chối sự kiện (Admin)
| Trường | Giá trị |
|--------|---------|
| **Steps** | POST /api/events/{publicId}/reject |
| **Expected** | 200 OK, event.status = Rejected |

#### TC-EVENT-006: Cập nhật sự kiện (không phải chủ sở hữu)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | User A là organizer của event, User B là organizer khác |
| **Steps** | User B gọi PUT /api/events/{eventOfA} |
| **Expected** | 403 Forbidden |

---

### 2.3 Registration (Phase 3)

#### TC-REG-001: Đăng ký thành công (còn chỗ, không cần duyệt)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event E01 capacity=10, currentRegistrations=5, requireApproval=false |
| **Steps** | POST /api/registrations `{ "eventId": "E01" }` |
| **Expected** | 201 Created, registration.status = Approved, currentRegistrations tăng lên 6 |

#### TC-REG-002: Đăng ký thành công (cần duyệt)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event E02 requireApproval=true |
| **Steps** | POST /api/registrations |
| **Expected** | 201 Created, registration.status = Pending |

#### TC-REG-003: Đăng ký thất bại (hết chỗ)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event E03 capacity=10, currentRegistrations=10 |
| **Steps** | POST /api/registrations |
| **Expected** | 400 Bad Request, message "Event is full" |

#### TC-REG-004: Đăng ký thất bại (vào waitlist)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event E04 capacity=10, currentRegistrations=10, allowWaitlist=true |
| **Steps** | POST /api/registrations |
| **Expected** | 202 Accepted, user được thêm vào waitlist collection |

#### TC-REG-005: Hủy đăng ký thành công (giải phóng chỗ)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Student đã đăng ký event E05, event chưa bắt đầu |
| **Steps** | DELETE /api/registrations/{eventId} |
| **Expected** | 200 OK, registration.status = Cancelled, currentRegistrations giảm 1 |

#### TC-REG-006: Hủy đăng ký thất bại (sự kiện đã bắt đầu)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event đã có startTime < now |
| **Expected** | 400 Bad Request, message "Cannot cancel past event" |

#### TC-REG-007: Waitlist tự động promotion khi có hủy
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event full (10/10), waitlist có 2 người (W1, W2) |
| **Steps** | Student đã đăng ký hủy |
| **Expected** | Người đầu waitlist (W1) được promote, registration.status=Approved, email được gửi |

#### TC-REG-008: Giới hạn đăng ký cá nhân
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Tenant setting maxRegistrationsPerStudent=5, student đã đăng ký 5 events |
| **Steps** | Đăng ký event thứ 6 |
| **Expected** | 400 Bad Request, message "Exceeded registration limit" |

---

### 2.4 Check-in (Phase 4)

#### TC-CHECK-001: Tạo mã check-in (Organizer)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event đã Approved, sắp diễn ra |
| **Steps** | POST /api/events/{publicId}/generate-code |
| **Expected** | 200 OK, trả về code 6 ký tự, event.checkInCodeExpiry = now + 30 phút |

#### TC-CHECK-002: Check-in thành công
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Student đã đăng ký, event đang Ongoing, có checkInCode hợp lệ |
| **Steps** | POST /api/events/{publicId}/checkin `{ "code": "ABC123" }` |
| **Expected** | 200 OK, registration.isAttended = true, checkInTime = now |

#### TC-CHECK-003: Check-in thất bại (mã sai)
| Trường | Giá trị |
|--------|---------|
| **Steps** | POST checkin với mã sai |
| **Expected** | 400 Bad Request, message "Invalid check-in code" |

#### TC-CHECK-004: Check-in thất bại (mã hết hạn)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Code đã được tạo hơn 30 phút trước |
| **Expected** | 400 Bad Request, message "Check-in code expired" |

#### TC-CHECK-005: Check-in thất bại (chưa đăng ký)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Student chưa đăng ký event |
| **Expected** | 403 Forbidden, message "Not registered for this event" |

#### TC-CHECK-006: Check-in thất bại (đã check-in trước đó)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Student đã check-in trước đó |
| **Expected** | 409 Conflict, message "Already checked in" |

---

### 2.5 Email Notification (Phase 6)

#### TC-EMAIL-001: Gửi email xác nhận đăng ký
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Hangfire đang chạy, SMTP configured |
| **Steps** | Đăng ký thành công |
| **Expected** | Email được enqueue vào Hangfire, trong vòng 5 giây email được gửi đến inbox student |

#### TC-EMAIL-002: Gửi email nhắc nhở 1 ngày trước
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Sự kiện bắt đầu vào ngày mai lúc 9:00, có student đã đăng ký |
| **Steps** | Chờ đến 8:00 sáng (recurring job chạy) |
| **Expected** | Email reminder được gửi |

#### TC-EMAIL-003: Gửi email khi được promote từ waitlist
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Student ở vị trí đầu waitlist |
| **Steps** | Có người hủy đăng ký → waitlist job chạy |
| **Expected** | Email "You have been promoted" được gửi |

#### TC-EMAIL-004: Template tùy chỉnh theo tenant
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Tenant A đã thay đổi template registration_confirmation |
| **Steps** | Student thuộc tenant A đăng ký |
| **Expected** | Email gửi đi sử dụng template đã tùy chỉnh (logo, màu sắc) |

---

### 2.6 Export (Phase 8)

#### TC-EXPORT-001: Export Excel danh sách đăng ký
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event có 50 registrations |
| **Steps** | GET /api/events/{publicId}/export/excel |
| **Expected** | 200 OK, Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, file tải về mở được, có đủ cột (STT, MSSV, Họ tên, Email, Thời gian đăng ký, Trạng thái) |

#### TC-EXPORT-002: Export PDF danh sách check-in
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Event có 30 students đã check-in |
| **Steps** | GET /api/events/{publicId}/export/pdf |
| **Expected** | 200 OK, Content-Type: application/pdf, file PDF có header với tên event, danh sách student, footer có ngày xuất |

---

### 2.7 Multi-tenant (Phase 1 & 9)

#### TC-TENANT-001: Tenant isolation - không thể truy cập dữ liệu tenant khác
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Tenant A và B tồn tại, mỗi tenant có user riêng |
| **Steps** | User A đăng nhập vào tenant A, gọi API /api/events (sẽ chỉ lấy events của tenant A) |
| **Expected** | Không có event của tenant B xuất hiện |

#### TC-TENANT-002: Tenant isolation - Firestore Security Rules
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | Tenant A user cố gắng truy cập document có path `tenant_B/events/xxx` |
| **Steps** | Gọi trực tiếp Firestore (nếu có thể) hoặc API |
| **Expected** | Bị từ chối (permission denied) |

#### TC-TENANT-003: Tạo tenant mới (SuperAdmin)
| Trường | Giá trị |
|--------|---------|
| **Preconditions** | SuperAdmin đăng nhập |
| **Steps** | POST /api/tenants `{ "name": "Đại học HUFLIT", "subdomain": "huflit", "adminEmail": "admin@huflit.edu", "adminPassword": "Temp@123" }` |
| **Expected** | 201 Created, tenant document được tạo, admin user được tạo, tenant settings mặc định được seed |

---

## 3. Template Test Case cho Developer

```markdown
### TC-[MODULE]-[XXX]: [Title]

| Trường | Giá trị |
|--------|---------|
| **ID** | TC-XXX-001 |
| **Title** | Đăng ký thành công khi còn chỗ |
| **Priority** | High / Medium / Low |
| **Type** | Functional / Security / Performance |
| **Preconditions** | Event capacity=10, current=5 |
| **Test Data** | EventId: evt_001, StudentId: stu_001 |
| **Steps** | 1. POST /api/registrations `{ "eventId": "evt_001" }` |
| **Expected Result** | 201 Created, registration status=Approved |
| **Actual Result** | (Developer điền sau khi test) |
| **Status** | Pass / Fail |
| **Executed By** | (Tên người test) |
| **Date** | YYYY-MM-DD |
```

---

## 4. Quy trình kiểm thử cho mỗi Pull Request

```mermaid
flowchart LR
    PR[Pull Request] --> Build[Build]
    Build --> Unit[Unit Tests]
    Unit --> Integration[Integration Tests]
    Integration --> API[API Tests (Manual)]
    API --> Review[Code Review]
    Review --> Merge[Merge]
```

**Developer phải chạy:**

1. Unit tests (nếu có)
2. Integration tests (nếu có)
3. Chạy thủ công các test case liên quan đến thay đổi

**QA sẽ chạy (trước release):**

1. Toàn bộ test case cho feature mới
2. Regression test (các test case cũ)

---

## 5. Công cụ hỗ trợ kiểm thử (cho Developer)

| Công cụ                                        | Mục đích                       |
| ------------------------------------------------ | --------------------------------- |
| **Postman / Bruno**                        | Kiểm thử API thủ công         |
| **Firebase Emulator Suite**                | Test với dữ liệu ảo           |
| **xUnit** (nếu developer viết unit test) | Unit test                         |
| **Moq**                                    | Mock dependency                   |
| **WebApplicationFactory**                  | Integration test cho ASP.NET Core |
