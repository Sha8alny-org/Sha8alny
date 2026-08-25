# fix_2.md — Remaining Issues After Migration Pass 1–7

This document lists every known remaining issue after the Supabase→.NET migration fixes.
It covers both mobile-side changes and backend-side changes required.
No code is written here — this is an instruction document only.

---

## Issue 1 — Profile Creation Returns 400 Bad Request

### Root Cause

The backend `CreateStudentProfileDto` requires six fields that the mobile create profile form never sends.
The previous fix session only fixed the *update* path. The *create* path was missed.

### What the form currently sends
- `academicYear` (string)
- `totalInternshipDays` (int)

### What the backend requires (all marked `[Required]`)
- `fullName` — student's full name
- `cvFileUrl` — a URL to a .pdf, .docx, or .pptx file
- `city` — city of residence
- `country` — country of residence
- `educations` — array with at least one entry, each entry needs: `universityName`, `degree`, `startYear`
- `experiences` — array with at least one entry, each entry needs: `companyName`, `role`, `startDate`

### Backend fixes required (file: `Sh8lny.Shared/DTOs/StudentProfile/CreateStudentProfileDto.cs`)

Two fields are unreasonable to require at profile creation time for university students:

1. **Make `cvFileUrl` optional** — Remove the `[Required]` attribute and `[AllowedFileExtensions]` attribute.
   Change the type to `string?` and default to `null`. Students should be able to create a profile
   without having a CV uploaded yet. They can add it later from the edit profile screen.

2. **Make `experiences` optional** — Remove the `[Required]` and `[MinLength(1)]` attributes.
   Change to `List<ExperienceDto> Experiences { get; set; } = new()` (already initialised, just remove
   the Required/MinLength). University students applying for their first internship will have zero
   work experience. Requiring at least one experience blocks all new users.

### Mobile fixes required (after backend is patched)

**File: `lib/features/profile/presentation/pages/create_profile_page.dart`**

Add two new text fields to the form:
- A `city` field (required, plain text input)
- A `country` field (required, plain text input)

Pass both values to `context.read<ProfileCubit>().createProfile(...)` alongside the existing
`academicYear`, `major`, `university`, `trainingDays` arguments.

**File: `lib/features/profile/presentation/cubit/profile_cubit.dart`**

Add `city` and `country` as required named parameters to the `createProfile()` method signature.
The cubit already calls `_getUser()` which returns a `UserEntity` that has the user's `name`.
Pass `user.name` (as `fullName`), plus `city` and `country`, down to the use case / datasource call.

**File: `lib/features/profile/domain/usecases/profile_usecase.dart` and `lib/features/profile/domain/repositories/profile_repository.dart`**

Add `fullName`, `city`, and `country` as parameters to the `createStudentProfile` method signature
through the domain layer so they reach the datasource.

**File: `lib/features/profile/data/datasources/profile_net_api_datasource.dart`**

Replace the existing `createStudentProfile` body (which currently calls `_profileToBody` — the update
helper) with a dedicated create body that includes:
- `fullName` from the passed parameter (sourced from `user.name` in the cubit)
- `city` from the passed parameter
- `country` from the passed parameter
- `academicYear` from the profile model
- `totalInternshipDays` from the profile model
- `educations` as a list with one entry auto-built from the form data:
  - `universityName` = `profile.university`
  - `degree` = `"Bachelor"` (hardcoded default — reasonable for this app's target users)
  - `fieldOfStudy` = `profile.major`
  - `startYear` = current year minus the academic year offset
    (FirstYear=0, SecondYear=1, ThirdYear=2, FourthYear=3, FifthYear=4)
- `experiences` = empty list `[]` (valid after backend removes the MinLength constraint)
- `cvFileUrl` = omit or send `null` (valid after backend removes the Required constraint)

---

## Issue 2 — Progress Screen Shows Fake/Hardcoded Data

### Root Cause

`lib/features/progress/data/datasources/progress_net_api_datasource.dart` never calls
`GET /api/Execution/application/{id}/progress`. Instead it calls `GET /api/Applications/my-applications`
and hardcodes progress values (duration, salary, progress percentage) from the flat application data.

### What needs to happen

**File: `lib/features/progress/data/datasources/progress_net_api_datasource.dart`**

Replace the fake implementation with a real call to `/api/Execution/application/{id}/progress`
where `{id}` is the application ID.

Before doing this, read the backend controller at
`Sh8lny.Web/Controllers/ExecutionController.cs` and the response DTO to know:
- Exact route and HTTP verb
- What fields the response contains (progress percentage, module completions, salary, duration, etc.)
- Whether the response is wrapped in `ServiceResponse<T>` or returned raw

Then update `ProgressModel.fromNetJson` in
`lib/features/progress/data/models/progress_model.dart`
to map from the actual response field names.

The cubit and repo do not need to change — only the datasource and model.

---

## Issue 3 — Skill Add/Remove Does Nothing

### Root Cause

`addStudentSkill` and `removeStudentSkill` in
`lib/features/profile/data/datasources/profile_net_api_datasource.dart`
are explicit no-op stubs left from the migration.

The backend `StudentProfileUpdateDto.SkillIds` is a `List<int>` — a list of skill integer IDs.
The mobile currently only has skill *names* (strings), not IDs.

### What needs to happen

**Step 1 — Fetch skills from MasterData**

`GET /api/MasterData/skills` returns a list of skill objects. Each has an `id` (int) and a `name` (string).
This endpoint is already partially wired in
`lib/features/master_data/data/datasources/` — confirm it works and that the response parses correctly.

**Step 2 — Wire addStudentSkill**

When the user selects a skill to add, the UI must pass the skill's integer `id` (not just its name)
down to the datasource. This means the skill selection UI must source its options from the MasterData
skill list (id + name pairs), not free-text input.

In the datasource, fetch the current student profile to get the existing `skillIds` list.
Append the new skill id. Then PUT to `/api/students/profile` (or whatever the student profile
update endpoint is) with `skillIds` set to the full updated list.

**Step 3 — Wire removeStudentSkill**

Same as add: fetch current profile to get current `skillIds`, remove the target id, PUT the updated list.

**Note:** `StudentProfileUpdateDto.SkillIds` replaces the entire skill set on each PUT.
There is no add-one or remove-one endpoint. Always send the full desired list.

---

## Issue 4 — University and Department Cannot Be Updated

### Root Cause

The backend `StudentProfileUpdateDto` requires integer `universityID` and `departmentID` fields.
`StudentProfileModel` only stores the university and department as name *strings*
(populated from `universityName` / `departmentName` in the GET response).
The `_profileToBody` method in the datasource has no IDs to send, so university/department
updates are silently dropped.

### What needs to happen

**Step 1 — Fetch universities and departments from MasterData**

Check if `GET /api/MasterData/universities` and `GET /api/MasterData/departments` (or equivalent)
exist in the backend. Read `Sh8lny.Web/Controllers/MasterDataController.cs` to confirm the exact routes.
Each should return a list of `{ id, name }` objects.

**Step 2 — Extend StudentProfileModel**

Add optional `universityId` (int?) and `departmentId` (int?) fields to `StudentProfileModel`.
Populate them from the GET profile response if the backend returns them
(check `StudentResponseDto` for `UniversityID` / `DepartmentID` fields).

**Step 3 — Update the edit profile UI**

Replace the free-text university and department inputs with dropdowns populated from MasterData.
When the user selects a university or department, store the integer ID alongside the name.

**Step 4 — Update _profileToBody**

Once the model carries IDs, add `universityID` and `departmentID` to the update body.

---

## Issue 5 — hasApplied and getCompletionStatus Always Return false/null

### Root Cause

`hasApplied(projectId)` and `getCompletionStatus(projectId)` in
`lib/features/apply_form/data/datasources/apply_form_net_api_datasource.dart`
are stubbed to return `false` and `null` respectively because
`ApplicationResponseDto` does not include `projectId` — so the mobile cannot
match an application to a project.

This was documented in `backend-dto-fixes.md` which was pushed to the backend repo.

### What needs to happen

**Backend (already documented in backend-dto-fixes.md):**
Add `ProjectId` to `ApplicationResponseDto` and map it in `MappingProfile.cs`.

**Mobile (after backend ships the fix):**

In `apply_form_net_api_datasource.dart`:

- `hasApplied(projectId)`: Call `GET /api/Applications/my-applications`.
  Parse the response list. Return `true` if any application's `projectId` matches
  the passed `projectId`.

- `getCompletionStatus(projectId)`: Same call. Find the matching application by `projectId`.
  Return its `status` string (e.g. `"Completed"`, `"InProgress"`) or `null` if not found.

Also update `ApplicationModel.fromNetJson` in
`lib/features/apply_form/data/models/application_model.dart`
to read `json['projectId']` as an int field (the DTO will now include it).

And update `fetchAppliedOpportunities` in
`lib/features/opportunities/data/datasourse/opportunities_net_api_datasource.dart`
to use the real `projectId` for matching instead of falling back to application id.

---

## Summary Table

| Issue | Mobile files to change | Backend files to change | Blocked on backend? |
|---|---|---|---|
| Profile creation 400 | create_profile_page, profile_cubit, profile_usecase, profile_repository, profile_net_api_datasource | CreateStudentProfileDto.cs — make cvFileUrl and experiences optional | Yes — must fix backend first |
| Progress screen fake data | progress_net_api_datasource, progress_model | None | No |
| Skill add/remove no-op | profile_net_api_datasource, skill selection UI | None | No |
| University/dept not updating | student_profile_model, edit profile UI, profile_net_api_datasource | None (if StudentResponseDto already returns IDs) | Possibly — check StudentResponseDto |
| hasApplied always false | apply_form_net_api_datasource, application_model, opportunities_net_api_datasource | ApplicationResponseDto.cs + MappingProfile.cs (already in backend-dto-fixes.md) | Yes — must fix backend first |
