# Unit 5: DTOs, AutoMapper, and ServiceResponse — How Data Changes Shape As It Travels

> **Before reading this unit:** You should have read Unit 3 (request lifecycle) and Unit 4 (database layer). This unit explains how data changes form between the database and the API response — and how the system communicates success or failure.

---

## 5.1 The Problem: Why Not Just Return the Database Entity Directly?

Imagine your restaurant has a recipe card for each dish. The recipe card says: "For the chicken sandwich: 100g chicken breast, 2 slices bread, 1 tsp salt, 1 tsp pepper, the supplier's secret marinade mix, cost: 15 EGP."

When a customer asks "What is in the chicken sandwich?", you do not hand them the recipe card. You hand them a menu description: "Grilled chicken breast with fresh bread." Same dish — different level of detail, no internal secrets, shaped for the audience.

**The same problem exists in APIs.** Your database entity `Student` contains fields you should never send to a client:

```csharp
// Database entity — contains EVERYTHING, including sensitive internals
public class Student
{
    public int StudentID { get; set; }
    public int UserID { get; set; }          // internal FK reference
    public string PasswordHash { get; set; } // 💀 NEVER send to client
    public string VerificationCode { get; set; } // 💀 NEVER send to client
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Bio { get; set; }
    // ... 20+ more fields, navigation properties, etc.
}
```

If you return the `Student` entity directly as an API response:
- The password hash is exposed to anyone who calls the API.
- Internal foreign key IDs that mean nothing to the frontend are included.
- Navigation properties (like `Applications`, `Education`) might trigger additional database queries or serialization loops.
- The frontend gets far more data than it needs, wasting bandwidth.

**The solution: Data Transfer Objects (DTOs).**

---

## 5.2 What Is a DTO? (Data Transfer Object)

A DTO is a plain C# class with no logic — just properties. It is a carefully chosen subset of fields, shaped specifically for one purpose.

Think of it as a **carrying container** — you decide exactly what goes in it.

For example, a `StudentResponseDto` contains only what the frontend needs to display a student profile:

```csharp
// DTO — only what the client needs to see
public class StudentResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; }  // combines FirstName + LastName
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    public string AcademicYear { get; set; }   // enum converted to readable string
    public string Status { get; set; }         // enum converted to readable string
    public decimal AverageRating { get; set; }
    // ← no PasswordHash, no VerificationCode, no internal IDs
}
```

No sensitive data. No internal references. Just the data the frontend actually uses.

---

## 5.3 The Three DTO Patterns Used in Sha8alny

Sha8alny uses three kinds of DTOs for most features:

**Create DTOs** — What the client sends when creating something new.
The client sends only the fields needed to create the resource. Internal fields (IDs, timestamps, computed values) are never sent by the client — they are set by the server.

Example: `CreateProjectDto` has `ProjectName`, `Description`, `ProjectType`, `Deadline`, `RequiredSkillIds` — but NOT `ProjectID`, `CreatedAt`, `ViewCount`, or `ApplicationCount`.

**Response DTOs** — What the server sends back after a query or action.
These are shaped for what the frontend needs to display. They may combine fields from multiple entities (e.g., including `CompanyName` from the related `Company` entity).

Example: `ProjectResponseDto` has `Id`, `CompanyName`, `ProjectName`, `Description`, `Status` as a readable string (not the raw enum integer), `ViewCount`, etc.

**Update DTOs** — What the client sends when modifying an existing resource.
Similar to Create DTOs but may have optional fields (you only update what you send).

Example: `UpdateProjectDto` has the same fields as `CreateProjectDto`, all nullable, so the client only sends what changed.

All DTOs live in `Sh8lny.Shared` — the project that all layers reference. This means the controller, service, and any future test can all import the same DTO classes without circular dependencies.

---

## 5.4 AutoMapper — The Automatic Translator

Converting an entity to a DTO manually looks like this:

```csharp
// ❌ Manual mapping — tedious and error-prone
var dto = new ProjectResponseDto
{
    Id = project.ProjectID,              // different names
    CompanyId = project.CompanyID,
    CompanyName = project.Company.CompanyName,  // from a related entity
    ProjectName = project.ProjectName,
    Status = project.Status.ToString(),  // enum to string
    // ... 15 more fields
};
```

If you rename a property, add a field, or change how an enum is serialized, you have to find and update every manual mapping in every service. This is repetitive and breaks silently when you forget one.

**AutoMapper** solves this: you declare the mapping once, and AutoMapper handles the conversion everywhere.

In `MappingProfile.cs` (in `Sh8lny.Web/Mappings/`), every entity-to-DTO mapping is declared:

```csharp
// Tell AutoMapper: "when mapping Project → ProjectResponseDto, do this:"
CreateMap<Project, ProjectResponseDto>()
    .ForMember(dest => dest.Id,
               opt => opt.MapFrom(src => src.ProjectID))          // rename
    .ForMember(dest => dest.CompanyId,
               opt => opt.MapFrom(src => src.CompanyID))
    .ForMember(dest => dest.CompanyName,
               opt => opt.MapFrom(src => src.Company.CompanyName)) // from related entity
    .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.Status.ToString()));   // enum to string
```

Then anywhere in a service, one line does the conversion:

```csharp
var dto = _mapper.Map<ProjectResponseDto>(project);
// ↑ AutoMapper reads the MappingProfile and does all 15 property assignments for you
```

Or for a list:
```csharp
var dtos = _mapper.Map<List<ProjectResponseDto>>(projects);
```

AutoMapper is registered in `Program.cs`:
```csharp
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

The `IMapper` interface is then injected into any service that needs it, just like any other DI dependency.

---

## 5.5 ServiceResponse\<T\> — The Standard Envelope

Every service method in Sha8alny returns a `ServiceResponse<T>`. This is the agreed-upon envelope format — it wraps the actual result with metadata about whether the operation succeeded.

Here is the exact class from `Sh8lny.Shared/DTOs/Common/ServiceResponse.cs`:

```csharp
public class ServiceResponse<T>
{
    public bool IsSuccess { get; set; }   // did the operation succeed?
    public T? Data { get; set; }          // the actual result (only set when IsSuccess = true)
    public string? Message { get; set; }  // human-readable message
    public List<string> Errors { get; set; } = new(); // list of validation errors

    // Factory method for success
    public static ServiceResponse<T> Success(T data, string? message = null)

    // Factory method for failure
    public static ServiceResponse<T> Failure(string message, List<string>? errors = null)
}
```

**How a service uses it:**

```csharp
public async Task<ServiceResponse<int>> CreateProjectAsync(int userId, CreateProjectDto dto)
{
    // Validate
    var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
    if (company == null)
        return ServiceResponse<int>.Failure("You must have a company profile to post projects.");

    // Create
    var project = new Project { ... };
    await _unitOfWork.Projects.AddAsync(project);
    await _unitOfWork.SaveAsync();

    return ServiceResponse<int>.Success(project.ProjectID, "Project created successfully.");
}
```

**How a controller uses it:**

```csharp
var result = await _projectService.CreateProjectAsync(userId.Value, dto);

if (!result.IsSuccess)
    return BadRequest(result);   // 400 — sends the failure message and errors to client

return CreatedAtAction(..., result); // 201 — sends result.Data (the new project ID) to client
```

**Why this pattern?**
Without `ServiceResponse<T>`, the service would have to either throw exceptions for every validation failure (slow and verbose) or return raw values that give the controller no information about what went wrong. `ServiceResponse<T>` is a self-describing envelope — the controller just checks `IsSuccess` and acts accordingly.

---

## 5.6 Where Does Sh8lny.Shared Fit In?

As established in Unit 2, `Sh8lny.Shared` is the standalone utility project that everyone can import without creating circular dependencies.

All DTOs (`CreateProjectDto`, `ProjectResponseDto`, `StudentResponseDto`, etc.) live in `Sh8lny.Shared/DTOs/`, organized by feature folder:

```
Sh8lny.Shared/DTOs/
├── Admin/
├── Applications/
├── Auth/
├── Certificates/
├── Chat/
├── Common/         ← ServiceResponse<T>, PagedResult
├── CompanyProfile/
├── Execution/
├── MasterData/
├── Media/
├── Notifications/
├── Payments/
├── Projects/
├── Reviews/
├── Settings/
└── StudentProfile/
```

When `ProjectService` (in `Sh8lny.Service`) needs to return a `ProjectResponseDto`, it imports from `Sh8lny.Shared`. When `ProjectsController` (in `Sh8lny.Web`) needs to accept a `CreateProjectDto`, it also imports from `Sh8lny.Shared`. Both use the same class without either knowing about the other.

---

## 5.7 What to Say in Your Defense

- "We never return database entities directly from the API. Instead, we use DTOs — plain C# classes shaped for one specific purpose. This prevents accidental exposure of sensitive fields like password hashes, and gives us control over the exact format the frontend receives."
- "We use AutoMapper to convert between entities and DTOs. All mapping logic lives in one file — `MappingProfile.cs`. When we add a field to a DTO, we update the mapping in one place, and it works everywhere."
- "Every service method returns `ServiceResponse<T>` — a generic envelope with `IsSuccess`, `Data`, `Message`, and `Errors`. Controllers just check `IsSuccess` and return the appropriate HTTP status code. This creates a consistent pattern across all 16 controllers."
- "All DTOs live in `Sh8lny.Shared` — the dependency-free utility project. This means the service layer and the controller layer both import the same DTO classes without creating circular dependencies."
- "The three DTO patterns we use are: Create DTOs (client sends when creating), Response DTOs (server sends back), and Update DTOs (client sends when modifying). Each is shaped for its specific purpose."

---

## 5.8 Self-Check Questions

**Q1: Why is it dangerous to return a `Student` entity directly from an API endpoint?**
The `Student` entity contains sensitive fields from the related `User` entity (accessible via navigation properties), such as `PasswordHash` and `VerificationCode`. It also includes internal foreign key IDs and navigation collections that could cause serialization loops. DTOs let you expose only what the client should see.

**Q2: What are the four fields in `ServiceResponse<T>`?**
`IsSuccess` (bool), `Data` (T?), `Message` (string?), `Errors` (List\<string\>).

**Q3: Where do all DTOs live in the solution?**
In `Sh8lny.Shared/DTOs/`, organized by feature subfolder. `Sh8lny.Shared` has no dependencies, so every project can import DTOs without creating circular references.

**Q4: What does AutoMapper's `MappingProfile.cs` contain?**
`CreateMap<Source, Destination>()` declarations — one for each entity-to-DTO conversion the system needs. It handles field renaming (`ProjectID` → `Id`), type conversion (enum to string), and cross-entity data (pulling `CompanyName` from the related `Company` navigation property).

**Q5: What happens in the controller when `result.IsSuccess == false`?**
The controller returns `BadRequest(result)` — HTTP 400. The full `ServiceResponse<T>` object (including `Message` and `Errors`) is serialized to JSON and sent back to the client, explaining exactly what went wrong.

**Q6: What is a `CreateProjectDto` vs a `ProjectResponseDto`?**
`CreateProjectDto` is what the client sends when posting a new project — it contains only the fields the user provides. `ProjectResponseDto` is what the server sends back — it contains the computed fields (ID, ViewCount), related data (CompanyName), and enums converted to readable strings. Same concept, different shapes for different directions of travel.

**Q7: Why use AutoMapper instead of writing manual mapping code?**
Manual mapping is repetitive, verbose, and breaks silently when properties change. AutoMapper centralizes all mapping logic in one place (`MappingProfile.cs`), so a change in mapping only needs to be made once, and is applied consistently everywhere in the codebase.
