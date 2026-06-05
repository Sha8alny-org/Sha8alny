# Unit 9: Projects and Applications — The Core Marketplace

> **Before reading this unit:** You should have read Unit 8 (Students and Companies — how profiles work) and Unit 6 (authentication — how roles are enforced). This unit explains how companies post work opportunities and how students apply for them.

---

## 9.1 What Is a "Project" in Sha8alny?

The word "project" is Sha8alny's umbrella term for any opportunity a company can post for students. It is not only software projects — it covers five types:

| `ProjectType` | What it means |
|---|---|
| `Internship` | Unpaid training placement (bid amount is always 0) |
| `GraduationProject` | A graduation project for engineering or other students |
| `Training` | A structured training program with deliverables |
| `PartTime` | Part-time paid work |
| `FullTime` | Full-time paid employment |

Think of a `Project` as a **job posting** on a recruitment board — but Sha8alny makes it smarter: it tracks required skills, counts views and applications, has a deadline, and changes status automatically as work progresses.

A `Project` is not a contract — it becomes a contract only when a company accepts a specific student's application and work actually begins (that is handled in the `Execution` layer, covered in Unit 10).

---

## 9.2 The Project Entity — Field by Field

```
Project table:
ProjectID   | CompanyID | ProjectName | ProjectCode | Description
ProjectType | StartDate | EndDate     | Deadline    | Duration
MinAcademicYear | MaxApplicants | Status | IsVisible
CreatedBy   | CreatedByName | ViewCount | ApplicationCount
CreatedAt   | UpdatedAt
```

**The key fields explained:**

- `ProjectCode` — auto-generated short code, useful for reference (e.g., "SH-2026-001")
- `Deadline` — the last date a student can apply. The system rejects applications submitted after this.
- `Duration` — free text like "3 months" or "6 weeks" — not a computed value
- `MinAcademicYear` — a string filter (e.g., "ThirdYear") — the service does not enforce it in code, it is informational
- `MaxApplicants` — if set, the system rejects applications once this count is reached
- `IsVisible` — a toggle: companies can hide projects from the public listing without deleting them
- `ViewCount` — incremented every time someone calls `GET /api/Projects/{id}`. It is a denormalized counter — not computed by counting logs, just incremented in place.
- `ApplicationCount` — incremented when a new application is accepted, decremented when an application is withdrawn

**Navigation properties on `Project`:**
- `Company` — the posting company
- `ProjectRequiredSkills` — which skills are needed (join table)
- `Applications` — all student applications
- `Modules` — execution modules (Unit 10)
- `Payments`, `Certificates`, `Conversations` — linked features (later units)

---

## 9.3 Project Status — The State Machine

A project's lifecycle is tracked by `ProjectStatus`. Think of it like the status on a delivery order:

```
Draft → Active → Pending → Complete
                  ↓           ↓
               Cancelled   Closed
```

| Status | What it means |
|---|---|
| `Draft` | Created but not yet published — invisible to students |
| `Active` | Published and accepting applications |
| `Pending` | Applications closed, company is reviewing candidates |
| `Complete` | All work finished, students rated, project wrapped up |
| `Cancelled` | Company cancelled before work began |
| `Closed` | Administratively closed (by Admin) |

When `ProjectService.CreateProjectAsync()` creates a new project, it sets `Status = ProjectStatus.Active` immediately — there is no draft-then-publish step in the current implementation. A company can set status manually via `UpdateProjectAsync`.

**Important:** `ApplicationService.ApplyForProjectAsync()` checks two things before allowing an application:
1. `project.IsVisible == true` — the project must be public
2. `project.Status == ProjectStatus.Active` — the project must be active

If either condition fails, the application is rejected with "This project is no longer accepting applications."

---

## 9.4 Creating a Project — What Happens

When TechCorp calls `POST /api/Projects`:

```
1. Authorize: [Authorize(Roles = "Company")] — Student tokens are blocked here

2. ProjectService.CreateProjectAsync(userId, dto):
   a. Look up the User by userId — must exist
   b. Confirm UserType == Company — belt-and-suspenders check
   c. Find the Company profile by userId — must exist (profile must be created first)
   d. Validate Deadline is in the future
   e. Validate StartDate < EndDate (if both provided)
   f. Parse ProjectType from string → enum
   
3. BeginTransactionAsync() — wrap in a database transaction

4. Create the Project entity:
   Status = Active, ViewCount = 0, ApplicationCount = 0

5. SaveAsync() → INSERT INTO Projects ...

6. For each skill in dto.RequiredSkillIds:
   a. Verify the skill exists in the Skills lookup table
   b. If not found → RollbackTransactionAsync() → return failure
   c. Create a ProjectRequiredSkill row

7. SaveAsync() → INSERT INTO ProjectRequiredSkills ...

8. CommitTransactionAsync() → make both inserts permanent

9. Return ServiceResponse<int>.Success(project.ProjectID)
```

**Why the transaction?** The project INSERT and the skill INSERTs must succeed or fail together. If skill ID 99 does not exist, the whole operation is rolled back — no orphan project rows with no skills.

---

## 9.5 The Application Entity — What It Stores

When Ahmad applies for TechCorp's internship, an `Application` row is created:

```
Application table:
ApplicationID | ProjectID | StudentID | ProposalFileUrl | StudentCvUrl
BidAmount | Resume | Status | AppliedAt
ReviewedBy | ReviewedAt | ReviewNotes
```

Key fields:
- `ProposalFileUrl` — optional PDF proposal (uploaded via `/api/Media` first — see Unit 7)
- `StudentCvUrl` — the student's CV file URL (also from `/api/Media`)
- `BidAmount` — how much the student is asking (for paid project types). For `Internship` type, the service hardcodes this to 0.
- `Resume` — currently a duplicate of `StudentCvUrl` (same value stored in both columns — a minor redundancy in the current implementation)
- `ReviewedBy` — the UserID of the company member who made the decision
- `ReviewNotes` — the company's feedback note when accepting or rejecting

---

## 9.6 Application Status — The State Machine

`ApplicationStatus` has more states than you might expect, because applications can go through several stages:

```
Pending → UnderReview → Accepted → InProgress → Completed
                      ↓
                   Rejected
Pending/UnderReview → Withdrawn  (student changes their mind)
```

| Status | Who sets it | What it means |
|---|---|---|
| `Pending` | System (on creation) | Submitted, not yet reviewed |
| `UnderReview` | Company | Company is actively considering it |
| `Accepted` | Company | Student is selected |
| `InProgress` | System (execution layer) | Work has actually started |
| `Completed` | System (execution layer) | Work is finished |
| `Rejected` | Company | Not selected |
| `Withdrawn` | Student | Student pulled out |
| `Submit` | (legacy/unused value) | Not used in current flows |

**The review flow** is the most important state change: when a company calls `ReviewApplicationAsync()`, only `Accepted` or `Rejected` are permitted — any other status string returns a validation error. This prevents a company from accidentally skipping states.

---

## 9.7 Applying for a Project — The Full Validation Chain

When Ahmad calls `POST /api/Applications/apply`, `ApplicationService.ApplyForProjectAsync()` runs through seven checks in order:

```
1. Student profile exists? (Student row for this userId)
2. Project exists? (by dto.ProjectId)
3. Deadline not passed? (project.Deadline >= DateTime.UtcNow)
4. Project is visible and Active?
5. MaxApplicants not reached? (if MaxApplicants is set)
6. Duplicate application? (same student + same project)
7. Required skills check:
   - Load all skills marked IsRequired=true on this project
   - Load all skills on the student's profile
   - Find the difference (missing skills)
   - If any missing → return "You are missing: Python, SQL"
```

Only after all seven checks pass does the service create the `Application` row and increment `project.ApplicationCount`.

The skill check is notable — it is not advisory ("you should have these skills") but a hard gate. If the project requires React and the student has not added React to their profile, the application is blocked.

**For Internship projects:** `BidAmount` is always set to 0, regardless of what the student sends. Internships are never paid.

---

## 9.8 Company Reviews an Application — With Real-Time Notification

When TechCorp calls `PUT /api/Applications/{id}/review` with `{ status: "Accepted", note: "Great candidate" }`:

```
1. Get the Application
2. Verify company owns the project (security check — prevent cross-company review)
3. Validate status: only "Accepted" or "Rejected" allowed
4. Update application:
   Status = Accepted
   ReviewedBy = companyUserId
   ReviewedAt = DateTime.UtcNow
   ReviewNotes = "Great candidate"
5. Create a Notification row for the student:
   Title: "Application Update"
   Message: "Your application for 'Backend Internship' was Accepted."
   NotificationType: Acceptance
   RelatedProjectID = project.ProjectID
   RelatedApplicationID = application.ApplicationID
6. SaveAsync() → commit both the Application update and the Notification insert
7. Send real-time notification via INotifier:
   _notifier.SendNotificationAsync(student.UserID, notificationDto)
   → SignalR pushes a message to Ahmad's device instantly
```

This is where two systems work together: the database notification record (persistent — Ahmad can read it later) and the SignalR push (immediate — Ahmad sees it right now even if he is on the app). Notifications and SignalR are covered in detail in Unit 14.

---

## 9.9 Withdrawing an Application — With Guard Rails

Ahmad can withdraw his application if he changes his mind — but only if the status is `Pending` or `UnderReview`. The service explicitly checks:

```csharp
if (application.Status != ApplicationStatus.Pending && 
    application.Status != ApplicationStatus.UnderReview)
{
    return Failure($"Cannot withdraw an application with status '{application.Status}'.");
}
```

If the application is already `Accepted`, `Rejected`, `Completed`, or `InProgress`, withdrawal is blocked. It does not make sense to withdraw after the company has already made a decision.

On successful withdrawal:
- `application.Status = ApplicationStatus.Withdrawn`
- `project.ApplicationCount--` (decremented so the slot opens again)

---

## 9.10 Searching and Filtering Projects — Pagination

`GET /api/Projects` with filter parameters goes through `GetFilteredProjectsAsync()`. Because `IGenericRepository<T>` does not expose `IQueryable`, the service loads all projects into memory and filters with LINQ:

```csharp
var allProjects = await _unitOfWork.Projects.GetAllAsync();
var query = allProjects.AsEnumerable();  // in-memory LINQ from here

query = query.Where(p => p.IsVisible == filter.IsVisible);
query = query.Where(p => p.ProjectName.Contains(filter.Keyword));
// ... more filters
```

Available filter options:
- `Keyword` — matches ProjectName or Description (case-insensitive contains)
- `ProjectType` — exact match (Internship, Training, etc.)
- `Status` — exact match
- `CompanyId` — filter by specific company
- `DeadlineAfter` / `DeadlineBefore` — date range
- `SkillIds` — at-least-one-match: projects that require any of the listed skills

**Sorting presets** (passed as a string in `SortBy`):
- `newest` (default) — most recently created first
- `oldest`, `deadline_asc`, `deadline_desc`, `views_desc`, `applications_desc`, `title_asc`, `title_desc`

**Pagination** is handled by `PagedResult<T>`:
```csharp
var pagedResult = PagedResult<ProjectResponseDto>.Create(
    items,          // the page of results
    filter.PageNumber,
    filter.PageSize,
    totalCount      // total items (for frontend to compute "page 2 of 5")
);
```

The response tells the frontend both the current page's data and the total record count so it can display "showing 10 of 47 projects."

---

## 9.11 What to Say in Your Defense

- "A `Project` is any opportunity a company posts — internship, graduation project, training, part-time, or full-time work. When a company creates a project, it specifies required skills from the platform's canonical skill list, a deadline, and optionally a max applicant count."
- "Applications go through a validated state machine: `Pending → UnderReview → Accepted/Rejected → InProgress → Completed`. Students can only withdraw when status is `Pending` or `UnderReview` — the service enforces this explicitly."
- "Before a student can apply, the service runs seven checks in sequence: student profile exists, project exists, deadline not passed, project is visible and active, max applicants not reached, no duplicate application, and required skills check. Only if all seven pass is the `Application` row created."
- "When a company accepts or rejects an application, the system creates a `Notification` row in the database AND sends a real-time push via SignalR — both in the same request, so the student sees the update immediately."
- "Project creation uses a database transaction: the project INSERT and the skill INSERTs are wrapped in `BeginTransactionAsync/CommitTransactionAsync`. If any skill ID is invalid, the transaction rolls back and nothing is saved."
- "`ViewCount` is incremented every time someone fetches a project by ID — it is a denormalized counter updated in-place, not computed from a log table. This keeps the read fast."
- "For Internship-type projects, `BidAmount` is hardcoded to 0 in the service — the client cannot override this."

---

## 9.12 Self-Check Questions

**Q1: What are the five project types in Sha8alny?**
`Internship`, `GraduationProject`, `Training`, `PartTime`, `FullTime`. For Internships, the bid amount is always forced to 0 by the service.

**Q2: What status does a newly created project get?**
`Active` — the service sets `Status = ProjectStatus.Active` on creation. There is no draft-then-publish workflow in the current implementation.

**Q3: Why does `CreateProjectAsync` use a database transaction?**
Because two things must succeed together: inserting the `Project` row AND inserting all the `ProjectRequiredSkill` rows. If any skill ID is invalid, `RollbackTransactionAsync` is called and neither the project nor any skills are saved.

**Q4: What seven things does the service check before creating an application?**
(1) Student profile exists, (2) project exists, (3) deadline not passed, (4) project is visible and Active, (5) max applicants not reached, (6) no duplicate application from the same student, (7) student has all required skills on their profile.

**Q5: What happens when a company calls `ReviewApplicationAsync` with status "InProgress"?**
The service parses "InProgress" as a valid `ApplicationStatus` value but then immediately checks: `if (newStatus != Accepted && newStatus != Rejected)` — and returns failure "Invalid status. Only 'Accepted' or 'Rejected' are allowed for review." The database is not modified.

**Q6: What happens to `project.ApplicationCount` when a student withdraws an application?**
It is decremented by 1 (down to a minimum of 0). This re-opens the slot if `MaxApplicants` was the limiting factor.

**Q7: How does filtering work if `IGenericRepository` doesn't expose `IQueryable`?**
`GetFilteredProjectsAsync` calls `GetAllAsync()` to load all projects into memory, then applies LINQ `.Where()` filters in C# (not SQL). This works correctly but is less efficient than a SQL `WHERE` clause for large datasets — the entire Projects table is loaded before filtering.
