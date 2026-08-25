# backend-fix.md — Prompt: Fix the Two Remaining Backend Gaps

> **How to use this file:** Give this entire document as the task prompt to whoever
> (or whatever agent) works on the backend repository. It contains the full context,
> exact files, exact changes, and verification steps. No exploration should be needed.
>
> **Backend repository:** `E:\LLM testing\Sha8alny` (ASP.NET Core 9 Web API, deployed
> to Cloud Run at `https://sha8alny-backend-857164936517.us-central1.run.app`).
>
> Both fixes were discovered during live end-to-end testing of the Flutter mobile app
> against the deployed backend on 2026-06-12. The mobile app already has fallback
> workarounds in place, so nothing is on fire — but these fixes make the workarounds
> unnecessary and fix the root causes.

---

## Fix 1 — Auth responses carry no user name

### Symptom observed in the app

After signup or login, the app's home screen greeting shows only the role
("Student") with no user name, because the cached user has an empty name.

### Root cause

The mobile app sends `firstName` and `lastName` in the register request body, and
already parses `firstName` / `lastName` from auth responses
(`UserModel.fromNetJson` in the mobile repo reads exactly those keys). But on the
backend:

1. `Sh8lny.Shared/DTOs/Auth/RegisterDto.cs` only has `Email`, `Password`, `Role` —
   the incoming `firstName`/`lastName` JSON keys are **silently dropped** by model
   binding.
2. The `User` entity (`Core/Sh8lny.Domain/Models/User.cs`) **already has**
   `FirstName` and `LastName` properties (lines 15–16) — they are just never set
   during registration.
3. `Sh8lny.Shared/DTOs/Auth/AuthResponseDto.cs` has no name fields, so neither
   register nor login can return one.

### Changes required

**File: `Sh8lny.Shared/DTOs/Auth/RegisterDto.cs`**

Add two optional properties:

```csharp
public string? FirstName { get; set; }
public string? LastName { get; set; }
```

**File: `Sh8lny.Shared/DTOs/Auth/AuthResponseDto.cs`**

Add two optional properties:

```csharp
public string? FirstName { get; set; }
public string? LastName { get; set; }
```

**File: `Core/Sh8lny.Service/AuthService.cs`**

1. In `RegisterAsync` (~line 60), set the names when creating the `User` entity:

```csharp
var user = new User
{
    Email = dto.Email,
    PasswordHash = passwordHash,
    UserType = userType,
    FirstName = dto.FirstName,   // ← add
    LastName = dto.LastName,     // ← add
    // ... existing properties unchanged
};
```

2. In the success return of `RegisterAsync` (~line 75), include the names:

```csharp
return new AuthResponseDto
{
    IsSuccess = true,
    Token = token,
    Expiration = expiration,
    UserId = user.UserID,
    Email = user.Email,
    Role = user.UserType.ToString(),
    FirstName = user.FirstName,   // ← add
    LastName = user.LastName,     // ← add
    Message = "Registration successful."
};
```

3. In the success return of `LoginAsync` (~line 119), include the names the same way.

**Backfill consideration (important):** users registered *before* this fix have
`NULL` `FirstName`/`LastName` on the `User` row, but their **Student profile**
(`Student.FirstName` / `Student.LastName`) has the real name (it is set from
`FullName` during profile creation). To cover those accounts, in `LoginAsync`,
when `user.FirstName` is null/empty and the user is a Student, fall back to the
student profile:

```csharp
var firstName = user.FirstName;
var lastName = user.LastName;
if (string.IsNullOrEmpty(firstName) && user.UserType == UserType.Student)
{
    var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == user.UserID);
    firstName = student?.FirstName;
    lastName = student?.LastName;
}
```

(Optionally do the symmetric fallback to the Company profile for company users.)

### Mobile impact

None — the app already reads `firstName`/`lastName` from auth responses and
composes the display name. It will start working the moment this deploys.

---

## Fix 2 — `CreateProfileAsync` drops AcademicYear and never links University/Department

### Symptom observed in the app

Right after creating a profile:
- "Academic Year" displayed empty even though the user selected one in the form.
- Opening Edit Profile forces the user to re-select university, department, and
  academic year from scratch because `GET /api/students/profile` returns
  `universityID: null`, `departmentID: null`, `academicYear: null`.

After the user performs one *edit* (which goes through `StudentProfileUpdateDto` —
that DTO supports all three fields), everything persists and pre-fills correctly.
So the gap is **only in the create path**.

### Root cause

**File: `Sh8lny.Shared/DTOs/StudentProfile/CreateStudentProfileDto.cs`** has no
`AcademicYear`, `UniversityID`, or `DepartmentID` properties. The mobile app sends
`academicYear` in the create body and it is silently dropped by model binding.

**File: `Core/Sh8lny.Service/StudentService.cs`**, `CreateProfileAsync` (~line 49):
the `Student` entity is created without `AcademicYear`, `UniversityID`, or
`DepartmentID`. The university the student typed is only stored as a free-text
`UniversityName` string inside the `Educations` child rows — it is never linked to
the `Universities` master-data table, so `StudentResponseDto.UniversityName`
(which is mapped from the `Student.University` navigation property) comes back null.

### Changes required

**File: `Sh8lny.Shared/DTOs/StudentProfile/CreateStudentProfileDto.cs`**

Add three optional properties (reuse the `AcademicYearDto` enum already declared in
`StudentProfileUpdateDto.cs`):

```csharp
public AcademicYearDto? AcademicYear { get; set; }
public int? UniversityID { get; set; }
public int? DepartmentID { get; set; }
```

**File: `Core/Sh8lny.Service/StudentService.cs`** — in `CreateProfileAsync`, when
building the `Student` entity (~line 49):

```csharp
var student = new Student
{
    // ... existing properties unchanged
    AcademicYear = dto.AcademicYear.HasValue
        ? (AcademicYear)dto.AcademicYear.Value   // enums share the same member order
        : null,                                   // ← add
    UniversityID = dto.UniversityID,              // ← add
    DepartmentID = dto.DepartmentID,              // ← add
};
```

Check how the update path (`UpdateProfileAsync` in the same file) converts
`AcademicYearDto?` to the domain `AcademicYear?` enum and use the identical
conversion for consistency.

**Optional nicety (only if cheap):** if `dto.UniversityID` is null but the first
education entry's `UniversityName` exactly matches a row in the Universities
master table, link it:

```csharp
if (student.UniversityID is null && dto.Educations.Count > 0)
{
    var match = await _unitOfWork.Universities
        .FindSingleAsync(u => u.UniversityName == dto.Educations[0].UniversityName);
    student.UniversityID = match?.UniversityID;
}
```

(Verify the actual repository/property names — `Universities` repo and
`UniversityName` column — against `UnitOfWork.cs` and the `University` entity
before using this snippet verbatim.)

### Mobile impact

The mobile app currently works around the missing `academicYear` by issuing a
follow-up `PUT /api/students/profile` immediately after the create `POST`
(see `createStudentProfile` in
`lib/features/profile/data/datasources/profile_net_api_datasource.dart` in the
mobile repo). That workaround is harmless and can stay; once this backend fix
ships, the create `POST` body's `academicYear` value will simply be honored.
The mobile create form sends `academicYear` as a **string** (e.g. `"ThirdYear"`) —
ASP.NET's default `JsonStringEnumConverter` setup must be able to bind it; if the
API does not have string-enum binding enabled globally, either enable it for this
DTO or keep accepting the string and parse it manually (the update endpoint already
accepts integer enum values, so check what convention the project uses and stay
consistent — the safest is to make create accept the same wire format as update).

If `UniversityID`/`DepartmentID` are added to the create DTO, a future mobile
improvement can replace the free-text university/major fields on the create-profile
form with the same MasterData dropdowns the edit page uses, and send the IDs at
creation time.

---

## Bonus cleanup (data, not code)

The production database contains junk master-data rows that appear in real app
dropdowns:

- `Skills` table: a skill literally named `string`
- `Universities` table: five universities literally named `string`

Delete these rows or set their `IsActive = false`. They are almost certainly
leftovers from Swagger "try it out" requests with default example bodies.

---

## Verification (run after implementing)

1. `dotnet build` from the backend repo root — must compile with 0 errors.
2. Register a fresh user and assert the response now echoes the name:

```bash
curl -s -X POST https://<host>/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"verify.fix.<timestamp>@test.com","password":"VerifyFix123","role":"Student","firstName":"Verify","lastName":"Fix"}'
# expect: "isSuccess":true, "firstName":"Verify", "lastName":"Fix"
```

3. Login with the same account — response must also include the names.
4. With the returned token, create a profile **including an academic year** and
   confirm it round-trips without any PUT:

```bash
curl -s -X POST https://<host>/api/students/profile \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"fullName":"Verify Fix","city":"Cairo","country":"Egypt","academicYear":"ThirdYear","universityID":1,"departmentID":2,"totalInternshipDays":0,"educations":[{"universityName":"Cairo University","degree":"Bachelor","fieldOfStudy":"CS","startYear":2024}],"experiences":[]}'

curl -s https://<host>/api/students/profile -H "Authorization: Bearer <token>"
# expect: "academicYear":"ThirdYear", "universityID":1, "universityName":"Cairo University", "departmentID":2
```

5. Login as an account created **before** the fix (e.g. `mock.tester.*@sha8alnytest.com`)
   and confirm the student-profile name fallback populates `firstName`/`lastName`.
6. Redeploy to Cloud Run — the mobile app points at the deployed instance, so none
   of this is visible to the app until deployment.
