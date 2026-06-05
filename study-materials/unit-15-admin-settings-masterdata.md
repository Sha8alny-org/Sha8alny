# Unit 15: Admin, Settings, and Master Data — Platform Management

> **Before reading this unit:** You should have read Unit 6 (authentication — how roles are enforced), Unit 4 (database layer — repository pattern). This unit explains the three management surfaces of the platform: Admin dashboard, user settings, and the reference lookup tables.

---

## 15.1 Three Cross-Cutting Concerns

This unit covers three different but related management features:

1. **Admin** — the platform operators who oversee everything: user management, platform statistics, force-deletion of content
2. **User Settings** — per-user preferences (notification toggles, language, visibility)
3. **Master Data** — the lookup tables that serve as the "vocabulary" of the platform: skills, universities, departments

Think of these as the **back office** of the application. The student-company marketplace (Units 8–12) is the storefront. Admin, Settings, and Master Data are the warehouse and management office behind it.

---

## 15.2 Admin — Who They Are and What They Can Do

Admins are users with `UserType = Admin`. Every endpoint in the Admin area is protected by:

```csharp
[Authorize(Roles = "Admin")]
```

A student or company token is rejected with HTTP 403 before any code runs.

**What Admin can do:**

| Operation | Endpoint | What it does |
|---|---|---|
| View dashboard stats | `GET /api/Admin/stats` | Real-time platform metrics |
| View metric history | `GET /api/Admin/metrics/history` | Historical snapshots (last N days) |
| List all users | `GET /api/Admin/users` | Every registered user with profile info |
| Get one user | `GET /api/Admin/users/{id}` | One user's full management info |
| Ban/unban a user | `PUT /api/Admin/users/{id}/ban` | Toggles `User.IsActive` |
| List all projects | `GET /api/Admin/projects` | Every project on the platform |
| Force-delete a project | `DELETE /api/Admin/projects/{id}` | Bypasses ownership checks |

---

## 15.3 The Dashboard — Real-Time Stats and Historical Snapshots

`GetDashboardStatsAsync()` queries the live database and returns:

- **User statistics:** Total students, total companies, total users, active users, banned users, new users in last 30 days
- **Project statistics:** Total projects, active projects, closed/complete projects, new projects last 30 days
- **Application statistics:** Total applications, completed applications
- **Financial statistics:** Total transaction volume (sum of completed transactions), transaction count

This is computed fresh on each admin page load — it reflects the exact current state.

**The `DashboardMetric` snapshot pattern:**

After computing the live stats, `GetDashboardStatsAsync` silently calls `SaveSnapshotIfNeededAsync`. If no snapshot for today exists in the `DashboardMetrics` table, it saves one with the same numbers just computed:

```csharp
var exists = await _unitOfWork.DashboardMetrics.AnyAsync(m => m.MetricDate == today);
if (exists) return;  // Already saved today

var snapshot = new DashboardMetric { ...all the same numbers..., MetricDate = today };
await _unitOfWork.DashboardMetrics.AddAsync(snapshot);
await _unitOfWork.SaveAsync();
```

The failure of this snapshot save is silently swallowed — it does NOT affect the live stats response to the admin.

The `GetMetricHistoryAsync(days)` method reads from `DashboardMetrics` to return a time-series view of platform growth — e.g., "how many students joined each day over the last 30 days." This powers trend charts in the admin UI.

---

## 15.4 Banning and Unbanning Users

`ToggleUserBanAsync(userId)` flips `user.IsActive`:
- `IsActive = false` → user is banned. Every login attempt returns failure ("Your account has been deactivated").
- `IsActive = true` → user is unbanned.

Two important restrictions:
1. **Admins cannot ban other Admins.** The service checks `user.UserType == UserType.Admin` and returns failure if true.
2. **Existing JWT tokens are NOT immediately invalidated.** Because JWT is stateless (the server stores no sessions), a banned user's existing token will still pass signature validation until it expires (60 minutes). However, services that check `user.IsActive` on login would reject further logins. This is a known limitation of stateless JWT.

The response message adapts: `"User has been banned successfully."` or `"User has been unbanned successfully."` based on the new state.

---

## 15.5 Force-Deleting a Project

Normal project deletion (`DELETE /api/Projects/{id}`) requires the caller to be the project owner. Admin's force-delete bypasses this check entirely.

`DeleteProjectForceAsync(projectId)` cascades through dependent records manually:

```
1. Load and delete all ApplicationModuleProgress rows for each module
2. Delete all ProjectModule rows
3. Delete all Application rows for the project
4. Delete all ProjectRequiredSkill rows
5. Delete the Project row
6. SaveAsync()
```

Why manual cascades? Because the database relationships use `OnDelete(DeleteBehavior.Restrict)` to prevent accidental cascades — a student's completed application record should not disappear because a company changed their mind. The admin force-delete explicitly removes everything in the right order to avoid foreign key violations.

---

## 15.6 Master Data — The Platform's Vocabulary

Master Data refers to the reference/lookup tables that define the "vocabulary" of the platform:

**`Skill`** — the canonical list of all skills on the platform:
```
Skills table:
SkillID | SkillName | SkillCategory (enum) | Description | IsActive | CreatedAt
```

- Students select from this list when building their profile (see Unit 8)
- Projects specify required skills from this list (see Unit 9)
- Admin creates, updates, and deletes skills via `MasterDataService`
- `SkillCategory` organizes skills (e.g., Programming, Design, Marketing, Languages)

**`University`** — list of Egyptian universities and their details:
```
Universities table:
UniversityID | UniversityName | UniversityLogo | ContactEmail | ContactPhone
Website | Address | City | Country | UniversityType (enum) | IsActive
```

- Students select their university from this list when creating their profile

**`Department`** — academic departments, linked to a University:
```
Departments table:
DepartmentID | UniversityID | DepartmentName | Description | IsActive
```

- Students select their department (which automatically constrains to their university's departments)

**Who manages these?** Admin only. Students and companies can read the lists (`GET /api/MasterData/skills`, `GET /api/MasterData/universities`), but only Admins can create/update/delete entries. This prevents free-text entries that cause inconsistency ("React.js" vs "ReactJS" vs "react" would all be different skills without this control).

**Pre-loaded data:** `DbInitializer.SeedAsync` (run at startup — see Unit 16) pre-loads common Egyptian universities and a set of technology skills so the platform is not empty for the first user.

---

## 15.7 User Settings — Per-User Preferences

`UserSettings` is a one-to-one optional relationship with `User` — one settings row per user. If a user has never touched settings, the service creates default settings on first read.

**`UserSettings` entity:**

```
UserSettings table:
UserID | EmailNotifications | PushNotifications | MessageNotifications | ApplicationNotifications
Language | Timezone | ProfileVisibility | UpdatedAt
```

**Notification toggles:**
| Field | What it controls |
|---|---|
| `EmailNotifications` | Whether the system sends email for events |
| `PushNotifications` | General push notifications |
| `MessageNotifications` | Notifications for new chat messages |
| `ApplicationNotifications` | Notifications for application status changes |

**`ProfileVisibility`** — who can see the user's profile:
- `Public` — visible to everyone (default)
- `UniversityOnly` — visible only to users from the same university
- `Private` — hidden from browsing (can still apply to projects)

**Default settings** (created automatically if not found):
```csharp
new UserSettings
{
    EmailNotifications = true,
    PushNotifications = true,
    MessageNotifications = true,
    ApplicationNotifications = true,
    Language = "en",
    Timezone = "UTC",
    ProfileVisibility = ProfileVisibility.Public
}
```

**Upsert pattern:** `UpdateSettingsAsync` checks if settings exist. If yes, update. If no, create. Same find-or-create pattern as Company profile (Unit 8) and Conversation (Unit 13) — consistent across the codebase.

---

## 15.8 What to Say in Your Defense

- "Admin users have access to a management dashboard with real-time platform statistics. Every time an Admin views the dashboard, the system also silently saves a daily snapshot to the `DashboardMetrics` table, so historical growth trends are available without a separate background job."
- "User banning is implemented by toggling `User.IsActive`. Subsequent login attempts are rejected by `AuthService.LoginAsync` which checks `IsActive` before generating a token. Existing valid tokens continue to work until they expire (60 minutes) — a known limitation of stateless JWT."
- "Master Data (Skills, Universities, Departments) is maintained by Admin through CRUD endpoints. These are the canonical lookup tables that students and projects reference. Using controlled lookup tables instead of free text prevents 'React vs ReactJS vs react.js' inconsistency."
- "User Settings are auto-created on first access with sensible defaults (all notifications enabled, Public visibility, English). The `GetSettingsAsync` method creates the default row if none exists, so the client never gets a 'not found' error for settings."
- "The Admin force-delete endpoint manually cascades through dependent records (module progress, modules, applications, required skills) before deleting the project. The database uses `Restrict` on delete behavior, so manual ordering is required to avoid foreign key violations."

---

## 15.9 Self-Check Questions

**Q1: What does `ToggleUserBanAsync` do, and what can it NOT do?**
It flips `user.IsActive` between `true` (active) and `false` (banned). If `IsActive = false`, login fails with "Your account has been deactivated." It cannot ban Admin users — the service returns failure if `user.UserType == Admin`.

**Q2: When does the `DashboardMetric` snapshot get saved?**
Silently, every time an Admin calls `GET /api/Admin/stats`. `SaveSnapshotIfNeededAsync` checks if a snapshot for today already exists. If not, it saves one with the same numbers just computed. One snapshot per day, automatically, without any separate background job or cron.

**Q3: Why does Admin force-delete manually remove dependent records instead of relying on SQL CASCADE?**
The project's relationships use `OnDelete(DeleteBehavior.Restrict)`, which prevents automatic cascades to protect student application history. Admin force-delete explicitly deletes in the correct dependency order: progress records → modules → applications → required skills → project.

**Q4: What are the three Master Data lookup tables?**
`Skills` (canonical list of skills with SkillCategory), `Universities` (Egyptian universities with location/contact info), and `Departments` (academic departments linked to a University). All are managed via Admin CRUD endpoints and used as foreign key references by Student and Project entities.

**Q5: What happens when `GetSettingsAsync` is called for a user who has never set preferences?**
The service checks `FindSingleAsync(s => s.UserID == userId)` and gets null. It then creates a new `UserSettings` row with default values (all notifications enabled, language "en", timezone "UTC", visibility Public), saves it, and returns it. The user always gets a valid settings response.

**Q6: What is `ProfileVisibility` and what are its three values?**
An enum on `UserSettings` that controls who can see the user's profile when browsing. `Public` (default) = everyone. `UniversityOnly` = only users from the same university. `Private` = hidden from browsing searches (but the user can still apply to projects and be contacted).

**Q7: How does skill consistency work on the platform?**
Skills are a centralized lookup table managed by Admin. Students and projects reference skills by `SkillID`, not by free text. Admin controls what skills exist. This prevents the classic "React vs ReactJS vs react.js" fragmentation problem that makes skill-matching unreliable.
