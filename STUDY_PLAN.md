# STUDY_PLAN.md — Sha8alny Backend Study Materials Generator

> **WHO THIS FILE IS FOR:** This file is a master instruction set for **DeepSeek** (or any AI agent
> with file-read and file-write access). DeepSeek executes the numbered tasks below, one per session,
> to produce a complete set of study materials for the Sha8alny backend project.
>
> **WHO THE STUDY MATERIALS ARE FOR:** 2nd-year Egyptian engineering students who:
> - Know SQL and SQL Server (tables, joins, foreign keys, SELECT/INSERT/UPDATE)
> - Have zero or near-zero experience with .NET, C#, ASP.NET, or any backend framework
> - Do not know what dependency injection, middleware, an ORM, or a service layer is
> - Built this project heavily with AI assistance and now feel lost in their own codebase
> - Need to understand it well enough to **explain, defend, and extend it** in their graduation defense
>
> **REPO ROOT:** `e:\LLM testing\Sha8alny`  
> **OUTPUT FOLDER:** All generated unit files go into `e:\LLM testing\Sha8alny\study-materials\`  
> (Create the folder if it does not exist)
>
> **HOW TO USE THIS FILE:**
> 1. Read this entire README block once before starting any task.
> 2. Execute tasks in order: Task 0 first, then Unit 1 through Unit 17, then the final Index task.
> 3. Each task is self-contained. A fresh session can pick up any single task, read the files
>    listed, and produce the output — no memory of prior sessions required.
> 4. For units that list "Prerequisites", open those previously generated `.md` files to ensure
>    your new unit references and builds on them correctly.
> 5. Never skip Task 0. It calibrates your understanding of the whole project before you write
>    anything for students.

---

## UNIVERSAL TONE & STYLE RULES
> These rules apply to EVERY unit. DeepSeek must follow them without exception.

1. **Analogy before explanation.** Never introduce a technical concept without first giving a
   real-world or SQL-based analogy the student already understands.
   - Example: Before explaining "ORM", say "Think of it like this: you know how in SQL you write
     `SELECT * FROM Projects WHERE Status = 'Open'`? An ORM lets you write that same query in C#
     using objects instead of SQL strings — and it generates the SQL for you automatically."

2. **Why before what.** Always answer "why does this exist?" before "what is it?" and "how does it work?"

3. **No code dumps.** Never paste 50+ lines of code without explaining every meaningful part.
   When showing code snippets, annotate inline with `// ← what this line does` comments.

4. **Short paragraphs.** Max 4-5 sentences per paragraph. Students skim. Use headers and bullet
   points aggressively.

5. **Build on previous units.** Use cross-references like `(explained in Unit 2)` instead of
   re-explaining concepts. Do not assume the reader forgot everything — assume they read the
   prior unit and need a brief reminder at most.

6. **Defense-ready talking points.** Every unit must end with a "What to say in your defense"
   section: 3-5 bullet points phrased as confident, first-person statements a student can
   rehearse and speak out loud.

7. **Self-check questions.** Every unit must end with 5-7 questions a student can ask themselves
   to verify they understood. Include answers or hints.

8. **Plain language first.** Avoid Egyptian engineering-school jargon. Write as if explaining
   to a smart friend who has never written a backend API.

---

## TASK 0 — ORIENTATION SCAN (No output file)

**Purpose:** Before writing any study material, DeepSeek must build a mental model of the entire
project. This task has no output file. It is purely a read-and-internalize step.

### Files to Read

Open and read each of these files in full:

1. `context.md` — The full project context document. This is your primary reference for the
   entire plan. Read every section.
2. `Sh8lny.Web/Program.cs` — The application entry point. Note the middleware pipeline order,
   every `builder.Services.AddScoped<...>` registration, JWT configuration, SignalR setup,
   CORS policy, and how the app maps controllers and hubs.
3. `Core/Sh8lny.Domain/Models/Project.cs` — One representative domain entity.
4. `Core/Sh8lny.Abstraction/Services/IProjectService.cs` — Its service interface.
5. `Core/Sh8lny.Service/ProjectService.cs` — Its service implementation.
6. `Sh8lny.Web/Controllers/ProjectsController.cs` — Its controller.
7. `Infrastructure/Sh8lny.Persistence/Contexts/Sha8lnyDbContext.cs` — The EF Core DbContext.
8. `Infrastructure/Sh8lny.Persistence/Repositories/GenericRepository.cs` — The repository.

### What to Internalize

After reading, you should be able to answer these questions in your own words (no need to write them down — just confirm you can):

- What does a student do on this platform? What does a company do?
- How many C# projects are in the solution, and what is each one's job?
- If a student submits an application, which files are involved (controller → service → repository → DB)?
- Why is `IFormFile` never used in DTOs or service interfaces?
- What is `ServiceResponse<T>` and why does every service return it?
- What is the "Onion Architecture" dependency rule, and why would breaking it be a problem?

**No output file. Proceed to Unit 1.**

---

## TASK 1 — Unit 1: Helicopter View

**Output file:** `study-materials/unit-01-helicopter-view.md`

### Prerequisites
- None. This is the first unit. But you must have completed Task 0 (the orientation scan).

### Files to Read
1. `context.md` — Focus on sections 1 (Project Overview), 3 (Entity Relationship Overview), and 4 (Completed Capabilities).

### What to Extract and Understand
Answer these questions internally before writing:
- What real-world problem does Sha8alny solve? (What existed before it? What pain does it remove?)
- Who are the four user roles, and what does each one do on the platform?
- What is the full lifecycle of one piece of work, from the moment a company posts a project to the
  moment a student receives a certificate? (The "core workflow")
- What are the 10+ features the system supports today?
- What data flows between users? (What does a company give the system? What does the student give?
  What does the system give back to each?)
- How are Egyptian universities and students especially relevant? (Internship context, graduation projects)

### Required Output Structure

The output file must have exactly these sections, in this order:

```
# Unit 1: The Helicopter View — What Is Sha8alny and What Does It Do?

## 1.1 The Problem This System Solves
## 1.2 The Four People Who Use This System
## 1.3 The Journey: From "We Need a Developer" to "Certificate Issued"
   (A step-by-step story with no technical terms — pure narrative)
## 1.4 What the System Can Do Today (Feature Map)
   (A plain-language list of all major capabilities, grouped by role)
## 1.5 The Data That Flows Through the System
   (What information each user type puts in, and what they get out)
## 1.6 Why This Is Not Just a Job Board
   (Explain what makes it different: milestones, progress tracking, payments, certificates, reviews)
## 1.7 What to Say in Your Defense
## 1.8 Self-Check Questions
```

### Tone Reminders for This Unit
- Zero code. Zero architecture terms. Treat the reader like a smart friend who asked "what did you build?"
- Use the metaphor of a hiring agency or a university internship office if helpful.
- The "Journey" section (1.3) should read like a story: "Ahmad, a 3rd-year student at Cairo
  University, sees a project posted by TechCorp Egypt..."

---

## TASK 2 — Unit 2: Map of the Codebase

**Output file:** `study-materials/unit-02-code-map.md`

### Prerequisites
- Read `study-materials/unit-01-helicopter-view.md` for context on the system's purpose.

### Files to Read
1. `context.md` — Sections 2 (Tech Stack & Architecture), 6 (Rule 1 — Onion Architecture dependency rules).
2. `Sh8lny.Domain/Sh8lny.Domain.csproj` — Note what it references (nothing).
3. `Core/Sh8lny.Abstraction/Sh8lny.Abstraction.csproj` — Note what it references.
4. `Core/Sh8lny.Service/Sh8lny.Service.csproj` — Note what it references.
5. `Infrastructure/Sh8lny.Persistence/Sh8lny.Persistence.csproj` — Note what it references.
6. `Sh8lny.Shared/Sh8lny.Shared.csproj` — Note what it references (nothing).
7. `Sh8lny.Web/Sh8lny.Web.csproj` — Note what it references.

### What to Extract and Understand
- Why split one application into 7 separate projects? What problem does this solve?
- What is "Onion Architecture"? Why is the direction of dependencies so important?
- What is each project's single responsibility?
- What does "the inner layers don't know about the outer layers" mean in practice?
- What would break if `Sh8lny.Domain` referenced `Sh8lny.Persistence`?
- What is `Sh8lny.Shared` and why does it have no dependencies but is referenced by everyone?
- How do the `.csproj` files prove the dependency rules are enforced?

### Required Output Structure

```
# Unit 2: Map of the Codebase — Why Is the Code Split Into So Many Projects?

## 2.1 The Problem with Putting Everything in One Project
   (Analogy: a restaurant where the chef, waiter, and accountant all sit in one room)
## 2.2 The Seven Projects and What Each One Does
   (One subsection per project: Sh8lny.Domain, Sh8lny.Abstraction, Sh8lny.Service,
    Sh8lny.Persistence, Sh8lny.Shared, Sh8lny.Web, Sh8lny.Presentation)
   (Each subsection: one-sentence job description, what kinds of files live here,
    what it CAN reference, what it CANNOT reference)
## 2.3 The Onion Architecture — The Rule That Holds It All Together
   (Text-based diagram showing the layers: Domain at center, Web at edge)
   (Explain the rule: inner layers know nothing about outer layers)
   (Analogy: the kitchen doesn't know who the customer is — it only knows "make this dish")
## 2.4 Why Can't the Database Layer Know About the Web Layer?
   (Explain the practical benefit: you could swap SQL Server for another database,
    or swap the REST API for a command-line tool, without touching the business logic)
## 2.5 Sh8lny.Shared — The One Exception
   (Why Shared has no dependencies but is referenced by everyone)
## 2.6 How the Projects Talk to Each Other (Interfaces)
   (Brief preview — will be fully explained in Unit 3)
## 2.7 What to Say in Your Defense
## 2.8 Self-Check Questions
```

### Tone Reminders for This Unit
- The restaurant analogy (chef/waiter/accountant) is very effective here — build on it.
- The student has probably seen "import" in Python — relate `.csproj` references to imports.
- Avoid the word "abstraction" without explaining it means "a description of what something does,
  without saying how it does it."

---

## TASK 3 — Unit 3: How a Request Travels Through the System

**Output file:** `study-materials/unit-03-request-lifecycle.md`

### Prerequisites
- Read `study-materials/unit-01-helicopter-view.md`
- Read `study-materials/unit-02-code-map.md`

### Files to Read
1. `Sh8lny.Web/Program.cs` — Read the full file. Focus on:
   - All `builder.Services.AddScoped<...>()` calls
   - The middleware pipeline: `app.UseSwagger()`, `app.UseHttpsRedirection()`,
     `app.UseStaticFiles()`, `app.UseMiddleware<...>()`, `app.UseCors()`,
     `app.UseAuthentication()`, `app.UseAuthorization()`, `app.MapControllers()`,
     `app.MapHub<...>()`
   - JWT Bearer configuration
   - CORS configuration
2. `context.md` — Sections: "Middleware Pipeline Order", "DI Registration", "Rule 6 (extract user ID from JWT)".
3. `Sh8lny.Web/Controllers/ProjectsController.cs` — Read the top of the file: how `[ApiController]`,
   `[Route]`, `[Authorize]` attributes work; how the constructor injects `IProjectService`; how
   `GetCurrentUserId()` works.
4. `Core/Sh8lny.Abstraction/Services/IProjectService.cs` — The interface (contract).
5. `Core/Sh8lny.Service/ProjectService.cs` — The implementation; how the constructor injects
   `IUnitOfWork`; how one method uses `_unitOfWork` and `_mapper`.

### What to Extract and Understand
- What happens the moment an HTTP request arrives at the server? What checks does it pass through?
- What is "middleware"? What does each middleware in the pipeline actually do?
- What is "Dependency Injection"? What problem does it solve? How does ASP.NET know which class
  to give when a controller asks for `IProjectService`?
- What is `[Authorize]` and how does it know who the user is?
- What is the chain: Controller → Service Interface → Service Implementation → Repository → DB?
- How does a response travel back from the DB to the HTTP response?

### Required Output Structure

```
# Unit 3: How a Request Travels — From Your Phone to the Database and Back

## 3.1 The Story of One Request
   (Narrative: a student's phone sends a GET /api/Projects request — trace it step by step
    in plain language before any technical explanation)
## 3.2 What Is Middleware? (The Airport Security Analogy)
   (Each middleware = one security gate; explain CORS, Authentication, Authorization,
    Request Timing in plain language)
## 3.3 The Sha8alny Middleware Pipeline — Every Gate in Order
   (List all 9 stages of the pipeline from context.md with one-sentence plain explanation each)
## 3.4 What Is Dependency Injection? (The "Supply Closet" Analogy)
   (Explain the problem: hardcoding a class creates tight coupling; DI = asking for what you
    need and letting the system provide it)
   (How Program.cs registers services, how a controller constructor receives them)
## 3.5 From Controller to Database — The Full Chain
   (Diagram in text: HTTP Request → Middleware → Controller → IService → Service → IUnitOfWork
    → IGenericRepository → EF Core → SQL Server)
   (Walk through ProjectsController → IProjectService → ProjectService → IUnitOfWork step by step)
## 3.6 How the Response Comes Back
   (The chain in reverse; what ServiceResponse<T> looks like to the caller)
## 3.7 What Lives in Program.cs and Why
   (Three jobs of Program.cs: register services, configure middleware, map routes)
## 3.8 Glossary: New Terms in This Unit
   (Middleware, Dependency Injection, Scoped Lifetime, Interface, Controller, Route,
    Attribute, JWT Bearer, CORS)
## 3.9 What to Say in Your Defense
## 3.10 Self-Check Questions
```

### Tone Reminders for This Unit
- "Airport security gates" is the gold standard analogy for middleware — every gate checks
  something different, and if you fail any gate, you don't get on the plane.
- "Supply closet" or "ingredient pantry" works well for DI — the controller says "I need an
  IProjectService" and the DI container hands one in, pre-assembled.
- Connect DI back to the Onion Architecture from Unit 2: "This is HOW the inner layers stay
  ignorant of the outer layers — they only know the interface, not the concrete class."

---

## TASK 4 — Unit 4: The Database Layer

**Output file:** `study-materials/unit-04-database-layer.md`

### Prerequisites
- Read `study-materials/unit-02-code-map.md`
- Read `study-materials/unit-03-request-lifecycle.md`

### Files to Read
1. `Infrastructure/Sh8lny.Persistence/Contexts/Sha8lnyDbContext.cs` — Full file. Note every
   `DbSet<T>` property. Note `OnModelCreating` and how it calls configurations.
2. `Infrastructure/Sh8lny.Persistence/Repositories/GenericRepository.cs` — Full file. Understand
   every method: `GetByIdAsync`, `GetAllAsync`, `GetQueryable`, `AddAsync`, `Update`, `Delete`.
3. `Infrastructure/Sh8lny.Persistence/Repositories/UnitOfWork.cs` — Full file. Understand
   `SaveChangesAsync` and how it exposes each repository.
4. `Core/Sh8lny.Abstraction/Repositories/IGenericRepository.cs` — The interface.
5. `Core/Sh8lny.Abstraction/Repositories/IUnitOfWork.cs` — The interface.
6. `Infrastructure/Sh8lny.Persistence/Configurations/ProjectConfiguration.cs` — One example of
   Fluent API configuration. Note `HasKey`, `HasOne`, `WithMany`, `HasForeignKey`, `HasMaxLength`.
7. The list of migration files in `Infrastructure/Sh8lny.Persistence/Migrations/` — Read the
   filenames (you do not need to read every migration file in full; read
   `20251207020220_InitialCreation` and one recent one like `20260423130812_AddSavedProjectsAndReviews`).
8. `context.md` — Sections: "Migration History", Rule 2 (IQueryable + Include), Rule 4 (migrations),
   Rule 8 (database conventions).

### What to Extract and Understand
- What is EF Core and how does it replace writing raw SQL? (The ORM concept)
- What is `DbContext` and what is `DbSet<T>`? (Relate to SQL Server tables)
- What is the Repository Pattern? Why wrap EF Core in a `GenericRepository`?
- What is the Unit of Work Pattern? Why not just call `SaveChanges` directly?
- What is Fluent API configuration? Why configure relationships here instead of annotations?
- What is a migration and why does the app run `MigrateAsync()` on startup?
- What is `IQueryable` and why does the codebase use `.Include()` for navigation properties?
- What is a navigation property and why does it need `.Include()` to load?

### Required Output Structure

```
# Unit 4: The Database Layer — How Sha8alny Talks to SQL Server

## 4.1 The Problem with Writing Raw SQL in Your Application Code
   (Analogy: imagine writing SQL strings everywhere — what happens when you rename a table?)
## 4.2 What Is an ORM? (Entity Framework Core as Your SQL Writer)
   (EF Core = a translator between C# objects and SQL Server tables)
   (Show the parallel: SQL table ↔ C# class; SQL column ↔ C# property; SQL row ↔ C# object instance)
## 4.3 DbContext — The Connection to the Database
   (Sha8alnyDbContext; what DbSet<T> is; how it maps to a SQL table)
   (Show the Student DbSet and relate it to the Students table in SQL Server)
## 4.4 The Repository Pattern — Why We Wrap EF Core
   (Analogy: a waiter who takes orders — you don't go directly to the chef / kitchen)
   (IGenericRepository<T> methods explained: GetByIdAsync, GetAllAsync, GetQueryable,
    AddAsync, Update, Delete)
   (Why generic? Because all entities need the same basic CRUD operations)
## 4.5 The Unit of Work — The "Transaction Wrapper"
   (Analogy: shopping cart checkout — you don't pay for each item one by one;
    everything commits together or nothing does)
   (How UnitOfWork exposes all repositories and SaveChangesAsync)
## 4.6 Fluent API Configuration — Teaching EF Core Your Rules
   (Why not just use column annotations? Fluent API keeps domain entities clean)
   (Walk through ProjectConfiguration: primary key, required fields, relationships)
## 4.7 Migrations — How the Database Schema Evolves
   (What a migration is: a C# file that describes "add this column" or "add this table")
   (Why run MigrateAsync on startup? So the database is always in sync with the code)
   (The 8 migrations in chronological order — what each one added)
## 4.8 The Golden Rule: Always Use .Include() for Navigation Properties
   (What is a navigation property? — Relate to SQL JOINs)
   (Why .Include() is required: EF Core is lazy by default — it won't load related data
    unless you explicitly ask for it)
   (Show a correct vs. broken example from context.md Rule 2)
## 4.9 Glossary: New Terms in This Unit
   (ORM, DbContext, DbSet, Repository Pattern, Unit of Work, Migration,
    Navigation Property, Eager Loading, Fluent API)
## 4.10 What to Say in Your Defense
## 4.11 Self-Check Questions
```

### Tone Reminders for This Unit
- Students know SQL well. Constantly bridge to SQL: "This is exactly like writing a JOIN between
  Projects and Companies — EF Core writes that JOIN for you when you call `.Include(p => p.Company)`."
- The migration concept maps perfectly to "ALTER TABLE" — a migration is just a managed, version-
  controlled ALTER TABLE script.

---

## TASK 5 — Unit 5: DTOs, AutoMapper, and ServiceResponse

**Output file:** `study-materials/unit-05-dtos-mapper-response.md`

### Prerequisites
- Read `study-materials/unit-03-request-lifecycle.md`
- Read `study-materials/unit-04-database-layer.md`

### Files to Read
1. `Sh8lny.Shared/DTOs/Common/` — Read all files here (ServiceResponse<T> and related common DTOs).
2. `Sh8lny.Shared/DTOs/Projects/` — Read all DTO files here as a concrete example
   (CreateProjectDto, ProjectResponseDto, etc.)
3. `Sh8lny.Web/Mappings/MappingProfile.cs` — Full file. Note every `CreateMap<Source, Destination>()`.
4. `Core/Sh8lny.Service/ProjectService.cs` — Read one complete method that:
   - Receives a DTO from the controller
   - Does business logic
   - Returns a `ServiceResponse<SomeType>`
5. `context.md` — Rule 3 (never use IFormFile in DTOs), Rule 5 (always use ServiceResponse<T>),
   section on Sh8lny.Shared.

### What to Extract and Understand
- What is a DTO? Why not just expose the database entity directly in the API response?
- What data does `CreateProjectDto` contain vs what `Project` (the entity) contains?
- What is AutoMapper and why use it instead of manually assigning properties?
- What is `MappingProfile.cs` and how does AutoMapper use it?
- What is `ServiceResponse<T>`? What fields does it have? How do controllers use it?
- Why is `IFormFile` forbidden in DTOs? (Connect to Rule 3 from Unit 2's Onion rules)

### Required Output Structure

```
# Unit 5: DTOs, AutoMapper, and ServiceResponse — How Data Changes Shape As It Travels

## 5.1 The Problem: Why Not Just Return the Database Entity Directly?
   (Security: your Student entity has PasswordHash — you don't want to send that to the frontend)
   (Control: the frontend might need different fields than what's in the database)
   (Analogy: a restaurant menu item is not the same as the chef's recipe card)
## 5.2 What Is a DTO? (Data Transfer Object)
   (A DTO is a "carrying container" — it only holds the fields needed for one specific purpose)
   (Show CreateProjectDto vs ProjectResponseDto — input shape vs output shape)
## 5.3 The Three DTO Patterns Used in Sha8alny
   (Create DTOs: what the client sends when creating something)
   (Response DTOs: what the server sends back)
   (Update DTOs: what the client sends when updating something)
## 5.4 AutoMapper — The Automatic Translator
   (The problem: copying 20 properties one by one is tedious and error-prone)
   (AutoMapper: tell it once how to map A to B, then call _mapper.Map<B>(a) anywhere)
   (Walk through MappingProfile.cs: what CreateProjectDto maps to, what Project maps to
    ProjectResponseDto)
## 5.5 ServiceResponse<T> — The Standard Envelope
   (Every service method returns this; it wraps the actual data with success/failure info)
   (Fields: IsSuccess, Data, Message, Errors — show the structure)
   (Why? Controllers should not need to know why something failed — the response tells them)
   (How controllers use it: check IsSuccess, then return Ok(response.Data) or BadRequest(response.Message))
## 5.6 Where Does Sh8lny.Shared Fit In?
   (All DTOs live in Sh8lny.Shared — recap from Unit 2 — because everyone needs them:
    the service receives them, the controller sends them, the tests use them)
## 5.7 What to Say in Your Defense
## 5.8 Self-Check Questions
```

---

## TASK 6 — Unit 6: Authentication and JWT

**Output file:** `study-materials/unit-06-auth-jwt.md`

### Prerequisites
- Read `study-materials/unit-03-request-lifecycle.md`
- Read `study-materials/unit-05-dtos-mapper-response.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/User.cs` — Full file. Note all auth-related fields:
   `PasswordHash`, `IsEmailVerified`, `VerificationCode`, `VerificationCodeExpiry`,
   `PasswordResetToken`, `ResetTokenExpires`, `UserType`, `IsActive`.
2. `Core/Sh8lny.Abstraction/Services/IAuthService.cs` — All method signatures.
3. `Core/Sh8lny.Service/AuthService.cs` — Full file. Focus on:
   - How `RegisterAsync` creates a user and hashes the password with BCrypt
   - How `LoginAsync` validates credentials and generates a JWT
   - How `ForgotPasswordAsync` and `ResetPasswordAsync` work
   - How `VerifyEmailAsync` works with an OTP code
4. `Sh8lny.Web/Controllers/AuthController.cs` — Full file.
5. `Sh8lny.Shared/DTOs/Auth/` — All DTO files.
6. `Sh8lny.Shared/Options/JwtOptions.cs`
7. `Sh8lny.Web/Program.cs` — The JWT Bearer configuration block specifically.
8. `context.md` — Section 4.1 (JWT configuration), Rule 6 (extracting user ID from claims),
   Rule 7 (role-based authorization).

### What to Extract and Understand
- What is password hashing and why is storing a plain password a catastrophic security mistake?
- What is a JWT token? What is inside it? How long does it last?
- How does the server verify a JWT without storing it anywhere?
- What are "claims" in a JWT? How does the server know the UserID and Role from the token?
- What is the email verification flow? Why require email verification?
- What is the forgot-password flow?
- How does `[Authorize(Roles = "Student")]` work at the controller level?
- What is `UserType` (Student, Company, Admin) and how does it become a "role" in the JWT?

### Required Output Structure

```
# Unit 6: Authentication and JWT — How Sha8alny Knows Who You Are

## 6.1 The Problem: How Does the Server Know Who Is Making This Request?
   (HTTP is stateless — every request starts fresh; the server has no memory of you)
   (Analogy: like a bank that forgets who you are the moment you leave the counter)
## 6.2 Passwords: Why We Never Store the Real Password
   (BCrypt hashing explained: one-way transformation; the server stores the hash,
    not the password; why this matters if the database is stolen)
## 6.3 What Happens When You Register
   (Walk through the RegisterAsync flow: validate → hash password → create User row
    → send verification email → return user ID)
## 6.4 What Happens When You Log In
   (Walk through LoginAsync: find user by email → verify BCrypt hash → check IsActive
    and IsEmailVerified → generate JWT → return token)
## 6.5 What Is a JWT Token?
   (Three parts: Header, Payload, Signature)
   (What is in the Sha8alny JWT payload: UserID (NameIdentifier), Email, Role (UserType))
   (60-minute lifetime; why it expires)
   (Analogy: like a signed wristband at an event — anyone can read it, but only the
    event organizer can issue a valid one)
## 6.6 How the Server Validates a JWT on Every Request
   (The client sends the token in the Authorization header)
   (ASP.NET Core's JWT middleware verifies the signature using the secret key)
   (If valid → User.Claims is populated → controllers can call GetCurrentUserId())
## 6.7 Role-Based Authorization: [Authorize(Roles = "Student")]
   (How the Role claim in the JWT becomes the authorization check)
   (Which endpoints require which roles — from Rule 7 in context.md)
## 6.8 Email Verification and Password Reset
   (The OTP flow: register → receive code by email → POST /verify-email → IsEmailVerified = true)
   (The reset flow: POST /forgot-password → receive token by email → POST /reset-password)
## 6.9 The User Entity — What Each Field Stores
   (Walk through each auth-relevant field in User.cs and explain its purpose)
## 6.10 What to Say in Your Defense
## 6.11 Self-Check Questions
```

---

## TASK 7 — Unit 7: File Uploads and the Media Pattern

**Output file:** `study-materials/unit-07-file-uploads.md`

### Prerequisites
- Read `study-materials/unit-02-code-map.md`
- Read `study-materials/unit-03-request-lifecycle.md`

### Files to Read
1. `Sh8lny.Web/Controllers/MediaController.cs` — Full file.
2. `Core/Sh8lny.Abstraction/Services/IFileService.cs` — All method signatures.
3. `Core/Sh8lny.Service/FileService.cs` — Full file. Focus on:
   - File extension validation
   - Size limit (5 MB)
   - ImageSharp resize and WebP conversion
   - Thumbnail generation
   - Where files are saved (`wwwroot/uploads/{folder}/`)
   - What URL is returned
4. `Core/Sh8lny.Abstraction/Services/IVirusScanService.cs`
5. `Core/Sh8lny.Service/ClamAvService.cs` — Note that it is a stub (always returns clean).
6. `Sh8lny.Shared/Validation/AllowedFileExtensionsAttribute.cs`
7. `Sh8lny.Shared/DTOs/Media/` — Any upload response DTOs.
8. `context.md` — Section 4.12 (Media endpoints), Rule 3 (never IFormFile in domain DTOs).

### What to Extract and Understand
- Why does a separate `/api/Media` endpoint handle ALL file uploads?
- What validations does FileService apply before saving a file?
- What does ImageSharp do? What is WebP? What is a thumbnail?
- What is a "stub" virus scanner? Why is ClamAV disabled?
- What does the Media controller return after a successful upload? (The URL string)
- How does a student profile endpoint use that URL? (It only receives the URL string)
- Why is `IFormFile` forbidden outside the Media layer?

### Required Output Structure

```
# Unit 7: File Uploads — How Sha8alny Handles Profile Pictures, CVs, and Documents

## 7.1 The Problem with Accepting Files Everywhere
   (If every endpoint accepted files, every service would need file-handling logic —
    duplication, inconsistency, security gaps)
## 7.2 The Sha8alny Rule: All Files Go Through /api/Media
   (One controller, one service, one place for all file logic)
   (Two-step flow: (1) upload file to /api/Media → get URL back; (2) use that URL in any other endpoint)
## 7.3 What FileService Does to Your File
   (Validate extension: only .jpg, .jpeg, .png, .gif, .pdf allowed)
   (Validate size: max 5 MB)
   (For images: resize to max 1920px wide using ImageSharp; convert to WebP format;
    generate a 300px thumbnail)
   (Save to wwwroot/uploads/{folder}/; return the public URL)
## 7.4 What Is WebP? Why Convert to It?
   (WebP is a modern image format: same quality, much smaller file size than JPEG or PNG)
   (Analogy: like compressing a Word document to a ZIP — same content, less space)
## 7.5 The Virus Scanner — Why It Exists and Why It's Disabled
   (ClamAV: an open-source antivirus; ClamAvService is a stub that always returns "clean")
   (Why disabled: running ClamAV in a Docker container on Cloud Run adds complexity;
    the architecture is ready for it when needed)
## 7.6 Why IFormFile Is Forbidden Outside the Media Layer
   (IFormFile is an HTTP concept — it belongs in the Web layer, not in Domain or Service)
   (Connect back to Onion Architecture: inner layers must not know about HTTP)
   (What happens instead: the inner layers receive a string URL, which is a plain C# type)
## 7.7 Walk-Through: Uploading a CV
   (Step 1: Student calls POST /api/Media/upload?folder=cvs with the file)
   (Step 2: MediaController → FileService → validate → save → return "/uploads/cvs/file.pdf")
   (Step 3: Student calls PUT /api/students/profile with { "cvFileUrl": "/uploads/cvs/file.pdf" })
   (Step 4: StudentService stores the URL string in the Student.CvFileUrl column)
## 7.8 What to Say in Your Defense
## 7.9 Self-Check Questions
```

---

## TASK 8 — Unit 8: Students and Companies — Profile Management

**Output file:** `study-materials/unit-08-students-companies.md`

### Prerequisites
- Read `study-materials/unit-05-dtos-mapper-response.md`
- Read `study-materials/unit-06-auth-jwt.md`
- Read `study-materials/unit-07-file-uploads.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/Student.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/Company.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/Education.cs`
4. `Core/Sh8lny.Domain/Models/Experience.cs`
5. `Core/Sh8lny.Domain/Models/StudentSkill.cs`
6. `Core/Sh8lny.Domain/Models/SavedOpportunity.cs`
7. `Core/Sh8lny.Abstraction/Services/IStudentService.cs`
8. `Core/Sh8lny.Abstraction/Services/ICompanyService.cs`
9. `Core/Sh8lny.Service/StudentService.cs` — Focus on `CreateProfileAsync`, `UpdateProfileAsync`,
   `GetProfileAsync`, `SaveOpportunityAsync`, `GetSavedOpportunitiesAsync`.
10. `Core/Sh8lny.Service/CompanyService.cs` — Focus on `CreateOrUpdateProfileAsync`, `GetProfileAsync`.
11. `Sh8lny.Web/Controllers/StudentsController.cs` — Full file.
12. `Sh8lny.Web/Controllers/CompaniesController.cs` — Full file.
13. `Sh8lny.Shared/DTOs/StudentProfile/` — All files.
14. `Sh8lny.Shared/DTOs/CompanyProfile/` — All files.

### What to Extract and Understand
- Why are Student and Company separate entities from User? What does User store vs. what does Student store?
- What is `ProfileCompleteness` (0-100 score) and why does it exist?
- What is `TotalInternshipDays` on Student and when does it get updated?
- What navigation properties does Student have? (Education, Experience, Skills, Applications, etc.)
- What is a `SavedOpportunity`? How does bookmarking work?
- What is the difference between `Student.AverageRating` and the review entities?
- How does company profile creation work as an upsert (create or update)?

### Required Output Structure

```
# Unit 8: Students and Companies — Profile Management

## 8.1 Why Is There a User Entity AND a Student Entity?
   (User = the login identity; Student = the professional profile)
   (Analogy: your university ID card vs. your academic transcript)
   (What User stores: email, password hash, user type, verification status)
   (What Student stores: bio, CV URL, skills, education, experience, ratings)
## 8.2 The Student Profile — What It Contains
   (Walk through every meaningful field in Student.cs with plain-language explanations)
   (ProfileCompleteness: why it exists, what filling it incentivizes)
   (TotalInternshipDays: cumulative counter, updated when a job is marked complete)
   (CvFileUrl: connects back to the Media pattern from Unit 7)
## 8.3 Student Sub-Records: Education, Experience, Skills
   (These are separate rows in separate tables — relate to SQL: StudentSkill is a JOIN table
    between Students and Skills)
   (Why normalized? Because a student can have multiple education records, multiple skills, etc.)
## 8.4 Bookmarking Projects — SavedOpportunity
   (A simple join table: Student ID + Project ID + timestamp)
   (What the API endpoints do: POST to save, DELETE to remove, GET to list)
## 8.5 The Company Profile — What It Contains
   (Walk through Company.cs: name, logo URL, industry, description, ratings)
   (Why CreateOrUpdateProfile (upsert) instead of separate Create and Update?)
## 8.6 Profile Search
   (What filters are available when searching students and companies)
   (How search works at a high level — IQueryable filtering)
## 8.7 What to Say in Your Defense
## 8.8 Self-Check Questions
```

---

## TASK 9 — Unit 9: Projects and Applications — The Core Marketplace

**Output file:** `study-materials/unit-09-projects-applications.md`

### Prerequisites
- Read `study-materials/unit-08-students-companies.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/Project.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/Application.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/ProjectRequiredSkill.cs`
4. `Core/Sh8lny.Abstraction/Services/IProjectService.cs`
5. `Core/Sh8lny.Abstraction/Services/IApplicationService.cs`
6. `Core/Sh8lny.Service/ProjectService.cs` — Full file. Focus on:
   - `CreateProjectAsync`: what it validates, what it creates, what notifications it triggers
   - `GetProjectsAsync`: filtering, pagination, ViewCount increment
   - `DeleteProjectAsync`: what checks prevent deletion
7. `Core/Sh8lny.Service/ApplicationService.cs` — Full file. Focus on:
   - `ApplyAsync`: duplicate check, status = Submit → Pending, notification to company
   - `ReviewApplicationAsync`: accept/reject, status transitions, notification to student
   - `UpdateApplicationStatusAsync`
8. `Sh8lny.Web/Controllers/ProjectsController.cs` — Full file.
9. `Sh8lny.Web/Controllers/ApplicationsController.cs` — Full file.
10. `Sh8lny.Shared/DTOs/Projects/` — All files.
11. `Sh8lny.Shared/DTOs/Applications/` — All files.
12. `context.md` — Sections 4.4 and 4.5; `ApplicationStatus` enum in Appendix B.

### What to Extract and Understand
- What fields does a Project have? What is the difference between `Deadline` (application deadline)
  and `EndDate` (project end date)?
- What is `ProjectStatus`? What are the five states and when does each apply?
- What is `ApplicationStatus`? What are the eight states and what triggers each transition?
- What prevents a student from applying twice to the same project?
- What notification is sent when a student applies? When a company accepts?
- How does `MaxApplicants` work?
- What is the difference between `ViewCount` and `ApplicationCount`?
- What is `ProjectRequiredSkill`? How does it relate to the Skills lookup table?

### Required Output Structure

```
# Unit 9: Projects and Applications — The Core Marketplace

## 9.1 What a Project Is
   (Walk through Project entity: name, type, deadline, duration, status, visibility)
   (ProjectType enum: Internship, GraduationProject, Training, PartTime, FullTime)
   (The difference between Deadline (apply by this date) and EndDate (project ends this date))
   (MaxApplicants: why a cap might exist)
## 9.2 The Project Lifecycle — ProjectStatus
   (Five states: Open → InProgress → Completed, or → Cancelled, or → Closed)
   (When each state applies; what actions are blocked in each state)
## 9.3 How a Company Posts a Project
   (Walk through ProjectService.CreateProjectAsync: what is validated, what is created,
    what notification is triggered)
   (ProjectRequiredSkills: how skills are attached to the project post)
## 9.4 How Students Browse Projects
   (GET /api/Projects with filters: type, status, skills, keyword, pagination)
   (ViewCount: incremented every time someone opens a project)
## 9.5 What an Application Contains
   (Walk through Application entity: cover letter, resume URL, portfolio URL,
    proposal file URL, bid amount, status)
   (BidAmount: optional price the student proposes — relevant for freelance projects)
## 9.6 The Application Status Machine — Eight States
   (Draw the state machine as a text diagram: Submit → Pending → UnderReview → 
    Accepted/Rejected; Accepted → InProgress → Completed/Withdrawn)
   (What triggers each transition; who can trigger it)
## 9.7 Notifications Triggered by Application Events
   (When student applies: company gets notified)
   (When company accepts/rejects: student gets notified)
   (How notifications connect to Unit 14 — SignalR)
## 9.8 What to Say in Your Defense
## 9.9 Self-Check Questions
```

---

## TASK 10 — Unit 10: Execution and Modules — Managing the Work

**Output file:** `study-materials/unit-10-execution-modules.md`

### Prerequisites
- Read `study-materials/unit-09-projects-applications.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/ProjectModule.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/ApplicationModuleProgress.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/ProjectGroup.cs`
4. `Core/Sh8lny.Domain/Models/GroupMember.cs`
5. `Core/Sh8lny.Domain/Models/CompletedOpportunity.cs` — Full file.
6. `Core/Sh8lny.Abstraction/Services/IProjectExecutionService.cs`
7. `Core/Sh8lny.Service/ProjectExecutionService.cs` — Full file. Focus on:
   - `AddModuleAsync`: how milestones are created after acceptance
   - `UpdateModuleProgressAsync`: how student reports progress (0-100%)
   - `ReviewModuleAsync`: how company approves/rejects a module
   - `MarkJobCompleteAsync`: what happens at completion (TotalInternshipDays, CompletedOpportunity)
   - `GetCompletionSummaryAsync`
8. `Sh8lny.Web/Controllers/ExecutionController.cs` — Full file.
9. `Sh8lny.Shared/DTOs/Execution/` — All files.
10. `context.md` — Section 4.6, `ModuleStatus` enum in Appendix B.

### What to Extract and Understand
- What is a ProjectModule (milestone)? Why break a project into modules?
- What is the `Weight` field on ProjectModule? How do weights affect overall progress calculation?
- What is `ApplicationModuleProgress`? How does it differ from `ProjectModule`?
  (ProjectModule = the task definition; ApplicationModuleProgress = one student's progress on that task)
- What is `ModuleStatus` and how does it progress?
- What happens when `MarkJobCompleteAsync` is called? What does it create?
- What is `CompletedOpportunity`? Why is it a separate entity and not just a flag on Application?
- How does `TotalInternshipDays` on Student get updated at completion?
- What is a `ProjectGroup`? When would a project have multiple group members?

### Required Output Structure

```
# Unit 10: Execution and Modules — How the Work Actually Gets Done

## 10.1 From "Accepted" to "Certificate" — The Execution Phase
   (Recap from Unit 9: Application status is Accepted; now what?)
   (Overview of the execution flow: create modules → student updates progress →
    company reviews → complete → certificate)
## 10.2 Project Modules (Milestones) — Breaking Work Into Steps
   (Why milestones? Large projects need checkpoints)
   (Walk through ProjectModule: title, description, estimated duration, order index, weight, status)
   (What Weight means: if a module is worth 40%, completing it moves progress by 40%)
## 10.3 Module Status — The Milestone Lifecycle
   (Five states: Pending → InProgress → Completed → Approved/Rejected)
   (Company creates modules (Pending); student works (InProgress); student marks done (Completed);
    company reviews (Approved or Rejected with feedback))
## 10.4 ApplicationModuleProgress — Tracking One Student's Work on One Module
   (Why a separate table? A project can have multiple accepted students on different modules)
   (ProgressPercentage: 0-100; Note field: student's update message)
## 10.5 Marking the Job Complete
   (What MarkJobCompleteAsync does: update Application.Status to Completed,
    create CompletedOpportunity record, update Student.TotalInternshipDays,
    trigger certificate generation — preview of Unit 11)
## 10.6 CompletedOpportunity — The Historical Record
   (Why a separate entity? So the student has a permanent record even if the project is deleted)
   (What it stores: reference to Student, Project, Application, dates, type)
## 10.7 Project Groups — Team Work
   (What ProjectGroup is; what GroupMember stores)
   (When would a project use groups? Multi-student internship cohorts)
## 10.8 What to Say in Your Defense
## 10.9 Self-Check Questions
```

---

## TASK 11 — Unit 11: Reviews and Certificates — Closing the Loop

**Output file:** `study-materials/unit-11-reviews-certificates.md`

### Prerequisites
- Read `study-materials/unit-10-execution-modules.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/StudentReview.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/CompanyReview.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/Certificate.cs` — Full file.
4. `Core/Sh8lny.Abstraction/Services/IReviewService.cs`
5. `Core/Sh8lny.Abstraction/Services/ICertificateService.cs`
6. `Core/Sh8lny.Service/ReviewService.cs` — Full file. Focus on how reviews are validated
   (must have a completed opportunity), how AverageRating is updated on Student/Company,
   and the response mechanism (StudentResponse, CompanyResponse).
7. `Core/Sh8lny.Service/CertificateService.cs` — Full file. Focus on how a certificate is
   created, what CertificateNumber looks like, and what CertificateURL stores.
8. `Sh8lny.Web/Controllers/ReviewsController.cs` — Full file.
9. `Sh8lny.Web/Controllers/CertificatesController.cs` — Full file.
10. `Sh8lny.Shared/DTOs/Reviews/` — All files.
11. `Sh8lny.Shared/DTOs/Certificates/` — All files.
12. `context.md` — Sections 4.9, 4.10; `ReviewStatus` enum.

### What to Extract and Understand
- Who reviews whom? (Company reviews Student → StudentReview; Student reviews Company → CompanyReview)
- What are the detailed rating breakdown fields in StudentReview? (TechnicalSkills, Communication, etc.)
- What is `ReviewStatus` (`Approved`/`Rejected`)? Who sets it and what does it mean?
- Can a student respond to a company's review of them? How?
- What prevents someone from leaving a review without completing a project with the other party?
- What is a `CertificateNumber`? What is `CertificateURL`? Why can certificates be publicly verified?
- What does the public `/api/Certificates/verify/{uniqueId}` endpoint do?

### Required Output Structure

```
# Unit 11: Reviews and Certificates — Closing the Loop After Project Completion

## 11.1 The Purpose of Mutual Reviews
   (Why both parties review each other: accountability, trust-building, portfolio value)
   (Analogy: Uber ratings — driver rates passenger AND passenger rates driver)
## 11.2 Company Reviews a Student (StudentReview)
   (Walk through StudentReview entity: overall rating + 6 category breakdowns)
   (WouldHireAgain field; Strengths and AreasForImprovement)
   (StudentResponse: the student can reply to the review)
   (ReviewStatus: Approved/Rejected — admin moderation)
   (IsVerified: confirms the review comes from a real completed engagement)
## 11.3 Student Reviews a Company (CompanyReview)
   (Walk through CompanyReview: work environment, learning opportunities, mentorship, compensation)
   (WouldRecommend; Pros and Cons; IsAnonymous option)
   (CompanyResponse: the company can reply)
## 11.4 How Average Ratings Are Maintained
   (AverageRating and TotalReviews on Student and Company entities)
   (How ReviewService updates these fields after each new review)
## 11.5 Certificates — The Official Record of Achievement
   (Walk through Certificate entity: CertificateNumber, CertificateTitle, CertificateURL,
    IssuedAt, ExpiresAt)
   (CertificateNumber: a unique identifier used for public verification)
   (CertificateURL: the generated certificate image/PDF URL)
## 11.6 Public Certificate Verification
   (GET /api/Certificates/verify/{uniqueId} is public — anyone can verify)
   (Why this is important: employers can verify a certificate is real)
## 11.7 What to Say in Your Defense
## 11.8 Self-Check Questions
```

---

## TASK 12 — Unit 12: Payments — Getting Students Paid

**Output file:** `study-materials/unit-12-payments.md`

### Prerequisites
- Read `study-materials/unit-10-execution-modules.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/Payment.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/Transaction.cs` — Full file.
3. `Core/Sh8lny.Abstraction/Services/IPaymentService.cs`
4. `Core/Sh8lny.Service/PaymentService.cs` — Full file. Focus on:
   - `ProcessPaymentAsync`: what it creates, how Paymob is called
   - The webhook handler: how Paymob notifies the server of payment success
   - `GetPaymentHistoryAsync`
5. `Sh8lny.Web/Controllers/PaymentsController.cs` — Full file.
6. `Sh8lny.Shared/DTOs/Payments/` — All files.
7. `context.md` — Section 4.11; `PaymentMethod` enum (`Card`, `Wallet`, `Kiosk`).

### What to Extract and Understand
- What is Paymob? Why use a payment gateway instead of handling card details directly?
- What is the "Order Registration + Webhook" flow? Why two steps?
- What does `PaymobOrderId` store vs. `PaymobTransactionId`?
- What is `GatewayRawResponse` and why store it?
- What is the difference between `Payment` and `Transaction` entities?
- What does `IsPaid` on Application mean and when is it set to true?
- What payment methods are supported: Card, Wallet (mobile wallet), Kiosk (Fawry)?

### Required Output Structure

```
# Unit 12: Payments — How Companies Pay Students Through Sha8alny

## 12.1 Why Use a Payment Gateway?
   (Handling card numbers directly requires PCI-DSS compliance — a massive regulatory burden)
   (A gateway like Paymob handles the sensitive data; Sha8alny only sees order IDs and status)
   (Analogy: using Fawry or Paymob in an Egyptian app — you redirect to their page, they handle the money)
## 12.2 What Is Paymob?
   (Egypt's leading payment gateway; supports card payments, mobile wallets, and Fawry kiosk)
   (How it's used in millions of Egyptian apps)
## 12.3 The Two-Step Payment Flow
   (Step 1 — Order Registration: Company calls POST /api/Payments/pay →
    PaymentService calls Paymob's Order Registration API →
    Paymob returns an order ID → stored in Payment.PaymobOrderId)
   (Step 2 — Webhook: After the user completes payment on Paymob's page,
    Paymob calls our server's webhook endpoint →
    PaymentService updates Payment.Status to Completed → sets Application.IsPaid = true)
## 12.4 The Payment Entity — What Each Field Stores
   (Amount, Currency (EGP), PaymentMethod (Card/Wallet/Kiosk))
   (PaymobOrderId: from Step 1; PaymobTransactionId: from the webhook)
   (GatewayRawResponse: the full JSON response from Paymob, stored for debugging)
   (PaidAt: timestamp set when webhook confirms payment)
## 12.5 Payment vs. Transaction — Two Different Entities
   (Payment: the intent and gateway record — created when company initiates payment)
   (Transaction: the ledger record — created after confirmed completion; stores payer/payee IDs and amount)
   (Why two? Separation of "we tried to pay" from "payment was definitely received")
## 12.6 Payment History
   (GET /api/Payments/history: returns payments filtered by the current user's role)
## 12.7 What to Say in Your Defense
## 12.8 Self-Check Questions
```

---

## TASK 13 — Unit 13: Chat and Messaging

**Output file:** `study-materials/unit-13-chat.md`

### Prerequisites
- Read `study-materials/unit-03-request-lifecycle.md`
- Read `study-materials/unit-08-students-companies.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/Conversation.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/Message.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/ConversationParticipant.cs` — Full file.
4. `Core/Sh8lny.Abstraction/Services/IChatService.cs`
5. `Core/Sh8lny.Service/ChatService.cs` — Full file. Focus on:
   - How a new conversation is created (or reused if one already exists between the same parties)
   - How `SendMessageAsync` saves a message and triggers a notification
   - How `GetConversationsAsync` loads all conversations for a user
   - How `GetMessagesAsync` paginates messages
6. `Sh8lny.Web/Controllers/ChatController.cs` — Full file.
7. `Sh8lny.Shared/DTOs/Chat/` — All files.
8. `context.md` — Section 4.7 (Chat endpoints), 5.1 (What's missing: ChatHub), `ConversationType` and `MessageType` enums.

### What to Extract and Understand
- What is `ConversationType` (Direct vs. Group)? When would a Group conversation exist?
- What is `ConversationParticipant`? Why is it a separate table instead of just `User1` and `User2` columns?
- What does `LastMessageAt` on Conversation enable? (Sorting conversations by most recent activity)
- What is `MessageType` (Text, File, Image, Link)?
- What is `AttachmentURL` and `AttachmentName` on Message?
- What is the current limitation: Chat is REST-based, not real-time? (Real-time is the missing ChatHub from 5.1)
- How does sending a message also trigger a notification?

### Required Output Structure

```
# Unit 13: Chat and Messaging — How Users Communicate Inside Sha8alny

## 13.1 Why In-App Messaging?
   (Keeps communication tracked; both parties have a record; no external apps needed)
## 13.2 The Data Model — Three Tables for Chat
   (Conversation: the channel between 2+ users; ConversationType: Direct vs Group)
   (ConversationParticipant: join table — who is in this conversation)
   (Message: the actual text/file content)
   (Why a join table for participants? Extensibility — group chats can have many participants)
## 13.3 Direct Conversations vs. Group Conversations
   (Direct: between two users, typically a student and a company rep)
   (Group: linked to a ProjectGroup — team members can message together)
## 13.4 The Message Entity
   (Walk through fields: MessageText, MessageType, AttachmentURL, IsRead, IsEdited, SentAt)
   (MessageType: Text (plain chat), File (document), Image, Link)
## 13.5 Sending a Message — The REST Flow
   (POST /api/Chat/send → ChatService.SendMessageAsync →
    find or create conversation → create Message record →
    trigger SignalR notification for real-time delivery — preview of Unit 14)
## 13.6 Loading Conversations and Messages
   (GET /api/Chat/conversations: all conversations for the current user, sorted by LastMessageAt)
   (GET /api/Chat/conversations/{id}/messages: paginated messages for one conversation)
## 13.7 The Current Limitation — Chat Is REST, Not Real-Time (Yet)
   (Explain what real-time means: message appears instantly without refreshing)
   (Current state: message is saved; SignalR notification is sent; but no dedicated ChatHub yet)
   (What's planned: a ChatHub like NotificationHub — explained in Unit 14)
## 13.8 What to Say in Your Defense
## 13.9 Self-Check Questions
```

---

## TASK 14 — Unit 14: Notifications and SignalR — Real-Time Communication

**Output file:** `study-materials/unit-14-notifications-signalr.md`

### Prerequisites
- Read `study-materials/unit-03-request-lifecycle.md`
- Read `study-materials/unit-13-chat.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/Notification.cs` — Full file.
2. `Core/Sh8lny.Abstraction/Services/INotificationService.cs`
3. `Core/Sh8lny.Abstraction/Services/INotifier.cs`
4. `Core/Sh8lny.Service/NotificationService.cs` — Full file. Focus on how notifications are
   created and stored, and how `INotifier` is called for real-time delivery.
5. `Sh8lny.Web/Hubs/NotificationHub.cs` — Full file.
6. `Sh8lny.Web/Services/SignalRNotifier.cs` — Full file. Understand how it implements INotifier,
   how it uses `IHubContext<NotificationHub>`, and why failures are logged but not thrown.
7. `Sh8lny.Web/Controllers/NotificationsController.cs` — Full file.
8. `Sh8lny.Shared/DTOs/Notifications/` — All files.
9. `Sh8lny.Web/Program.cs` — The SignalR configuration and hub mapping sections specifically.
10. `context.md` — Section 4.17 (SignalR); SignalR JWT configuration (reads access_token from query string).

### What to Extract and Understand
- What is the difference between HTTP (request-response) and WebSocket (persistent connection)?
- What is SignalR? Why use it instead of making the client poll every second?
- How does `NotificationHub` know which connection belongs to which user?
- How does `SignalRNotifier` send a notification to a specific user?
- Why is `INotifier` an interface? (Connect to Onion Architecture — inner layers use INotifier,
  not SignalRNotifier directly)
- What is `IHubContext<T>` and why is it used instead of the Hub directly?
- Why does SignalR JWT read from the query string (`access_token`) instead of the header?
- What does `JoinGroup` and `LeaveGroup` enable in the hub?
- What happens if the SignalR delivery fails? (Logged, not thrown — non-blocking)

### Required Output Structure

```
# Unit 14: Notifications and SignalR — How Sha8alny Pushes Updates in Real Time

## 14.1 The Problem with "Please Refresh the Page"
   (HTTP is request-response: the client has to ask; the server can't push unsolicited)
   (The old solution: polling — ask every 5 seconds "anything new?" — expensive and slow)
   (Analogy: the difference between checking your email manually every 5 minutes vs.
    having email notifications that appear instantly on your phone)
## 14.2 What Is WebSocket? What Is SignalR?
   (WebSocket: a persistent two-way connection — the server can push at any time)
   (SignalR: Microsoft's library that makes WebSocket easy to use in .NET and JavaScript)
   (Fallback: SignalR automatically falls back to long-polling if WebSocket isn't available)
## 14.3 The Notification Entity — What Gets Stored in the Database
   (Walk through Notification.cs: UserID (recipient), NotificationType, Title, Message,
    RelatedProjectID/ApplicationID (deep links), ActionURL, IsRead, CreatedAt)
   (Every notification is both stored in DB AND delivered in real-time)
## 14.4 The Architecture: INotifier, NotificationService, and SignalRNotifier
   (INotifier: the interface (inner layer contract) — SendNotificationAsync, SendMessageToUserAsync)
   (SignalRNotifier: the implementation (outer layer) — uses IHubContext<NotificationHub>)
   (NotificationService: orchestrates — saves to DB, then calls INotifier for real-time delivery)
   (Why this separation? Inner layers (service) don't know about SignalR — they only know INotifier)
## 14.5 How NotificationHub Works
   (Hub = the WebSocket endpoint at /hubs/notifications)
   (Authentication: the JWT is passed in the query string as ?access_token=... because WebSocket
    handshakes can't carry custom headers in all clients)
   (User mapping: ASP.NET Core maps each connection to the authenticated User.Identity)
   (JoinGroup / LeaveGroup: for project-specific group notifications)
## 14.6 Delivering a Notification — The Full Flow
   (Example: student submits application → ApplicationService calls NotificationService →
    NotificationService saves Notification to DB →
    NotificationService calls INotifier.SendNotificationAsync(companyUserId, payload) →
    SignalRNotifier calls IHubContext.Clients.User(userId).SendAsync("ReceiveNotification", ...) →
    Company's browser receives the notification instantly)
## 14.7 Failure Handling — Why Notification Failure Never Crashes the App
   (SignalRNotifier wraps every send in try-catch; logs errors; never throws)
   (If the user is offline, SignalR delivery fails silently — they see the notification from
    the DB the next time they call GET /api/Notifications)
## 14.8 The REST Notification Endpoints
   (GET /api/Notifications: load all notifications; GET unread-count; PUT mark-read; PUT read-all)
## 14.9 What to Say in Your Defense
## 14.10 Self-Check Questions
```

---

## TASK 15 — Unit 15: Admin, Settings, and Master Data

**Output file:** `study-materials/unit-15-admin-settings-masterdata.md`

### Prerequisites
- Read `study-materials/unit-06-auth-jwt.md`

### Files to Read
1. `Core/Sh8lny.Domain/Models/UserSettings.cs` — Full file.
2. `Core/Sh8lny.Domain/Models/DashboardMetric.cs` — Full file.
3. `Core/Sh8lny.Domain/Models/ActivityLog.cs` — Full file.
4. `Core/Sh8lny.Domain/Models/Skill.cs`
5. `Core/Sh8lny.Domain/Models/University.cs`
6. `Core/Sh8lny.Domain/Models/Department.cs`
7. `Core/Sh8lny.Abstraction/Services/IAdminService.cs`
8. `Core/Sh8lny.Abstraction/Services/IUserSettingsService.cs`
9. `Core/Sh8lny.Abstraction/Services/IMasterDataService.cs`
10. `Core/Sh8lny.Service/AdminService.cs` — Full file.
11. `Core/Sh8lny.Service/UserSettingsService.cs` — Full file.
12. `Core/Sh8lny.Service/MasterDataService.cs` — Full file.
13. `Sh8lny.Web/Controllers/AdminController.cs` — Full file.
14. `Sh8lny.Web/Controllers/SettingsController.cs` — Full file.
15. `Sh8lny.Web/Controllers/MasterDataController.cs` — Full file.
16. `Sh8lny.Web/Controllers/MaintenanceController.cs` — Full file.
17. `context.md` — Sections 4.13, 4.14, 4.15, 4.16.

### What to Extract and Understand
- What is the Admin role's "God Mode"? What can an Admin do that others cannot?
- What is `DashboardMetric`? How is it populated? What does it aggregate?
- What is `ActivityLog`? What actions does it track?
- What is `UserSettings`? What preferences does it store? (Language, notification toggles, privacy)
- What are "master data" tables? Why are Skills, Universities, and Departments lookup tables?
- How does the seeding process (DbInitializer) pre-populate Skills and Universities?
- What does the `/api/Maintenance/backup` endpoint trigger?

### Required Output Structure

```
# Unit 15: Admin, Settings, and Master Data — Platform Management

## 15.1 The Admin Role — Full System Access
   (What makes Admin different from Student and Company)
   (Admin can: view all users, ban/activate users, view platform statistics, trigger backups)
## 15.2 The Admin Dashboard — DashboardMetric
   (DashboardMetric: a daily snapshot of platform-wide statistics)
   (What it stores: counts of users, projects, applications, completions, payments)
   (How it's populated — preview of Unit 16 (BackupWorker/background services))
## 15.3 ActivityLog — The Audit Trail
   (What ActivityLog tracks: who did what, when, from which IP)
   (Why: accountability, debugging, security audit)
## 15.4 User Management — Ban and Activate
   (PUT /api/Admin/users/{id}/ban: sets User.IsActive = false)
   (Effect: banned user's JWT will be rejected by the login check)
   (PUT /api/Admin/users/{id}/activate: reverses the ban)
## 15.5 User Settings — Each User's Personal Preferences
   (UserSettings entity: notification preferences (push, email), language, profile visibility)
   (One UserSettings row per User — created automatically at registration)
   (GET /api/Settings and PUT /api/Settings: read and update own settings)
## 15.6 Master Data — Lookup Tables That Power Dropdowns
   (Skill: ID + name + category (Backend, Frontend, Mobile, etc.))
   (University: ID + name)
   (Department: ID + name)
   (Why separate tables? Consistency — everyone selects from the same list;
    no typos like "cairo unversity" vs "Cairo University")
   (Seeding: DbInitializer pre-populates Skills and Universities on first startup)
   (Admin-only: add/update/delete skills via /api/MasterData)
## 15.7 Maintenance — On-Demand Backup
   (POST /api/Maintenance/backup: triggers BackupService — preview of Unit 16)
## 15.8 What to Say in Your Defense
## 15.9 Self-Check Questions
```

---

## TASK 16 — Unit 16: Background Services and Infrastructure

**Output file:** `study-materials/unit-16-background-infrastructure.md`

### Prerequisites
- Read `study-materials/unit-03-request-lifecycle.md`
- Read `study-materials/unit-15-admin-settings-masterdata.md`

### Files to Read
1. `Sh8lny.Web/Services/BackupWorker.cs` — Full file.
2. `Infrastructure/Sh8lny.Persistence/BackupService.cs` — Full file.
3. `Infrastructure/Sh8lny.Persistence/Seeding/DbInitializer.cs` — Full file.
4. `Infrastructure/Sh8lny.Persistence/MailService.cs` — Full file.
5. `Sh8lny.Web/Logging/DiscordWebhookLoggerProvider.cs` — Full file.
6. `Sh8lny.Shared/Options/MailSettings.cs`
7. `Sh8lny.Web/Program.cs` — The BackupWorker registration, seeding call (`DbInitializer.SeedAsync`),
   migration call (`context.Database.MigrateAsync()`), and Discord logger registration sections.
8. `context.md` — Section 4.18 (Infrastructure table), section 5.1 (RequestTimingMiddleware).

### What to Extract and Understand
- What is a "hosted service" / background service in ASP.NET Core?
- What does BackupWorker do? When does it run? How often?
- What does BackupService actually backup? What is the 7-day retention policy?
- What does `DbInitializer.SeedAsync` do on startup? What data does it create?
- Why does the app run `MigrateAsync()` on startup? Is this safe for production?
- How does MailService work? What SMTP settings does it use?
- What is the Discord webhook logger? Why log to Discord?
- What is `RequestTimingMiddleware`? What does it log?

### Required Output Structure

```
# Unit 16: Background Services and Infrastructure — What Runs Behind the Scenes

## 16.1 What Is a Background Service?
   (A background service runs independently of HTTP requests — it does work on its own schedule)
   (Analogy: your phone has an app for calls (HTTP requests) but the system automatically
    checks for updates in the background — that's a background service)
## 16.2 BackupWorker — The Automatic Database Backup
   (IHostedService: an ASP.NET Core interface for background workers)
   (BackupWorker runs every 24 hours)
   (What it does: calls BackupService → creates a backup file → deletes backups older than 7 days)
   (The /api/Maintenance/backup endpoint triggers this on demand — connects to Unit 15)
## 16.3 Database Seeding — Starting With Good Data
   (DbInitializer.SeedAsync: runs once on startup if the database is empty)
   (What it creates: the list of Skills (Backend, Frontend, etc.), Egyptian universities,
    and demo data for development)
   (Why seed? Without it, the skills dropdown would be empty; students couldn't fill their profiles)
## 16.4 Auto-Migration on Startup — Keeping Schema in Sync
   (context.Database.MigrateAsync(): applies any pending migrations on startup)
   (Why: on Cloud Run, there's no manual deployment step — migrations run automatically)
   (Risk: if a migration has a bug, the app won't start — this is intentional; fail fast)
## 16.5 Email — How Sha8alny Sends Emails
   (MailService uses SMTP via Gmail: host, port, username, password from configuration)
   (What triggers emails: registration (OTP), forgot password (reset token))
   (MailSettings: loaded from appsettings.json or environment variables on Cloud Run)
## 16.6 Discord Webhook Logger — Seeing Errors in Real Time
   (DiscordWebhookLoggerProvider: an ILoggerProvider that sends log messages to a Discord channel)
   (Why Discord? Free, instant, visible to the whole team without logging into the server)
   (What gets logged: errors, warnings, critical events — connects to the middleware timing logs)
## 16.7 Request Timing Middleware
   (Records: HTTP method, path, status code, elapsed milliseconds for every request)
   (Why: performance monitoring — if an endpoint is slow, you see it in the Discord log)
## 16.8 The Startup Sequence — What Happens When the App Boots
   (In order: build configuration → register DI services → apply migrations →
    seed database → start middleware pipeline → start background workers → accept requests)
## 16.9 What to Say in Your Defense
## 16.10 Self-Check Questions
```

---

## TASK 17 — Unit 17: End-to-End Trace — One Feature, Every Layer

**Output file:** `study-materials/unit-17-end-to-end-trace.md`

### Prerequisites
Read ALL previously generated unit files before starting this unit:
- `study-materials/unit-01-helicopter-view.md`
- `study-materials/unit-02-code-map.md`
- `study-materials/unit-03-request-lifecycle.md`
- `study-materials/unit-04-database-layer.md`
- `study-materials/unit-05-dtos-mapper-response.md`
- `study-materials/unit-06-auth-jwt.md`
- `study-materials/unit-07-file-uploads.md`
- `study-materials/unit-08-students-companies.md`
- `study-materials/unit-09-projects-applications.md`
- `study-materials/unit-10-execution-modules.md`
- `study-materials/unit-11-reviews-certificates.md`
- `study-materials/unit-12-payments.md`
- `study-materials/unit-13-chat.md`
- `study-materials/unit-14-notifications-signalr.md`
- `study-materials/unit-15-admin-settings-masterdata.md`
- `study-materials/unit-16-background-infrastructure.md`

Also re-read these source files:
1. `Sh8lny.Web/Controllers/ApplicationsController.cs`
2. `Core/Sh8lny.Service/ApplicationService.cs`
3. `Infrastructure/Sh8lny.Persistence/Repositories/GenericRepository.cs`
4. `Infrastructure/Sh8lny.Persistence/Repositories/UnitOfWork.cs`
5. `Core/Sh8lny.Domain/Models/Application.cs`
6. `Sh8lny.Web/Program.cs`

### What to Extract and Understand
This unit synthesizes everything. The chosen scenario is: **A student submits an application for a project**.

Trace this single action — `POST /api/Applications/apply` — through every layer of the system:

1. The student's phone/browser sends an HTTP POST request. What headers does it include? (Authorization: Bearer <token>)
2. The request arrives at the server. What middleware runs first? (Request Timing → CORS → Authentication → Authorization)
3. Authentication middleware: what does it do with the JWT? How does it populate `User.Claims`?
4. The request reaches `ApplicationsController.Apply()`. How does the controller get `IApplicationService`? (DI — from Unit 3)
5. The controller calls `GetCurrentUserId()`. How? (Reads `ClaimTypes.NameIdentifier` from the JWT claims)
6. The controller calls `_applicationService.ApplyAsync(userId, dto)`. The controller doesn't know HOW this works — it only knows the interface.
7. `ApplicationService.ApplyAsync` runs. What does it check first? (Does the project exist? Has the student already applied? Is the deadline past? Is the project open?)
8. How does it fetch the project? (`_unitOfWork.Projects.GetQueryable().Include(...).FirstOrDefaultAsync(...)`)
9. How does EF Core translate that query to SQL? (It generates a SELECT with JOINs)
10. What does it create? (A new `Application` entity with Status = `Submit`)
11. How does it save? (`_unitOfWork.Applications.AddAsync(app)` then `_unitOfWork.SaveChangesAsync()`)
12. What SQL does EF Core generate for the save? (An INSERT INTO Applications...)
13. What notification is triggered? (`_notificationService.CreateAndSendAsync(companyUserId, ...)`)
14. How does the notification reach the company in real time? (SignalRNotifier → IHubContext → WebSocket)
15. What does `ApplicationService` return? (`ServiceResponse<int>.Success(applicationId)`)
16. How does the controller turn that into an HTTP response? (`return Ok(response.Data)` or `return BadRequest(response.Message)`)
17. What does the student's phone receive? (HTTP 200 with the new Application ID in the body)

### Required Output Structure

```
# Unit 17: End-to-End Trace — Following One Action Through Every Layer

## 17.1 Introduction: Why This Unit Exists
   (You've learned each layer separately — now let's watch them work together)
   (The action: a student submits an application for a project)
   (Why this action? It touches almost every system: auth, database, business logic, notifications, real-time)
## 17.2 Before the Request — Setup (JWT and Client)
   (The student already logged in; they have a JWT in their app/browser)
   (What the JWT contains: UserID=42, Role="Student", Email="ahmed@example.com", expires in 60 min)
   (The client sends: POST /api/Applications/apply with Authorization: Bearer <jwt> and the application JSON body)
## 17.3 Stage 1 — The Middleware Gauntlet
   (Request Timing Middleware: starts the stopwatch)
   (CORS: checks if the origin is allowed — yes, AllowAll policy)
   (Authentication: validates the JWT signature → populates User.Claims)
   (Authorization: checks if the endpoint requires [Authorize(Roles = "Student")] → yes → pass)
## 17.4 Stage 2 — Entering the Controller
   (How DI gave ApplicationsController its IApplicationService — set up in Program.cs at startup)
   (Controller calls GetCurrentUserId() → reads NameIdentifier claim → returns 42)
   (Controller deserializes the request body into ApplyForProjectDto)
   (Controller calls: await _applicationService.ApplyAsync(42, dto))
## 17.5 Stage 3 — Business Logic in ApplicationService
   (Service checks: does the project exist? Is it Open? Has the deadline passed? Already applied?)
   (Each check = a database query; show what the IQueryable chain looks like)
   (Validation passes → create Application entity: Status=Submit, StudentID=42, ProjectID=dto.ProjectID)
   (Call _unitOfWork.Applications.AddAsync(application))
   (Call _unitOfWork.SaveChangesAsync() → EF Core generates INSERT INTO Applications SQL)
## 17.6 Stage 4 — The Database
   (EF Core translates the entity to SQL: INSERT INTO Applications (StudentID, ProjectID, Status, AppliedAt, ...) VALUES (42, 7, 'Submit', ...))
   (SQL Server executes it; returns the new ApplicationID = 1001)
   (EF Core updates the entity's ApplicationID property with the returned value)
## 17.7 Stage 5 — Notification and Real-Time Delivery
   (Service calls _notificationService.CreateAndSendAsync(companyUserId, "New Application", ...))
   (NotificationService: INSERT INTO Notifications → then calls INotifier.SendNotificationAsync)
   (SignalRNotifier: calls IHubContext.Clients.User(companyUserId).SendAsync("ReceiveNotification", ...))
   (If the company is online: their browser receives the push instantly via WebSocket)
   (If offline: the notification sits in the DB; they'll see it on next login)
## 17.8 Stage 6 — The Response Travels Back
   (ApplicationService returns ServiceResponse<int>.Success(1001, "Application submitted"))
   (ApplicationsController: checks response.IsSuccess → true → return Ok(response.Data))
   (ASP.NET Core serializes this to JSON: {"applicationId": 1001})
   (Request Timing Middleware: stops the stopwatch → logs "POST /api/Applications/apply 200 OK 45ms")
   (HTTP 200 response with body {"applicationId": 1001} arrives at the student's phone)
## 17.9 The Complete Picture — Every Layer in One Diagram
   (Text diagram showing the full vertical slice:
    Phone → HTTP → Middleware → Controller → IApplicationService → ApplicationService →
    IUnitOfWork → UnitOfWork → IGenericRepository<Application> → GenericRepository →
    EF Core → SQL Server → (returns) → EF Core → GenericRepository → UnitOfWork →
    ApplicationService → INotificationService → NotificationService → INotifier →
    SignalRNotifier → WebSocket → Company's browser
    → ApplicationService → ServiceResponse → Controller → HTTP 200 → Student's phone)
## 17.10 Connecting the Threads — What Each Unit Contributed
   (Unit 2 explained why layers exist; Unit 3 explained DI and middleware; Unit 4 explained
    EF Core; Unit 5 explained DTOs and ServiceResponse; Unit 6 explained JWT auth;
    Unit 9 explained Application; Unit 14 explained SignalR — all came together here)
## 17.11 What to Say in Your Defense
   (This section should be especially thorough — 8-10 points — since this is the most likely
    question in a defense: "Walk us through what happens when a student applies for a project")
## 17.12 Self-Check Questions
```

### Tone Reminders for This Unit
- This is the grand synthesis. Be the most detailed here of any unit.
- Every reference to a layer should link back: "(this is the Onion Architecture from Unit 2)",
  "(this is the INotifier from Unit 14)", etc.
- The text diagram in section 17.9 is critical — make it readable and complete.
- The "What to Say in Your Defense" section in 17.11 should be the most polished, rehearsal-ready
  text in the entire curriculum.

---

## FINAL TASK — Curriculum Index

**Output file:** `study-materials/curriculum-index.md`

### Prerequisites
Read ALL 17 generated unit files before writing this index:
- `study-materials/unit-01-helicopter-view.md` through `study-materials/unit-17-end-to-end-trace.md`

### Purpose
This file is the **table of contents for NotebookLM** (or any study tool). A student or professor
should be able to read this file and know exactly what each unit covers and in what order to study.

### Required Output Structure

```
# Sha8alny Backend — Complete Study Materials Index

> **Project:** Sha8alny (شغلني) — Freelancing & Field Training Platform  
> **Stack:** ASP.NET Core 9 Web API, EF Core 9, SQL Server 2022, SignalR, AutoMapper, JWT  
> **Architecture:** Strict Onion Architecture (Domain → Abstraction → Service → Persistence → Web)  
> **Audience:** 2nd-year engineering students preparing for graduation project defense  
> **Total Units:** 17 + this index  

## How to Use These Materials
[Brief paragraph: recommended reading order, how each unit builds on previous ones,
 what "Defense-Ready Talking Points" and "Self-Check Questions" sections are for]

## Unit Summaries

### Unit 1: The Helicopter View
[One paragraph: what this unit covers, what the student will be able to explain after reading it]

### Unit 2: Map of the Codebase
[One paragraph summary]

### Unit 3: How a Request Travels
[One paragraph summary]

### Unit 4: The Database Layer
[One paragraph summary]

### Unit 5: DTOs, AutoMapper, and ServiceResponse
[One paragraph summary]

### Unit 6: Authentication and JWT
[One paragraph summary]

### Unit 7: File Uploads and the Media Pattern
[One paragraph summary]

### Unit 8: Students and Companies
[One paragraph summary]

### Unit 9: Projects and Applications
[One paragraph summary]

### Unit 10: Execution and Modules
[One paragraph summary]

### Unit 11: Reviews and Certificates
[One paragraph summary]

### Unit 12: Payments
[One paragraph summary]

### Unit 13: Chat and Messaging
[One paragraph summary]

### Unit 14: Notifications and SignalR
[One paragraph summary]

### Unit 15: Admin, Settings, and Master Data
[One paragraph summary]

### Unit 16: Background Services and Infrastructure
[One paragraph summary]

### Unit 17: End-to-End Trace
[One paragraph summary]

## Recommended Study Paths

### Path A: Complete Beginner (8-10 hours)
Units 1 → 2 → 3 → 4 → 6 → 9 → 17

### Path B: Defense Preparation (4-5 hours)
Units 1 → 2 → 3 → 17 → (any units from the features you will be questioned on)

### Path C: Full Curriculum (20-25 hours)
Units 1 through 17 in order

## Glossary of Key Terms
[A master glossary combining all "New Terms" from all units, alphabetically sorted,
 with one-sentence plain-language definitions and "(see Unit N)" cross-references]
```

### Tone Reminders for This Unit
- The summaries must be accessible: assume a professor who has not read any unit could read
  this index and understand what the student learned.
- The glossary must be thorough — this is the most valuable quick-reference artifact in the curriculum.
- The study paths must be realistic: explain who each path is for and how long it should take.

---

## END OF STUDY_PLAN.md

> **Checklist for DeepSeek before starting:**
> - [ ] Completed Task 0 (Orientation Scan)
> - [ ] Output folder `study-materials/` exists (create it if not)
> - [ ] Starting with Unit 1, not skipping ahead
> - [ ] Each unit reads its listed prerequisite files before writing
> - [ ] Every unit follows the Universal Tone & Style Rules at the top of this file
> - [ ] No unit references a later unit as a prerequisite (only earlier units)
