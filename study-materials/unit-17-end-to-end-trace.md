# Unit 17: End-to-End Trace — Following One Action Through Every Layer

> **This unit is the capstone.** Before reading it, you should have read all previous units (1–16, 18, 19). This unit traces a single real-world action from the first TCP packet to the last database write, touching every architectural layer, showing how everything connects. Units 18 and 19 cover the surrounding infrastructure: the email system and password reset flow, NuGet package justifications, and the CI/CD pipeline that delivers this code to production.

---

## 17.1 The Scenario

TechCorp's recruiter opens the app and accepts Ahmad's application for the "Backend Development Internship" project.

She clicks "Accept." Behind the scenes, the following HTTP request leaves her device:

```
PUT /api/Applications/42/review
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "status": "Accepted",
  "note": "Great candidate, strong SQL skills."
}
```

What happens next is the subject of this entire unit.

---

## 17.2 Layer 0 — Network and Kestrel

The request travels over HTTPS to Google Cloud Run. The container runs Kestrel — ASP.NET Core's built-in web server. Kestrel accepts the TCP connection, terminates TLS (decrypts the request), and hands an `HttpContext` object to the ASP.NET Core middleware pipeline.

The `HttpContext` contains:
- `Request.Method = "PUT"`
- `Request.Path = "/api/Applications/42/review"`
- `Request.Headers["Authorization"] = "Bearer eyJ..."`
- `Request.Body = { "status": "Accepted", "note": "..." }`

---

## 17.3 Layer 1 — The Middleware Pipeline

Every middleware in `Program.cs` processes this request in order (Unit 3):

**1. Swagger middleware** — Path is not `/swagger`, pass through.

**2. HTTPS Redirection** — Already HTTPS, pass through.

**3. Static Files** — Path is not a file, pass through.

**4. Request Timing middleware** — `var stopwatch = Stopwatch.StartNew()`. Records start time. Will log at the end.

**5. CORS middleware** — Checks `Origin` header. `"AllowAll"` policy accepts all origins. Pass through.

**6. Authentication middleware** — Extracts the JWT from `Authorization: Bearer eyJ...`:
   - Splits into header, payload, signature
   - Re-computes HMAC-SHA256 of header+payload using the server's secret key
   - Compares computed signature to the token's signature → match
   - Reads `exp` claim → not expired (within 60 minutes)
   - Decodes payload: `sub=7` (TechCorp's UserID), `role=Company`, `email=techcorp@example.com`
   - Populates `HttpContext.User` with these claims
   
**7. Authorization middleware** — Reads the controller's `[Authorize(Roles = "Company")]` attribute. Checks `HttpContext.User.IsInRole("Company")` → true. Access granted.

**8. Routing / Endpoint middleware** — Matches `PUT /api/Applications/{id}/review` to `ApplicationsController.ReviewApplication(int id, ReviewApplicationDto dto)`. Extracts route value `id = 42`.

**9. Controller execution** — Creates an `ApplicationsController` instance via DI. Injects `IApplicationService`.

---

## 17.4 Layer 2 — The Controller

```csharp
// ApplicationsController.cs
[HttpPut("{id}/review")]
[Authorize(Roles = "Company")]
public async Task<ActionResult> ReviewApplication(int id, [FromBody] ReviewApplicationDto dto)
{
    var userId = GetCurrentUserId();
    // userId = 7 (read from JWT's "sub" claim via ClaimTypes.NameIdentifier)

    if (userId is null)
        return Unauthorized(...);

    var result = await _applicationService.ReviewApplicationAsync(userId.Value, dto);
    // dto.ApplicationId = 42, dto.Status = "Accepted", dto.Note = "Great candidate..."

    if (!result.IsSuccess)
        return BadRequest(result);  // sends the failure message to TechCorp

    return Ok(result);  // sends the success response
}
```

The controller does three things:
1. Reads the UserID from the JWT claims (this is TechCorp's UserID = 7)
2. Calls the service
3. Maps `IsSuccess` to the HTTP status code (`200 OK` or `400 Bad Request`)

The controller knows nothing about SQL, SignalR, or business rules. It is the thin HTTP adapter.

---

## 17.5 Layer 3 — The Service (Business Logic)

`ApplicationService.ReviewApplicationAsync(companyUserId=7, dto)` is called. This is where the real work happens:

**Step 1 — Get the application:**
```csharp
var application = await _unitOfWork.Applications.GetByIdAsync(42);
// → Finds: ProjectID=15, StudentID=3, Status=Pending
```

**Step 2 — Verify company ownership:**
```csharp
var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == 7);
// → Finds TechCorp with CompanyID=2

var project = await _unitOfWork.Projects.GetByIdAsync(application.ProjectID);
// → Finds the "Backend Development Internship" project

if (project.CompanyID != company.CompanyID)
    return Failure("Unauthorized.");
// → CompanyID=2 == CompanyID=2 → authorized
```

**Step 3 — Parse and validate the status:**
```csharp
Enum.TryParse<ApplicationStatus>("Accepted", ..., out var newStatus);
// → newStatus = ApplicationStatus.Accepted ✓

if (newStatus != ApplicationStatus.Accepted && newStatus != ApplicationStatus.Rejected)
    return Failure("Invalid status. Only 'Accepted' or 'Rejected'...");
// → newStatus = Accepted → valid
```

**Step 4 — Update the application:**
```csharp
application.Status = ApplicationStatus.Accepted;
application.ReviewedBy = 7;  // TechCorp's UserID
application.ReviewedAt = DateTime.UtcNow;
application.ReviewNotes = "Great candidate, strong SQL skills.";

_unitOfWork.Applications.Update(application);
// → EF Core marks this entity as Modified
```

**Step 5 — Get the student:**
```csharp
var student = await _unitOfWork.Students.GetByIdAsync(application.StudentID);
// → Finds Ahmad: StudentID=3, UserID=5, FirstName="Ahmad", LastName="Hassan"
```

**Step 6 — Create a Notification:**
```csharp
var notification = new Notification
{
    UserID = student.UserID,         // 5 (Ahmad's UserID)
    NotificationType = NotificationType.Acceptance,
    Title = "Application Update",
    Message = "Your application for 'Backend Development Internship' was Accepted.",
    RelatedProjectID = project.ProjectID,
    RelatedApplicationID = application.ApplicationID,
    ActionURL = "/applications/42",
    IsRead = false,
    CreatedAt = DateTime.UtcNow
};

await _unitOfWork.Notifications.AddAsync(notification);
// → EF Core queues an INSERT for this Notification row
```

**Step 7 — Save everything:**
```csharp
await _unitOfWork.SaveAsync();
// → Calls dbContext.SaveChangesAsync()
// → EF Core executes two SQL statements in one round-trip:
//   UPDATE Applications SET Status='Accepted', ReviewedBy=7, ReviewedAt=..., ReviewNotes=... WHERE ApplicationID=42
//   INSERT INTO Notifications (UserID, NotificationType, Title, Message, ...) VALUES (5, 'Acceptance', ...)
```

**Step 8 — Send real-time notification:**
```csharp
var notificationDto = new NotificationDto { ... };
await _notifier.SendNotificationAsync(student.UserID, notificationDto);
// → Calls SignalRNotifier.SendNotificationAsync(5, dto)
```

**Step 9 — Return success:**
```csharp
return ServiceResponse<bool>.Success(true, "Application Accepted successfully. Student has been notified.");
```

---

## 17.6 Layer 4 — The Repository and EF Core (Database Layer)

Let's zoom into what happens when `_unitOfWork.SaveAsync()` runs.

`UnitOfWork.SaveAsync()` calls `_context.SaveChangesAsync()`. At this point, EF Core has been tracking two pending changes since earlier in the service call:

1. **Modified:** `Application` entity (Status, ReviewedBy, ReviewedAt, ReviewNotes changed)
2. **Added:** `Notification` entity (new row)

EF Core generates SQL and sends it to SQL Server:

```sql
-- Statement 1: Update the application
UPDATE [Applications]
SET [Status] = 'Accepted',
    [ReviewedBy] = 7,
    [ReviewedAt] = '2026-05-26T14:32:15.000Z',
    [ReviewNotes] = 'Great candidate, strong SQL skills.'
WHERE [ApplicationID] = 42;

-- Statement 2: Insert the notification
INSERT INTO [Notifications]
    ([UserID], [NotificationType], [Title], [Message], [RelatedProjectID], 
     [RelatedApplicationID], [ActionURL], [IsRead], [CreatedAt])
VALUES
    (5, 'Acceptance', 'Application Update', 
     'Your application for ''Backend Development Internship'' was Accepted.',
     15, 42, '/applications/42', 0, '2026-05-26T14:32:15.000Z');

SELECT SCOPE_IDENTITY();  -- returns the new NotificationID
```

SQL Server executes these, commits, and returns. EF Core updates `notification.NotificationID` with the value from `SCOPE_IDENTITY()`.

Back in the service: `notification.NotificationID` is now, say, 87.

---

## 17.7 Layer 5 — Real-Time Delivery (SignalR)

After `SaveAsync()` completes, the service calls:

```csharp
await _notifier.SendNotificationAsync(5, notificationDto);
```

`SignalRNotifier.SendNotificationAsync` runs:

```csharp
await _hubContext.Clients.User("5")
    .SendAsync("ReceiveNotification", notificationDto);
```

`IHubContext<NotificationHub>` looks up all WebSocket connections where the JWT's `NameIdentifier` claim equals `"5"` (Ahmad's UserID). If Ahmad is currently connected to `/hubs/notifications`, his connection is found.

The `SignalRNotifier` sends the `NotificationDto` object to Ahmad's device over the persistent WebSocket. On Ahmad's Flutter app, the registered handler fires:

```dart
hubConnection.on("ReceiveNotification", (notification) {
    showToast("Application Update: Your application was Accepted!");
    updateNotificationBadge();
});
```

Ahmad sees the notification appear on his phone **at the same time TechCorp sees the "sent" confirmation** — all within the same request/response cycle.

If Ahmad is offline, `Clients.User("5")` finds no active connections and does nothing. The `Notification` row (NotificationID=87) is already in the database, waiting for Ahmad to retrieve it when he opens the app.

---

## 17.8 Layer 6 — Response Travels Back Up

The service returns `ServiceResponse<bool>.Success(true, "...")` to the controller.

```csharp
result.IsSuccess = true
result.Data = true
result.Message = "Application Accepted successfully. Student has been notified."
```

The controller calls `return Ok(result)` → HTTP 200.

ASP.NET Core serializes the `ServiceResponse<bool>` to JSON:

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Application Accepted successfully. Student has been notified.",
  "errors": []
}
```

This JSON travels back through the same middleware layers in reverse order.

**Request Timing middleware finishes:** `stopwatch.Stop()`. Elapsed = 127ms. Logs:
```
PUT /api/Applications/42/review responded 200 in 127ms
```

This log line flows to `DiscordWebhookLogger`, which posts it to the team's Discord channel.

The response reaches TechCorp's app. The UI updates: "Application accepted successfully."

---

## 17.9 The Full Architecture Map — Everything Connected

```
TechCorp's Device
    ↓ (HTTPS PUT request)
Kestrel (web server)
    ↓
Middleware Pipeline (Program.cs):
  → Request Timing (Stopwatch starts)
  → CORS
  → Authentication (JWT validated, UserID=7, Role=Company extracted)
  → Authorization ([Authorize(Roles="Company")] → pass)
  → Routing (→ ApplicationsController)
    ↓
ApplicationsController
  → GetCurrentUserId() reads JWT claim → 7
  → Calls IApplicationService.ReviewApplicationAsync(7, dto)
    ↓
ApplicationService (Sh8lny.Service — business logic)
  → 4 database reads (via IUnitOfWork → GenericRepository → EF Core)
  → 2 database writes queued (Application update + Notification insert)
  → _unitOfWork.SaveAsync() → EF Core → SQL Server
    ↓ (two SQL statements)
SQL Server (SQL Server 2022 on Google Cloud Run)
    ↑ (NotificationID=87 returned)
ApplicationService (resumes)
  → _notifier.SendNotificationAsync(5, dto) → INotifier interface
    ↓
SignalRNotifier (Sh8lny.Web — implements INotifier)
  → _hubContext.Clients.User("5").SendAsync("ReceiveNotification", dto)
    ↓ (WebSocket push)
NotificationHub (Sh8lny.Web)
    ↓ (WebSocket frame)
Ahmad's Device (Flutter app — if connected)
  → Shows toast: "Your application was Accepted!"
    ↑
ApplicationService returns ServiceResponse<bool>.Success(...)
    ↑
ApplicationsController returns Ok(result) → HTTP 200
    ↑
Middleware Pipeline (response journey):
  → Request Timing (Stopwatch stops → logs "PUT responded 200 in 127ms")
  → DiscordWebhookLogger posts to Discord
    ↑
TechCorp's Device receives HTTP 200 response
  → UI: "Application accepted successfully"
```

---

## 17.10 Cross-Layer Summary — What Each Layer Contributed

| Layer | File/Class | Contribution |
|---|---|---|
| HTTP transport | Kestrel | Accepted TCP connection, terminated TLS |
| Request timing | `Program.cs` (inline) | Measured and logged 127ms |
| Authentication | JWT middleware (Program.cs) | Validated JWT, extracted UserID=7, Role=Company |
| Authorization | `[Authorize(Roles="Company")]` | Blocked non-Company users |
| Controller | `ApplicationsController` | Read UserID claim, called service, mapped IsSuccess to HTTP status |
| Business logic | `ApplicationService` | All 3 verifications + state changes + notification creation |
| Persistence abstraction | `IUnitOfWork`, `GenericRepository` | Provided repository methods without exposing EF |
| ORM | EF Core + Fluent API configs | Generated SQL, tracked changes, sent to SQL Server |
| Database | SQL Server 2022 | Stored application update + notification row |
| Real-time | `SignalRNotifier`, `NotificationHub` | Pushed event to Ahmad's WebSocket connection |
| Logging | `DiscordWebhookLogger` | Posted timing log to Discord |
| Email | `MailService`, Gmail SMTP | Delivers password reset codes via MailKit + STARTTLS (Unit 18) |
| Deployment | GitHub Actions → GCR → Cloud Run | Built, tested, and delivered this entire codebase to production (Unit 19) |

---

## 17.11 What to Say in Your Defense

- "Every HTTP request passes through 7 middleware layers before reaching the controller. Authentication validates the JWT and extracts the user's identity. Authorization checks the role claim. By the time business logic runs, we already know who the caller is and whether they are allowed."
- "The service layer contains all business rules. `ReviewApplicationAsync` validates ownership, enforces that only `Accepted` or `Rejected` are valid review statuses, updates the application, creates a notification, saves everything, and then triggers real-time delivery — all within one method call."
- "The controller is intentionally thin. It reads the UserID from JWT claims, calls the service, and maps `IsSuccess` to an HTTP status code. No SQL, no SignalR, no business logic."
- "Database writes and notifications are atomic in the sense that both the application update and the notification INSERT go through one `SaveAsync()` call. If the database write fails, no partial state is committed."
- "Real-time delivery is best-effort. `SignalRNotifier` catches all exceptions and does not rethrow. If Ahmad is offline, the notification stays in the database and he reads it when he opens the app. The service does not fail because SignalR is unavailable."
- "The full round-trip — from TechCorp's button press to Ahmad's toast notification — takes about 127ms in this example. That includes 4 database reads, 2 database writes, and 1 WebSocket push."
- "Beyond the request lifecycle, the platform has two other critical infrastructure paths. First, email: if Ahmad forgets his password, `ForgotPasswordAsync` generates a 6-digit code with a 15-minute expiry, stores it on the User record, and sends an HTML email via MailKit + Gmail SMTP on port 587 with STARTTLS. The endpoint always returns the same response whether or not the email is registered — this prevents email enumeration attacks. Second, deployment: every push to master triggers GitHub Actions, which builds all 7 projects, runs xUnit integration tests, and on success builds a Docker image tagged with the git commit SHA, pushes it to Google Container Registry, and deploys it to Cloud Run — fully automated, zero manual steps."

---

## 17.12 The Lifecycle of Ahmad's Entire Journey

To see the full picture, here is the entire system trace across all 19 units:

```
Unit 19 → Before Ahmad ever opens the app, a developer pushes code to GitHub.
           GitHub Actions builds all 7 projects, runs integration tests, builds a
           Docker image tagged with the commit SHA, and deploys to Cloud Run.
           The platform is live.

Unit 6  → Ahmad registers (BCrypt hash, JWT token returned immediately)
Unit 18 → Ahmad forgets his password → ForgotPasswordAsync generates a 6-digit
           code (15-min expiry), emails it via MailKit + Gmail SMTP. Ahmad
           calls /reset-password with the code → new BCrypt hash saved, token cleared.
Unit 7  → Ahmad uploads his CV to /api/Media (FileService, WebP, URL returned)
Unit 8  → Ahmad creates his Student profile (Student entity, skills added)
Unit 6  → TechCorp registers and creates Company profile
Unit 9  → TechCorp posts internship (ProjectService, transaction wraps project+skills)
Unit 9  → Ahmad saves the project for later (SavedOpportunity)
Unit 9  → Ahmad applies (7 validations, skill check, BidAmount=0)
Unit 17 → [THIS UNIT] TechCorp accepts (Application.Status=Accepted + Notification + SignalR)
Unit 10 → TechCorp adds modules to the project (weight ≤ 100%)
Unit 10 → Ahmad reports progress (Accepted → InProgress auto-transition)
Unit 10 → All modules 100% → company notified via SignalR
Unit 10 → TechCorp marks job complete (Application.Status=Completed, Project.Status=Closed)
Unit 11 → Certificate auto-generated (CERT-XXXXXXXXXXXX)
Unit 11 → TechCorp reviews Ahmad (AverageRating recalculated, notification sent)
Unit 11 → Ahmad reviews TechCorp (company AverageRating recalculated)
Unit 12 → TechCorp processes payment (Transaction record, application.IsPaid=true)
Unit 13 → Throughout the journey, Ahmad and TechCorp exchanged messages via chat
Unit 14 → Every event above produced a real-time SignalR push AND a database notification
Unit 15 → Admin can see all of this in the dashboard stats
Unit 16 → BackupWorker backed up all this data at 3 AM automatically
Unit 19 → MaintenanceController allows Admin to trigger an on-demand backup at any time
```

Every unit in the study guide represents one piece of this journey. Together, they tell the complete story of how Sha8alny works — from the CI/CD pipeline that ships the code, through every user interaction, to the automated backups that protect the data.
