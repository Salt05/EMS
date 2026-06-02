# Cấu trúc Thư mục Dự án EMS

## 1. Tổng quan Solution

```text
EMS/
├── src/
│   ├── EMS.Core/
│   ├── EMS.Infrastructure/
│   ├── EMS.WebAPI/
│   ├── EMS.Mvc/
│   ├── EMS.BlazorWASM/
│   └── EMS.Shared/
├── tests/
│   └── EMS.Tests/
├── docs/
├── scripts/
├── .github/
├── firebase/
└── docker/
```

## 2. Chi tiết từng Project

### 2.1 EMS.Core (Class Library - net6.0)

**Trách nhiệm:** Chứa entities, interfaces, enums, exceptions, validators. Không phụ thuộc vào infrastructure.

```text
EMS.Core/
├── Entities/
│   ├── BaseEntity.cs           # Base class với Id, TenantId, CreatedAt, UpdatedAt
│   ├── User.cs                 # User entity (global)
│   ├── Tenant.cs               # Tenant entity
│   ├── Event.cs                # Event entity
│   ├── Venue.cs                # Venue entity
│   ├── Registration.cs         # Registration entity
│   ├── Waitlist.cs             # Waitlist entity
│   ├── AgendaItem.cs           # Agenda item entity
│   └── EmailTemplate.cs        # Email template entity
├── Enums/
│   ├── EventStatus.cs          # Pending, Approved, Ongoing, Ended, Cancelled
│   ├── RegistrationStatus.cs # Pending, Approved, Rejected, Cancelled
│   └── UserRole.cs             # Student, Organizer, Admin, SuperAdmin
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IEventService.cs
│   ├── IRegistrationService.cs
│   ├── ITenantService.cs
│   └── ...                     # Các interface khác theo domain
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── BusinessRuleException.cs
│   ├── ConcurrencyException.cs
│   └── TenantNotFoundException.cs
└── Validators/                 # FluentValidation (Phase 9+)
    ├── EventValidators.cs
    ├── RegistrationValidators.cs
    └── UserValidators.cs
```

### 2.2 EMS.Infrastructure (Class Library - net6.0)

**Trách nhiệm:** Triển khai repository, service, tích hợp Firebase, Hangfire, export.

```text
EMS.Infrastructure/
├── Repositories/
│   ├── EventRepository.cs
│   ├── RegistrationRepository.cs
│   ├── VenueRepository.cs
│   └── ...
├── Services/
│   ├── AuthService.cs
│   ├── EventService.cs
│   ├── RegistrationService.cs
│   ├── EmailService.cs
│   ├── CheckInService.cs
│   ├── ExportService.cs
│   └── ...
├── Jobs/
│   ├── WaitlistJob.cs
│   ├── EmailJobs.cs
│   ├── ReminderJobs.cs
│   └── EventStatusJob.cs
└── Helpers/
    ├── ClaimsHelper.cs
    ├── CheckInCodeGenerator.cs
    └── HtmlSanitizer.cs
```

### 2.3 EMS.WebAPI (ASP.NET Core 6 Web API)

**Trách nhiệm:** REST API, middleware, cấu hình DI và pipeline.

```text
EMS.WebAPI/
├── Controllers/
│   ├── AuthController.cs
│   ├── EventsController.cs
│   ├── RegistrationsController.cs
│   ├── TenantsController.cs
│   └── ...
├── Middlewares/
│   ├── TenantMiddleware.cs
│   ├── GlobalExceptionMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Extensions/
│   └── HttpContextExtensions.cs
├── secure/
│   └── firebase-service-account.json   # Không commit
├── Program.cs
└── appsettings*.json
```

### 2.4 EMS.Mvc (ASP.NET Core MVC - Student Portal)

**Trách nhiệm:** Giao diện sinh viên (Razor + Bootstrap 5).

```text
EMS.Mvc/
├── Controllers/
│   ├── HomeController.cs
│   ├── AuthController.cs
│   └── EventsController.cs
├── Views/
│   ├── Auth/
│   ├── Events/
│   └── Shared/
├── ViewModels/
└── wwwroot/
    ├── css/
    └── js/
```

### 2.5 EMS.BlazorWASM (Blazor WebAssembly)

**Trách nhiệm:** Dashboard Admin/Organizer, gọi API qua HTTP.

```text
EMS.BlazorWASM/
├── Pages/
│   ├── Admin/
│   ├── Organizer/
│   └── Login.razor
├── Components/
├── Services/
│   ├── CustomAuthStateProvider.cs
│   └── HttpInterceptor.cs
├── Shared/
│   └── TenantSwitcher.razor
└── wwwroot/
```

### 2.6 EMS.Shared (Class Library - net6.0)

**Trách nhiệm:** DTOs dùng chung giữa API, MVC và Blazor.

```text
EMS.Shared/
└── DTOs/
    ├── AuthDTOs.cs
    ├── EventDTOs.cs
    ├── RegistrationDTOs.cs
    ├── TenantDTOs.cs
    └── StatisticsDTOs.cs
```

### 2.7 EMS.Tests (xUnit)

**Trách nhiệm:** Unit test và integration test (do developer viết).

```text
EMS.Tests/
├── Unit/
├── Integration/
└── Fixtures/
```

## 3. Thư mục hỗ trợ

```text
docs/                   # Tài liệu dự án
scripts/                # start-emulator.sh, test-rules.sh, ...
.github/workflows/      # CI/CD pipelines
firebase/               # firestore.rules, firestore.indexes.json, firebase.json
docker/                 # docker-compose.yml, Dockerfiles
```

## 4. Quy ước đặt tên Namespace

```text
EMS.Core.Entities
EMS.Core.Enums
EMS.Core.Interfaces
EMS.Core.Exceptions
EMS.Core.Validators

EMS.Infrastructure.Repositories
EMS.Infrastructure.Services
EMS.Infrastructure.Jobs
EMS.Infrastructure.Helpers

EMS.WebAPI.Controllers
EMS.WebAPI.Middlewares
EMS.WebAPI.Extensions

EMS.Mvc.Controllers
EMS.Mvc.ViewModels

EMS.BlazorWASM.Pages.Admin
EMS.BlazorWASM.Pages.Organizer
EMS.BlazorWASM.Components
EMS.BlazorWASM.Services

EMS.Shared.DTOs
```

## 5. Luồng tham chiếu giữa các Project

```text
EMS.Shared (không tham chiếu gì)
    ↑
EMS.Core (tham chiếu EMS.Shared)
    ↑
EMS.Infrastructure (tham chiếu EMS.Core)
    ↑
EMS.WebAPI (tham chiếu EMS.Infrastructure, EMS.Shared)
EMS.Mvc (tham chiếu EMS.Infrastructure, EMS.Shared)
EMS.BlazorWASM (tham chiếu EMS.Shared, gọi API qua HTTP)
    ↑
EMS.Tests (tham chiếu tất cả)
```

**Lưu ý:** Không có tham chiếu vòng tròn. BlazorWASM không tham chiếu Infrastructure trực tiếp.

## 6. File cấu hình chính

### 6.1 Root directory

```text
EMS/
├── EMS.sln
├── .gitignore
├── .editorconfig
├── README.md
├── LICENSE
├── global.json (nếu cần)
└── NuGet.config
```

### 6.2 .gitignore mẫu (bổ sung Firebase)

```gitignore
# Build output
**/bin/
**/obj/

# User files
*.user
*.suo
.vs/

# Firebase credentials
**/secure/*.json
**/firebase-service-account.json
!.firebase/service-account.json

# Environment
.env
.env.local
appsettings.Development.local.json

# Logs
**/logs/
*.log

# OS
.DS_Store
Thumbs.db
```

---
