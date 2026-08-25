# Sha8alny — Project Context Document

> **Purpose:** This document is the single source of truth for any AI coding agent working on the Sha8alny codebase. It is strictly factual, derived from the current codebase state as of **May 2026**. Every AI agent MUST read this file before making any code changes.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Tech Stack & Architecture](#2-tech-stack--architecture)
3. [Domain & Database Schema (Current State)](#3-domain--database-schema-current-state)
4. [Completed Capabilities (What Works Right Now)](#4-completed-capabilities-what-works-right-now)
5. [Pending Features & Technical Debt (The Roadmap)](#5-pending-features--technical-debt-the-roadmap)
6. [AI Agent Directives (Strict Coding Rules)](#6-ai-agent-directives-strict-coding-rules)

---

## 1. Project Overview

| Property            | Value                                                                 |
|---------------------|-----------------------------------------------------------------------|
| **Name**            | Sha8alny (شغلني)                                                      |
| **Type**            | Freelancing & Field Training Platform (University Graduation Project) |
| **Core Goal**       | Connect university students with freelance work, internships, and training opportunities from companies |
| **License**         | MIT                                                                   |
| **Repository Root** | `e:\LLM testing\Sha8alny`                                            |
| **Solution File**   | `Sh8lnySolution.sln` (Visual Studio 2022, .NET 9)                    |

### User Roles

| Role         | Description                                                       |
|--------------|-------------------------------------------------------------------|
| **Student**  | Browses opportunities, applies for projects, tracks progress, earns certificates |
| **Company**  | Posts projects/opportunities, manages applications, reviews students, processes payments |
| **Admin**    | Full system oversight — user management, analytics, backups, moderation ("God Mode") |
| **University** | *(Planned)* University representative role for student verification |

### Core Workflow

```
Company Posts Project → Student Applies → Company Reviews Application → Accepted
→ Milestones/Modules Execution → Progress Tracking → Job Completion
→ Payment Processing → Mutual Review → Certificate Generation
```

---

## 2. Tech Stack & Architecture

### Technology Stack

| Layer              | Technology                                                     | Version    |
|--------------------|----------------------------------------------------------------|------------|
| **Runtime**        | .NET (ASP.NET Core Web API)                                    | **9.0**    |
| **ORM**            | Entity Framework Core                                          | 9.x        |
| **Database**       | SQL Server                                                     | 2022       |
| **Authentication** | JWT Bearer Tokens (Symmetric Security Key)                     | —          |
| **Real-time**      | ASP.NET Core SignalR                                           | Built-in   |
| **Object Mapping** | AutoMapper                                                     | —          |
| **Password Hashing** | BCrypt.Net                                                      | —          |
| **Image Processing** | SixLabors.ImageSharp (resize + WebP conversion)               | —          |
| **Virus Scanning** | ClamAV *(currently disabled/stub — always returns clean)*      | —          |
| **Email**          | SMTP via Gmail (MailKit/SmtpClient)                            | —          |
| **Logging**        | Built-in ILogger + Discord Webhook Logger                      | —          |
| **API Docs**       | Swagger / Swashbuckle (served at root `/`)                     | —          |
| **Frontend (Web)** | React                                                          | —          |
| **Frontend (Mobile)** | Flutter                                                      | —          |
| **Deployment**     | Google Cloud Run (Dockerized) + `gcloud CLI`                   | —          |
| **Database (Prod)** | Cloud-hosted SQL Server (databaseasp.net)                      | —          |

### Solution Architecture — Strict Onion Architecture

```
Sh8lnySolution.sln
│
├── Core/                              ← INNER LAYERS (no external dependencies)
│   ├── Sh8lny.Domain/                 ← Entities, Enums, Domain Models (NO logic)
│   ├── Sh8lny.Abstraction/            ← Interfaces: Repositories (IGenericRepository<T>, IUnitOfWork)
│   │                                    and Services (IAuthService, IProjectService, etc.)
│   └── Sh8lny.Service/                ← Business Logic Implementations (AuthService, ProjectService, etc.)
│                                         References: Abstraction + Domain only
│
├── Infrastructure/                    ← OUTER LAYERS (implements abstractions)
│   ├── Sh8lny.Persistence/            ← EF Core DbContext, GenericRepository, UnitOfWork,
│   │                                    Configurations (Fluent API), Migrations, Seeding, MailService
│   └── Sh8lny.Presentation/           ← (Reserved — currently minimal)
│
├── Sh8lny.Web/                        ← COMPOSITION ROOT (API Host)
│   ├── Controllers/                   ← 17 API Controllers
│   ├── Hubs/                          ← SignalR Hub(s)
│   ├── Services/                      ← Web-layer services (SignalRNotifier, BackupWorker)
│   ├── Mappings/                      ← AutoMapper profiles
│   ├── Logging/                       ← Discord webhook logger
│   ├── DTOs/                          ← Web-specific DTOs (if any)
│   └── Program.cs                     ← DI container, middleware pipeline, app startup
│
├── Sh8lny.Shared/                     ← SHARED PROJECT (DTOs, Options, Validation)
│   ├── DTOs/                          ← All Data Transfer Objects (organized by feature)
│   ├── Options/                       ← Configuration POCOs (JwtOptions, MailSettings)
│   └── Validation/                    ← Custom validation attributes
│
└── Tests/
    └── Sh8lny.IntegrationTests/       ← Integration test project (scaffolded, minimal)
```

### Dependency Flow (Onion Rule)

```
Web → Service → Abstraction → Domain
Web → Persistence → Abstraction → Domain
Shared → (no dependencies, referenced by all)
```

**CRITICAL:** Domain layer has ZERO outward dependencies. Abstraction layer depends ONLY on Domain. Service layer depends ONLY on Abstraction + Domain. Persistence implements Abstraction interfaces. Web is the composition root.

### Project References

| Project                | References                                                                 |
|------------------------|---------------------------------------------------------------------------|
| `Sh8lny.Domain`        | *(none — innermost)*                                                      |
| `Sh8lny.Abstraction`   | `Sh8lny.Domain`, `Sh8lny.Shared`                                          |
| `Sh8lny.Service`       | `Sh8lny.Abstraction`, `Sh8lny.Domain`, `Sh8lny.Shared`                    |
| `Sh8lny.Persistence`   | `Sh8lny.Abstraction`, `Sh8lny.Domain`, `Sh8lny.Shared`                    |
| `Sh8lny.Presentation`  | `Sh8lny.Abstraction`                                                      |
| `Sh8lny.Web`           | All above                                                                  |
| `Sh8lny.Shared`        | *(none — standalone)*                                                     |

### DI Registration (in `Program.cs`)

All services are registered as **Scoped**:

```csharp
// Repository layer
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IVirusScanService, ClamAvService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IProjectExecutionService, ProjectExecutionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

// Real-time
builder.Services.AddScoped<INotifier, SignalRNotifier>();

// Background services
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddHostedService<BackupWorker>();

// Field Training
builder.Services.AddScoped<ITrainingSubmissionService, TrainingSubmissionService>();

// App Configuration (Maintenance)
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();

// Announcements
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
```

### Middleware Pipeline Order

```
Swagger → HTTPS Redirect (non-dev) → Static Files → Request Timing Middleware
→ CORS ("AllowAll") → Authentication → Authorization → Controllers → SignalR Hub (/hubs/notifications)
```

---

## 3. Domain & Database Schema (Current State)

### Entity Relationship Overview

```
User (1) ──→ (0..1) Student
User (1) ──→ (0..1) Company
User (1) ──→ (0..1) University
User (1) ──→ (0..1) UserSettings
User (1) ──→ (N) Notification
User (1) ──→ (N) ActivityLog
User (1) ──→ (N) ConversationParticipant
User (1) ──→ (N) Message (as Sender)

Company (1) ──→ (N) Project
Company (1) ──→ (N) Certificate (Issued)
Company (1) ──→ (N) Payment
Company (1) ──→ (N) CompanyReview (written by students)
Company (1) ──→ (N) StudentReview (written by company)
Company (1) ──→ (N) DashboardMetric

Project (1) ──→ (N) Application
Project (1) ──→ (N) ProjectModule
Project (1) ──→ (N) ProjectRequiredSkill
Project (1) ──→ (N) ProjectGroup
Project (1) ──→ (N) Conversation
Project (1) ──→ (N) Certificate
Project (1) ──→ (N) Payment
Project (1) ──→ (N) CompletedOpportunity
Project (1) ──→ (N) SavedOpportunity

Application (1) ──→ (N) ApplicationModuleProgress
Application (1) ──→ (N) TrainingSubmission

Student (1) ──→ (N) Application
Student (1) ──→ (N) StudentSkill
Student (1) ──→ (N) Education
Student (1) ──→ (N) Experience
Student (1) ──→ (N) GroupMember
Student (1) ──→ (N) Certificate
Student (1) ──→ (N) Payment
Student (1) ──→ (N) CompletedOpportunity
Student (1) ──→ (N) CompanyReview (written by student)
Student (1) ──→ (N) StudentReview (received from company)
Student (1) ──→ (N) SavedOpportunity
Student (1) ──→ (N) TrainingSubmission

Conversation (1) ──→ (N) Message
Conversation (1) ──→ (N) ConversationParticipant
```

### Core Entities — Detailed Schema

#### `User`
| Property                 | Type         | Notes                                     |
|--------------------------|--------------|-------------------------------------------|
| `UserID`                 | `int` (PK)   | Auto-increment                            |
| `FirstName`              | `string?`    | Optional at registration                  |
| `LastName`               | `string?`    | Optional at registration                  |
| `Email`                  | `string`     | Required, unique                          |
| `PasswordHash`           | `string`     | BCrypt hashed                             |
| `UserType`               | `UserType`   | Enum: `Student`, `Company`, `University`, `Admin` |
| `IsEmailVerified`        | `bool`       | Email verification flag                   |
| `VerificationCode`       | `string?`    | OTP for email verification                |
| `VerificationCodeExpiry` | `DateTime?`  | Expiration for verification code          |
| `PasswordResetToken`     | `string?`    | Token for forgot-password flow            |
| `ResetTokenExpires`      | `DateTime?`  | Expiration for reset token                |
| `FcmToken`               | `string?`    | FCM push notification device token         |
| `IsActive`               | `bool`       | Default `true`; can be banned/deactivated |
| `CreatedAt`              | `DateTime`   |                                           |
| `LastLoginAt`            | `DateTime?`  |                                           |
| `UpdatedAt`              | `DateTime`   |                                           |

**Navigation:** `Student?`, `Company?`, `University?`, `Settings?`, `ConversationParticipants`, `SentMessages`, `Notifications`, `ActivityLogs`, `VerifiedOpportunities`

#### `Student`
| Property                | Type              | Notes                                    |
|-------------------------|-------------------|------------------------------------------|
| `StudentID`             | `int` (PK)        |                                          |
| `UserID`                | `int` (FK → User) |                                          |
| `FirstName`             | `string`          | Required, denormalized from User         |
| `LastName`              | `string`          | Required, denormalized from User         |
| `Bio`                   | `string?`         |                                          |
| `Phone`                 | `string?`         |                                          |
| `ProfilePicture`        | `string?`         | URL from Media controller                |
| `GitHubProfile`         | `string?`         |                                          |
| **`CvFileUrl`**         | `string?`         | **URL to uploaded CV (PDF/DOCX)**        |
| `UniversityID`          | `int?` (FK)       |                                          |
| `DepartmentID`          | `int?` (FK)       |                                          |
| `AcademicYear`          | `AcademicYear?`   | Enum: `FirstYear` through `FifthYear`    |
| `StudentIDNumber`       | `string?`         | University student ID                    |
| `City`                  | `string?`         |                                          |
| `State`                 | `string?`         |                                          |
| `Country`               | `string`          | Required                                 |
| `ProfileCompleteness`   | `int`             | 0-100 score                              |
| `Status`                | `StudentStatus`   |                                          |
| `AverageRating`         | `decimal`         | Computed from reviews                    |
| `TotalReviews`          | `int`             |                                          |
| **`TotalInternshipDays`** | `int`           | **Cumulative internship/training days**  |
| `CreatedAt`             | `DateTime`        |                                          |
| `UpdatedAt`             | `DateTime`        |                                          |

**Navigation:** `User`, `University?`, `Department?`, `StudentSkills`, `Educations`, `Experiences`, `Applications`, `GroupMemberships`, `Certificates`, `Payments`, `CompletedOpportunities`, `CompanyReviews`, `ReceivedReviews`, `SavedOpportunities`

**Computed:** `FullName => $"{FirstName} {LastName}"`

#### `Company`
| Property        | Type              | Notes                                    |
|-----------------|-------------------|------------------------------------------|
| `CompanyID`     | `int` (PK)        |                                          |
| `UserID`        | `int` (FK → User) |                                          |
| `CompanyName`   | `string`          | Required                                 |
| `CompanyLogo`   | `string?`         | URL from Media controller                |
| `ContactEmail`  | `string`          | Required                                 |
| `ContactPhone`  | `string?`         |                                          |
| `Website`       | `string?`         |                                          |
| `Address`       | `string?`         |                                          |
| `City`          | `string?`         |                                          |
| `State`         | `string?`         |                                          |
| `Country`       | `string?`         |                                          |
| `Industry`      | `string?`         |                                          |
| `Description`   | `string?`         |                                          |
| `AverageRating` | `decimal`         | Computed from reviews                    |
| `TotalReviews`  | `int`             |                                          |
| `CreatedAt`     | `DateTime`        |                                          |
| `UpdatedAt`     | `DateTime`        |                                          |

**Navigation:** `User`, `Projects`, `IssuedCertificates`, `DashboardMetrics`, `Payments`, `Reviews` (CompanyReview), `StudentReviews`

**Note:** Partnership/Verification fields have been intentionally removed from the entity.

#### `Project`
| Property                | Type              | Notes                                    |
|-------------------------|-------------------|------------------------------------------|
| `ProjectID`             | `int` (PK)        |                                          |
| `CompanyID`             | `int` (FK)        |                                          |
| `ProjectName`           | `string`          | Required                                 |
| `ProjectCode`           | `string?`         | Optional reference code                  |
| `Description`           | `string`          | Required                                 |
| `ProjectType`           | `ProjectType?`    | Enum: `Internship`, `GraduationProject`, `Training`, `PartTime`, `FullTime` |
| `StartDate`             | `DateTime?`       |                                          |
| `EndDate`               | `DateTime?`       |                                          |
| `Deadline`              | `DateTime`        | Application deadline                     |
| `Duration`              | `string?`         | Human-readable duration                  |
| `RequiredSkills`        | `string?`         | Legacy free-text field                   |
| `MinAcademicYear`       | `string?`         |                                          |
| `MaxApplicants`         | `int?`            |                                          |
| `Status`                | `ProjectStatus`   | Enum: `Open`, `InProgress`, `Closed`, `Completed`, `Cancelled` |
| `IsVisible`             | `bool`            | Visibility toggle                        |
| `CreatedBy`             | `int`             | UserID of creator                        |
| `CreatedByName`         | `string?`         | Denormalized creator name                |
| `ViewCount`             | `int`             | Analytics                                |
| `ApplicationCount`      | `int`             | Analytics                                |
| `CreatedAt`             | `DateTime`        |                                          |
| `UpdatedAt`             | `DateTime`        |                                          |

**Navigation:** `Company`, `ProjectRequiredSkills`, `Applications`, `ProjectGroups`, `Conversations`, `Certificates`, `Payments`, `CompletedOpportunities`, `Modules` (ProjectModule), `SavedOpportunities`

#### `Application`
| Property               | Type                | Notes                                    |
|------------------------|---------------------|------------------------------------------|
| `ApplicationID`        | `int` (PK)          |                                          |
| `ProjectID`            | `int` (FK)          |                                          |
| `StudentID`            | `int` (FK)          |                                          |
| `CoverLetter`          | `string?`           |                                          |
| `Resume`               | `string`            | Required                                 |
| `PortfolioURL`         | `string?`           |                                          |
| `ProposalFileUrl`      | `string?`           | URL from Media controller                |
| `StudentCvUrl`         | `string?`           | URL from Media controller                |
| `BidAmount`            | `decimal?`          | Student's proposed price                 |
| `Status`               | `ApplicationStatus` | See enum below                           |
| `ReviewedBy`           | `int?`              | UserID of reviewer                       |
| `ReviewedAt`           | `DateTime?`         |                                          |
| `ReviewNotes`          | `string?`           |                                          |
| `CompletedAt`          | `DateTime?`         |                                          |
| `CompanyFeedbackNote`  | `string?`           | Company feedback on completion           |
| `FinalDeliverableUrl`  | `string?`           | URL from Media controller                |
| `IsPaid`               | `bool`              |                                          |
| `PaidAt`               | `DateTime?`         |                                          |
| `AppliedAt`            | `DateTime`          |                                          |

**ApplicationStatus enum:** `Submit`, `Pending`, `UnderReview`, `Accepted`, `InProgress`, `Completed`, `Rejected`, `Withdrawn`

**Navigation:** `Project`, `Student`, `CompletedOpportunity?`, `ModuleProgress` (ApplicationModuleProgress)

#### `ProjectModule` (Milestones)
| Property            | Type           | Notes                                    |
|---------------------|----------------|------------------------------------------|
| `Id`                | `int` (PK)     |                                          |
| `ProjectId`         | `int` (FK)     |                                          |
| `Title`             | `string`       | Required                                 |
| `Description`       | `string?`      |                                          |
| `EstimatedDuration` | `string?`      | Human-readable                           |
| `OrderIndex`        | `int`          | Ordering within project                  |
| `Weight`            | `decimal`      | Percentage (0-100) of total project      |
| `Status`            | `ModuleStatus` | `Pending`, `InProgress`, `Completed`, `Approved`, `Rejected` |

**Navigation:** `Project`, `ModuleProgress` (ApplicationModuleProgress)

#### `ApplicationModuleProgress`
| Property             | Type      | Notes                                    |
|----------------------|-----------|------------------------------------------|
| `Id`                 | `int` (PK)|                                          |
| `ApplicationId`      | `int` (FK)|                                          |
| `ProjectModuleId`    | `int` (FK)|                                          |
| `ProgressPercentage` | `int`     | 0-100                                    |
| `Note`               | `string?` | Progress update note                     |
| `IsCompleted`        | `bool`    |                                          |
| `CompletedAt`        | `DateTime?` |                                        |
| `UpdatedAt`          | `DateTime`|                                          |

#### `SavedOpportunity` (Bookmarking Join Table)
| Property      | Type        | Notes                                    |
|---------------|-------------|------------------------------------------|
| `SavedID`     | `int` (PK)  |                                          |
| `StudentID`   | `int` (FK)  |                                          |
| `ProjectID`   | `int` (FK)  |                                          |
| `SavedAt`     | `DateTime`  |                                          |

**Navigation:** `Student`, `Project`

#### `Payment`
| Property               | Type           | Notes                                    |
|------------------------|----------------|------------------------------------------|
| `PaymentID`            | `int` (PK)     |                                          |
| `ProjectID`            | `int` (FK)     |                                          |
| `StudentID`            | `int` (FK)     |                                          |
| `CompanyID`            | `int?` (FK)    |                                          |
| `Amount`               | `decimal`      |                                          |
| `Currency`             | `string`       | Required (e.g., "EGP")                   |
| `Status`               | `PaymentStatus`|                                          |
| `PaymobOrderId`        | `string?`      | Paymob Order Registration API            |
| `PaymobTransactionId`  | `string?`      | From Paymob webhook                      |
| `GatewayRawResponse`   | `string?`      | Raw JSON for debugging                   |
| `PaymentMethod`        | `PaymentMethod`| `Card`, `Wallet`, `Kiosk`               |
| `Description`          | `string?`      |                                          |
| `CreatedAt`            | `DateTime`     |                                          |
| `PaidAt`               | `DateTime?`    |                                          |
| `UpdatedAt`            | `DateTime`     |                                          |

**Navigation:** `Project`, `Student`, `Company?`

#### `AppConfig` (Singleton — App-Wide Configuration)
| Property              | Type         | Notes                                    |
|-----------------------|--------------|------------------------------------------|
| `Id`                  | `int` (PK)   | Always 1 (singleton row)                 |
| `IsMaintenanceMode`   | `bool`       | When true, mobile blocks entry           |
| `MaintenanceTitle`    | `string`     | Title shown on maintenance screen        |
| `MaintenanceMessage`  | `string`     | Body text on maintenance screen          |
| `MinSupportedVersion` | `string`     | Semver (e.g. "1.0.0") — mobile version gate |
| `UpdatedAt`           | `DateTime`   | Last update timestamp                    |

#### `Transaction`
| Property          | Type        | Notes                                    |
|-------------------|-------------|------------------------------------------|
| `Id`              | `int` (PK)  |                                          |
| `ApplicationId`   | `int` (FK)  |                                          |
| `PayerId`         | `int`       | Company's UserID                         |
| `PayeeId`         | `int`       | Student's UserID                         |
| `Amount`          | `decimal`   |                                          |
| `TransactionDate` | `DateTime`  |                                          |
| `PaymentMethod`   | `string`    | e.g., "Credit Card", "Visa", "Wallet"    |

#### `TrainingSubmission` (Field Training Document Submission)
| Property              | Type                      | Notes                                    |
|-----------------------|---------------------------|------------------------------------------|
| `TrainingSubmissionID`| `int` (PK)                | Auto-increment                           |
| `ApplicationID`       | `int` (FK → Application)  | Associated application                   |
| `StudentID`           | `int` (FK → Student)      | Submitting student                       |
| `CertificateUrl`      | `string?`                 | Training certificate document URL        |
| `ReportUrl`           | `string?`                 | Detailed training report URL             |
| `PresentationUrl`     | `string?`                 | Presentation document URL                |
| `CompanyEvaluationUrl`| `string?`                 | Company evaluation form URL              |
| `StudentSurveyUrl`    | `string?`                 | Student field training survey URL        |
| `Status`              | `TrainingSubmissionStatus`| See enum below                           |
| `IsAdminApproved`     | `bool`                    | Admin academic approval                  |
| `IsCompanyVerified`   | `bool`                    | Company industry verification            |
| `TrainingDays`        | `int?`                    | Days to credit on full completion        |
| `AdminNotes`          | `string?`                 | Admin reviewer notes                     |
| `RejectionReason`     | `string?`                 | Reason for rejection                     |
| `ReviewedByAdminId`   | `int?` (FK → User)        | Admin who reviewed                       |
| `AdminReviewedAt`     | `DateTime?`               | When admin reviewed                      |
| `CompanyVerifiedAt`   | `DateTime?`               | When company verified                    |
| `CompletedAt`         | `DateTime?`               | When fully completed                     |
| `SubmittedAt`         | `DateTime`                | Creation timestamp                       |
| `UpdatedAt`           | `DateTime`                | Last update timestamp                    |

**TrainingSubmissionStatus enum:** `Pending`, `AdminApproved`, `CompanyVerified`, `FullyCompleted`, `Rejected`

**Navigation:** `Application`, `Student`, `ReviewedByAdmin` (User)

**Workflow:** Student submits documents → Admin reviews (approve/reject) → Company verifies → If both approved: Status = FullyCompleted, Student.TotalInternshipDays incremented

#### `Announcement` (Platform-Wide Announcement)
| Property      | Type        | Notes                                    |
|---------------|-------------|------------------------------------------|
| `Id`          | `int` (PK)  | Auto-increment                           |
| `Title`       | `string`    | Required, max 200                        |
| `Description` | `string`    | Required, max 2000                       |
| `ImageUrl`    | `string?`   | Optional, max 1000                       |
| `Link`        | `string?`   | Optional, max 1000                       |
| `CreatedAt`   | `DateTime`  | Creation timestamp                       |
| `UpdatedAt`   | `DateTime?` | Last update timestamp                    |

**Access:** `GET /api/Announcements` is public (AllowAnonymous). Create/update/delete require Admin role.

#### `Certificate`
| Property           | Type        | Notes                                    |
|--------------------|-------------|------------------------------------------|
| `CertificateID`    | `int` (PK)  |                                          |
| `StudentID`        | `int` (FK)  |                                          |
| `ProjectID`        | `int` (FK)  |                                          |
| `CompanyID`        | `int` (FK)  |                                          |
| `CertificateNumber`| `string`    | Required, unique identifier              |
| `CertificateTitle` | `string`    | Required                                 |
| `Description`      | `string?`   |                                          |
| `CertificateURL`   | `string?`   | Generated certificate file URL           |
| `IssuedAt`         | `DateTime`  |                                          |
| `ExpiresAt`        | `DateTime?` |                                          |

**Navigation:** `Student`, `Project`, `Company`, `CompletedOpportunity?`

#### `Conversation`
| Property           | Type               | Notes                                    |
|--------------------|--------------------|------------------------------------------|
| `ConversationID`   | `int` (PK)         |                                          |
| `ConversationType` | `ConversationType` | `Direct`, `Group`                        |
| `GroupID`          | `int?`             | FK to ProjectGroup for group chats       |
| `ConversationName` | `string?`          |                                          |
| `CreatedAt`        | `DateTime`         |                                          |
| `LastMessageAt`    | `DateTime?`        |                                          |

**Navigation:** `Project?`, `Group?`, `Participants`, `Messages`

#### `Message`
| Property          | Type          | Notes                                    |
|-------------------|---------------|------------------------------------------|
| `MessageID`       | `int` (PK)    |                                          |
| `ConversationID`  | `int` (FK)    |                                          |
| `SenderID`        | `int` (FK)    | FK to User                               |
| `MessageText`     | `string`      | Required                                 |
| `MessageType`     | `MessageType` | `Text`, `File`, `Image`, `Link`          |
| `AttachmentURL`   | `string?`     |                                          |
| `AttachmentName`  | `string?`     |                                          |
| `IsRead`          | `bool`        |                                          |
| `IsEdited`        | `bool`        |                                          |
| `SentAt`          | `DateTime`    |                                          |
| `EditedAt`        | `DateTime?`   |                                          |

**Navigation:** `Conversation`, `Sender` (User)

#### `Notification`
| Property              | Type               | Notes                                    |
|-----------------------|--------------------|------------------------------------------|
| `NotificationID`      | `int` (PK)         |                                          |
| `UserID`              | `int` (FK)         | Recipient                                |
| `NotificationType`    | `NotificationType` | (enum defined in model)                  |
| `Title`               | `string`           | Required                                 |
| `Message`             | `string`           | Required                                 |
| `RelatedProjectID`    | `int?`             | Deep link context                        |
| `RelatedApplicationID`| `int?`             | Deep link context                        |
| `ActionURL`           | `string?`          | Deep link URL                            |
| `IsRead`              | `bool`             |                                          |
| `CreatedAt`           | `DateTime`         |                                          |
| `ReadAt`              | `DateTime?`        |                                          |

**Navigation:** `User`

#### `StudentReview` (Company reviews Student)
| Property                   | Type        | Notes                                    |
|----------------------------|-------------|------------------------------------------|
| `ReviewID`                 | `int` (PK)  |                                          |
| `StudentID`                | `int` (FK)  |                                          |
| `CompanyID`                | `int` (FK)  |                                          |
| `CompletedOpportunityID`   | `int?` (FK) |                                          |
| `ProjectID`                | `int?` (FK) |                                          |
| `ApplicationID`            | `int?` (FK) |                                          |
| `Rating`                   | `decimal`   | Overall rating                           |
| `ReviewTitle`              | `string?`   |                                          |
| `ReviewText`               | `string?`   |                                          |
| `TechnicalSkillsRating`    | `decimal?`  | Breakdown                                |
| `CommunicationRating`      | `decimal?`  | Breakdown                                |
| `ProfessionalismRating`    | `decimal?`  | Breakdown                                |
| `TimeManagementRating`     | `decimal?`  | Breakdown                                |
| `TeamworkRating`           | `decimal?`  | Breakdown                                |
| `ProblemSolvingRating`     | `decimal?`  | Breakdown                                |
| `WouldHireAgain`           | `bool`      |                                          |
| `Strengths`                | `string?`   |                                          |
| `AreasForImprovement`      | `string?`   |                                          |
| `Status`                   | `ReviewStatus` | `Approved`, `Rejected`               |
| `IsVerified`               | `bool`      |                                          |
| `IsPublic`                 | `bool`      |                                          |
| `CreatedAt`                | `DateTime`  |                                          |
| `UpdatedAt`                | `DateTime?` |                                          |
| `StudentResponse`          | `string?`   | Student can respond                      |
| `StudentRespondedAt`       | `DateTime?` |                                          |

#### `CompanyReview` (Student reviews Company)
| Property                       | Type        | Notes                                    |
|--------------------------------|-------------|------------------------------------------|
| `ReviewID`                     | `int` (PK)  |                                          |
| `CompanyID`                    | `int` (FK)  |                                          |
| `StudentID`                    | `int` (FK)  |                                          |
| `CompletedOpportunityID`       | `int?` (FK) |                                          |
| `ProjectID`                    | `int?` (FK) |                                          |
| `ApplicationID`                | `int?` (FK) |                                          |
| `Rating`                       | `decimal`   | Overall rating                           |
| `ReviewTitle`                  | `string?`   |                                          |
| `ReviewText`                   | `string?`   |                                          |
| `WorkEnvironmentRating`        | `decimal?`  | Breakdown                                |
| `LearningOpportunityRating`    | `decimal?`  | Breakdown                                |
| `MentorshipRating`             | `decimal?`  | Breakdown                                |
| `CompensationRating`           | `decimal?`  | Breakdown                                |
| `CommunicationRating`          | `decimal?`  | Breakdown                                |
| `WouldRecommend`               | `bool`      |                                          |
| `Pros`                         | `string?`   |                                          |
| `Cons`                         | `string?`   |                                          |
| `Status`                       | `ReviewStatus` | `Approved`, `Rejected`               |
| `IsVerified`                   | `bool`      |                                          |
| `IsAnonymous`                  | `bool`      |                                          |
| `CreatedAt`                    | `DateTime`  |                                          |
| `UpdatedAt`                    | `DateTime?` |                                          |
| `CompanyResponse`              | `string?`   | Company can respond                      |
| `CompanyRespondedAt`           | `DateTime?` |                                          |

#### Supporting Entities

| Entity                     | Purpose                                                |
|----------------------------|--------------------------------------------------------|
| `Skill`                    | Lookup table of skills (with `SkillCategory` enum)     |
| `StudentSkill`             | Join table: Student ↔ Skill (with proficiency level)   |
| `ProjectRequiredSkill`     | Join table: Project ↔ Skill                            |
| `Education`                | Student's education history                            |
| `Experience`               | Student's work experience                              |
| `University`               | Lookup table of universities                           |
| `Department`               | Lookup table of academic departments                   |
| `ProjectGroup`             | Team groups within a project                           |
| `GroupMember`              | Join table: Group ↔ Student (with role)                |
| `ConversationParticipant`  | Join table: Conversation ↔ User                        |
| `ActivityLog`              | Audit trail of user actions                            |
| `DashboardMetric`          | Daily platform-wide statistics snapshot                |
| `UserSettings`             | User preferences (notifications, language, privacy)    |
| `CompletedOpportunity`     | Historical record of finished jobs/internships         |
| `AppConfig`                | Singleton: maintenance mode, min version gate          |

### Migration History

| Migration                                                  | Date         | Description                                    |
|------------------------------------------------------------|--------------|------------------------------------------------|
| `20251207020220_InitialCreation`                           | 2025-12-07   | Initial database schema                        |
| `20260126215341_UpdateModels`                              | 2026-01-26   | Model updates and refinements                  |
| `20260214032853_AddPasswordResetFields`                    | 2026-02-14   | Forgot-password flow fields on User            |
| `20260221205827_AlignDashboardMetricsSchema`               | 2026-02-21   | Dashboard metric schema alignment              |
| `20260328195323_FixPaymentForeignKeysAndDecimalPrecision`  | 2026-03-28   | Payment FK fixes and decimal precision         |
| `20260329161809_AddInternshipDays`                         | 2026-03-29   | `TotalInternshipDays` on Student               |
| `20260422183336_SyncPendingModelChanges`                   | 2026-04-22   | Sync pending model changes                     |
| `20260423130812_AddSavedProjectsAndReviews`                | 2026-04-23   | `SavedOpportunity`, `StudentReview`, `CompanyReview` tables |
| `AddFcmTokenToUser`                                       | Pending      | `FcmToken` column on `User` table                         |
| `AddAppConfig`                                            | Pending      | `AppConfigs` table for maintenance/version config          |

### Recent Architectural Changes (Verified in Codebase)

1. **`Project` does NOT have `ExpectedDurationInDays` as a database column.** The duration is stored as a free-text `Duration` string field. Any "expected duration in days" logic should be computed or added as a new column if needed.
2. **`Student.TotalInternshipDays`** — Present as `int`, tracks cumulative internship/training days.
3. **`Student.CvFileUrl`** — Present as `string?`, stores URL returned by the `/api/Media` upload endpoint.
4. **`SavedOpportunity`** — Dedicated join table for bookmarking (Student ↔ Project), NOT a collection on Project.
5. **Module reviews** — `ProjectModule.Status` supports `Approved` and `Rejected` statuses. `ApplicationModuleProgress` tracks per-application progress.
6. **Reviews** — Both `StudentReview` and `CompanyReview` have a `Status` field (`ReviewStatus` enum with `Approved`/`Rejected`), plus company/student response fields.

---

## 4. Completed Capabilities (What Works Right Now)

### 4.1 Authentication & Authorization (`/api/Auth`)

| Endpoint                       | Method | Auth    | Description                                    |
|--------------------------------|--------|---------|------------------------------------------------|
| `/api/Auth/register`           | POST   | Public  | Register new user (Student/Company/Admin)      |
| `/api/Auth/login`              | POST   | Public  | Authenticate, returns JWT token                |
| `/api/Auth/me`                 | GET    | Auth    | Get current user summary                       |
| `/api/Auth/forgot-password`    | POST   | Public  | Send password reset code via email             |
| `/api/Auth/reset-password`     | POST   | Public  | Reset password with token                      |
| `/api/Auth/verify-email`       | POST   | Public  | Verify email with OTP code                     |
| `/api/Auth/fcm-token`          | PUT    | Auth    | Update FCM push notification device token       |
| `/api/Auth/change-password`    | POST   | Auth    | Change password for authenticated user          |

**JWT Configuration:**
- Issuer: `Sha8alny`
- Audience: `Sha8alnyUsers`
- Token Lifetime: 60 minutes
- Claims: `NameIdentifier` (UserID), `Email`, `Role` (UserType)
- SignalR support: Token read from query string `access_token` for WebSocket connections

### 4.2 Student Profile Management (`/api/students`)

| Endpoint                         | Method | Auth   | Description                                    |
|----------------------------------|--------|--------|------------------------------------------------|
| `/api/students/profile`          | POST   | Student | Create complete student profile               |
| `/api/students/profile`          | GET    | Student | Get own profile                               |
| `/api/students/profile`          | PUT    | Student | Update own profile                            |
| `/api/students/{id}`             | GET    | Auth   | Get student profile by ID                     |
| `/api/students/search`           | GET    | Auth   | Search students with filters                  |
| `/api/students/saved-projects`   | GET    | Student | Get bookmarked projects                      |
| `/api/students/saved-projects`   | POST   | Student | Bookmark a project                           |
| `/api/students/saved-projects/{id}` | DELETE | Student | Remove bookmark                          |

### 4.3 Company Profile Management (`/api/companies`)

| Endpoint                         | Method | Auth    | Description                                    |
|----------------------------------|--------|---------|------------------------------------------------|
| `/api/companies/profile`         | POST   | Company | Create or update company profile               |
| `/api/companies/profile`         | GET    | Company | Get own profile                               |
| `/api/companies/{id}`            | GET    | Auth    | Get company profile by ID                     |
| `/api/companies/search`          | GET    | Auth    | Search companies with filters                 |

### 4.4 Project/Opportunity Management (`/api/Projects`)

| Endpoint                         | Method | Auth    | Description                                    |
|----------------------------------|--------|---------|------------------------------------------------|
| `/api/Projects`                  | POST   | Company | Create new project/opportunity                |
| `/api/Projects/{id}`             | PUT    | Company | Update project                                |
| `/api/Projects/{id}`             | DELETE | Company | Delete project                                |
| `/api/Projects/{id}`             | GET    | Public  | Get project by ID                             |
| `/api/Projects`                  | GET    | Public  | List/search projects with filters & pagination|
| `/api/Projects/my-projects`      | GET    | Company | Get company's own projects                    |

### 4.5 Application Flow (`/api/Applications`)

| Endpoint                                    | Method | Auth    | Description                              |
|---------------------------------------------|--------|---------|------------------------------------------|
| `/api/Applications/apply`                   | POST   | Student | Submit application for a project         |
| `/api/Applications/{id}`                    | GET    | Auth    | Get application details                  |
| `/api/Applications/project/{projectId}`     | GET    | Company | Get all applications for a project       |
| `/api/Applications/my-applications`         | GET    | Student | Get student's own applications           |
| `/api/Applications/{id}/review`             | PUT    | Company | Review (accept/reject) an application    |
| `/api/Applications/{id}/status`             | PUT    | Auth    | Update application status                |

### 4.6 Execution & Module Management (`/api/Execution`)

| Endpoint                                            | Method | Auth    | Description                              |
|-----------------------------------------------------|--------|---------|------------------------------------------|
| `/api/Execution/project/{projectId}/modules`        | POST   | Company | Add module/milestone to project          |
| `/api/Execution/project/{projectId}/modules`        | GET    | Auth    | Get all modules for a project            |
| `/api/Execution/modules/{moduleId}`                 | DELETE | Company | Delete a module                          |
| `/api/Execution/modules/{moduleId}/progress`        | PUT    | Student | Update progress on a module              |
| `/api/Execution/modules/{moduleId}/review`          | PUT    | Company | Review a module (approve/reject)         |
| `/api/Execution/application/{applicationId}/progress` | GET  | Auth    | Get progress for an application          |
| `/api/Execution/application/{applicationId}/complete` | POST | Company | Mark job as complete                    |
| `/api/Execution/application/{applicationId}/summary`  | GET  | Auth    | Get completion summary                  |

### 4.7 Chat / Messaging (`/api/Chat`)

| Endpoint                         | Method | Auth | Description                                    |
|----------------------------------|--------|------|------------------------------------------------|
| `/api/Chat/send`                 | POST   | Auth | Send a message                                 |
| `/api/Chat/conversations`        | GET    | Auth | Get all conversations for current user         |
| `/api/Chat/conversations/{id}`   | GET    | Auth | Get conversation with messages                 |
| `/api/Chat/conversations/{id}/messages` | GET | Auth | Get paginated messages for a conversation |
| `/api/Chat/conversations/{id}/read` | PUT  | Auth | Mark conversation as read                      |

**Note:** `POST /api/Chat/send` accepts a `ReceiverId` and **automatically finds or creates**
a Direct conversation between the two users. There is no need for a separate "create conversation"
endpoint — just send the first message to any `ReceiverId`.

**Note:** Chat is currently REST-based. Real-time delivery via SignalR is partially implemented (see Section 5).

### 4.8 User Search (`/api/users`)

| Endpoint                         | Method | Auth | Description                              |
|----------------------------------|--------|------|------------------------------------------|
| `/api/users/search`             | GET    | Auth | Search users by name/email (for chat)   |

**Query parameters:** `query` (string, min 2 chars), `excludeSelf` (bool, default true).
Returns top 20 results with `UserId`, `FullName`, `UserType`, `ProfilePictureUrl`.
Searches across Students (by name), Companies (by name), and all Users (by email).

### 4.9 Notifications (`/api/Notifications`)

| Endpoint                              | Method | Auth | Description                              |
|---------------------------------------|--------|------|------------------------------------------|
| `/api/Notifications`                  | GET    | Auth | Get all notifications for current user   |
| `/api/Notifications/unread-count`     | GET    | Auth | Get unread notification count            |
| `/api/Notifications/{id}/read`        | PUT    | Auth | Mark notification as read                |
| `/api/Notifications/read-all`         | PUT    | Auth | Mark all notifications as read           |

### 4.10 Reviews (`/api/Reviews`)

| Endpoint                         | Method | Auth    | Description                              |
|----------------------------------|--------|---------|------------------------------------------|
| `/api/Reviews/student`           | POST   | Company | Review a student after job completion    |
| `/api/Reviews/company`           | POST   | Student | Review a company after job completion    |
| `/api/Reviews/student/{id}`      | GET    | Public  | Get reviews for a student                |
| `/api/Reviews/company/{id}`      | GET    | Public  | Get reviews for a company                |

**Review statuses:** `Approved` / `Rejected` — Company feedback is supported.

### 4.11 Certificates (`/api/Certificates`)

| Endpoint                              | Method | Auth    | Description                              |
|---------------------------------------|--------|---------|------------------------------------------|
| `/api/Certificates/my-certificates`   | GET    | Student | Get all certificates for current student |
| `/api/Certificates/verify/{uniqueId}` | GET    | Public  | Verify a certificate by unique ID        |

### 4.12 Payments (`/api/Payments`)

| Endpoint                         | Method | Auth    | Description                              |
|----------------------------------|--------|---------|------------------------------------------|
| `/api/Payments/pay`              | POST   | Company | Process payment to student               |
| `/api/Payments/history`          | GET    | Auth    | Get payment history                      |
| `/api/Payments/{id}`             | GET    | Auth    | Get payment details                      |

**Payment Gateway:** Paymob integration (Order Registration + Webhook flow). `PaymentMethod` supports `Card`, `Wallet`, `Kiosk`.

### 4.13 Media / File Uploads (`/api/Media`)

| Endpoint                          | Method | Auth | Description                              |
|-----------------------------------|--------|------|------------------------------------------|
| `/api/Media/upload/profile`       | POST   | Auth | Upload profile picture                   |
| `/api/Media/upload/logo`          | POST   | Auth | Upload company logo                      |
| `/api/Media/upload/project`       | POST   | Auth | Upload project attachment                |
| `/api/Media/upload/certificate`   | POST   | Auth | Upload certificate image                 |
| `/api/Media/upload?folder=xxx`    | POST   | Auth | Generic upload with folder specification |
| `/api/Media?filePath=xxx`         | DELETE | Auth | Delete a previously uploaded file        |

**CRITICAL ARCHITECTURE RULE:**
- File uploads are handled **exclusively** by `/api/Media`.
- All other entities (Student, Company, Application, Certificate, etc.) only store the **resulting URL string** returned by the Media controller.
- **NEVER** use `IFormFile` in domain DTOs or service interfaces.
- Files are stored in `wwwroot/uploads/{folder}/`.
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.gif`, `.pdf`. Max size: 5 MB.
- Images are automatically resized (max 1920px wide) and converted to WebP format.
- Thumbnails (300px) are generated for images.
- Virus scanning via ClamAV is **currently disabled** (stub always returns `true`).

### 4.14 Master Data (`/api/MasterData`)

| Endpoint                         | Method | Auth | Description                              |
|----------------------------------|--------|------|------------------------------------------|
| `/api/MasterData/skills`         | GET    | Public | List all skills for dropdowns           |
| `/api/MasterData/skills`         | POST   | Admin  | Create a new skill                      |
| `/api/MasterData/skills/{id}`    | PUT    | Admin  | Update a skill                          |
| `/api/MasterData/skills/{id}`    | DELETE | Admin  | Delete a skill                          |
| `/api/MasterData/universities`   | GET    | Public | List all universities                   |
| `/api/MasterData/departments`    | GET    | Public | List all departments                    |

### 4.15 Admin Dashboard (`/api/Admin`)

| Endpoint                         | Method | Auth  | Description                              |
|----------------------------------|--------|-------|------------------------------------------|
| `/api/Admin/stats`               | GET    | Admin | Get dashboard statistics                 |
| `/api/Admin/users`               | GET    | Admin | Get all users for management             |
| `/api/Admin/users/{id}/ban`      | PUT    | Admin | Ban a user                               |
| `/api/Admin/users/{id}/activate` | PUT    | Admin | Activate a banned user                   |
| `/api/Admin/metrics`             | GET    | Admin | Get historical dashboard metrics         |

### 4.16 User Settings (`/api/Settings`)

| Endpoint                         | Method | Auth | Description                              |
|----------------------------------|--------|------|------------------------------------------|
| `/api/Settings`                  | GET    | Auth | Get current user's settings              |
| `/api/Settings`                  | PUT    | Auth | Update user settings                     |

### 4.17 Maintenance (`/api/Maintenance`)

| Endpoint                         | Method | Auth  | Description                              |
|----------------------------------|--------|-------|------------------------------------------|
| `/api/Maintenance/backup`        | POST   | Admin | Trigger on-demand database backup        |
| `/api/Maintenance/config`        | GET    | Public | Get app configuration (maintenance, version) |
| `/api/Maintenance/config`        | PUT    | Admin  | Update app configuration                 |

### 4.18 Field Training Submissions (`/api/TrainingSubmissions`)

| Endpoint                                          | Method | Auth           | Description                                    |
|---------------------------------------------------|--------|----------------|------------------------------------------------|
| `/api/TrainingSubmissions`                        | POST   | Student        | Submit training documents (URLs from /api/Media) |
| `/api/TrainingSubmissions/{id}`                   | GET    | Auth           | Get submission status                          |
| `/api/TrainingSubmissions/my`                     | GET    | Student        | Student's own submissions                      |
| `/api/TrainingSubmissions/{id}/admin-review`      | PUT    | Admin/University | Admin academic approval (approve/reject)       |
| `/api/TrainingSubmissions/{id}/company-verify`    | PUT    | Company        | Company industry verification                  |
| `/api/TrainingSubmissions/pending-admin`          | GET    | Admin/University | Admin review queue                            |
| `/api/TrainingSubmissions/pending-company`        | GET    | Company        | Company verify queue                           |

**Dual-Approval Workflow:**
1. Student submits training documents → Status = `Pending`
2. Admin reviews → If approved: Status = `AdminApproved`, If rejected: Status = `Rejected`
3. Company verifies → Status = `CompanyVerified`
4. If both Admin approved AND Company verified → Status = `FullyCompleted`, Student.TotalInternshipDays incremented

### 4.19 Announcements (`/api/Announcements`)

| Endpoint                      | Method | Auth    | Description                              |
|-------------------------------|--------|---------|------------------------------------------|
| `/api/Announcements`          | GET    | Public  | Get all announcements (newest first)     |
| `/api/Announcements`          | POST   | Admin   | Create a new announcement                |
| `/api/Announcements/{id}`     | PUT    | Admin   | Update an existing announcement          |
| `/api/Announcements/{id}`     | DELETE | Admin   | Delete an announcement                   |

**Note:** `GET /api/Announcements` is `AllowAnonymous` — the mobile home screen reads this without authentication.

### 4.20 Real-time (SignalR)

| Hub Endpoint                | Auth | Events                                        |
|-----------------------------|------|-----------------------------------------------|
| `/hubs/notifications`       | Yes  | `ReceiveNotification`, `ReceiveMessage`       |
|                             |      | `JoinGroup(groupName)`, `LeaveGroup(groupName)` |

**SignalR Implementation:**
- `NotificationHub` — Authorised hub at `/hubs/notifications`
- `SignalRNotifier` — Implements `INotifier` interface (injected as Scoped)
- Methods: `SendNotificationAsync`, `SendNotificationToManyAsync`, `SendMessageToUserAsync`
- Failure in real-time delivery is **non-blocking** (logged, not thrown)

### 4.20 Infrastructure

| Feature                  | Status   | Details                                        |
|--------------------------|----------|------------------------------------------------|
| **Database Backups**     | ✅ Active | `BackupWorker` background service (24h interval, 7-day retention) |
| **Discord Logging**      | ✅ Active | Webhook-based error/info logging to Discord    |
| **Request Timing**       | ✅ Active | Middleware logs HTTP method, path, status, elapsed ms |
| **Database Seeding**     | ✅ Active | `DbInitializer.SeedAsync` on startup — seeds Skills, Universities, demo data |
| **Auto-Migration**       | ✅ Active | `context.Database.MigrateAsync()` on startup   |
| **Static Files**         | ✅ Active | `wwwroot/` served via `UseStaticFiles()`       |
| **CORS**                 | ✅ Active | "AllowAll" policy (all origins, methods, headers, credentials) |

---

## 5. Pending Features & Technical Debt (The Roadmap)

### 5.1 Real-time Chat & Notifications Upgrade

**Current State:**
- Chat messages are sent via REST (`POST /api/Chat/send`) and **immediately pushed to the receiver via SignalR**.
- `NotificationHub` exists at `/hubs/notifications` and handles both `ReceiveNotification` and `ReceiveMessage` events.
- `SignalRNotifier` implements `INotifier` with `SendMessageToUserAsync` — called from `ChatService.SendMessageAsync` after DB save.
- SignalR JWT auth reads `access_token` from query string for WebSocket connections.
- Mobile connects to `/hubs/notifications` and listens for `ReceiveMessage` events.

**What's Still Missing:**
- Typing indicators, read receipts, online presence.
- Push notification integration for mobile (FCM/APNs) for offline users.

### 5.2 CI/CD Pipeline

**Current State:** No CI/CD pipeline exists. Manual deployment via `gcloud CLI` with Docker.

**Required:**
- GitHub Actions workflow for:
  - Build & restore on PR
  - Run automated tests
  - Docker image build & push to Container Registry
  - Deploy to Google Cloud Run
- Environment-specific configuration (staging vs production)

### 5.3 Automated Testing Suite

**Current State:** `Tests/Sh8lny.IntegrationTests` project exists but is scaffolded with minimal tests.

**Required:**
- **Unit Tests** (xUnit + Moq):
  - Service layer tests for all business logic
  - Repository pattern tests with in-memory database
- **Integration Tests:**
  - API endpoint tests with `WebApplicationFactory`
  - Database integration tests
- **Test Coverage Target:** 70%+ for service layer

### 5.4 Infrastructure Expansion

| Item                   | Current           | Target                                    |
|------------------------|-------------------|-------------------------------------------|
| **Caching**            | None              | Redis for session, frequently accessed data (skills, universities) |
| **Load Testing**       | None              | k6 or NBomber for concurrent user simulation |
| **Telemetry**          | Discord webhook   | Application Insights or Grafana + Prometheus dashboards |
| **Rate Limiting**      | None              | ASP.NET Core rate limiting middleware     |
| **Health Checks**      | None              | `/health` endpoint for Cloud Run          |
| **API Versioning**     | None              | `v1`, `v2` support                        |
| **ClamAV**             | Disabled (stub)   | Re-enable with containerized ClamAV in docker-compose |

### 5.5 Code Quality & Cleanup

- **Remove** commented-out code blocks (e.g., `CompanyStatus`, `TimePreference`, `IsVerified` in Certificate).
- **Standardise** response format across all controllers (some return `ServiceResponse<T>`, others return raw `IActionResult`).
- **Add** global exception handling middleware.
- **Add** request validation (FluentValidation or DataAnnotations consistency).
- **Implement** pagination consistently across all list endpoints.
- **Add** API rate limiting to prevent abuse.

### 5.6 Feature Gaps

| Feature                        | Status          | Notes                                        |
|--------------------------------|-----------------|----------------------------------------------|
| Password change (authenticated)| ✅ Implemented | `POST /api/Auth/change-password`                |
| Profile picture cropping       | Not implemented | Resize exists, no client-side crop           |
| File type expansion            | Partial         | Only `.jpg/.jpeg/.png/.gif/.pdf` allowed     |
| Email templates                | Not implemented | Plain text emails only                       |
| Analytics dashboard (charts)   | Backend ready   | `DashboardMetric` exists; frontend charts needed |
| University verification flow   | Not implemented | `University` role exists but no workflow     |
| Multi-language support         | Not implemented | `UserSettings.Language` field exists          |
| Notification preferences       | Schema ready    | `UserSettings` has toggle fields; not enforced|

---

## 6. AI Agent Directives (Strict Coding Rules)

> **Any AI agent modifying this codebase MUST follow these rules without exception.**

### Rule 1: Never Break Onion Architecture Dependency Rules

```
✅ ALLOWED:
  Sh8lny.Web          → references → everything
  Sh8lny.Service      → references → Abstraction, Domain, Shared
  Sh8lny.Persistence  → references → Abstraction, Domain, Shared
  Sh8lny.Abstraction  → references → Domain, Shared
  Sh8lny.Domain       → references → nothing (or Shared only)

❌ FORBIDDEN:
  Sh8lny.Domain       → references → Abstraction, Service, Persistence, Web
  Sh8lny.Abstraction  → references → Service, Persistence, Web
  Sh8lny.Service      → references → Persistence, Web
```

- Domain models MUST NOT have any dependency on EF Core, HTTP abstractions, or infrastructure concerns.
- Service interfaces (`I*Service`) go in `Sh8lny.Abstraction/Services/`.
- Repository interfaces (`IGenericRepository<T>`, `IUnitOfWork`) go in `Sh8lny.Abstraction/Repositories/`.
- Service implementations go in `Sh8lny.Service/`.
- Repository and DbContext implementations go in `Sh8lny.Persistence/`.

### Rule 2: Always Use `IQueryable` and `.Include()` for Complex Reads

```csharp
// ✅ CORRECT — eager load navigation properties
var project = await _unitOfWork.Projects
    .GetQueryable()                          // returns IQueryable<Project>
    .Include(p => p.Company)
    .Include(p => p.Applications)
        .ThenInclude(a => a.Student)
    .Include(p => p.Modules)
    .Include(p => p.ProjectRequiredSkills)
        .ThenInclude(prs => prs.Skill)
    .FirstOrDefaultAsync(p => p.ProjectID == id);

// ❌ WRONG — will cause NullReferenceException on navigation properties
var project = await _unitOfWork.Projects.GetByIdAsync(id);
var companyName = project.Company.CompanyName; // 💥 NullReferenceException
```

- Always use `.Include()` for navigation properties that will be accessed.
- Use `.ThenInclude()` for nested navigation properties.
- Return `IQueryable<T>` from repositories for flexible querying.

### Rule 3: Never Use Raw `IFormFile` in Domain DTOs or Service Interfaces

```csharp
// ✅ CORRECT — DTOs use string URLs
public class CreateStudentProfileDto
{
    public string? ProfilePictureUrl { get; set; }  // URL from /api/Media
    public string? CvFileUrl { get; set; }          // URL from /api/Media
}

// ❌ WRONG — IFormFile in DTOs breaks Onion Architecture
public class CreateStudentProfileDto
{
    public IFormFile ProfilePicture { get; set; }   // ❌ IFormFile is HTTP-specific
}
```

**File upload flow:**
1. Client uploads file to `/api/Media/upload/profile` (or appropriate endpoint).
2. Media controller saves file to `wwwroot/uploads/{folder}/` and returns a URL string.
3. Client passes the returned URL string to the relevant entity endpoint (e.g., `/api/students/profile`).
4. Entity endpoints and services only deal with string URLs, never raw files.

### Rule 4: Always Output EF Core Migration Commands When Domain Entities Change

When a domain model is modified (new property, changed type, new entity, etc.), the AI agent MUST:

1. **Identify** the change and its impact on the database schema.
2. **Output** the exact migration command for the developer to run:

```bash
# From the Sh8lny.Persistence project directory:
dotnet ef migrations add <DescriptiveMigrationName> --startup-project ../Sh8lny.Web

# Example:
dotnet ef migrations add AddExpectedDurationInDaysToProject --startup-project ../Sh8lny.Web
```

3. **NEVER** create migration files manually — always instruct the developer to use `dotnet ef`.
4. **ALWAYS** check if the migration would cause data loss and warn about it.

### Rule 5: Use `ServiceResponse<T>` for All Service Return Types

```csharp
// ✅ CORRECT
public async Task<ServiceResponse<int>> CreateProjectAsync(int userId, CreateProjectDto dto)
{
    // ... business logic
    return ServiceResponse<int>.Success(project.ProjectID, "Project created successfully.");
}

// ❌ WRONG — returning raw types
public async Task<int> CreateProjectAsync(int userId, CreateProjectDto dto)
{
    return project.ProjectID;  // No success/failure indication
}
```

### Rule 6: Extract User ID from JWT Claims Correctly

```csharp
// ✅ CORRECT pattern used across all controllers
private int? GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
    
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        return null;
    
    return userId;
}
```

### Rule 7: Respect Role-Based Authorization

| Endpoint Category     | Required Role | Notes                        |
|-----------------------|---------------|------------------------------|
| Profile creation      | Respective    | Student endpoints → Student  |
| Project CRUD          | Company       | Only companies post projects |
| Application submit    | Student       | Only students apply          |
| Application review    | Company       | Only companies review        |
| Module management     | Company       | Company creates modules      |
| Progress updates      | Student       | Student updates progress     |
| Payment processing    | Company       | Company pays student         |
| Admin operations      | Admin         | Full access                  |
| Public endpoints      | `[AllowAnonymous]` | Browse, search, verify  |

### Rule 8: Database Conventions

- **Primary Keys:** `{EntityName}ID` (e.g., `StudentID`, `ProjectID`) — except for `ProjectModule`, `ApplicationModuleProgress`, `Transaction` which use `Id`.
- **Foreign Keys:** `{RelatedEntity}ID` (e.g., `CompanyID`, `UserID`).
- **Timestamps:** All entities have `CreatedAt` and most have `UpdatedAt`.
- **Soft Deletes:** Not implemented. Deletions are hard deletes.
- **Configuration:** All EF configurations are in `Infrastructure/Sh8lny.Persistence/Configurations/` using Fluent API.
- **DbContext:** `Sha8lnyDbContext` in `Infrastructure/Sh8lny.Persistence/Contexts/`.

### Rule 9: File & Folder Naming Conventions

```
Controllers:    {Feature}Controller.cs       (e.g., ProjectsController.cs)
Services:       {Feature}Service.cs          (e.g., ProjectService.cs)
Interfaces:     I{Feature}Service.cs         (e.g., IProjectService.cs)
DTOs:           {Action}{Feature}Dto.cs      (e.g., CreateProjectDto.cs, ProjectResponseDto.cs)
Models:         {EntityName}.cs              (e.g., Project.cs, Student.cs)
Configurations: {EntityName}Configuration.cs (e.g., ProjectConfiguration.cs)
```

### Rule 10: Error Handling Patterns

```csharp
// Services should catch exceptions and return failure responses
try
{
    // ... business logic
    return ServiceResponse<T>.Success(data);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in {MethodName}", nameof(MethodName));
    return ServiceResponse<T>.Failure("An unexpected error occurred.");
}
```

- Real-time delivery failures (SignalR) MUST NOT throw — log and continue.
- Background service failures (BackupWorker) MUST NOT crash the application.

### Rule 11: When Adding a New Entity

1. Create the model in `Core/Sh8lny.Domain/Models/`.
2. Create the EF configuration in `Infrastructure/Sh8lny.Persistence/Configurations/`.
3. Add `DbSet<T>` to `Sha8lnyDbContext`.
4. Add repository property to `IUnitOfWork` interface and `UnitOfWork` implementation.
5. Create DTOs in `Sh8lny.Shared/DTOs/{Feature}/`.
6. Create service interface in `Core/Sh8lny.Abstraction/Services/`.
7. Create service implementation in `Core/Sh8lny.Service/`.
8. Register service in `Program.cs` DI container.
9. Create controller in `Sh8lny.Web/Controllers/`.
10. Generate EF migration with `dotnet ef migrations add`.

### Rule 12: Never Hardcode Secrets

- JWT key, SMTP password, connection strings, and webhook URLs come from:
  - `appsettings.json` (local development)
  - `cloudrun-env.yaml` (production environment variables)
- **NEVER** commit real secrets to source control.
- Use `IConfiguration` / `IOptions<T>` pattern for all configuration access.

---

## Appendix A: Project File Structure Reference

```
Sh8lnySolution.sln
├── Core/
│   ├── Sh8lny.Domain/
│   │   ├── Sh8lny.Domain.csproj
│   │   └── Models/
│   │       ├── ActivityLog.cs
│   │       ├── Announcement.cs
│   │       ├── AppConfig.cs
│   │       ├── Application.cs
│   │       ├── ApplicationModuleProgress.cs
│   │       ├── Certificate.cs
│   │       ├── Company.cs
│   │       ├── CompanyReview.cs
│   │       ├── CompletedOpportunity.cs
│   │       ├── Conversation.cs
│   │       ├── ConversationParticipant.cs
│   │       ├── DashboardMetric.cs
│   │       ├── Department.cs
│   │       ├── Education.cs
│   │       ├── Experience.cs
│   │       ├── GroupMember.cs
│   │       ├── Message.cs
│   │       ├── Notification.cs
│   │       ├── Payment.cs
│   │       ├── Project.cs
│   │       ├── ProjectGroup.cs
│   │       ├── ProjectModule.cs
│   │       ├── ProjectRequiredSkill.cs
│   │       ├── SavedOpportunity.cs
│   │       ├── Skill.cs
│   │       ├── Student.cs
│   │       ├── StudentReview.cs
│   │       ├── StudentSkill.cs
│   │       ├── Transaction.cs
│   │       ├── University.cs
│   │       ├── User.cs
│   │       └── UserSettings.cs
│   │
│   ├── Sh8lny.Abstraction/
│   │   ├── Sh8lny.Abstraction.csproj
│   │   ├── Repositories/
│   │   │   ├── IGenericRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Services/
│   │       ├── IAdminService.cs
│   │       ├── IAnnouncementService.cs
│   │       ├── IApplicationService.cs
│   │       ├── IAuthService.cs
│   │       ├── IBackupService.cs
│   │       ├── ICertificateService.cs
│   │       ├── IChatService.cs
│   │       ├── ICompanyService.cs
│   │       ├── IFileService.cs
│   │       ├── IMailService.cs
│   │       ├── IMasterDataService.cs
│   │       ├── INotificationService.cs
│   │       ├── INotifier.cs
│   │       ├── IPaymentService.cs
│   │       ├── IProjectExecutionService.cs
│   │       ├── IProjectService.cs
│   │       ├── IReviewService.cs
│   │       ├── IStudentService.cs
│   │       ├── IUserSettingsService.cs
│   │       └── IVirusScanService.cs
│   │
│   └── Sh8lny.Service/
│       ├── Sh8lny.Service.csproj
│       ├── AdminService.cs
│       ├── AnnouncementService.cs
│       ├── ApplicationService.cs
│       ├── AuthService.cs
│       ├── CertificateService.cs
│       ├── ChatService.cs
│       ├── ClamAvService.cs          ← Stub (always returns clean)
│       ├── CompanyService.cs
│       ├── FileService.cs
│       ├── MasterDataService.cs
│       ├── NotificationService.cs
│       ├── PaymentService.cs
│       ├── ProjectExecutionService.cs
│       ├── ProjectService.cs
│       ├── ReviewService.cs
│       ├── StudentService.cs
│       └── UserSettingsService.cs
│
├── Infrastructure/
│   ├── Sh8lny.Persistence/
│   │   ├── Sh8lny.Persistence.csproj
│   │   ├── BackupService.cs
│   │   ├── MailService.cs
│   │   ├── Contexts/
│   │   │   └── Sha8lnyDbContext.cs
│   │   ├── Configurations/           ← 28 Fluent API configurations
│   │   ├── Migrations/               ← 8 migrations (Dec 2025 – Apr 2026)
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs
│   │   │   └── UnitOfWork.cs
│   │   └── Seeding/
│   │       └── DbInitializer.cs
│   │
│   └── Sh8lny.Presentation/
│       └── Sh8lny.Presentation.csproj
│
├── Sh8lny.Shared/
│   ├── Sh8lny.Shared.csproj
│   ├── DTOs/
│   │   ├── Announcements/
│   │   ├── Admin/
│   │   ├── Applications/
│   │   ├── Auth/
│   │   ├── Certificates/
│   │   ├── Chat/
│   │   ├── Common/                    ← ServiceResponse<T>
│   │   ├── CompanyProfile/
│   │   ├── Execution/
│   │   ├── MasterData/
│   │   ├── Media/
│   │   ├── Notifications/
│   │   ├── Payments/
│   │   ├── Projects/
│   │   ├── Reviews/
│   │   ├── Settings/
│   │   └── StudentProfile/
│   ├── Options/
│   │   ├── JwtOptions.cs
│   │   └── MailSettings.cs
│   └── Validation/
│       └── AllowedFileExtensionsAttribute.cs
│
├── Sh8lny.Web/
│   ├── Sh8lny.Web.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/                   ← 16 controllers
│   ├── Hubs/
│   │   └── NotificationHub.cs
│   ├── Services/
│   │   ├── SignalRNotifier.cs
│   │   └── BackupWorker.cs
│   ├── Mappings/
│   │   └── MappingProfile.cs
│   ├── Logging/
│   │   └── DiscordWebhookLoggerProvider.cs
│   ├── DTOs/
│   ├── wwwroot/
│   └── Properties/
│
├── Tests/
│   └── Sh8lny.IntegrationTests/
│
├── Dockerfile
├── docker-compose.yml
├── cloudrun-env.yaml
└── README.md
```

## Appendix B: Key Enumerations

| Enum                  | Values                                                                     |
|-----------------------|----------------------------------------------------------------------------|
| `UserType`            | `Student`, `Company`, `University`, `Admin`                                |
| `ProjectType`         | `Internship`, `GraduationProject`, `Training`, `PartTime`, `FullTime`      |
| `ProjectStatus`       | `Open`, `InProgress`, `Closed`, `Completed`, `Cancelled`                   |
| `ApplicationStatus`   | `Submit`, `Pending`, `UnderReview`, `Accepted`, `InProgress`, `Completed`, `Rejected`, `Withdrawn` |
| `ModuleStatus`        | `Pending`, `InProgress`, `Completed`, `Approved`, `Rejected`               |
| `ConversationType`    | `Direct`, `Group`                                                          |
| `MessageType`         | `Text`, `File`, `Image`, `Link`                                            |
| `PaymentMethod`       | `Card`, `Wallet`, `Kiosk`                                                  |
| `SkillCategory`       | `Backend`, `Frontend`, `UIUX`, `Mobile`, `AIML`, `Data`, `Testing`, `Marketing`, `Other` |
| `AcademicYear`        | `FirstYear`, `SecondYear`, `ThirdYear`, `FourthYear`, `FifthYear`          |
| `ProfileVisibility`   | *(defined in UserSettings)*                                                |
| `ReviewStatus`        | `Approved`, `Rejected`                                                     |
| `OpportunityType`     | *(defined in CompletedOpportunity)*                                        |
| `CompletionStatus`    | *(defined in CompletedOpportunity)*                                        |

## Appendix C: Configuration Sources

| Setting                  | `appsettings.json` Section    | Environment Variable (Cloud Run)         |
|--------------------------|-------------------------------|------------------------------------------|
| Database Connection      | `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| JWT Key                  | `JwtSettings:Key`             | `JwtSettings__Key`                       |
| JWT Issuer               | `JwtSettings:Issuer`          | —                                        |
| JWT Audience             | `JwtSettings:Audience`        | —                                        |
| JWT Duration             | `JwtSettings:DurationInMinutes` | —                                      |
| SMTP Host                | `MailSettings:SmtpHost`       | —                                        |
| SMTP Password            | `MailSettings:SmtpPass`       | `MailSettings__SmtpPass`                 |
| ClamAV Host              | `ClamAV:Host`                 | —                                        |
| Backup Interval          | `Backup:IntervalHours`        | —                                        |
| Discord Webhook URL      | `DiscordSettings:WebhookUrl`  | `DiscordSettings__WebhookUrl`            |
| Environment              | —                             | `ASPNETCORE_ENVIRONMENT`                 |

---

> **Last Updated:** May 2026
> **Maintainer:** Sha8alny Development Team