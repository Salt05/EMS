# Danh sách Kỹ năng và Thư viện Cần thiết

## 1. Kỹ năng cho AI Agent

### 1.1 Coding Skills (Bắt buộc)

| Skill | Level | Lý do |
|-------|-------|-------|
| **ASP.NET Core 6** | Advanced | Xây dựng Web API, middleware, dependency injection |
| **C# 10+** | Advanced | Ngôn ngữ chính, async/await, LINQ |
| **REST API Design** | Advanced | Thiết kế endpoint chuẩn RESTful |
| **Entity Framework Core** | Intermediate | Code-First (dùng cho local test, nhưng production dùng Firestore) |
| **Firebase Admin SDK (.NET)** | Intermediate | Kết nối Firestore, Auth, Storage |
| **JWT Authentication** | Intermediate | Tạo và xác thực token |
| **Blazor WebAssembly** | Intermediate | Dashboard cho Admin/Organizer |
| **MVC Razor** | Intermediate | Student portal |
| **Bootstrap 5** | Intermediate | Giao diện responsive |
| **Hangfire** | Intermediate | Background jobs |

### 1.2 Database Skills (Bắt buộc)

| Skill | Level | Lý do |
|-------|-------|-------|
| **Firestore NoSQL** | Advanced | Thiết kế collection, document, subcollection |
| **Firestore Query** | Advanced | Composite queries, indexing, pagination |
| **Firebase Security Rules** | Intermediate | Bảo mật dữ liệu multi-tenant |
| **Denormalization** | Intermediate | Tối ưu read performance |

### 1.3 Testing Skills (Chỉ liệt kê test case, không code test)

| Skill | Level | Lý do |
|-------|-------|-------|
| **Test Case Design** | Intermediate | Viết test case có cấu trúc |
| **Boundary Value Analysis** | Basic | Xác định test case biên |
| **Equivalence Partitioning** | Basic | Nhóm test case tương đương |

### 1.4 Documentation Skills (Bắt buộc)

| Skill | Level | Lý do |
|-------|-------|-------|
| **Markdown** | Intermediate | Viết tài liệu, README |
| **Swagger / OpenAPI** | Intermediate | Tự động document API |
| **Mermaid** | Basic | Vẽ sơ đồ trong markdown |

### 1.5 Code Intelligence Skills (Nên có)

| Skill | Source | Lý do |
|-------|--------|-------|
| **CodeGraph** | https://github.com/colbymchenry/codegraph | Agent hiểu cấu trúc code, phân tích tác động |
| **Taste-Skill** | https://github.com/leonxlnx/taste-skill | Sinh giao diện đẹp, chất lượng cao |

#### 1.5.1 CodeGraph Setup

```bash
# Installation (macOS/Linux)
curl -fsSL https://raw.githubusercontent.com/colbymchenry/codegraph/main/install.sh | sh

# Initialize in project
cd /path/to/EMS
codegraph init -i

# Commands for Agent
codegraph explore "Find all dependencies of EventService"
codegraph analyze "Impact of changing Registration model"
```

#### 1.5.2 Taste-Skill Setup

```bash
# Install all skills
npx skills add https://github.com/Leonxlnx/taste-skill

# Install specific skill
npx skills add https://github.com/Leonxlnx/taste-skill --skill "design-taste-frontend"

# Use in prompts
"Use design-taste-frontend to create a modern event card layout"
"Use redesign-existing-projects to improve the MyEvents page"
```

---

## 2. Thư viện cho Dự án

### 2.1 Thư viện Bắt buộc (Core)

| Thư viện                                              | Version | Mục đích                      | Project        |
| ------------------------------------------------------- | ------- | -------------------------------- | -------------- |
| **FirebaseAdmin**                                 | 2.4.0   | Firebase Admin SDK               | Infrastructure |
| **Google.Cloud.Firestore**                        | 3.5.0   | Firestore .NET client            | Infrastructure |
| **Hangfire.AspNetCore**                           | 1.8.6   | Background jobs                  | Infrastructure |
| **Hangfire.Firestore**                            | 1.0.0   | Hangfire storage trên Firestore | Infrastructure |
| **Serilog.AspNetCore**                            | 6.1.0   | Logging                          | WebAPI, MVC    |
| **Serilog.Sinks.Console**                         | 4.1.0   | Console sink                     | WebAPI, MVC    |
| **Serilog.Sinks.File**                            | 5.0.0   | File sink                        | WebAPI, MVC    |
| **Swashbuckle.AspNetCore**                        | 6.5.0   | Swagger UI                       | WebAPI         |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 6.0.25  | JWT authentication               | WebAPI         |

### 2.2 Thư viện Bắt buộc (Frontend)

| Thư viện                  | Version                        | Mục đích          | Project         |
| --------------------------- | ------------------------------ | -------------------- | --------------- |
| **Bootstrap**         | 5.3.2                          | CSS framework        | MVC, BlazorWASM |
| **Chart.js**          | 4.4.0                          | Biểu đồ dashboard | BlazorWASM      |
| **Blazor-ApexCharts** | 2.0.0 (hoặc Chart.js wrapper) | Biểu đồ           | BlazorWASM      |

### 2.3 Thư viện Nên có

| Thư viện                     | Version   | Mục đích        | Project        | Lý do                                    |
| ------------------------------ | --------- | ------------------ | -------------- | ----------------------------------------- |
| **FluentValidation**     | 11.8.0    | Input validation   | Core           | Thay thế DataAnnotation, linh hoạt hơn |
| **ClosedXML**            | 0.102.0   | Export Excel       | Infrastructure | Export danh sách đăng ký              |
| **QuestPDF**             | 2023.10.0 | Export PDF         | Infrastructure | Export danh sách check-in                |
| **ICal.NET**             | 4.2.0     | Generate .ics file | Infrastructure | Calendar integration                      |
| **SixLabors.ImageSharp** | 3.1.0     | Resize ảnh upload | Infrastructure | Tối ưu banner sự kiện                 |
| **HtmlSanitizer**        | 8.0.0     | Lọc HTML XSS      | Infrastructure | Bảo mật mô tả sự kiện               |

### 2.4 Thư viện Tùy chọn (Phase 10+)

| Thư viện                    | Mục đích                              | Thời điểm           |
| ----------------------------- | ---------------------------------------- | ---------------------- |
| **StackExchange.Redis** | Cache (giảm read Firestore)             | Phase 10+              |
| **MediatR**             | CQRS pattern                             | Khi codebase quá lớn |
| **AutoMapper**          | Auto mapping Entity ↔ DTO               | Phase 3+               |
| **Polly**               | Retry policy cho external API            | Phase 6                |
| **MailKit**             | SMTP client (nếu không dùng Firebase) | Phase 6                |

---

## 3. Cài đặt Thư viện (CLI Commands)

### 3.1 Core Project

```bash
cd EMS.Core
dotnet add package FluentValidation --version 11.8.0
```

### 3.2 Infrastructure Project

```bash
cd EMS.Infrastructure
dotnet add package FirebaseAdmin --version 2.4.0
dotnet add package Google.Cloud.Firestore --version 3.5.0
dotnet add package Hangfire.AspNetCore --version 1.8.6
dotnet add package Hangfire.Firestore --version 1.0.0
dotnet add package ClosedXML --version 0.102.0
dotnet add package QuestPDF --version 2023.10.0
dotnet add package ICal.NET --version 4.2.0
dotnet add package SixLabors.ImageSharp --version 3.1.0
dotnet add package HtmlSanitizer --version 8.0.0
```

### 3.3 WebAPI Project

```bash
cd EMS.WebAPI
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
dotnet add package Serilog.AspNetCore --version 6.1.0
dotnet add package Serilog.Sinks.Console --version 4.1.0
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 6.0.25
dotnet add package Microsoft.AspNetCore.Mvc.Versioning --version 5.1.0
```

### 3.4 MVC Project

```bash
cd EMS.Mvc
dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation --version 6.0.25
```

### 3.5 BlazorWASM Project

```bash
cd EMS.BlazorWASM
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication --version 6.0.25
dotnet add package Microsoft.Extensions.Http --version 6.0.0
```

### 3.6 Tests Project (nếu developer viết test)

```bash
cd EMS.Tests
dotnet add package coverlet.collector --version 6.0.0
dotnet add package Moq --version 4.20.70
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 6.0.25
```

---

## 4. Cấu hình Ví dụ (appsettings.json)

```json
{
  "Firebase": {
    "ProjectId": "ems-production",
    "CredentialPath": "/secure/firebase-service-account.json"
  },
  "Jwt": {
    "Issuer": "https://ems.com",
    "Audience": "ems-api",
    "ExpiryMinutes": 60
  },
  "Hangfire": {
    "FirestoreDatabase": "hangfire-jobs"
  },
  "Smtp": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "Username": "apikey",
    "Password": "SG.xxxxx",
    "FromEmail": "noreply@ems.com",
    "FromName": "EMS System"
  },
  "RateLimiting": {
    "LoginPerMinute": 5,
    "CheckinPerMinute": 10,
    "RegistrationPerMinute": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## 5. Cấu hình Firebase Emulator (docker-compose.yml)

```yaml
version: '3.8'
services:
  firebase-emulator:
    image: andreysenov/firebase-tools:latest
    ports:
      - "4000:4000"   # UI
      - "8080:8080"   # Firestore
      - "9099:9099"   # Auth
    command: firebase emulators:start --project demo-test
    volumes:
      - ./firebase:/home/node/.firebase
```
