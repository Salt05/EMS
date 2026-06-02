# Phân rã Nhiệm vụ cho AI Agent

## Quy ước đặt mã Task

```text
{Phase}_{Epic}_{Feature}_{Task}
Ví dụ: P1_E1_F1_T1 = Phase 1, Epic 1, Feature 1, Task 1
```

---

## Phase 0: Environment & Infrastructure Setup

### Epic E0.1: Solution Structure

| Mã Task | Tên Task | Mô tả | Output |
|---------|----------|-------|--------|
| P0_E1_F1_T1 | Tạo Solution | `dotnet new sln -n EMS` | EMS.sln |
| P0_E1_F1_T2 | Tạo Core Project | `dotnet new classlib -n EMS.Core -f net6.0` | EMS.Core.csproj |
| P0_E1_F1_T3 | Tạo Infrastructure Project | `dotnet new classlib -n EMS.Infrastructure -f net6.0` | EMS.Infrastructure.csproj |
| P0_E1_F1_T4 | Tạo WebAPI Project | `dotnet new webapi -n EMS.WebAPI -f net6.0` | EMS.WebAPI.csproj |
| P0_E1_F1_T5 | Tạo MVC Project | `dotnet new mvc -n EMS.Mvc -f net6.0` | EMS.Mvc.csproj |
| P0_E1_F1_T6 | Tạo BlazorWASM Project | `dotnet new blazorwasm -n EMS.BlazorWASM -f net6.0` | EMS.BlazorWASM.csproj |
| P0_E1_F1_T7 | Tạo Shared Project | `dotnet new classlib -n EMS.Shared -f net6.0` | EMS.Shared.csproj |
| P0_E1_F1_T8 | Tạo Test Project | `dotnet new xunit -n EMS.Tests -f net6.0` | EMS.Tests.csproj |
| P0_E1_F1_T9 | Thêm Project References | Tham chiếu đúng thứ tự Core→Infra→WebAPI | Reference chain hoàn chỉnh |

### Epic E0.2: NuGet Packages

| Mã Task | Tên Task | Packages | Project |
|---------|----------|----------|---------|
| P0_E2_F1_T1 | Core Packages | None (netstandard) | Core |
| P0_E2_F1_T2 | Infrastructure Packages | FirebaseAdmin, Google.Cloud.Firestore, Hangfire.AspNetCore, Hangfire.Firestore | Infrastructure |
| P0_E2_F1_T3 | WebAPI Packages | Swashbuckle.AspNetCore, Serilog.AspNetCore, Serilog.Sinks.Console, Microsoft.AspNetCore.Authentication.JwtBearer | WebAPI |
| P0_E2_F1_T4 | MVC Packages | Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation | MVC |
| P0_E2_F1_T5 | BlazorWASM Packages | Microsoft.AspNetCore.Components.WebAssembly.Authentication | BlazorWASM |
| P0_E2_F1_T6 | Test Packages | coverlet.collector, Moq, Microsoft.AspNetCore.Mvc.Testing | Tests |

### Epic E0.3: Configuration & Logging

| Mã Task | Tên Task | Mô tả | File |
|---------|----------|-------|------|
| P0_E3_F1_T1 | Cấu hình Serilog | Console + File sink | Program.cs |
| P0_E3_F1_T2 | Cấu hình appsettings | Dev, Staging, Production | appsettings*.json |
| P0_E3_F1_T3 | Cấu hình Swagger | JWT Bearer support | Program.cs |
| P0_E3_F1_T4 | Health Checks | `/health`, `/health/ready`, `/health/live` | Program.cs |
| P0_E3_F1_T5 | CORS Policy | Cho phép Blazor và MVC | Program.cs |

### Epic E0.4: Docker & CI/CD

| Mã Task | Tên Task | Mô tả | File |
|---------|----------|-------|------|
| P0_E4_F1_T1 | Dockerfile API | Multi-stage build | EMS.WebAPI/Dockerfile |
| P0_E4_F1_T2 | Dockerfile MVC | Multi-stage build | EMS.Mvc/Dockerfile |
| P0_E4_F1_T3 | Dockerfile Blazor | Nginx serving static files | EMS.BlazorWASM/Dockerfile |
| P0_E4_F1_T4 | Docker Compose | Firebase Emulator, Hangfire | docker-compose.yml |
| P0_E4_F1_T5 | GitHub Actions CI | Build + Test trên push | .github/workflows/ci.yml |
| P0_E4_F1_T6 | Firebase Emulator Script | Script khởi động emulator | scripts/start-emulator.sh |

---

## Phase 1: Multi-tenant Core & Authentication

### Epic E1.1: Firebase Setup

| Mã Task    | Tên Task                     | Mô tả                     | Output               |
| ----------- | ----------------------------- | --------------------------- | -------------------- |
| P1_E1_F1_T1 | Tạo Firebase Project         | Trên Firebase Console      | GCP Project ID       |
| P1_E1_F1_T2 | Enable Authentication         | Email/Password provider     | Firebase Auth        |
| P1_E1_F1_T3 | Enable Firestore              | Native mode                 | Firestore DB         |
| P1_E1_F1_T4 | Tạo Service Account          | Download JSON key           | service-account.json |
| P1_E1_F1_T5 | Cấu hình Firebase Admin SDK | Khởi tạo trong Program.cs | FirebaseApp          |

### Epic E1.2: Tenant Resolution

| Mã Task    | Tên Task                         | Mô tả                        | File                                       |
| ----------- | --------------------------------- | ------------------------------ | ------------------------------------------ |
| P1_E2_F1_T1 | Tạo Tenant Entity                | Tenant model cho Firestore     | Core/Entities/Tenant.cs                    |
| P1_E2_F1_T2 | Tạo ITenantService Interface     | GetTenant, ValidateTenant      | Core/Interfaces/ITenantService.cs          |
| P1_E2_F1_T3 | Implement TenantService           | Lấy tenant từ Firestore      | Infrastructure/Services/TenantService.cs   |
| P1_E2_F1_T4 | Tạo TenantMiddleware             | Đọc subdomain, attach tenant | WebAPI/Middlewares/TenantMiddleware.cs     |
| P1_E2_F1_T5 | Register Middleware               | Thêm vào pipeline            | Program.cs                                 |
| P1_E2_F1_T6 | Tạo Tenant HttpContext Extension | Lấy tenant từ context        | WebAPI/Extensions/HttpContextExtensions.cs |

### Epic E1.3: Firebase Authentication Integration

| Mã Task    | Tên Task                   | Mô tả                                     | File                                   |
| ----------- | --------------------------- | ------------------------------------------- | -------------------------------------- |
| P1_E3_F1_T1 | Tạo Auth DTOs              | RegisterRequest, LoginRequest, AuthResponse | Shared/DTOs/AuthDTOs.cs                |
| P1_E3_F1_T2 | Tạo IAuthService Interface | Register, Login, Logout                     | Core/Interfaces/IAuthService.cs        |
| P1_E3_F1_T3 | Implement AuthService       | Gọi Firebase Auth REST API                 | Infrastructure/Services/AuthService.cs |
| P1_E3_F1_T4 | Tạo Custom Claims Helper   | Gán tenantId, role, mssv                   | Infrastructure/Helpers/ClaimsHelper.cs |
| P1_E3_F1_T5 | Tạo JWT Generator          | Tạo token từ Firebase UID                 | Infrastructure/Services/JwtService.cs  |
| P1_E3_F1_T6 | Tạo Auth Controller        | Register, Login, Logout endpoints           | WebAPI/Controllers/AuthController.cs   |
| P1_E3_F1_T7 | Cấu hình JWT Bearer       | Validation parameters                       | Program.cs                             |

### Epic E1.4: User Management

| Mã Task    | Tên Task                 | Mô tả                  | File                                       |
| ----------- | ------------------------- | ------------------------ | ------------------------------------------ |
| P1_E4_F1_T1 | Tạo User Entity          | User model cho Firestore | Core/Entities/User.cs                      |
| P1_E4_F1_T2 | Tạo IUserService         | GetById, Update, Delete  | Core/Interfaces/IUserService.cs            |
| P1_E4_F1_T3 | Implement UserService     | CRUD users Firestore     | Infrastructure/Services/UserService.cs     |
| P1_E4_F1_T4 | Tạo Users Controller     | GET, PUT, DELETE user    | WebAPI/Controllers/UsersController.cs      |
| P1_E4_F1_T5 | Tạo Forgot Password Flow | Reset email via Hangfire | Infrastructure/Services/PasswordService.cs |

### Epic E1.5: Tenant Onboarding

| Mã Task    | Tên Task                         | Mô tả                                   | File                                               |
| ----------- | --------------------------------- | ----------------------------------------- | -------------------------------------------------- |
| P1_E5_F1_T1 | Tạo Tenant Onboarding DTO        | CreateTenantRequest                       | Shared/DTOs/TenantDTOs.cs                          |
| P1_E5_F1_T2 | Tạo ITenantOnboardingService     | CreateTenant, SetupDefaults               | Core/Interfaces/ITenantOnboardingService.cs        |
| P1_E5_F1_T3 | Implement TenantOnboardingService | Tạo tenant, admin user, default settings | Infrastructure/Services/TenantOnboardingService.cs |
| P1_E5_F1_T4 | Tạo Tenants Controller           | POST /api/tenants (SuperAdmin)            | WebAPI/Controllers/TenantsController.cs            |
| P1_E5_F1_T5 | Tạo Tenant Settings CRUD         | GET/PUT settings                          | WebAPI/Controllers/TenantSettingsController.cs     |

### Epic E1.6: MVC Authentication UI

| Mã Task    | Tên Task                   | Mô tả                              | File                                 |
| ----------- | --------------------------- | ------------------------------------ | ------------------------------------ |
| P1_E6_F1_T1 | Tạo Login Page             | Form đăng nhập với MSSV/password | MVC/Views/Auth/Login.cshtml          |
| P1_E6_F1_T2 | Tạo Register Page          | Form đăng ký sinh viên           | MVC/Views/Auth/Register.cshtml       |
| P1_E6_F1_T3 | Tạo Forgot Password Page   | Form gửi email reset                | MVC/Views/Auth/ForgotPassword.cshtml |
| P1_E6_F1_T4 | Tạo Reset Password Page    | Form nhập mật khẩu mới           | MVC/Views/Auth/ResetPassword.cshtml  |
| P1_E6_F1_T5 | Tạo Auth Controller        | Login, Register actions              | MVC/Controllers/AuthController.cs    |
| P1_E6_F1_T6 | Tạo Tenant Branding Layout | Dynamic CSS từ tenant settings      | MVC/Views/Shared/_Layout.cshtml      |

### Epic E1.7: Blazor WASM Authentication

| Mã Task    | Tên Task                         | Mô tả                       | File                                           |
| ----------- | --------------------------------- | ----------------------------- | ---------------------------------------------- |
| P1_E7_F1_T1 | Tạo CustomAuthStateProvider      | Lưu token, check auth        | BlazorWASM/Services/CustomAuthStateProvider.cs |
| P1_E7_F1_T2 | Tạo Login Component              | Form login gọi API           | BlazorWASM/Pages/Login.razor                   |
| P1_E7_F1_T3 | Tạo Logout Component             | Clear token, redirect         | BlazorWASM/Pages/Logout.razor                  |
| P1_E7_F1_T4 | Cấu hình AuthorizeView          | Protected routes              | BlazorWASM/App.razor                           |
| P1_E7_F1_T5 | Tạo Tenant Switcher (SuperAdmin) | Dropdown chuyển tenant       | BlazorWASM/Shared/TenantSwitcher.razor         |
| P1_E7_F1_T6 | Tạo Http Interceptor             | Attach JWT token vào request | BlazorWASM/Services/HttpInterceptor.cs         |

---

## Phase 2: Event Management

### Epic E2.1: Core Entities

| Mã Task    | Tên Task             | Mô tả                                          | File                      |
| ----------- | --------------------- | ------------------------------------------------ | ------------------------- |
| P2_E1_F1_T1 | Tạo EventStatus Enum | Pending, Approved, Ongoing, Ended, Cancelled     | Core/Enums/EventStatus.cs |
| P2_E1_F1_T2 | Tạo Event Entity     | Event model cho Firestore                        | Core/Entities/Event.cs    |
| P2_E1_F1_T3 | Tạo Venue Entity     | Venue model                                      | Core/Entities/Venue.cs    |
| P2_E1_F1_T4 | Tạo Event DTOs       | CreateEventDto, UpdateEventDto, EventResponseDto | Shared/DTOs/EventDTOs.cs  |

### Epic E2.2: Event Repository & Service

| Mã Task    | Tên Task                       | Mô tả                         | File                                           |
| ----------- | ------------------------------- | ------------------------------- | ---------------------------------------------- |
| P2_E2_F1_T1 | Tạo IEventRepository Interface | CRUD operations                 | Core/Interfaces/IEventRepository.cs            |
| P2_E2_F1_T2 | Implement EventRepository       | Firestore queries               | Infrastructure/Repositories/EventRepository.cs |
| P2_E2_F1_T3 | Tạo IEventService Interface    | Business logic                  | Core/Interfaces/IEventService.cs               |
| P2_E2_F1_T4 | Implement EventService          | Create, update, delete, approve | Infrastructure/Services/EventService.cs        |
| P2_E2_F1_T5 | Tạo Event Status Update Job    | Auto update Ongoing/Ended       | Infrastructure/Jobs/EventStatusJob.cs          |

### Epic E2.3: Event API Endpoints

| Mã Task    | Tên Task          | Endpoint                            | File                                   |
| ----------- | ------------------ | ----------------------------------- | -------------------------------------- |
| P2_E3_F1_T1 | Create Event       | POST /api/events                    | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T2 | Get Events List    | GET /api/events                     | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T3 | Get Event Detail   | GET /api/events/{publicId}          | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T4 | Update Event       | PUT /api/events/{publicId}          | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T5 | Delete Event       | DELETE /api/events/{publicId}       | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T6 | Approve Event      | POST /api/events/{publicId}/approve | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T7 | Reject Event       | POST /api/events/{publicId}/reject  | WebAPI/Controllers/EventsController.cs |
| P2_E3_F1_T8 | Upload Event Image | POST /api/events/{publicId}/image   | WebAPI/Controllers/EventsController.cs |

### Epic E2.4: Venue Management (Admin)

| Mã Task    | Tên Task       | Endpoint                     | File                                   |
| ----------- | --------------- | ---------------------------- | -------------------------------------- |
| P2_E4_F1_T1 | Create Venue    | POST /api/venues             | WebAPI/Controllers/VenuesController.cs |
| P2_E4_F1_T2 | Get Venues List | GET /api/venues              | WebAPI/Controllers/VenuesController.cs |
| P2_E4_F1_T3 | Update Venue    | PUT /api/venues/{venueId}    | WebAPI/Controllers/VenuesController.cs |
| P2_E4_F1_T4 | Delete Venue    | DELETE /api/venues/{venueId} | WebAPI/Controllers/VenuesController.cs |

### Epic E2.5: MVC Student UI

| Mã Task    | Tên Task                 | Mô tả                      | File                                 |
| ----------- | ------------------------- | ---------------------------- | ------------------------------------ |
| P2_E5_F1_T1 | Tạo Events Controller    | Index, Details actions       | MVC/Controllers/EventsController.cs  |
| P2_E5_F1_T2 | Tạo Event Listing View   | Cards layout, filter sidebar | MVC/Views/Events/Index.cshtml        |
| P2_E5_F1_T3 | Tạo Event Detail View    | Banner, description, agenda  | MVC/Views/Events/Details.cshtml      |
| P2_E5_F1_T4 | Tạo Filter Component     | Filter by venue, date        | MVC/Views/Shared/_EventFilter.cshtml |
| P2_E5_F1_T5 | Tạo Pagination Component | Phân trang                  | MVC/Views/Shared/_Pagination.cshtml  |

### Epic E2.6: Blazor Organizer UI

| Mã Task    | Tên Task                       | Mô tả                          | File                                         |
| ----------- | ------------------------------- | -------------------------------- | -------------------------------------------- |
| P2_E6_F1_T1 | Tạo MyEvents Page              | Danh sách events của organizer | BlazorWASM/Pages/Organizer/MyEvents.razor    |
| P2_E6_F1_T2 | Tạo CreateEvent Form           | Form tạo event                  | BlazorWASM/Pages/Organizer/CreateEvent.razor |
| P2_E6_F1_T3 | Tạo EditEvent Form             | Form sửa event                  | BlazorWASM/Pages/Organizer/EditEvent.razor   |
| P2_E6_F1_T4 | Tạo EventDetail Organizer View | Tab navigation                   | BlazorWASM/Pages/Organizer/EventDetail.razor |
| P2_E6_F1_T5 | Tạo Image Upload Component     | Upload lên Firebase Storage     | BlazorWASM/Components/ImageUpload.razor      |

### Epic E2.7: Blazor Admin UI

| Mã Task    | Tên Task                 | Mô tả                       | File                                         |
| ----------- | ------------------------- | ----------------------------- | -------------------------------------------- |
| P2_E7_F1_T1 | Tạo PendingEvents Page   | Danh sách events chờ duyệt | BlazorWASM/Pages/Admin/PendingEvents.razor   |
| P2_E7_F1_T2 | Tạo VenueManagement Page | CRUD venues                   | BlazorWASM/Pages/Admin/VenueManagement.razor |

---

## Phase 3: Registration System

### Epic E3.1: Registration Entities

| Mã Task    | Tên Task                    | Mô tả                                | File                             |
| ----------- | ---------------------------- | -------------------------------------- | -------------------------------- |
| P3_E1_F1_T1 | Tạo RegistrationStatus Enum | Pending, Approved, Rejected, Cancelled | Core/Enums/RegistrationStatus.cs |
| P3_E1_F1_T2 | Tạo Registration Entity     | Registration model                     | Core/Entities/Registration.cs    |
| P3_E1_F1_T3 | Tạo Waitlist Entity         | Waitlist model                         | Core/Entities/Waitlist.cs        |
| P3_E1_F1_T4 | Tạo Registration DTOs       | RegisterDto, RegistrationResponseDto   | Shared/DTOs/RegistrationDTOs.cs  |

### Epic E3.2: Registration Repository & Service

| Mã Task    | Tên Task                        | Mô tả                      | File                                                  |
| ----------- | -------------------------------- | ---------------------------- | ----------------------------------------------------- |
| P3_E2_F1_T1 | Tạo IRegistrationRepository     | CRUD + check capacity        | Core/Interfaces/IRegistrationRepository.cs            |
| P3_E2_F1_T2 | Implement RegistrationRepository | Firestore queries            | Infrastructure/Repositories/RegistrationRepository.cs |
| P3_E2_F1_T3 | Tạo IWaitlistRepository         | CRUD waitlist                | Core/Interfaces/IWaitlistRepository.cs                |
| P3_E2_F1_T4 | Implement WaitlistRepository     | Firestore queries            | Infrastructure/Repositories/WaitlistRepository.cs     |
| P3_E2_F1_T5 | Tạo IRegistrationService        | Register, cancel, approve    | Core/Interfaces/IRegistrationService.cs               |
| P3_E2_F1_T6 | Implement RegistrationService    | Business logic + transaction | Infrastructure/Services/RegistrationService.cs        |
| P3_E2_F1_T7 | Tạo Waitlist Processing Job     | Auto promote from waitlist   | Infrastructure/Jobs/WaitlistJob.cs                    |

### Epic E3.3: Registration API Endpoints

| Mã Task    | Tên Task               | Endpoint                            | File                                          |
| ----------- | ----------------------- | ----------------------------------- | --------------------------------------------- |
| P3_E3_F1_T1 | Register Event          | POST /api/registrations             | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T2 | Cancel Registration     | DELETE /api/registrations/{eventId} | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T3 | Approve Registration    | PUT /api/registrations/{id}/approve | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T4 | Reject Registration     | PUT /api/registrations/{id}/reject  | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T5 | Get My Events           | GET /api/registrations/my-events    | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T6 | Get Event Registrations | GET /api/events/{id}/registrations  | WebAPI/Controllers/RegistrationsController.cs |
| P3_E3_F1_T7 | Get Event Waitlist      | GET /api/events/{id}/waitlist       | WebAPI/Controllers/RegistrationsController.cs |

### Epic E3.4: MVC Student UI

| Mã Task    | Tên Task               | Mô tả                                | File                             |
| ----------- | ----------------------- | -------------------------------------- | -------------------------------- |
| P3_E4_F1_T1 | Add Register Button     | Nút đăng ký trên detail view      | MVC/Views/Events/Details.cshtml  |
| P3_E4_F1_T2 | Add Cancel Button       | Nút hủy nếu đã đăng ký         | MVC/Views/Events/Details.cshtml  |
| P3_E4_F1_T3 | Tạo MyEvents Page      | Danh sách events đã đăng ký      | MVC/Views/Events/MyEvents.cshtml |
| P3_E4_F1_T4 | Tạo Confirmation Popup | Xác nhận trước khi đăng ký/hủy | MVC/wwwroot/js/confirmation.js   |

### Epic E3.5: Blazor Organizer UI

| Mã Task    | Tên Task                   | Mô tả                           | File                                            |
| ----------- | --------------------------- | --------------------------------- | ----------------------------------------------- |
| P3_E5_F1_T1 | Tạo PendingApprovals Tab   | Danh sách đăng ký chờ duyệt | BlazorWASM/Pages/Organizer/EventDetail.razor    |
| P3_E5_F1_T2 | Tạo Registrations Tab      | Danh sách đã duyệt            | BlazorWASM/Pages/Organizer/EventDetail.razor    |
| P3_E5_F1_T3 | Tạo Waitlist Tab           | Danh sách waitlist               | BlazorWASM/Pages/Organizer/EventDetail.razor    |
| P3_E5_F1_T4 | Tạo Approve/Reject Buttons | Duyệt từ chối                  | BlazorWASM/Components/RegistrationActions.razor |

---

## Phase 4: Attendance & Check-in

### Epic E4.1: Check-in Code Management

| Mã Task    | Tên Task                    | Mô tả                         | File                                           |
| ----------- | ---------------------------- | ------------------------------- | ---------------------------------------------- |
| P4_E1_F1_T1 | Tạo CheckInCodeGenerator    | Sinh mã 6 ký tự ngẫu nhiên | Infrastructure/Helpers/CheckInCodeGenerator.cs |
| P4_E1_F1_T2 | Tạo CheckInService          | Generate, validate code         | Infrastructure/Services/CheckInService.cs      |
| P4_E1_F1_T3 | Tạo CheckIn Code Expiry Job | Auto-expire codes               | Infrastructure/Jobs/CheckInExpiryJob.cs        |

### Epic E4.2: Check-in API Endpoints

| Mã Task    | Tên Task              | Endpoint                            | File                                   |
| ----------- | ---------------------- | ----------------------------------- | -------------------------------------- |
| P4_E2_F1_T1 | Generate Check-in Code | POST /api/events/{id}/generate-code | WebAPI/Controllers/EventsController.cs |
| P4_E2_F1_T2 | Student Check-in       | POST /api/events/{id}/checkin       | WebAPI/Controllers/EventsController.cs |
| P4_E2_F1_T3 | Get Attendees List     | GET /api/events/{id}/attendees      | WebAPI/Controllers/EventsController.cs |

### Epic E4.3: MVC Student UI

| Mã Task    | Tên Task            | Mô tả                            | File                            |
| ----------- | -------------------- | ---------------------------------- | ------------------------------- |
| P4_E3_F1_T1 | Tạo Check-in Form   | Ô nhập mã + nút xác nhận     | MVC/Views/Events/Checkin.cshtml |
| P4_E3_F1_T2 | Tạo Check-in Button | Nút hiển thị nếu event Ongoing | MVC/Views/Events/Details.cshtml |
| P4_E3_F1_T3 | Tạo Mobile Keyboard | Bàn phím số tự động          | MVC/wwwroot/js/checkin.js       |

### Epic E4.4: Blazor Organizer UI

| Mã Task    | Tên Task                  | Mô tả                    | File                                         |
| ----------- | -------------------------- | -------------------------- | -------------------------------------------- |
| P4_E4_F1_T1 | Tạo Check-in Code Display | Hiển thị mã + countdown | BlazorWASM/Pages/Organizer/EventDetail.razor |
| P4_E4_F1_T2 | Tạo Generate Code Button  | Sinh mã mới              | BlazorWASM/Pages/Organizer/EventDetail.razor |
| P4_E4_F1_T3 | Tạo Attendees List        | Real-time check-in list    | BlazorWASM/Pages/Organizer/EventDetail.razor |
| P4_E4_F1_T4 | Tạo Auto Refresh          | Polling 5 giây            | BlazorWASM/Services/AttendanceHub.cs         |

---

## Phase 5: Agenda & Venue Management

### Epic E5.1: Agenda Entities

| Mã Task    | Tên Task              | Mô tả                        | File                        |
| ----------- | ---------------------- | ------------------------------ | --------------------------- |
| P5_E1_F1_T1 | Tạo AgendaItem Entity | Agenda model                   | Core/Entities/AgendaItem.cs |
| P5_E1_F1_T2 | Tạo Agenda DTOs       | AgendaItemDto, CreateAgendaDto | Shared/DTOs/AgendaDTOs.cs   |

### Epic E5.2: Agenda Repository & Service

| Mã Task    | Tên Task                  | Mô tả           | File                                            |
| ----------- | -------------------------- | ----------------- | ----------------------------------------------- |
| P5_E2_F1_T1 | Tạo IAgendaRepository     | CRUD agenda       | Core/Interfaces/IAgendaRepository.cs            |
| P5_E2_F1_T2 | Implement AgendaRepository | Firestore queries | Infrastructure/Repositories/AgendaRepository.cs |
| P5_E2_F1_T3 | Tạo IAgendaService        | Business logic    | Core/Interfaces/IAgendaService.cs               |
| P5_E2_F1_T4 | Implement AgendaService    | CRUD + ordering   | Infrastructure/Services/AgendaService.cs        |

### Epic E5.3: Agenda API Endpoints

| Mã Task    | Tên Task          | Endpoint                     | File                                   |
| ----------- | ------------------ | ---------------------------- | -------------------------------------- |
| P5_E3_F1_T1 | Get Agenda         | GET /api/events/{id}/agenda  | WebAPI/Controllers/AgendaController.cs |
| P5_E3_F1_T2 | Create Agenda Item | POST /api/events/{id}/agenda | WebAPI/Controllers/AgendaController.cs |
| P5_E3_F1_T3 | Update Agenda Item | PUT /api/agenda/{id}         | WebAPI/Controllers/AgendaController.cs |
| P5_E3_F1_T4 | Delete Agenda Item | DELETE /api/agenda/{id}      | WebAPI/Controllers/AgendaController.cs |

### Epic E5.4: MVC Student UI

| Mã Task    | Tên Task                 | Mô tả                          | File                                    |
| ----------- | ------------------------- | -------------------------------- | --------------------------------------- |
| P5_E4_F1_T1 | Tạo Agenda Timeline View | Hiển thị agenda dạng timeline | MVC/Views/Events/_AgendaTimeline.cshtml |

### Epic E5.5: Blazor Organizer UI

| Mã Task    | Tên Task                  | Mô tả                           | File                                         |
| ----------- | -------------------------- | --------------------------------- | -------------------------------------------- |
| P5_E5_F1_T1 | Tạo Agenda Management Tab | Danh sách agenda items           | BlazorWASM/Pages/Organizer/EventDetail.razor |
| P5_E5_F1_T2 | Tạo Agenda Form           | Thêm/sửa agenda item            | BlazorWASM/Components/AgendaForm.razor       |
| P5_E5_F1_T3 | Tạo Drag-Drop Ordering    | Sắp xếp thứ tự                | BlazorWASM/Components/AgendaSortable.razor   |
| P5_E5_F1_T4 | Tạo Material Upload       | Upload file lên Firebase Storage | BlazorWASM/Components/MaterialUpload.razor   |

---

## Phase 6: Notification System

### Epic E6.1: Email Service

| Mã Task    | Tên Task                     | Mô tả                       | File                                                   |
| ----------- | ----------------------------- | ----------------------------- | ------------------------------------------------------ |
| P6_E1_F1_T1 | Tạo EmailTemplate Entity     | Template model                | Core/Entities/EmailTemplate.cs                         |
| P6_E1_F1_T2 | Tạo IEmailService Interface  | Send email                    | Core/Interfaces/IEmailService.cs                       |
| P6_E1_F1_T3 | Implement EmailService        | SMTP client + template render | Infrastructure/Services/EmailService.cs                |
| P6_E1_F1_T4 | Tạo EmailTemplate Repository | CRUD templates                | Infrastructure/Repositories/EmailTemplateRepository.cs |

### Epic E6.2: Hangfire Jobs

| Mã Task    | Tên Task                   | Schedule               | File                                |
| ----------- | --------------------------- | ---------------------- | ----------------------------------- |
| P6_E2_F1_T1 | Tạo Registration Email Job | Enqueue                | Infrastructure/Jobs/EmailJobs.cs    |
| P6_E2_F1_T2 | Tạo Waitlist Promotion Job | Enqueue                | Infrastructure/Jobs/EmailJobs.cs    |
| P6_E2_F1_T3 | Tạo Reminder 1 Day Job     | Chạy 8:00 hàng ngày | Infrastructure/Jobs/ReminderJobs.cs |
| P6_E2_F1_T4 | Tạo Reminder 1 Hour Job    | Chạy mỗi giờ        | Infrastructure/Jobs/ReminderJobs.cs |
| P6_E2_F1_T5 | Tạo Hangfire Dashboard     | Cấu hình dashboard   | Program.cs                          |

### Epic E6.3: Email Templates API

| Mã Task    | Tên Task       | Endpoint                      | File                                          |
| ----------- | --------------- | ----------------------------- | --------------------------------------------- |
| P6_E3_F1_T1 | Get Templates   | GET /api/email-templates      | WebAPI/Controllers/EmailTemplateController.cs |
| P6_E3_F1_T2 | Update Template | PUT /api/email-templates/{id} | WebAPI/Controllers/EmailTemplateController.cs |

### Epic E6.4: Blazor Admin UI

| Mã Task    | Tên Task                 | Mô tả                     | File                                        |
| ----------- | ------------------------- | --------------------------- | ------------------------------------------- |
| P6_E4_F1_T1 | Tạo Email Templates Page | Danh sách templates        | BlazorWASM/Pages/Admin/EmailTemplates.razor |
| P6_E4_F1_T2 | Tạo Template Editor      | Rich text editor (Quill)    | BlazorWASM/Components/TemplateEditor.razor  |
| P6_E4_F1_T3 | Tạo Template Preview     | Preview với dữ liệu mẫu | BlazorWASM/Components/TemplatePreview.razor |

---

## Phase 7: Calendar Integration (.ics)

### Epic E7.1: ICS Generator

| Mã Task    | Tên Task                       | Mô tả                  | File                                       |
| ----------- | ------------------------------- | ------------------------ | ------------------------------------------ |
| P7_E1_F1_T1 | Cài đặt ICal.NET             | NuGet: ICal.NET          | EMS.WebAPI.csproj                          |
| P7_E1_F1_T2 | Tạo ICalendarService Interface | Generate .ics            | Core/Interfaces/ICalendarService.cs        |
| P7_E1_F1_T3 | Implement CalendarService       | Tạo file .ics từ Event | Infrastructure/Services/CalendarService.cs |

### Epic E7.2: ICS API Endpoint

| Mã Task    | Tên Task   | Endpoint                          | File                                   |
| ----------- | ----------- | --------------------------------- | -------------------------------------- |
| P7_E2_F1_T1 | Export .ics | GET /api/events/{id}/calendar.ics | WebAPI/Controllers/EventsController.cs |

### Epic E7.3: MVC UI

| Mã Task    | Tên Task                     | Mô tả                    | File                                      |
| ----------- | ----------------------------- | -------------------------- | ----------------------------------------- |
| P7_E3_F1_T1 | Tạo Calendar Button          | Nút "Thêm vào lịch"    | MVC/Views/Events/Details.cshtml           |
| P7_E3_F1_T2 | Tạo Calendar Dropdown        | Google Calendar + Download | MVC/Views/Events/_CalendarDropdown.cshtml |
| P7_E3_F1_T3 | Tạo Google Calendar Redirect | URL generator              | MVC/Helpers/GoogleCalendarHelper.cs       |

---

## Phase 8: Dashboard & Reporting

### Epic E8.1: Statistics Service

| Mã Task    | Tên Task                         | Mô tả                          | File                                         |
| ----------- | --------------------------------- | -------------------------------- | -------------------------------------------- |
| P8_E1_F1_T1 | Tạo IStatisticsService Interface | Get stats                        | Core/Interfaces/IStatisticsService.cs        |
| P8_E1_F1_T2 | Implement StatisticsService       | Aggregate queries Firestore      | Infrastructure/Services/StatisticsService.cs |
| P8_E1_F1_T3 | Tạo Statistics DTOs              | AdminStatsDto, OrganizerStatsDto | Shared/DTOs/StatisticsDTOs.cs                |

### Epic E8.2: Statistics API Endpoints

| Mã Task    | Tên Task                 | Endpoint                           | File                                      |
| ----------- | ------------------------- | ---------------------------------- | ----------------------------------------- |
| P8_E2_F1_T1 | Admin Dashboard Stats     | GET /api/dashboard/admin/stats     | WebAPI/Controllers/DashboardController.cs |
| P8_E2_F1_T2 | Organizer Dashboard Stats | GET /api/dashboard/organizer/stats | WebAPI/Controllers/DashboardController.cs |
| P8_E2_F1_T3 | Event Stats               | GET /api/events/{id}/stats         | WebAPI/Controllers/EventsController.cs    |

### Epic E8.3: Export Service

| Mã Task    | Tên Task                     | Thư viện            | File                                     |
| ----------- | ----------------------------- | --------------------- | ---------------------------------------- |
| P8_E3_F1_T1 | Cài đặt ClosedXML          | NuGet: ClosedXML      | EMS.Infrastructure.csproj                |
| P8_E3_F1_T2 | Cài đặt QuestPDF           | NuGet: QuestPDF       | EMS.Infrastructure.csproj                |
| P8_E3_F1_T3 | Tạo IExportService Interface | Excel, PDF export     | Core/Interfaces/IExportService.cs        |
| P8_E3_F1_T4 | Implement ExportService       | Excel + PDF generator | Infrastructure/Services/ExportService.cs |

### Epic E8.4: Export API Endpoints

| Mã Task    | Tên Task    | Endpoint                          | File                                   |
| ----------- | ------------ | --------------------------------- | -------------------------------------- |
| P8_E4_F1_T1 | Export Excel | GET /api/events/{id}/export/excel | WebAPI/Controllers/EventsController.cs |
| P8_E4_F1_T2 | Export PDF   | GET /api/events/{id}/export/pdf   | WebAPI/Controllers/EventsController.cs |

### Epic E8.5: Blazor Dashboard UI

| Mã Task    | Tên Task                     | Mô tả                | File                                       |
| ----------- | ----------------------------- | ---------------------- | ------------------------------------------ |
| P8_E5_F1_T1 | Cài đặt Chart.js           | Thư viện biểu đồ  | BlazorWASM/index.html                      |
| P8_E5_F1_T2 | Tạo Admin Dashboard Page     | Biểu đồ, số liệu  | BlazorWASM/Pages/Admin/Dashboard.razor     |
| P8_E5_F1_T3 | Tạo Organizer Dashboard Page | Biểu đồ event stats | BlazorWASM/Pages/Organizer/Dashboard.razor |
| P8_E5_F1_T4 | Tạo Export Buttons           | Excel + PDF export     | BlazorWASM/Components/ExportButtons.razor  |
| P8_E5_F1_T5 | Tạo Line Chart Component     | Biểu đồ xu hướng  | BlazorWASM/Components/LineChart.razor      |
| P8_E5_F1_T6 | Tạo Pie Chart Component      | Biểu đồ tỷ lệ     | BlazorWASM/Components/PieChart.razor       |

---

## Phase 9: Security Hardening

### Epic E9.1: Firebase Security Rules

| Mã Task    | Tên Task                      | Mô tả                 | File                  |
| ----------- | ------------------------------ | ----------------------- | --------------------- |
| P9_E1_F1_T1 | Viết Firestore Security Rules | Tenant isolation        | firestore.rules       |
| P9_E1_F1_T2 | Viết Storage Security Rules   | File upload rules       | storage.rules         |
| P9_E1_F1_T3 | Test Rules locally             | Firebase Emulator Suite | scripts/test-rules.sh |

### Epic E9.2: API Security

| Mã Task    | Tên Task                 | Mô tả                       | File                                     |
| ----------- | ------------------------- | ----------------------------- | ---------------------------------------- |
| P9_E2_F1_T1 | Cấu hình CORS           | Chỉ cho phép tenant domains | Program.cs                               |
| P9_E2_F1_T2 | Thêm Rate Limiting       | ASP.NET Core Rate Limiting    | Program.cs                               |
| P9_E2_F1_T3 | Cấu hình HTTPS Redirect | Enforce HTTPS                 | Program.cs                               |
| P9_E2_F1_T4 | Thêm Security Headers    | HSTS, X-Frame-Options         | Middlewares/SecurityHeadersMiddleware.cs |

### Epic E9.3: Input Validation

| Mã Task    | Tên Task                        | Mô tả                                    | File                                      |
| ----------- | -------------------------------- | ------------------------------------------ | ----------------------------------------- |
| P9_E3_F1_T1 | Cài đặt FluentValidation      | NuGet: FluentValidation                    | EMS.Core.csproj                           |
| P9_E3_F1_T2 | Tạo Validators cho Event        | CreateEventValidator, UpdateEventValidator | Core/Validators/EventValidators.cs        |
| P9_E3_F1_T3 | Tạo Validators cho Registration | RegisterValidator                          | Core/Validators/RegistrationValidators.cs |
| P9_E3_F1_T4 | Tạo Validators cho User         | UserValidators                             | Core/Validators/UserValidators.cs         |
| P9_E3_F1_T5 | Tạo HTML Sanitizer              | Loại bỏ script tags                      | Infrastructure/Helpers/HtmlSanitizer.cs   |

---

## Phase 10: Deployment & Go-Live

### Epic E10.1: Production Environment

| Mã Task     | Tên Task                        | Mô tả            | Output                 |
| ------------ | -------------------------------- | ------------------ | ---------------------- |
| P10_E1_F1_T1 | Tạo Production Firebase Project | Firebase Console   | GCP Project            |
| P10_E1_F1_T2 | Cấu hình Firebase Auth Rules   | Email verification | Firebase Console       |
| P10_E1_F1_T3 | Tạo Firestore Indexes           | Composite indexes  | firestore.indexes.json |
| P10_E1_F1_T4 | Cấu hình Firebase Hosting      | Cho Blazor WASM    | firebase.json          |

### Epic E10.2: Docker & Container Registry

| Mã Task     | Tên Task                  | Mô tả                   | Output      |
| ------------ | -------------------------- | ------------------------- | ----------- |
| P10_E2_F1_T1 | Build Docker Image API     | Multi-stage               | Dockerfile  |
| P10_E2_F1_T2 | Build Docker Image MVC     | Multi-stage               | Dockerfile  |
| P10_E2_F1_T3 | Build Docker Image Blazor  | Nginx                     | Dockerfile  |
| P10_E2_F1_T4 | Push to Container Registry | GitHub Container Registry | docker push |

### Epic E10.3: CI/CD Pipeline

| Mã Task     | Tên Task                       | Mô tả                | File                                 |
| ------------ | ------------------------------- | ---------------------- | ------------------------------------ |
| P10_E3_F1_T1 | Tạo Build Workflow             | Build + Test           | .github/workflows/build.yml          |
| P10_E3_F1_T2 | Tạo Deploy Staging Workflow    | Deploy lên staging    | .github/workflows/deploy-staging.yml |
| P10_E3_F1_T3 | Tạo Deploy Production Workflow | Deploy lên production | .github/workflows/deploy-prod.yml    |
| P10_E3_F1_T4 | Tạo Database Backup Workflow   | Backup Firestore       | .github/workflows/backup.yml         |

### Epic E10.4: Monitoring & Logging

| Mã Task     | Tên Task                     | Mô tả                        | File                         |
| ------------ | ----------------------------- | ------------------------------ | ---------------------------- |
| P10_E4_F1_T1 | Cấu hình Serilog Sinks      | File, Console, (Cloud Logging) | Program.cs                   |
| P10_E4_F1_T2 | Tạo Health Check Endpoint    | `/health`, `/health/ready` | Program.cs                   |
| P10_E4_F1_T3 | Cấu hình Hangfire Dashboard | Authentication                 | Program.cs                   |
| P10_E4_F1_T4 | Tạo Uptime Monitoring        | Health check + alert           | .github/workflows/uptime.yml |

### Epic E10.5: Documentation

| Mã Task     | Tên Task                    | Mô tả                          | File                      |
| ------------ | ---------------------------- | -------------------------------- | ------------------------- |
| P10_E5_F1_T1 | Tạo Deployment Guide        | Hướng dẫn triển khai         | docs/deployment-guide.md  |
| P10_E5_F1_T2 | Tạo Admin Manual            | Hướng dẫn sử dụng cho admin | docs/admin-manual.md      |
| P10_E5_F1_T3 | Tạo Tenant Onboarding Guide | Hướng dẫn tạo tenant mới    | docs/tenant-onboarding.md |
| P10_E5_F1_T4 | Tạo API Documentation       | Từ Swagger xuất OpenAPI        | docs/api-reference.md     |
| P10_E5_F1_T5 | Tạo README.md               | Tổng quan dự án               | README.md                 |

---

## Tổng hợp số lượng Task theo Phase

| Phase           | Số lượng Task   |
| --------------- | ------------------ |
| Phase 0         | 21                 |
| Phase 1         | 30                 |
| Phase 2         | 32                 |
| Phase 3         | 27                 |
| Phase 4         | 13                 |
| Phase 5         | 17                 |
| Phase 6         | 16                 |
| Phase 7         | 8                  |
| Phase 8         | 17                 |
| Phase 9         | 14                 |
| Phase 10        | 19                 |
| **Tổng** | **214 Task** |
