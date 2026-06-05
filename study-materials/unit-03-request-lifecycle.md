# Unit 3: How a Request Travels — From Your Phone to the Database and Back

> **Before reading this unit:** Make sure you have read Unit 1 (what the system does) and Unit 2 (how the code is split into layers). This unit explains what happens, step by step, when a user's app sends a request to the server.

---

## 3.1 The Story of One Request

Ahmad opens the Sha8alny app on his phone and taps "Browse Projects." His app sends a message to the server: "Give me a list of open projects."

Here is what happens, in plain language, before his screen shows anything:

1. The message (an HTTP request) arrives at the server over the internet.
2. The server checks: "Is this request coming from an allowed source?" (CORS check)
3. The server checks: "Is the user logged in? Let me read their identity from the token they sent." (Authentication)
4. The server checks: "Does this user have permission to call this endpoint?" (Authorization)
5. The request reaches the `ProjectsController` — the code that handles project-related requests.
6. The controller asks the `IProjectService` to "find all open projects and return them."
7. The `ProjectService` (the business logic) figures out the right query and asks the repository for data.
8. The repository (which wraps EF Core) translates the request into a SQL `SELECT` statement and sends it to SQL Server.
9. SQL Server returns rows of data.
10. EF Core converts the rows into C# objects (Project entities).
11. The service maps those entities to DTOs (a simpler shape, safe to expose publicly).
12. The response travels back through the controller, which wraps it in HTTP and sends it to Ahmad's phone.
13. Ahmad's screen shows the project list.

Total time for all of this: typically 30–100 milliseconds.

Now let us understand each step in detail.

---

## 3.2 What Is Middleware? (The Airport Security Analogy)

When you travel internationally, you do not walk straight from the street onto the plane. You pass through multiple checkpoints: check-in, security scan, passport control, gate check. Each checkpoint does one specific thing. If you fail any of them, you do not get on the plane.

**Middleware in a web server works exactly the same way.** Every incoming HTTP request passes through a series of middleware components, one after another, before it reaches the actual code that handles it. Each middleware does one specific job. If a check fails (e.g., the user is not logged in), the request is rejected immediately and never reaches the controller.

In Sha8alny, the middleware pipeline has these checkpoints:

| Checkpoint | What It Does |
|------------|--------------|
| **Swagger UI** | Serves the API documentation page at the root URL `/` |
| **HTTPS Redirect** | If the request came in on HTTP, redirect it to HTTPS (only in production) |
| **Static Files** | If the request is for a file (like an uploaded image), serve it directly from `wwwroot/` |
| **Request Timing** | Starts a stopwatch when the request arrives; logs the time when the response is sent |
| **CORS** | Checks if the request is coming from an allowed origin (configured as "AllowAll" — any origin is accepted) |
| **Authentication** | Reads the JWT token (if present), validates it, and identifies who the user is |
| **Authorization** | Checks if the identified user has permission to call this specific endpoint |
| **Controllers** | Routes the request to the right controller method |
| **SignalR Hub** | If the request is for `/hubs/notifications`, handles it as a WebSocket connection instead |

---

## 3.3 The Sha8alny Middleware Pipeline — Every Gate in Order

Here is the exact order (from `Program.cs`) with a plain-language explanation of each:

**1. Swagger / SwaggerUI**
Makes the interactive API documentation available. When a developer visits the root URL of the server in a browser, they see a page where they can test every endpoint. This runs first so the documentation is always accessible.

**2. HTTPS Redirect** *(only outside Development)*
Forces all traffic to use the secure encrypted channel. If you type `http://...`, you get redirected to `https://...` automatically.

**3. Static Files**
Before any code runs, the server checks: "Is the request for a file that already exists on disk?" If a student uploaded their CV and someone requests `/uploads/cvs/ahmad.pdf`, this middleware serves the file directly — no controller needed.

**4. Request Timing Middleware** *(custom, inline in Program.cs)*
Records exactly when the request arrived, waits for the full response to be sent, then logs: `"HTTP GET /api/Projects responded 200 in 42.5ms"`. This log goes to the Discord webhook, so the team can see slow endpoints.

**5. CORS** (`app.UseCors("AllowAll")`)
CORS stands for Cross-Origin Resource Sharing. Browsers block requests that come from a different domain than the server, as a security measure. The "AllowAll" policy tells the server to accept requests from any domain — any React frontend, any Flutter app, any Swagger tool.

**6. Authentication** (`app.UseAuthentication()`)
Reads the `Authorization: Bearer <token>` header (or `?access_token=...` for WebSocket connections). If a JWT is found, it validates: Is the signature correct? Is it expired? If valid, it populates the request's `User` object with the user's identity and claims (UserID, email, role). If no token is present or it is invalid, `User` remains anonymous — but the request is NOT rejected here. That happens at the next gate.

**7. Authorization** (`app.UseAuthorization()`)
Now checks: does the endpoint require a logged-in user (`[Authorize]`)? Does it require a specific role (`[Authorize(Roles = "Company")]`)? If the user does not meet the requirement, the request is rejected with `401 Unauthorized` or `403 Forbidden`. Endpoints marked `[AllowAnonymous]` skip this check.

**8. Controllers** (`app.MapControllers()`)
Routes the request to the correct controller method based on the URL and HTTP method. `GET /api/Projects` goes to `ProjectsController.GetProjects()`. `POST /api/Applications/apply` goes to `ApplicationsController.Apply()`.

**9. SignalR Hub** (`app.MapHub<NotificationHub>("/hubs/notifications")`)
If the request is for the WebSocket endpoint `/hubs/notifications`, it is handled as a persistent real-time connection instead of a normal HTTP request/response. Explained in detail in Unit 14.

---

## 3.4 What Is Dependency Injection? (The "Supply Closet" Analogy)

Imagine a doctor at a hospital. When the doctor needs a specific medicine, they do not manufacture it themselves. They open the supply closet and take what they need. The hospital made sure the closet is stocked with everything a doctor might need.

If the hospital runs out of one brand of medicine and switches to another brand, the doctor does not need to change anything about how they practice medicine — they just open the closet and get the new brand.

**Dependency Injection (DI) works exactly this way in software.**

Without DI, a controller would have to create its own service:

```csharp
// ❌ WRONG — tight coupling
public class ProjectsController
{
    private ProjectService _service = new ProjectService(
        new UnitOfWork(new Sha8lnyDbContext(...))
    ); // ← the controller has to know HOW to build everything
}
```

This is a problem because:
- If `ProjectService` changes its constructor, you have to fix every controller that uses it.
- You cannot test the controller with a fake service — it always uses the real one.
- The controller knows about database contexts and repositories — violating the Onion rules from Unit 2.

With DI, the controller just asks for what it needs:

```csharp
// ✅ CORRECT — loose coupling via DI
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;  // ← asks for the interface, not the class

    public ProjectsController(IProjectService projectService)  // ← "someone give me this please"
    {
        _projectService = projectService;
    }
}
```

And in `Program.cs`, the "supply closet" is stocked:

```csharp
// "When someone asks for IProjectService, give them ProjectService"
builder.Services.AddScoped<IProjectService, ProjectService>();

// "When someone asks for IUnitOfWork, give them UnitOfWork"
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

When `ProjectsController` is created to handle a request, ASP.NET Core automatically reads its constructor, sees it needs an `IProjectService`, looks up the registration, creates a `ProjectService` (which in turn gets a `UnitOfWork`, which in turn gets a `Sha8lnyDbContext`), and hands the whole assembled chain to the controller. The controller never knew what it was getting — just that it works.

**What is "Scoped"?** All services in Sha8alny are registered as `Scoped`, which means a new instance is created for each HTTP request and shared within that request. When the request is done, the instance is discarded. This is the right choice for database contexts (which track changes within a request and should be fresh per request).

---

## 3.5 From Controller to Database — The Full Chain

Here is the complete vertical path for one request, with each layer labeled:

```
HTTP Request (from phone/browser)
        │
        ▼
┌─────────────────────┐
│  Middleware Pipeline │  ← Swagger, CORS, Auth, Authorization, Timing
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│  ProjectsController  │  ← Sh8lny.Web layer
│  (HTTP: GET /api/Projects)
│  1. Extract UserID from JWT claims
│  2. Call _projectService.GetProjectsAsync(filters)
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│  IProjectService     │  ← Sh8lny.Abstraction (contract)
│  ↓                   │
│  ProjectService      │  ← Sh8lny.Service (business logic)
│  1. Validate inputs
│  2. Call _unitOfWork.Projects.FindAsync(...)
│  3. Map results to DTOs
│  4. Return ServiceResponse<List<ProjectResponseDto>>
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│  IUnitOfWork         │  ← Sh8lny.Abstraction (contract)
│  ↓                   │
│  UnitOfWork          │  ← Sh8lny.Persistence (implementation)
│  Exposes: .Projects, .Applications, .Students, ...
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│ IGenericRepository   │  ← Sh8lny.Abstraction (contract)
│  ↓                   │
│ GenericRepository    │  ← Sh8lny.Persistence (implementation)
│  FindAsync(p => p.Status == Active && p.IsVisible == true)
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│   EF Core / LINQ     │  ← Translates C# expression to SQL
│  "SELECT * FROM Projects WHERE Status = 'Active' AND IsVisible = 1"
└─────────────────────┘
        │
        ▼
┌─────────────────────┐
│     SQL Server       │  ← Executes the query, returns rows
└─────────────────────┘
```

---

## 3.6 How the Response Comes Back

The return journey is the reverse of the above:

1. SQL Server returns rows of data.
2. EF Core converts them into `Project` C# objects (entities).
3. `GenericRepository` returns the list to `ProjectService`.
4. `ProjectService` uses AutoMapper to convert each `Project` entity to a `ProjectResponseDto` (a simpler shape — no internal fields like `CreatedBy` IDs, just what the frontend needs).
5. `ProjectService` wraps the list in `ServiceResponse<List<ProjectResponseDto>>.Success(data)` and returns it.
6. `ProjectsController` receives the `ServiceResponse`, checks `result.IsSuccess == true`, and calls `return Ok(result)`.
7. ASP.NET Core serializes the response to JSON.
8. The JSON travels over the internet back to Ahmad's phone.
9. Ahmad's app shows the project list.

Notice that the controller does almost no work — it just asks the service and returns the result. All the real thinking happens in the service layer.

---

## 3.7 What Lives in Program.cs and Why

`Program.cs` is the conductor of the orchestra. It has three main jobs:

**Job 1: Register services in the DI container**
Every service and its concrete implementation is registered here. This is the full list of wiring that makes DI work. For example:
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
// ... 15 more service registrations
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

**Job 2: Configure the middleware pipeline**
The order in which middleware is added to `app` in Program.cs is the exact order in which every request passes through them. Order matters — Authentication must come before Authorization, CORS must come before Controllers.

**Job 3: Startup tasks — migration and seeding**
Before accepting any requests, Program.cs runs the database migrations (`context.Database.MigrateAsync()`) and seeds the database with starter data (`DbInitializer.SeedAsync(context)`). If either fails, the application refuses to start. This ensures the database is always in a consistent state.

**Job 4: Map endpoints and hubs**
`app.MapControllers()` tells ASP.NET Core to scan all controller classes and register their routes.  
`app.MapHub<NotificationHub>("/hubs/notifications")` registers the SignalR WebSocket endpoint.

---

## 3.8 Glossary: New Terms in This Unit

**Middleware** — A piece of code that processes every HTTP request before (or after) the controller handles it. Like a security gate at an event.

**Dependency Injection (DI)** — A pattern where classes declare what they need (via constructor parameters) and the framework provides it at runtime. The class never creates its own dependencies.

**Scoped Lifetime** — A DI registration mode where one instance is created per HTTP request and shared within that request. Discarded when the request ends.

**Interface** — A contract that says "this class MUST have these methods" without saying how they work. Allows DI to swap implementations without changing the caller.

**Controller** — A class in `Sh8lny.Web` that handles HTTP requests for a specific domain (projects, students, auth, etc.). Each method maps to one endpoint (URL + HTTP verb).

**Route** — The URL pattern that maps to a controller method. `[Route("api/[controller]")]` on `ProjectsController` means all endpoints start with `/api/Projects`.

**Attribute** — A label in square brackets on a C# class or method that adds behavior. `[Authorize]` adds authentication enforcement. `[HttpGet]` marks a method as handling GET requests. `[FromBody]` tells ASP.NET to read the parameter from the request body.

**JWT Bearer** — JSON Web Token — the signed token a logged-in user sends with every request to prove their identity. The server validates the signature without storing anything. Explained fully in Unit 6.

**CORS** — Cross-Origin Resource Sharing — a browser security mechanism that restricts which origins can call an API. "AllowAll" policy disables the restriction for Sha8alny (since it has both a React web app and a Flutter mobile app calling the same API).

---

## 3.9 What to Say in Your Defense

- "Every HTTP request in Sha8alny passes through a pipeline of 9 middleware components before reaching the controller. These include CORS validation, JWT authentication, role-based authorization, and request timing — each doing exactly one job."
- "We use Dependency Injection throughout the system. Controllers declare what service interface they need in their constructor; `Program.cs` registers which concrete class satisfies each interface. This means we can swap implementations without changing controllers."
- "All services are registered as Scoped — one instance per HTTP request. This is the correct lifetime for EF Core DbContext, which must track changes within a request and be discarded afterward."
- "The controller does minimal work — it extracts the user ID from JWT claims, calls the appropriate service method, and converts the `ServiceResponse<T>` to the right HTTP status code (`200 OK`, `400 Bad Request`, `401 Unauthorized`). All business logic is in the service layer."
- "The application performs two critical startup tasks before accepting requests: applying pending EF Core migrations and seeding the database. If either fails, the app refuses to start — ensuring data consistency before any user request is processed."

---

## 3.10 Self-Check Questions

**Q1: What does the Authentication middleware do, and what does the Authorization middleware do? Are they the same thing?**
Authentication identifies WHO the user is by reading and validating the JWT token. Authorization checks WHETHER that user is ALLOWED to access a specific endpoint. They are different: authentication runs first and never rejects requests on its own — authorization does the gating.

**Q2: A student tries to call `POST /api/Projects` (create a project). The endpoint has `[Authorize(Roles = "Company")]`. What happens?**
The Authentication middleware reads the student's JWT and identifies them as Role="Student". The Authorization middleware sees the endpoint requires Role="Company". The request is rejected with 403 Forbidden — the student's code in `ProjectService` is never even called.

**Q3: What is the "Scoped" lifetime for a service?**
One new instance is created per incoming HTTP request. The same instance is shared across all code within that single request. When the request finishes, the instance is thrown away.

**Q4: In the DI container, what does this line do?**
`builder.Services.AddScoped<IProjectService, ProjectService>();`
It registers the pairing: "Whenever any class asks for `IProjectService` in its constructor, give it a new `ProjectService` instance."

**Q5: Why is the order of middleware important?**
Because each middleware calls the next one in the chain. If Authorization came before Authentication, it would try to check permissions before the user's identity was established — and it would always fail. CORS must run before Controllers so the preflight check happens before any business code executes.

**Q6: How does `GetCurrentUserId()` work in a controller?**
It calls `User.FindFirst(ClaimTypes.NameIdentifier)` — `User` is an object automatically populated by the Authentication middleware from the JWT. The `NameIdentifier` claim holds the UserID that was put into the token when the user logged in. It parses that string to an integer and returns it.

**Q7: What is the purpose of Request Timing Middleware?**
It starts a stopwatch when the request arrives and logs the HTTP method, path, response status code, and elapsed milliseconds when the response is sent. This output goes to the Discord webhook, allowing the team to monitor performance without needing to log into the server.
