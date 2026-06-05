# Unit 10: Execution and Modules — Tracking Work After Acceptance

> **Before reading this unit:** You should have read Unit 9 (Projects and Applications — the state machine, how acceptance works). This unit explains what happens after a student is accepted: how work is tracked, how the company verifies progress, and how a project officially ends.

---

## 10.1 The Problem: Acceptance Is Not Completion

When TechCorp accepts Ahmad's application (Unit 9), a contract is formed. But there is no mechanism yet to answer:
- What work is Ahmad supposed to do?
- How far along is he?
- How does TechCorp know he finished?
- When does the platform generate a certificate?

The `Execution` layer answers all of these. It is the tracking engine for work-in-progress.

Think of it like a **project management board** (similar to Trello or Jira): the company defines the tasks (modules), the student marks them complete as he progresses, and the company approves each one. When everything is approved, the company presses "Close" and the platform generates the certificate automatically.

---

## 10.2 Modules — The Unit of Work

A `ProjectModule` is one defined piece of work within a project. Think of it as a chapter in a textbook — each chapter has a title, a description, and a weight (how much of the final grade it counts for).

**The `ProjectModule` entity:**

```
ProjectModule table:
Id | ProjectId | Title | Description | EstimatedDuration
OrderIndex | Weight | Status
```

- `Weight` — a decimal percentage (0–100). All module weights in a project must sum to ≤ 100%. This is enforced by the service on every `AddModuleAsync` call.
- `OrderIndex` — the sequence number. Automatically set to `max(existing) + 1` when a new module is added.
- `EstimatedDuration` — free text: "2 weeks", "10 days", etc.
- `Status` — the module's own state (separate from the application's state)

**`ModuleStatus` enum:**
| Status | What it means |
|---|---|
| `Pending` | Defined but no student progress yet |
| `InProgress` | Student has started but not completed it |
| `Completed` | Student marked it 100% done |
| `Approved` | Company verified and accepted the work |
| `Rejected` | Company reviewed and sent it back for revision |

**Critical restriction:** Modules only exist for `Internship`-type projects. If a company tries to add a module to a `GraduationProject` or `Training` project, the service returns: `"Modules/Milestones are only available for Internship projects."`

---

## 10.3 ApplicationModuleProgress — Tracking the Student's Work

There is a third table that links an `Application` to a `ProjectModule` — it tracks how far a specific student has progressed on each module:

```
ApplicationModuleProgress table:
ApplicationId | ProjectModuleId | ProgressPercentage | Note
IsCompleted   | CompletedAt     | UpdatedAt
```

This is a many-to-many in spirit: multiple applications can exist per project (if multiple students were accepted — though usually one). Each application tracks its own progress against each module independently.

The relationship is: one `Application` + one `ProjectModule` → one `ApplicationModuleProgress` row.

**Upsert behavior:** When `UpdateProgressAsync` is called, the service checks if a progress record already exists for that `(applicationId, moduleId)` pair:
- If not found → create a new `ApplicationModuleProgress` row
- If found → update the existing one

This is the same upsert pattern as Company profile creation (Unit 8).

---

## 10.4 The Progress Update Flow

Ahmad finishes the first module ("Database Design") and reports 100% progress:

```
Ahmad → PUT /api/Execution/applications/{applicationId}/progress
Body: { moduleId: 7, progressPercentage: 100, note: "ERD diagram attached" }

1. Verify Ahmad owns this application (StudentID check)
2. Verify application status is Accepted or InProgress (not Pending, Rejected, etc.)
3. Verify module exists and belongs to this application's project
4. Validate progressPercentage is between 0 and 100
5. Upsert the ApplicationModuleProgress record:
   IsCompleted = (progressPercentage == 100)
   CompletedAt = DateTime.UtcNow (if 100%)
6. SaveAsync()
7. Call CheckAndCompleteApplicationAsync(application, student)
```

**Step 2 transition rule:** The first time Ahmad reports any progress, if the application is still `Accepted`, the service automatically moves it to `InProgress`. This represents: "Ahmad has started working." The application never manually goes to `InProgress` — it is automatic.

**`CheckAndCompleteApplicationAsync`** — the auto-completion checker:

After every progress update, the service checks: "Are ALL modules at 100% and marked `IsCompleted = true`?" It does this by comparing:
- The set of all module IDs for this project
- The set of completed module IDs in progress records

If all match → notify the company: "Ahmad has completed all modules for 'Backend Internship'. You can now formally complete the job."

Importantly, the application does NOT automatically move to `Completed` here. The company must explicitly call `CompleteJobAsync`. This preserves human oversight — the company can review the work before marking it done.

---

## 10.5 Overall Progress Calculation

When anyone calls `GET /api/Execution/applications/{id}/progress`, the response includes a calculated `OverallProgress` percentage. It is not a simple average — it is **weighted**:

```csharp
overallProgress += (module.Weight / 100m) * progressPercentage;
```

Example: three modules with weights 50%, 30%, 20%:
- Module 1 (50% weight): 100% done → contributes 50
- Module 2 (30% weight): 60% done → contributes 18
- Module 3 (20% weight): 0% done → contributes 0
- **Overall progress = 68%**

This gives an accurate picture of how much of the *significant* work is done, not just how many modules are finished.

---

## 10.6 Company Reviews a Module

After Ahmad marks a module 100%, TechCorp reviews the submitted work and calls `PUT /api/Execution/modules/{moduleId}/review` with `{ status: "Approved", companyFeedback: "Well done" }`.

The service:
- Validates the company owns the project
- Only accepts `"Approved"` or `"Rejected"` (case-insensitive) — any other status is rejected
- Finds all `ApplicationModuleProgress` records for this module across all active applications

**If Approved:**
- `module.Status = ModuleStatus.Approved`
- Progress records: `ProgressPercentage = 100`, `IsCompleted = true`, feedback note saved
- Application status moved to `InProgress` (if not already Completed)

**If Rejected:**
- `module.Status = ModuleStatus.Rejected`
- Progress records: `IsCompleted = false`, `ProgressPercentage` capped at 99 (never 100)
- The student must redo the module

The progress cap at 99 on rejection is subtle: it prevents `CheckAndCompleteApplicationAsync` from triggering false "all complete" notifications — if a module is rejected, it is explicitly not 100%, even if the student had reported it as such.

---

## 10.7 Completing the Job — The Grand Finale

When all modules are approved, TechCorp calls `POST /api/Execution/complete` with `{ applicationId: ..., companyFeedbackNote: "...", finalDeliverableUrl: "..." }`. `CompleteJobAsync` runs through 10 steps:

```
1. Verify company exists
2. Get the Application
3. Verify company owns the project
4. Verify application is InProgress or Accepted (not already Completed)
5. CRUCIAL GATE: Check ALL modules have IsCompleted=true AND ProgressPercentage=100
   → If any incomplete → return failure "All modules must be completed before closing."
6. Get the Student
7. State changes:
   Application.Status = Completed
   Application.CompletedAt = DateTime.UtcNow
   Application.CompanyFeedbackNote = dto.CompanyFeedbackNote
   Application.FinalDeliverableUrl = dto.FinalDeliverableUrl
   Project.Status = Closed
   Project.EndDate = DateTime.UtcNow
8. Notify student: "Your work on 'Backend Internship' has been marked Completed!"
   + SignalR push
9. Auto-generate certificate:
   _certificateService.GenerateCertificateAsync(applicationId)
10. Build and return CompletionSummaryDto:
    - Duration (start → end, formatted as "2 months, 3 days")
    - DurationDays
    - TotalModulesCompleted
    - CertificateId (if generation succeeded)
    - TotalPaid (from BidAmount)
```

**The cascade of effects from one API call:**
- Application becomes `Completed`
- Project becomes `Closed`
- Student gets a real-time notification
- Certificate is generated automatically
- Company gets a summary with duration stats

This is the richest endpoint in the system — it coordinates four different concerns (application state, project state, notifications, certificate generation) in one atomic operation.

---

## 10.8 Module Weight Validation

The service enforces that total weights never exceed 100%:

```csharp
var existingModules = await _unitOfWork.ProjectModules.FindAsync(m => m.ProjectId == projectId);
var totalWeight = existingModules.Sum(m => m.Weight) + dto.Weight;

if (totalWeight > 100)
{
    var availableWeight = 100 - existingModules.Sum(m => m.Weight);
    return Failure($"Total module weight cannot exceed 100%. Available weight: {availableWeight}%");
}
```

The error message helpfully tells the company exactly how much weight is left (e.g., "Available weight: 30%"). This saves the company from doing the math themselves.

If a company tries to add a module with `Weight = 0` or negative, that is also rejected: "Module weight must be greater than 0."

---

## 10.9 What to Say in Your Defense

- "After a student is accepted, work is tracked through `ProjectModules` and `ApplicationModuleProgress`. The company defines modules (each with a weight percentage summing to at most 100%), and the student reports progress per module."
- "Modules are only available for Internship-type projects — the service enforces this at the API level."
- "The first time a student reports any progress, the application automatically transitions from `Accepted` to `InProgress`. The system does this without any explicit trigger from the client."
- "Overall progress is weighted — not a simple average. A module worth 50% contributes proportionally more to the overall percentage than one worth 20%."
- "When all modules hit 100%, the system notifies the company in real-time via SignalR. But it does NOT auto-complete — the company must explicitly call `CompleteJobAsync`. This preserves human oversight."
- "`CompleteJobAsync` does four things in one request: marks the application `Completed`, marks the project `Closed`, sends a real-time notification to the student, and auto-generates a certificate."
- "Module rejection caps progress at 99% — this prevents the auto-completion checker from falsely treating a rejected module as done."

---

## 10.10 Self-Check Questions

**Q1: Are modules available for all project types?**
No. Modules are only for `Internship`-type projects. The service explicitly rejects module creation for other types with an error message.

**Q2: What does `Weight` mean on a module, and what constraint applies to it?**
`Weight` is the module's percentage contribution to the total project (0–100). The sum of all module weights for a project cannot exceed 100%. The service validates this on every `AddModuleAsync` call and returns the available weight in the error message.

**Q3: When does an application move from `Accepted` to `InProgress`?**
Automatically, the first time the student calls `UpdateProgressAsync` to report any progress on any module. No explicit status change is needed from the client.

**Q4: When the service detects all modules are 100% complete, what does it do?**
It sends a notification (database row + SignalR push) to the company: "All modules completed. You can now formally complete the job." It does NOT automatically complete the application — that requires the company to call `CompleteJobAsync`.

**Q5: What does `CompleteJobAsync` produce as output?**
A `CompletionSummaryDto` containing: ApplicationId, ProjectId, StudentName, StartDate, EndDate, DurationDays, DurationText (human-readable like "2 months, 3 days"), TotalPaid (BidAmount), TotalModulesCompleted, CompanyFeedbackNote, FinalDeliverableUrl, CertificateGenerated (bool), CertificateId (the unique certificate identifier).

**Q6: What happens to `module.Status` when a company rejects a module review?**
It becomes `ModuleStatus.Rejected`. The student's progress percentage is capped at 99 (not 100), and `IsCompleted` is set to `false`. The student must redo the module and report 100% again before the company can approve it.

**Q7: How is `OverallProgress` calculated?**
It is a weighted sum: for each module, `(module.Weight / 100) × progressPercentage`. A module worth 50% that is 80% done contributes 40 points to the overall score. This reflects the significance of each module rather than treating all modules equally.
