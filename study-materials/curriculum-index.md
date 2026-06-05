# Sha8alny Backend Study Materials — Curriculum Index

This index summarizes all 19 study units and is designed for quick review and NotebookLM navigation. Each unit can be read independently; cross-references indicate prerequisites.

---

## Unit 1 — Helicopter View
**File:** `unit-01-helicopter-view.md`

**What it covers:** The Sha8alny platform from 10,000 feet — what problem it solves, who the four user types are (Student, Company, Admin, University), and how the platform connects Egyptian engineering students with companies for internships, graduation projects, and paid work. Walks through Ahmad's complete journey from registration to certificate. No code — pure product understanding.

**Key concepts:** Platform purpose, user roles, the marketplace model, why Sha8alny is more than a job board.

---

## Unit 2 — Map of the Codebase
**File:** `unit-02-code-map.md`

**What it covers:** The 7 C# projects in the solution and how they relate to each other. Explains Onion Architecture using the "restaurant" analogy — front-of-house (Web), management rules (Service), data layer (Persistence), contracts (Abstraction), core data types (Domain), utilities (Shared). Why dependency direction matters: inner layers cannot know about outer layers.

**Key concepts:** `Sh8lny.Web`, `Sh8lny.Service`, `Sh8lny.Persistence`, `Sh8lny.Domain`, `Sh8lny.Abstraction`, `Sh8lny.Shared`, Onion Architecture, dependency inversion, interfaces as contracts.

---

## Unit 3 — How a Request Travels
**File:** `unit-03-request-lifecycle.md`

**What it covers:** What happens from the moment an HTTP request arrives to when the response leaves. The middleware pipeline (Swagger → HTTPS → Static Files → Request Timing → CORS → Authentication → Authorization → Controllers → SignalR Hub). Dependency Injection as the "supply closet" pattern. How Program.cs is the assembly point for the entire application.

**Key concepts:** Middleware pipeline, request lifecycle, ASP.NET Core DI, Program.cs anatomy, Kestrel, IApplicationBuilder, startup sequence (MigrateAsync → SeedAsync → Accept requests).

---

## Unit 4 — The Database Layer
**File:** `unit-04-database-layer.md`

**What it covers:** How Sha8alny talks to SQL Server without writing raw SQL. Entity Framework Core as the "translator" that converts C# LINQ to SQL. The `Sha8lnyDbContext` with 28 DbSets. The Generic Repository pattern (`IGenericRepository<T>`) and its 11 methods. Unit of Work (`IUnitOfWork`) as the coordinator. Fluent API configurations keeping the Domain model clean. 8 EF Core migrations. The `.Include()` rule for loading related data.

**Key concepts:** EF Core, DbContext, DbSet, GenericRepository, UnitOfWork, Fluent API, migrations, navigation properties, `.Include()` eager loading, `OnDelete(DeleteBehavior.Restrict)`.

---

## Unit 5 — DTOs, AutoMapper, and ServiceResponse
**File:** `unit-05-dtos-mapper-response.md`

**What it covers:** Why database entities must never be returned directly from APIs (sensitive data, serialization loops). The three DTO patterns: Create (client → server), Response (server → client), Update (partial modifications). AutoMapper and `MappingProfile.cs` as the central translation registry. `ServiceResponse<T>` — the universal envelope with `IsSuccess`, `Data`, `Message`, and `Errors`.

**Key concepts:** DTO, AutoMapper, `MappingProfile.cs`, `ServiceResponse<T>`, `Sh8lny.Shared` as the dependency-free shared library, create/response/update pattern.

---

## Unit 6 — Authentication and JWT
**File:** `unit-06-auth-jwt.md`

**What it covers:** Why HTTP is stateless and how JWT solves the "who are you?" problem without server sessions. BCrypt password hashing (one-way, cannot be reversed). Registration flow, login flow, JWT token structure (header.payload.signature), the wristband analogy for signature verification. Role-based authorization with `[Authorize(Roles = "Company")]`. Email OTP verification and forgot-password flow. The `User` entity field-by-field.

**Key concepts:** JWT, BCrypt, stateless authentication, claims (`sub`, `email`, `role`, `jti`), `[Authorize]`, `[AllowAnonymous]`, `UserType` enum, `User.IsActive` for banning, 60-minute token lifetime.

---

## Unit 7 — File Uploads
**File:** `unit-07-file-uploads.md`

**What it covers:** Why all file uploads go through one endpoint (`POST /api/Media`) instead of being scattered across controllers. The two-step upload flow: client uploads file → gets URL → passes URL to other endpoints. What `FileService` does: validates type (.jpg/.jpeg/.png/.gif/.pdf), checks 5 MB size limit, virus scans (ClamAV stub), resizes to max 1920px, converts to WebP at 80% quality, generates 300px thumbnail, saves to `wwwroot/uploads/{folder}/`. Why `IFormFile` is forbidden in inner layers.

**Key concepts:** Centralized media endpoint, `IFormFile`, Onion Architecture file handling, SixLabors.ImageSharp, WebP format, ClamAV stub, `FileUploadResult`, two-step upload pattern.

---

## Unit 8 — Students and Companies
**File:** `unit-08-students-companies.md`

**What it covers:** Why `User` (authentication identity) and `Student` (professional profile) are separate entities — the "ID card vs. transcript" analogy. Every field on `Student` including `AcademicYear` enum (FirstYear through Graduate), `StudentStatus`, `ProfileCompleteness`, `AverageRating`, `TotalInternshipDays`. Sub-records: `StudentSkill` join table, `Education`, `Experience`. `SavedOpportunity` bookmarking. The `Company` entity and its upsert pattern. Profile search with filters.

**Key concepts:** `Student`, `Company`, `User` separation, `AcademicYear` enum, `ProfileCompleteness`, `SavedOpportunity`, upsert pattern, many-to-many skill join table, `Student.FullName` computed property.

---

## Unit 9 — Projects and Applications
**File:** `unit-09-projects-applications.md`

**What it covers:** The `Project` entity as the marketplace listing — five project types (Internship/GraduationProject/Training/PartTime/FullTime). `ProjectStatus` state machine (Draft → Active → Pending → Complete/Cancelled/Closed). Creating a project with a database transaction wrapping project + skill inserts. The seven application checks before `Application` is created (student profile, project exists, deadline, visible+active, max applicants, no duplicate, required skills). `ApplicationStatus` state machine. Company review flow with real-time notification. Application withdrawal. Filtered project search with pagination.

**Key concepts:** `ProjectStatus`, `ApplicationStatus`, `ProjectRequiredSkill`, skill-check gate, `BeginTransactionAsync/CommitTransactionAsync`, `ViewCount` increment, `ApplicationCount`, `PagedResult<T>`, `BidAmount=0` for internships.

---

## Unit 10 — Execution and Modules
**File:** `unit-10-execution-modules.md`

**What it covers:** What happens after acceptance — tracking actual work through `ProjectModule` and `ApplicationModuleProgress`. Modules only for Internship projects. Weight system (must sum ≤ 100%). Accepted → InProgress auto-transition on first progress update. Weighted overall progress calculation. Auto-notification when all modules hit 100% (without auto-completing). Company approval/rejection of modules. The 10-step `CompleteJobAsync` cascade: marks application Completed, project Closed, notifies student, auto-generates certificate.

**Key concepts:** `ProjectModule`, `ModuleStatus`, `ApplicationModuleProgress`, weighted progress, `CheckAndCompleteApplicationAsync`, `CompleteJobAsync`, scope-per-cycle DI, `CompletionSummaryDto`.

---

## Unit 11 — Reviews and Certificates
**File:** `unit-11-reviews-certificates.md`

**What it covers:** Mutual review system — `StudentReview` (company → student) and `CompanyReview` (student → company). Gate: only after application status is `Completed`. Duplicate prevention. `AverageRating` recalculated from all reviews on every submission (full recalc, not incremental). `WouldHireAgain`/`WouldRecommend` auto-set from rating ≥ 4. Certificate generation: auto-triggered by `CompleteJobAsync`, idempotent, `CERT-XXXXXXXXXXXX` format, publicly verifiable without authentication at `GET /api/Certificates/verify/{id}`.

**Key concepts:** `StudentReview`, `CompanyReview`, `ReviewStatus`, `AverageRating` recalculation, `WouldHireAgain`, `CertificateNumber` GUID-derived, public certificate verification, `[AllowAnonymous]`.

---

## Unit 12 — Payments
**File:** `unit-12-payments.md`

**What it covers:** Two data models: `Payment` (designed for Paymob gateway integration — stores `PaymobOrderId`, `PaymobTransactionId`, `GatewayRawResponse`) and `Transaction` (internal ledger: payer, payee, amount, reference). Currently a mock implementation that creates `Transaction` records directly. Gates: application must be `Completed` and `IsPaid = false`. Paymob's three-step webhook flow (order registration → hosted form → webhook confirmation). `GatewayRawResponse` for dispute resolution. "FailTest" test hook.

**Key concepts:** `Payment`, `Transaction`, `PaymentStatus`, `TransactionStatus`, Paymob, `IsPaid` idempotency guard, `GatewayRawResponse`, mock payment with `Task.Delay`.

---

## Unit 13 — Chat and Messaging
**File:** `unit-13-chat.md`

**What it covers:** REST-based chat with real-time delivery via SignalR. `Conversation`, `ConversationParticipant`, `Message` data models. `FindOrCreateConversationAsync` — auto-creates a conversation on first message, reuses it for subsequent messages. The full send-message flow: verify sender/receiver, find/create conversation, save message, update LastMessageAt, resolve display name (Student.FullName or Company.CompanyName), push via `INotifier.SendMessageToUserAsync`. Security check: participant verification before reading messages. Mark-as-read per conversation.

**Key concepts:** `Conversation`, `ConversationParticipant`, `ConversationType`, `Message`, `MessageType`, unread count, `INotifier.SendMessageToUserAsync`, find-or-create pattern, `LastReadAt`.

---

## Unit 14 — Notifications and SignalR
**File:** `unit-14-notifications-signalr.md`

**What it covers:** Two-layer notification design: database persistence (always) + SignalR push (best-effort). The `Notification` entity with 9 `NotificationType` values. `INotifier` interface keeping inner layers free of SignalR dependencies. `NotificationHub` — the SignalR hub class handling connection/disconnection and group join/leave. How the JWT travels via query string (`?access_token=...`) since WebSockets cannot set headers. `SignalRNotifier.SendAsync` using `_hubContext.Clients.User(userId)`. Silent failure pattern. `NotificationService` CRUD for the notification inbox.

**Key concepts:** `INotifier`, `SignalRNotifier`, `NotificationHub`, `IHubContext`, `Clients.User()`, `ReceiveNotification`/`ReceiveMessage` event names, JWT via query string, silent failure, `OnConnectedAsync`, Groups.

---

## Unit 15 — Admin, Settings, and Master Data
**File:** `unit-15-admin-settings-masterdata.md`

**What it covers:** Three management surfaces. Admin: dashboard stats (real-time aggregates), `DashboardMetric` daily snapshots (auto-saved on dashboard load), user management list, `ToggleUserBanAsync` (flips `IsActive`, cannot ban Admins), force-delete project with manual cascade. Master Data: Skills (with SkillCategory), Universities (with UniversityType), Departments (linked to University) — all managed by Admin only, read by everyone. User Settings: auto-created on first access, notification toggles, Language, Timezone, `ProfileVisibility` (Public/UniversityOnly/Private).

**Key concepts:** `[Authorize(Roles = "Admin")]`, `DashboardMetric` snapshot, `ToggleUserBanAsync`, force-delete cascade, `Skill`, `University`, `Department`, `UserSettings`, `ProfileVisibility`, upsert on first read.

---

## Unit 16 — Background Services and Infrastructure
**File:** `unit-16-background-infrastructure.md`

**What it covers:** The invisible workers that run without user requests. `BackupWorker` — extends `BackgroundService`, uses `PeriodicTimer` at 24-hour intervals, creates a DI scope per cycle, calls `IBackupService.CreateBackupAsync()`. `BackupService` executes SQL Server `BACKUP DATABASE` T-SQL with COMPRESSION and CHECKSUM, then `RESTORE VERIFYONLY`. `DbInitializer.SeedAsync` — idempotent seed using `AnyAsync()` checks, seeds 15 skills and 3 universities. Auto-migration via `MigrateAsync()` on startup. `DiscordWebhookLogger` — custom `ILogger`, posts to Discord, fire-and-forget, max 1900 chars. Request timing middleware using `Stopwatch`.

**Key concepts:** `BackgroundService`, `PeriodicTimer`, `CreateScope()` for Scoped-in-Singleton, `BACKUP DATABASE`, `RESTORE VERIFYONLY`, `DbInitializer`, `MigrateAsync`, `ILoggerProvider`, `DiscordWebhookLogger`, fire-and-forget logging.

---

## Unit 17 — End-to-End Trace
**File:** `unit-17-end-to-end-trace.md`

**What it covers:** The capstone unit. Traces one action (TechCorp accepts Ahmad's application) through every architectural layer: TCP/Kestrel → middleware pipeline (7 stages) → JWT authentication → role authorization → controller → service (business logic with 4 reads + 2 writes) → UnitOfWork → EF Core → SQL Server (2 SQL statements) → response returns → SignalR push to Ahmad's device → Discord log entry. Full architecture map showing every class and its contribution. Ahmad's complete journey across all 17 units.

**Key concepts:** Full-stack trace, layer-by-layer walkthrough, how authentication flows into authorization into business logic into persistence into real-time delivery, the 127ms round-trip.

---

## Unit 18 — Email, Password Reset & NuGet Package Justifications
**File:** `unit-18-email-password-reset-packages.md`

**What it covers:** The complete password reset flow: `POST /forgot-password` generates a 6-digit code stored in the `User` record with a 15-minute expiry, sends an HTML email via MailKit + Gmail SMTP, and always returns the same response regardless of whether the email exists (email enumeration protection). `POST /reset-password` validates code + expiry, hashes the new password with BCrypt, and clears the token. The `AuthController` full endpoint map including `GET /me` (display name resolution per role). Justifications for every NuGet package in the codebase: why BCrypt over SHA-256, why MailKit over `System.Net.Mail`, why EF Core over Dapper, why ImageSharp over `System.Drawing`, why SignalR over raw WebSockets, why AutoMapper, why ClamAV is installed but disabled. CORS AllowAll policy: why it is used and what the production alternative is. Three unimplemented-but-designed entities: `ProjectGroup` (team collaboration), `ActivityLog` (audit trail), `CompletedOpportunity` (portfolio record).

**Key concepts:** `ForgotPasswordAsync`, email enumeration prevention, 6-digit OTP, 15-minute token expiry, `PasswordResetToken`, `ResetTokenExpires`, MailKit STARTTLS port 587, Gmail App Password, MimeKit `BodyBuilder`, NuGet justifications, CORS AllowAll, `ProjectGroup`, `ActivityLog`, `CompletedOpportunity`.

---

## Unit 19 — CI/CD, Testing & Cloud Deployment
**File:** `unit-19-cicd-testing-deployment.md`

**What it covers:** The complete automated pipeline from code push to production. GitHub Actions workflow (`main-ci-cd.yml`): two jobs — `build-and-test` (restore, build, run xUnit tests on ubuntu-latest) and `deploy-to-cloud-run` (only runs if tests pass, only on direct push to master). Docker image tagged with the git commit SHA for traceability. Google Container Registry for image storage. Google Cloud Run: serverless container platform, scales to zero, `--allow-unauthenticated`, region `us-central1`, zero-downtime rolling deployments. GitHub Secrets for `GCP_CREDENTIALS` — principle of least privilege. Integration tests with `CustomWebApplicationFactory` vs unit tests. `MaintenanceController` — three Admin-only endpoints for on-demand backup, listing backups, and purging old backups.

**Key concepts:** CI/CD, GitHub Actions, `needs:` job dependency, `github.sha` image tag, Google Container Registry, Cloud Run serverless, `--allow-unauthenticated`, zero-downtime rollout, `CustomWebApplicationFactory`, xUnit, `MaintenanceController`, GitHub Secrets.

---

## Quick Reference: Key Numbers

| Fact | Value |
|---|---|
| C# projects in the solution | 7 |
| Domain entities | 30 |
| Controllers | 16 |
| Service classes | 18 (including SignalRNotifier) |
| EF Core migrations | 9 |
| Fluent API configuration files | 28 |
| JWT lifetime | 60 minutes |
| Password reset code | 6 digits, 15-minute expiry |
| Max file size | 5 MB |
| Max image width | 1920px |
| WebP quality | 80% |
| Thumbnail size | 300px |
| Backup interval | 24 hours |
| Backup retention | 7 days |
| Startup delay before first backup | 2 minutes |
| Discord message max length | 1900 characters |
| Cloud Run region | us-central1 |
| Cloud Run port | 8080 |
| SDK image size (build stage) | ~700 MB |
| Runtime image size (final stage) | ~200 MB |
| ProjectType values | 5 (Internship, GraduationProject, Training, PartTime, FullTime) |
| ProjectStatus values | 6 (Draft, Active, Pending, Complete, Cancelled, Closed) |
| ApplicationStatus values | 8 (Submit, Pending, UnderReview, Accepted, InProgress, Completed, Rejected, Withdrawn) |
| NotificationType values | 9 (Application, Message, Project, Deadline, Acceptance, Rejection, System, Certificate, Payment) |
| AcademicYear values | 5 (FirstYear, SecondYear, ThirdYear, FourthYear, Graduate) |
| AuthController endpoints | 5 (register, login, me, forgot-password, reset-password) |

---

## Quick Reference: Key Patterns

| Pattern | Where used | Unit |
|---|---|---|
| Repository Pattern | All data access via `IGenericRepository<T>` | 4 |
| Unit of Work | `IUnitOfWork` coordinates all repositories | 4 |
| ServiceResponse\<T\> | Universal service return envelope | 5 |
| AutoMapper | Entity-to-DTO conversion in `MappingProfile.cs` | 5 |
| JWT stateless auth | No server-side sessions | 6 |
| Two-step file upload | Upload to `/api/Media`, use URL elsewhere | 7 |
| Upsert (find-or-create) | Company profile, Settings, Conversation | 8, 13, 15 |
| Database transaction | Project creation (project + skills atomic) | 9 |
| State machine | ProjectStatus, ApplicationStatus, ModuleStatus | 9, 10 |
| Weighted progress | Module-weighted overall progress calculation | 10 |
| AverageRating recalculation | Full recalc from all reviews on each submission | 11 |
| Idempotent certificate | Return existing if already generated | 11 |
| Silent failure | SignalR push failure is logged, not thrown | 14 |
| DI scope per cycle | BackupWorker creates scope to use Scoped services | 16 |
| Fire-and-forget | Discord logging, SignalR push | 14, 16 |
| Inline middleware | Request timing, JWT query string for SignalR | 3, 14 |
| Email enumeration prevention | ForgotPassword always returns same response | 18 |
| 6-digit OTP with expiry | Password reset code stored on User entity | 18 |
| STARTTLS SMTP | MailKit connects on port 587, upgrades to TLS | 18 |
| Git SHA image tagging | Every Docker image tagged with commit hash | 19 |
| Job dependency (needs:) | Deploy only runs when build-and-test passes | 19 |
| GitHub Secrets | GCP credentials encrypted, never in source code | 19 |
| CustomWebApplicationFactory | Integration tests spin up real pipeline in memory | 19 |



All study materials are complete in study-materials/:

┌─────┬─────────────────────────────┬───────────────────────┐
│  #  │            File             │         Topic         │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ What Sha8alny is,     │
│ 1   │ unit-01-helicopter-view.md  │ user roles, Ahmad's   │
│     │                             │ journey               │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ 7 projects, Onion     │
│ 2   │ unit-02-code-map.md         │ Architecture          │
│     │                             │ explained             │
├─────┼─────────────────────────────┼───────────────────────┤
│ 3   │ unit-03-request-lifecycle.m │ Middleware pipeline,  │
│     │ d                           │ DI, Program.cs        │
├─────┼─────────────────────────────┼───────────────────────┤
│ 4   │ unit-04-database-layer.md   │ EF Core, Repository,  │
│     │                             │ Unit of Work          │
├─────┼─────────────────────────────┼───────────────────────┤
│ 5   │ unit-05-dtos-mapper-respons │ DTOs, AutoMapper,     │
│     │ e.md                        │ ServiceResponse<T>    │
├─────┼─────────────────────────────┼───────────────────────┤
│ 6   │ unit-06-auth-jwt.md         │ JWT, BCrypt, roles,   │
│     │                             │ email verification    │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ Centralized           │
│ 7   │ unit-07-file-uploads.md     │ /api/Media, WebP,     │
│     │                             │ ClamAV                │
├─────┼─────────────────────────────┼───────────────────────┤
│     │ unit-08-students-companies. │ Profile entities,     │
│ 8   │ md                          │ skills,               │
│     │                             │ SavedOpportunity      │
├─────┼────────────
│     │ unit-11-reviews-certificate │ Mutual reviews,       │
│ 11  │ s.md                        │ auto-certificates,    │
│     │                             │ public verify         │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ Payment vs            │
│ 12  │ unit-12-payments.md         │ Transaction, Paymob,  │
│     │                             │ mock flow             │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ REST chat + SignalR   │
│ 13  │ unit-13-chat.md             │ delivery,             │
│     │                             │ find-or-create        │
├─────┼─────────────────────────────┼───────────────────────┤
│     │ unit-14-notifications-signa │ INotifier,            │
│ 14  │ lr.md                       │ NotificationHub, JWT  │
│     │                             │ via query string      │
├─────┼─────────────────────────────┼───────────────────────┤
│     │ unit-15-admin-settings-mast │ Dashboard, banning,   │
│ 15  │ erdata.md                   │ skills/universities/s │
│     │                             │ ettings               │
├─────┼─────────────────────────────┼───────────────────────┤
│     │ unit-16-background-infrastr │ BackupWorker,         │
│ 16  │ ucture.md                   │ seeding, Discord      │
│     │                             │ logging               │
├─────┼─────────────────────────────┼───────────────────────┤
│ 17  │ unit-17-end-to-end-trace.md │ One request traced    │
│     │                             │ through all 7 layers  │
├─────┼─────────────────────────────┼───────────────────────┤
│     │                             │ One-paragraph summary │
│ —   │ curriculum-index.md         │  per unit + quick     │
│     │                             │ reference tables      │
└─────┴─────────────────────────────┴───────────────────────┘

Each unit has "What to Say in Your Defense" talking points and 7 self-check Q&As with full answers — ready for your graduation defense.
