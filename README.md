
# EMS - Event Management System

[![.NET](https://img.shields.io/badge/.NET-6.0-purple)](https://dotnet.microsoft.com/)
[![Firebase](https://img.shields.io/badge/Firebase-Multi--Tenant-orange)](https://firebase.google.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WASM-blue)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

## 🎯 Giới thiệu

EMS (Event Management System) là một hệ thống quản lý sự kiện đa nền tảng (multi-tenant)** mã nguồn mở, được thiết kế để phục vụ cho nhiều trường đại học, tổ chức và doanh nghiệp. Mỗi tổ chức (tenant) có không gian dữ liệu riêng biệt, có thể tùy chỉnh giao diện và cấu hình theo nhu cầu.

Hệ thống thay thế hoàn toàn phương pháp ghi danh và điểm danh thủ công, giúp sinh viên dễ dàng đăng ký, điểm danh và theo dõi sự kiện; hỗ trợ ban tổ chức quản lý người tham gia, xuất báo cáo; và cung cấp cho quản trị viên công cụ giám sát toàn bộ hoạt động.

## ✨ Tính năng nổi bật

### 🔐 Multi-tenant & Bảo mật

- Mỗi tổ chức có subdomain riêng (`tenanto.ems.com`)
- Dữ liệu cách ly hoàn toàn giữa các tenant
- Tùy chỉnh giao diện (màu sắc, logo) theo tenant
- Xác thực qua Firebase Auth + JWT
- Phân quyền chi tiết (Student, Organizer, Admin, SuperAdmin)

### 📅 Quản lý sự kiện

- Tạo, sửa, xóa sự kiện với quy trình phê duyệt (Admin)
- Đăng ký 1-click + cơ chế xét duyệt (tùy chọn theo sự kiện)
- Hủy đăng ký trước giờ bắt đầu
- Waitlist tự động khi hết chỗ, promotion khi có người hủy
- Giới hạn số lượng đăng ký cá nhân (cấu hình theo tenant)

### 🎟️ Điểm danh & Check-in

- Sinh mã check-in ngẫu nhiên 6 ký tự, hết hạn sau thời gian cấu hình
- Sinh viên nhập mã trực tiếp trên web (tối ưu mobile)
- Danh sách check-in real-time cập nhật trên dashboard

### 📧 Thông báo

- Email xác nhận đăng ký
- Email thông báo được duyệt từ waitlist
- Email nhắc nhở 1 ngày và 1 giờ trước sự kiện (Hangfire)
- Tenant có thể tùy chỉnh email template

### 📊 Dashboard & Báo cáo

- Admin dashboard: thống kê toàn tenant
- Organizer dashboard: tỷ lệ check-in/đăng ký, waitlist stats
- Xuất danh sách đăng ký (Excel) và danh sách điểm danh (PDF)
- Biểu đồ trực quan (Chart.js)

### 📍 Các tính năng khác

- Quản lý địa điểm (Venues) với sức chứa, bản đồ
- Lịch trình (Agenda) chi tiết cho từng sự kiện
- Xuất file .ics (Google Calendar, Outlook)
- Upload ảnh banner lên Firebase Storage
- Giao diện responsive, hỗ trợ dark mode

## 🚀 Công nghệ sử dụng

| Thành phần                        | Công nghệ                         |
| ----------------------------------- | ----------------------------------- |
| **Backend API**               | ASP.NET Core 6 Web API              |
| **Database**                  | Firebase Firestore (NoSQL)          |
| **Authentication**            | Firebase Auth + JWT                 |
| **Storage**                   | Firebase Storage (ảnh, tài liệu) |
| **Background Jobs**           | Hangfire                            |
| **Student Portal**            | MVC Razor + Bootstrap 5             |
| **Admin/Organizer Dashboard** | Blazor WebAssembly                  |
| **Logging**                   | Serilog                             |
| **Export Excel**              | ClosedXML                           |
| **Export PDF**                | QuestPDF                            |
| **Calendar .ics**             | ICal.NET                            |

## 📁 Cấu trúc dự án

```
EMS/
├── src/
│   ├── EMS.Core/           # Entities, Interfaces, Enums
│   ├── EMS.Infrastructure/ # Repositories, Services, Firebase, Hangfire
│   ├── EMS.WebAPI/         # REST API Controllers, Middlewares
│   ├── EMS.Mvc/            # Student portal (Razor)
│   ├── EMS.BlazorWASM/     # Admin/Organizer dashboard
│   └── EMS.Shared/         # DTOs dùng chung
├── tests/
│   └── EMS.Tests/          # Unit & Integration tests
├── docs/                   # Tài liệu dự án
├── scripts/                # Scripts hỗ trợ
├── .github/workflows/      # CI/CD
├── firebase/               # Firestore rules, indexes
└── docker/                 # Docker compose files
```

## 🔧 Cài đặt và chạy local

### Yêu cầu

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node.js](https://nodejs.org/) (để chạy Firebase Emulator)
- [Firebase CLI](https://firebase.google.com/docs/cli)
- [Git](https://git-scm.com/)

### Các bước

1. **Clone repository**

```bash
git clone https://github.com/Salto5/EMS.git
cd EMS
```

2. **Cài đặt Firebase Emulator**

```bash
npm install -g firebase-tools
firebase login
firebase init emulators
# Chọn Firestore, Auth, Storage, Emulator UI
```

3. **Cấu hình Firebase Service Account**

- Tạo service account key từ Firebase Console
- Đặt file JSON tại `EMS.WebAPI/secure/firebase-service-account.json`
- (Không commit file này lên Git)

4. **Khôi phục packages**

```bash
dotnet restore
```

5. **Chạy Firebase Emulator**

```bash
firebase emulators:start --project demo-test
```

6. **Chạy ứng dụng**

```bash
# Terminal 1: WebAPI
cd EMS.WebAPI
dotnet run

# Terminal 2: MVC (Student portal)
cd EMS.Mvc
dotnet run

# Terminal 3: Blazor WASM (Dashboard)
cd EMS.BlazorWASM
dotnet run
```

7. **Truy cập**

- Student portal: `http://localhost:5000`
- Dashboard: `http://localhost:6000`
- API Swagger: `http://localhost:5001/swagger`
- Hangfire Dashboard: `http://localhost:5001/hangfire`

## 📖 Tài liệu

| Tài liệu                                                        | Mô tả                                        |
| ----------------------------------------------------------------- | ---------------------------------------------- |
| [System Architecture](docs/01_System_Architecture_Overview.md)       | Tổng quan kiến trúc, multi-tenant decisions |
| [Development Roadmap](docs/02_Development_Roadmap_Phases.md)         | Lộ trình 10 phase chi tiết                  |
| [Task Breakdown](docs/03_Task_Breakdown_For_Agents.md)               | 214 task cho AI Agent                          |
| [Agent Workflow](docs/04_Agent_Workflow_Rules.md)                    | Quy trình làm việc của Agent               |
| [Skills &amp; Libraries](docs/05_Required_Skills_Libraries.md)       | Danh sách thư viện, kỹ năng               |
| [Testing Strategy](docs/06_Testing_Strategy_Manual.md)               | Test cases mẫu                                |
| [Firebase Guide](docs/07_Firebase_Integration_Guide.md)              | Tích hợp Firebase                            |
| [CodeGraph &amp; Taste-Skill](docs/08_CodeGraph_TasteSkill_Setup.md) | Công cụ AI Agent                             |
| [Source Code Structure](docs/09_Source_Code_Structure.md)            | Cấu trúc thư mục                           |

## 🤝 Đóng góp

Chúng tôi hoan nghênh mọi đóng góp! Vui lòng:

1. Fork repository
2. Tạo branch mới (`git checkout -b feature/amazing-feature`)
3. Commit thay đổi (`git commit -m 'feat: add amazing feature'`)
4. Push lên branch (`git push origin feature/amazing-feature`)
5. Mở Pull Request

Xem [CONTRIBUTING.md](CONTRIBUTING.md) để biết thêm chi tiết.

## 📄 Giấy phép

Dự án được phân phối dưới giấy phép MIT. Xem file [LICENSE](LICENSE) để biết thêm thông tin.

## 📧 Liên hệ

Dự án được phát triển bởi [Salt05](https://github.com/Salt05).
Mọi câu hỏi, góp ý xin gửi về email: `pminhphathi@gmail.com`

---

**⭐ Đừng quên star repository nếu bạn thấy dự án hữu ích!**
