# Unit 2: Map of the Codebase — Why Is the Code Split Into So Many Projects?

> **Before reading this unit:** You should have read Unit 1, which explains what Sha8alny does and who uses it. This unit explains how the code is organized — not what individual features do, but why the code is structured the way it is.

---

## 2.1 The Problem with Putting Everything in One Project

Imagine a restaurant where the chef, the waiter, the accountant, and the cleaning staff all work in the same tiny room with no separation. Every time the chef wants to change a recipe, they have to ask the accountant if it will affect the pricing system. Every time the waiter takes an order, they walk through the kitchen and bump into the cleaning staff.

It works — barely — when there are three people. But when the restaurant grows, the chaos becomes unbearable. A change in one area accidentally breaks something in another.

Software has the same problem. If you put all your code — database logic, business rules, web endpoints, email sending — into one single file or project, every change becomes risky. Touching the database layer might accidentally break the API. Changing a business rule might accidentally expose sensitive data. Testing one feature requires setting up everything else.

The solution is **separation of concerns** — split the code into separate zones, where each zone has one clear job and cannot accidentally reach into another zone's business.

Sha8alny uses this principle to its fullest extent. The code is split into **7 separate C# projects**, each with a single responsibility.

---

## 2.2 The Seven Projects and What Each One Does

Think of each project as a department in a large company. Each department has a clearly defined job, and they communicate through formal channels — not by walking into each other's offices unannounced.

---

### `Sh8lny.Domain` — The Rule Book
**Job:** Defines the data structures (entities) that represent real-world things in the system.

**What lives here:** The 28 C# class files that represent things like `User`, `Student`, `Company`, `Project`, `Application`, `Certificate`, etc. Also the enums (`ProjectStatus`, `ApplicationStatus`, `UserType`, etc.).

**What it CAN reference:** Nothing. No other project. No external libraries (except the basic .NET runtime).

**What it CANNOT reference:** Everything else. It knows nothing about databases, web requests, emails, or business logic.

**Why this matters:** The definition of what a "Student" is should never depend on how you store it in a database or how you serve it over HTTP. If you decide to change from SQL Server to another database, the `Student` class should not need to change at all.

---

### `Sh8lny.Abstraction` — The Job Descriptions
**Job:** Defines the *contracts* (interfaces) for everything the system can do — without saying HOW it does it.

**What lives here:** Two groups of interface files:
- Repository interfaces: `IGenericRepository<T>` (get/add/update/delete any entity), `IUnitOfWork` (a single entry point for all repositories)
- Service interfaces: `IAuthService`, `IProjectService`, `IStudentService`, `IApplicationService`, and 13 more — one for each major feature area

**What it CAN reference:** `Sh8lny.Domain` (because interfaces reference the entity types), `Sh8lny.Shared` (for DTOs).

**What it CANNOT reference:** `Sh8lny.Service`, `Sh8lny.Persistence`, `Sh8lny.Web`.

**Think of it like this:** A job description at a hospital says "The doctor must be able to diagnose patients and prescribe medicine." It does NOT say "Dr. Ahmed will do this using his specific method." The description defines WHAT, not WHO or HOW. That is exactly what an interface does.

---

### `Sh8lny.Service` — The Business Logic Engine
**Job:** Contains the actual business logic — the code that makes decisions, validates rules, and coordinates work.

**What lives here:** 17 service implementation files: `AuthService.cs`, `ProjectService.cs`, `ApplicationService.cs`, `StudentService.cs`, etc. Each one implements one of the interfaces from `Sh8lny.Abstraction`.

**What it CAN reference:** `Sh8lny.Abstraction` (to implement the interfaces), `Sh8lny.Domain` (to work with entities).

**What it CANNOT reference:** `Sh8lny.Persistence` (no direct database access), `Sh8lny.Web` (no knowledge of HTTP or controllers).

**Why this matters:** The business rule "a student cannot apply twice to the same project" lives here. This rule has nothing to do with whether the data comes from SQL Server or a JSON file. By keeping business logic separate from database code, you can test the rules without a real database.

---

### `Sh8lny.Persistence` — The Database Handler
**Job:** Everything related to actually reading and writing data to SQL Server.

**What lives here:**
- `Sha8lnyDbContext.cs` — the EF Core database context (the "connection" to the database)
- `GenericRepository.cs` — the implementation of `IGenericRepository<T>` (the actual database operations)
- `UnitOfWork.cs` — the implementation of `IUnitOfWork`
- 28 Fluent API configuration files (teaching EF Core the exact table structure)
- 8 migration files (the version-controlled history of database schema changes)
- `DbInitializer.cs` — seeds the database with starter data on first launch
- `MailService.cs`, `BackupService.cs` — infrastructure services

**What it CAN reference:** `Sh8lny.Abstraction` (to implement interfaces), `Sh8lny.Domain` (to work with entities), `Sh8lny.Shared` (for options/configuration).

**What it CANNOT reference:** `Sh8lny.Service`, `Sh8lny.Web`.

---

### `Sh8lny.Shared` — The Common Vocabulary
**Job:** Holds everything that multiple layers need to use — without creating circular dependencies.

**What lives here:**
- All DTOs (Data Transfer Objects) — the "shapes" of data sent to and received from the API (grouped into subfolders: Auth, Projects, Applications, Students, etc.)
- Options classes (`JwtOptions`, `MailSettings`) — configuration value containers
- Custom validation attributes (`AllowedFileExtensionsAttribute`)

**What it CAN reference:** Nothing. Zero dependencies. It has no project references at all.

**What it CANNOT reference:** Anything. It is the one project that everyone imports but that imports no one.

**Why the exception?** Every layer needs DTOs: the Service layer receives them from the controller, the controller sends them back to the client, and tests use them. If DTOs lived in `Sh8lny.Domain`, then Domain would need to reference web concepts. If they lived in `Sh8lny.Web`, then Services could not use them without knowing about the web layer. The solution: put them in a standalone project that everyone can safely import.

---

### `Sh8lny.Web` — The Front Door
**Job:** The composition root — the entry point of the application. Receives HTTP requests, routes them to the right service, and sends responses back.

**What lives here:**
- `Program.cs` — starts the app, wires up all the dependencies, configures middleware
- 16 controller files — handle the HTTP endpoints (`ProjectsController`, `AuthController`, etc.)
- `NotificationHub.cs` — the SignalR real-time WebSocket hub
- `SignalRNotifier.cs`, `BackupWorker.cs` — web-layer services
- `MappingProfile.cs` — tells AutoMapper how to convert entities to DTOs and back
- `DiscordWebhookLoggerProvider.cs` — sends log messages to a Discord channel

**What it CAN reference:** Everything. It is the outermost layer — the one that glues all the others together.

---

### `Sh8lny.Presentation` — Reserved
**Job:** Currently empty/minimal. Reserved for future use (e.g., a Blazor web UI or gRPC endpoints).

**What lives here:** Essentially nothing. An empty project shell.

---

## 2.3 The Onion Architecture — The Rule That Holds It All Together

The way these seven projects are structured has a name: **Onion Architecture**. Picture an onion cut in half:

```
         ┌─────────────────────────────┐
         │         Sh8lny.Web          │  ← Outermost layer (HTTP, controllers, SignalR)
         │   ┌─────────────────────┐   │
         │   │  Sh8lny.Persistence │   │  ← Outer-middle (database, EF Core)
         │   │  ┌───────────────┐  │   │
         │   │  │ Sh8lny.Service│  │   │  ← Middle (business logic)
         │   │  │  ┌─────────┐  │  │   │
         │   │  │  │Abstrac- │  │  │   │  ← Inner-middle (interfaces/contracts)
         │   │  │  │  tion   │  │  │   │
         │   │  │  │ ┌─────┐ │  │  │   │
         │   │  │  │ │Domai│ │  │  │   │  ← Core/innermost (pure data models)
         │   │  │  │ │  n  │ │  │  │   │
         │   │  │  │ └─────┘ │  │  │   │
         │   │  │  └─────────┘  │  │   │
         │   │  └───────────────┘  │   │
         │   └─────────────────────┘   │
         └─────────────────────────────┘
                  Sh8lny.Shared (referenced by all, depends on none)
```

**The Golden Rule:** Dependencies only point **inward**. The outer layers know about the inner layers. The inner layers know nothing about the outer layers.

- `Sh8lny.Web` can reference everything.
- `Sh8lny.Persistence` can reference `Abstraction` and `Domain`.
- `Sh8lny.Service` can reference `Abstraction` and `Domain`.
- `Sh8lny.Abstraction` can reference only `Domain`.
- `Sh8lny.Domain` references nothing.

This is enforced by the `.csproj` files — the project reference configuration files. If a developer accidentally adds a reference from `Domain` to `Persistence`, the compiler will refuse to build.

---

## 2.4 Why Can't the Database Layer Know About the Web Layer?

This might seem like unnecessary rules. Why does it matter which project knows about which?

Think about it practically. Suppose you are using SQL Server today. But next year, your university project gets picked up by a company that prefers PostgreSQL. If your database code is tangled with your business logic, you have to rewrite both. If they are separate, you only change `Sh8lny.Persistence` — the business rules in `Sh8lny.Service` stay exactly the same.

Or suppose you want to test the business rule "a student cannot apply twice to the same project." If this logic is inside a controller (which requires an HTTP server) or inside a database query (which requires a real SQL Server), testing it is a nightmare. But because the rule lives in `ApplicationService`, you can test it by passing in a fake repository that returns whatever data you want. No real database needed.

The separation also makes the codebase easier to understand. When you open `Sh8lny.Service`, you know you are reading business logic — nothing else. When you open `Sh8lny.Persistence`, you know you are reading database code — nothing else.

---

## 2.5 Sh8lny.Shared — The One Exception

You might notice that `Sh8lny.Shared` breaks the "only point inward" rule — it is referenced by everyone: `Web`, `Persistence`, `Service`, and `Abstraction`. But it references nobody.

This is intentional. `Sh8lny.Shared` is not a layer — it is a **utility library**. It contains things that have no dependency direction: DTOs are just plain data containers (no logic), options classes are just configuration holders, and validation attributes are simple annotations.

Think of it like a shared Google Doc that every department in a company can read. The doc does not "depend on" any department — it just provides a common reference point.

---

## 2.6 How the Projects Talk to Each Other (Interfaces)

You might wonder: if `Sh8lny.Service` cannot reference `Sh8lny.Persistence`, how does a service ever read data from the database?

The answer is **interfaces**. Instead of the service directly calling `GenericRepository` (which is in Persistence), it calls `IGenericRepository` (which is in Abstraction). The service says "I need something that can give me a list of projects" — and it does not care whether that something is a SQL Server database, a JSON file, or a test helper.

At startup (in `Program.cs`), the Web layer wires everything together:

```csharp
// "When someone asks for IGenericRepository<T>, give them GenericRepository<T>"
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// "When someone asks for IProjectService, give them ProjectService"
builder.Services.AddScoped<IProjectService, ProjectService>();
```

This wiring is called **Dependency Injection** — and it will be fully explained in Unit 3.

---

## 2.7 What to Say in Your Defense

- "We used Onion Architecture to separate concerns into 7 distinct projects, each with a single responsibility. This ensures that business logic never depends on database implementation details, and database code never depends on HTTP or web concepts."
- "The dependency direction always flows inward — from Web to Service to Abstraction to Domain. The inner layers are completely independent of the outer layers. This is enforced at compile time by the project references in the `.csproj` files."
- "If we wanted to swap SQL Server for a different database tomorrow, we would only change `Sh8lny.Persistence`. All 17 business logic services and all 16 controllers would remain untouched."
- "`Sh8lny.Domain` has zero external dependencies — it is pure C# with no NuGet packages. This means the core data model is completely portable and testable in isolation."
- "The `Sh8lny.Shared` project is a utility library — it contains DTOs and configuration objects that all layers need, but it depends on nothing itself, so it creates no circular dependencies."

---

## 2.8 Self-Check Questions

**Q1: How many separate C# projects are in the solution, and what is the innermost one?**
7 projects. The innermost is `Sh8lny.Domain`.

**Q2: Can `Sh8lny.Service` directly call `GenericRepository.cs`?**
No. `Sh8lny.Service` cannot reference `Sh8lny.Persistence`. It can only use `IGenericRepository<T>`, which is in `Sh8lny.Abstraction`.

**Q3: Where does the business rule "a student cannot apply to the same project twice" live?**
In `Sh8lny.Service` — specifically in `ApplicationService.cs`. It is business logic, not database logic or web logic.

**Q4: Why does `Sh8lny.Shared` reference no other project?**
Because it only contains plain data containers (DTOs) and configuration classes. These have no logic that depends on databases, web requests, or business rules. Every layer can safely import it without creating a circular dependency.

**Q5: What file wires all the layers together at runtime?**
`Program.cs` in `Sh8lny.Web`. It registers every service and its implementation in the DI container.

**Q6: What would break if you added a reference from `Sh8lny.Domain` to `Sh8lny.Persistence`?**
The Onion Architecture rule would be broken — the innermost layer would now depend on an outer layer. This creates a circular dependency and makes it impossible to use the domain models without also having a database available.

**Q7: What is the difference between `Sh8lny.Abstraction` and `Sh8lny.Service`?**
`Sh8lny.Abstraction` contains the interface definitions — the "what" (contracts). `Sh8lny.Service` contains the implementations — the "how" (actual code that makes things happen).
