# Backend DTO Fixes Required — Sha8alny Mobile Integration

This document was generated from a mobile-side audit of the .NET backend contract.
Hand it to Claude Code inside the `E:\LLM testing\Sha8alny` backend repo.

---

## Prompt for Claude Code (paste this into the backend repo session)

> You are working on the Sha8alny ASP.NET Core 9 backend at this repo root.
> The mobile app has been audited against the current DTO contract. One DTO change
> is required to unblock two mobile features. Everything else was fixed on the
> mobile side. Make the changes described below, then run the project to confirm
> it still builds. Do not change any behaviour — only add fields to DTOs and
> update the AutoMapper profile to populate them.

---

## Change 1 — Add `ProjectId` to `ApplicationResponseDto`

**File:** `Sh8lny.Shared/DTOs/Applications/ApplicationResponseDto.cs`

**Current:**
```csharp
public class ApplicationResponseDto
{
    public int Id { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public decimal BidAmount { get; set; }
}
```

**Required:**
```csharp
public class ApplicationResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }          // ADD THIS
    public string ProjectTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public decimal BidAmount { get; set; }
}
```

**Why:** The mobile app calls `GET /api/Applications/my-applications` to:
1. Show the "Applied Opportunities" list with clickable project links
2. Pre-check `hasApplied(projectId)` before showing the Apply button
3. Check `getCompletionStatus(projectId)` to show the completion flow

Without `ProjectId` in the response, all three features are broken — the mobile can
only see application IDs, not which project each application belongs to.

---

## Change 2 — Update MappingProfile to populate `ProjectId`

**File:** `Sh8lny.Web/Mappings/MappingProfile.cs` (or wherever `ApplicationResponseDto` is mapped)

Find the AutoMapper configuration for `Application` → `ApplicationResponseDto` and
add the `ProjectId` mapping. The exact member path depends on your entity model,
but it will be one of:

```csharp
// If Application has a direct ProjectId property:
.ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))

// If Application has a navigation property Project:
.ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.Project.Id))
```

Check `Sh8lny.Domain/Entities/Application.cs` to find the correct property name.

---

## Verification

After making the change:

1. Build the project: `dotnet build`
2. Run and hit `GET /api/Applications/my-applications` with a student JWT.
3. Confirm the response JSON now includes `"projectId": <integer>` on each item.
4. Example expected response element:
   ```json
   {
     "id": 42,
     "projectId": 7,
     "projectTitle": "Flutter Developer Internship",
     "status": "Pending",
     "appliedDate": "2026-05-10T14:22:00Z",
     "bidAmount": 0
   }
   ```

---

## No Other Backend Changes Required

The following issues were fixed on the mobile side and do NOT require backend changes:

| Mobile bug | Root cause | Already fixed in mobile |
|---|---|---|
| Training submission rejected | Mobile sent `applicationId` (lowercase d); DTO requires `applicationID` (capital ID) | ✅ Fixed |
| Profile picture not updating | Mobile sent `profilePictureUrl`; DTO field is `ProfilePicture` → wire `profilePicture` | ✅ Fixed |
| Student profile ID always 0 | Mobile read `data['studentID']`; `StudentResponseDto` field is `Id` → wire `id` | ✅ Fixed |
| Training submission fields unread | Mobile read `trainingSubmissionId`/`studentId`/`applicationId`; DTO uses capital `ID` variants | ✅ Fixed |
| Applied opportunities list empty | Mobile read `app['project']` (non-existent); now builds stubs from `projectTitle` + `status` | ✅ Fixed (partial — full navigation requires Change 1 above) |
| Status comparison always wrong | Mobile compared lowercase `'approved'`; backend sends PascalCase `'Accepted'` | ✅ Fixed |
