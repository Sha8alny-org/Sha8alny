# Sha8alny — Pre-Cutover Test Plan (B1–B18 Validation)

> **Purpose:** Verify every .NET datasource implemented in B1–B18 before running B19 (Supabase removal).
> **Flutter repo:** `E:\LLM testing\Sha8alny-front-end`
> **Backend repo:** `E:\LLM testing\Sha8alny`
> **Run date:** June 2026

---

## Pre-Test Setup (Do This First)

### 1. Flip the provider to netApi

File: `lib/core/config/auth_config.dart`

Change:
```dart
static const AuthProviderType _defaultProvider = AuthProviderType.supabase;
```
To:
```dart
static const AuthProviderType _defaultProvider = AuthProviderType.netApi;
```

> **This is temporary for testing only.** Revert to `supabase` if a test session needs to stop.
> B19 will make this permanent and clean up Supabase.

### 2. Verify `.env` has the backend URL

File: `.env` (project root)

```
NET_API_BASE_URL=https://sha8alny-backend-857164936517.us-central1.run.app
```

If testing locally, use `http://10.0.2.2:5000` for Android emulator or `http://localhost:5000` for web.

### 3. Ensure backend is running and seeded

- Run the .NET backend (`dotnet run` in `Sh8lny.Web` or Cloud Run URL is live)
- Confirm Swagger is accessible at the backend root URL
- Confirm `DbInitializer.SeedAsync` has seeded Skills and Universities

### 4. Test account prerequisites

Create these accounts via `POST /api/Auth/register` (Swagger or Postman) before running tests:

| Account | Email | Password | UserType |
|---|---|---|---|
| Student | `test.student@sha8alny.com` | `Test@1234` | `Student` |
| Company | `test.company@sha8alny.com` | `Test@1234` | `Company` |

Verify both emails (call `POST /api/Auth/verify-email` with the OTP from the backend logs/email).

### 5. flutter pub get

```bash
flutter pub get
```

Confirm `signalr_netcore` is resolved with no conflicts.

---

## T1 — Infrastructure (B1): Dio + JWT + ServiceResponse

**Goal:** Confirm the Dio client connects to the backend, injects JWT, and unwraps the ServiceResponse envelope.

### T1.1 — Base URL resolves
- [ ] `EnvConfig.netApiBaseUrl` returns the correct URL (add a temporary `debugPrint` in `main.dart` or check via DioConsumer log)
- [ ] First Dio request logs `REQUEST[GET] => PATH: /api/...` in the console

### T1.2 — Unauthenticated request returns 401
- [ ] Manually call any `[Authorize]` endpoint without logging in
- [ ] Confirm `UnauthorizedException` is thrown (not a crash)

### T1.3 — ServiceResponse envelope unwrapping
- [ ] A successful response surfaces `data` field contents (not the wrapper object)
- [ ] A failed response (success=false) throws `ValidationException` with the backend's `message` field
- [ ] A non-envelope response (e.g. `GET /api/Maintenance/config`) passes through raw

### T1.4 — JWT header injection
- [ ] After login, inspect a subsequent request log — confirm `Authorization: Bearer <token>` header is present
- [ ] Token is stored in SharedPreferences under `CacheKeys.authToken`

---

## T2 — Auth (B2): Login / Register / SignOut

**Goal:** End-to-end auth flow using `AuthNetApiDataSource`.

### T2.1 — Login success
- [ ] Login screen → enter `test.student@sha8alny.com` / `Test@1234`
- [ ] App navigates to home screen
- [ ] `CacheKeys.authToken` has a non-empty JWT stored in SharedPreferences
- [ ] `CacheKeys.userData` has serialized `UserModel` with correct `id`, `email`, `name`, `role`

### T2.2 — Login failure
- [ ] Wrong password → `AuthFailure` state shown with backend error message
- [ ] Non-existent email → `AuthFailure` shown

### T2.3 — Register new student
- [ ] Register screen → new email + password + name
- [ ] `POST /api/Auth/register` called with `userType: 'Student'`
- [ ] `AuthSignUpSuccess` state emitted
- [ ] App navigates to home (or email verification gate if required)

### T2.4 — Sign out
- [ ] Settings → Sign Out
- [ ] `CacheKeys.authToken` cleared from SharedPreferences
- [ ] `CacheKeys.userData` cleared
- [ ] App returns to login screen

### T2.5 — FCM token update
- [ ] After login, `updateFcmToken` fires silently (no crash even if FCM token is null in emulator)

---

## T3 — Master Data (B3): Skills / Universities / Departments

**Goal:** `MasterDataCubit` fetches all three lists in parallel from the .NET backend.

### T3.1 — fetchAll() completes without error
- [ ] Trigger `MasterDataCubit.fetchAll()` (e.g. from profile creation screen or a test route)
- [ ] `MasterDataLoaded` state emitted with non-empty `skills`, `universities`, `departments` lists

### T3.2 — Data integrity
- [ ] `SkillEntity.id` is an int (not null)
- [ ] `SkillEntity.name` and `category` are non-empty strings
- [ ] `UniversityEntity.id` and `name` are populated
- [ ] `DepartmentEntity.id` and `name` are populated

### T3.3 — Partial failure resilience
- [ ] If one endpoint is temporarily down, the other two still succeed (no full failure)

---

## T4 — Student Profile (B4): Get / Update / Upload

**Goal:** `ProfileNetApiDataSource` reads and writes the student profile.

### T4.1 — Get own profile
- [ ] Profile screen loads with data from `GET /api/students/profile`
- [ ] `StudentProfileModel.fromNetJson` maps: `studentID`, `departmentName`, `academicYear`, `cvFileUrl`, `gitHubProfile`, `universityName`, `userID`

### T4.2 — Update profile
- [ ] Edit a field (e.g. Bio or GitHubProfile) → save
- [ ] `PUT /api/students/profile` called
- [ ] Profile screen refreshes with updated data

### T4.3 — Upload CV (two-step)
- [ ] Pick a PDF file
- [ ] Step 1: `POST /api/Media/upload?folder=cv` → URL returned
- [ ] Step 2: `PUT /api/students/profile` with `CvFileUrl` = returned URL
- [ ] Profile shows updated `cvFileUrl`

### T4.4 — Upload profile picture (two-step)
- [ ] Pick an image
- [ ] Step 1: `POST /api/Media/upload?folder=profile` → URL returned
- [ ] Step 2: `PUT /api/students/profile` with `ProfilePictureUrl` = returned URL
- [ ] Avatar updates in profile screen

### T4.5 — Skills from profile DTO
- [ ] Student skills displayed on profile screen
- [ ] Skills are extracted from `studentSkills` array in profile DTO (not a separate API call)

---

## T5 — Opportunities (B5): List / Filter / Modules

**Goal:** `OpportunitiesNetApiDataSource` fetches projects from `GET /api/Projects`.

### T5.1 — Fetch all opportunities
- [ ] Home/Opportunities screen loads list from `GET /api/Projects`
- [ ] `OpportunityModel.fromNetJson` maps: `projectID`→`id`, `projectName`→`title`, `projectType`→`type`, `deadline`, `status`, `company.companyName`→`company`, skills list

### T5.2 — Filter by type (Internship vs Training)
- [ ] Toggle between Internship / Training filter
- [ ] `GET /api/Projects?projectType=Internship` called (or client-side filter applied)
- [ ] List refreshes with correct project type

### T5.3 — Fetch applied opportunities
- [ ] Applied tab shows student's own applications from `GET /api/Applications/my-applications`
- [ ] Nested `project` object extracted from `ApplicationResponseDto`

### T5.4 — Fetch modules for a project
- [ ] Open an opportunity detail with modules
- [ ] `GET /api/Execution/project/{id}/modules` called
- [ ] `ModuleModel.fromNetJson` maps: `id`, `title`, `description`, `estimatedDuration`, `projectID`

---

## T6 — Apply Form (B6): File Upload + Submit Application

**Goal:** `ApplyFormNetApiDataSource` follows the two-step upload pattern.

### T6.1 — Upload CV for application (two-step)
- [ ] Apply form → pick CV file
- [ ] Step 1: `POST /api/Media/upload?folder=cv` → URL stored in state
- [ ] Step 2: URL is sent as `studentCvUrl` in `POST /api/Applications/apply`

### T6.2 — Upload proposal file (two-step)
- [ ] Apply form → pick proposal file
- [ ] Step 1: `POST /api/Media/upload?folder=applications` → URL stored
- [ ] Step 2: URL sent as `proposalFileUrl` in `POST /api/Applications/apply`

### T6.3 — Submit application
- [ ] `POST /api/Applications/apply` called with `{ projectId, coverLetter, studentCvUrl, proposalFileUrl }`
- [ ] `ApplicationSuccess` state emitted
- [ ] Applied opportunities list updates to include new application

### T6.4 — Has applied check
- [ ] Open an opportunity the student already applied to
- [ ] `hasApplied` returns true → apply button disabled or "Applied" badge shown

### T6.5 — Previous CV reuse
- [ ] Apply form shows list of previously used CV URLs from past applications
- [ ] Selecting a previous CV skips the upload step

---

## T7 — Progress (B7): Active and Completed Projects

**Goal:** `ProgressNetApiDataSource` derives progress data from `GET /api/Applications/my-applications`.

### T7.1 — Active projects (ongoing)
- [ ] Progress screen shows projects with status: `Accepted`, `InProgress`, or `UnderReview`
- [ ] `Internship` model populated from nested `project` object in application DTO

### T7.2 — Completed projects
- [ ] Completed tab shows applications with `status == 'Completed'`
- [ ] Titles and dates display correctly

### T7.3 — Empty state
- [ ] New student with no applications → empty state UI shown (no crash)

---

## T8 — Save Opportunities (B8): Bookmark / Unbookmark

**Goal:** `SaveOppNetApiDataSource` uses SavedID (not ProjectID) for deletion.

### T8.1 — Get saved opportunities
- [ ] Saved tab loads from `GET /api/students/saved-projects`
- [ ] `_projectIdToSavedId` cache is populated

### T8.2 — Save an opportunity
- [ ] Tap bookmark on an unsaved opportunity
- [ ] `POST /api/students/saved-projects` with `{ projectId }` called
- [ ] Bookmark icon toggles to saved state

### T8.3 — Remove a saved opportunity
- [ ] Tap bookmark on a saved opportunity
- [ ] `DELETE /api/students/saved-projects/{savedId}` called with the **SavedID** (not ProjectID)
- [ ] Bookmark icon toggles to unsaved state

### T8.4 — SavedID cache miss recovery
- [ ] Simulate a cache miss (restart app without re-fetching)
- [ ] `removeSavedOpportunity` re-fetches the list to recover SavedID before deleting

---

## T9 — Chat REST (B9): Conversations + Messages

**Goal:** `ChatNetApiDataSource` using REST polling (5s interval) and correct int ID handling.

### T9.1 — Get conversations list
- [ ] Chat tab loads from `GET /api/Chat/conversations`
- [ ] `ChatModel.fromNetJson` maps: `conversationID`→`id` (as String), `otherUserName`, `lastMessage`, `lastMessageAt`

### T9.2 — Get messages for a conversation
- [ ] Open a chat → messages load from `GET /api/Chat/conversations/{id}/messages`
- [ ] `MessageModel.fromNetJson` maps: `messageID`, `senderID`, `messageText`→`content`, `sentAt`, `isRead`
- [ ] Int IDs are converted to String for UI compatibility

### T9.3 — Send a message
- [ ] Type and send a message
- [ ] `POST /api/Chat/send` called with `{ receiverId, content }`
- [ ] Message appears in the conversation immediately (optimistic or on next poll)

### T9.4 — Create new chat (auto-creates conversation)
- [ ] User search → pick a user → send first message
- [ ] `POST /api/Chat/send` with `receiverId` automatically finds/creates the Direct conversation
- [ ] New conversation appears in chat list

### T9.5 — 5-second polling
- [ ] Leave a chat open for 10+ seconds
- [ ] New messages sent from another client appear without manual refresh
- [ ] No memory leaks (stream controller disposed on close)

---

## T10 — Chat SignalR (B10): Real-Time Messages

**Goal:** `SignalRService` connects to `/hubs/notifications` and delivers `ReceiveMessage` events.

### T10.1 — SignalR connection established after login
- [ ] After login, `SignalRService.connect()` called
- [ ] Hub connects to `/hubs/notifications` with JWT via `accessTokenProvider`
- [ ] No connection error in console

### T10.2 — Real-time message delivery
- [ ] Open a chat conversation
- [ ] Send a message from a second account (web/Swagger)
- [ ] Message appears in the Flutter chat without waiting for the 5s poll

### T10.3 — Auto-reconnect on disconnect
- [ ] Briefly kill the backend, restart it
- [ ] SignalR service reconnects automatically
- [ ] No unhandled exception thrown

### T10.4 — Disconnect on sign-out
- [ ] Sign out
- [ ] `SignalRService.disconnect()` called
- [ ] No further SignalR events received after sign-out

---

## T11 — Notifications (B11): Fetch / Mark Read

**Goal:** `NotificationsCubit` is fully implemented (replaced stub).

### T11.1 — Fetch notifications
- [ ] Notifications screen loads from `GET /api/Notifications`
- [ ] `NotificationModel.fromJson` maps: `notificationID`, `title`, `message`, `notificationType`, `isRead`, `createdAt`
- [ ] `NotificationsLoaded` state emitted with list + `unreadCount`

### T11.2 — Unread count badge
- [ ] Badge/count on navigation shows unread count from `GET /api/Notifications/unread-count`
- [ ] Count updates after marking notifications as read

### T11.3 — Mark single notification as read
- [ ] Tap a notification
- [ ] `PUT /api/Notifications/{id}/read` called
- [ ] Notification `isRead` updates in UI, badge count decrements

### T11.4 — Mark all as read
- [ ] Tap "Mark all as read" (or equivalent)
- [ ] `PUT /api/Notifications/read-all` called
- [ ] All notifications show as read, badge count = 0

---

## T12 — Settings (B12): Get / Update / Password Change

**Goal:** `SettingNetApiDataSource` syncs server-side settings; password change uses `/api/Auth/change-password`.

### T12.1 — Load settings
- [ ] Settings screen loads from `GET /api/Settings`
- [ ] `SettingModel.fromJson` maps: `language`, `notificationsEnabled`, `profileVisibility`
- [ ] Local locale updates to match server-side `language`

### T12.2 — Save language preference
- [ ] Toggle language → `PUT /api/Settings` called with new `language`
- [ ] `AppPreferences` updated with new locale
- [ ] App locale changes immediately via `LocaleCubit`

### T12.3 — Save notification preference
- [ ] Toggle notifications → `PUT /api/Settings` called with `notificationsEnabled`
- [ ] Setting persists after app restart (reloaded from server)

### T12.4 — Change password
- [ ] Change password screen → old password + new password + confirm
- [ ] `POST /api/Auth/change-password` called with correct body
- [ ] `PasswordUpdateSuccess` state emitted
- [ ] User can log in with new password

---

## T13 — Reviews (B13): Submit / View

**Goal:** `ReviewsNetApiDataSource` and `ReviewsCubit` work end-to-end.

### T13.1 — Submit company review (student reviews company)
- [ ] Navigate to submit review after a completed application
- [ ] Fill in ratings and submit
- [ ] `POST /api/Reviews/company` called with review data
- [ ] `ReviewSubmitted` state emitted

### T13.2 — Submit student review (company reviews student)
- [ ] Log in as Company account
- [ ] Submit a review for a student
- [ ] `POST /api/Reviews/student` called
- [ ] `ReviewSubmitted` state emitted

### T13.3 — View student reviews
- [ ] Student profile reviews tab
- [ ] `GET /api/Reviews/student/{id}` called
- [ ] `ReviewsLoaded` state with list of reviews

### T13.4 — View company reviews
- [ ] Company profile reviews tab
- [ ] `GET /api/Reviews/company/{id}` called
- [ ] `ReviewsLoaded` state with list of reviews

---

## T14 — Certificates (B14): List / Verify

**Goal:** `CertificatesCubit` fetches and verifies certificates.

### T14.1 — Fetch my certificates
- [ ] Certificates screen (accessible from profile/progress)
- [ ] `GET /api/Certificates/my-certificates` called
- [ ] `CertificatesLoaded` state with list of `CertificateEntity` items
- [ ] `certificateID`, `certificateTitle`, `certificateNumber`, `certificateURL` populated

### T14.2 — Verify a certificate (public endpoint — no login needed)
- [ ] Verify certificate screen
- [ ] Enter a valid certificate unique ID
- [ ] `GET /api/Certificates/verify/{uniqueId}` called (no auth header required)
- [ ] `CertificateVerified` state emitted with certificate details

### T14.3 — Invalid certificate number
- [ ] Enter an invalid/nonexistent ID
- [ ] `CertificatesFailure` state with appropriate error message

---

## T15 — Payments / Wallet (B15): Payment History

**Goal:** `PaymentNetApiDataSource` replaces the local wallet mock.

### T15.1 — Fetch payment history
- [ ] Wallet screen loads from `GET /api/Payments/history`
- [ ] `PaymentTransactionModel.fromNetJson` maps: `description`/`projectName`, `paidAt`/`createdAt`, `paymentMethod`/`status`, `amount`
- [ ] Transaction list displays real backend data (not hardcoded mock)

### T15.2 — Empty wallet state
- [ ] New student with no payments → empty state UI shown (no crash)

### T15.3 — Payment detail (optional if screen exists)
- [ ] Tap a transaction → `GET /api/Payments/{id}` called
- [ ] Detail view shows full payment information

---

## T16 — Home + Announcements (B16): Home Feed

**Goal:** `HomeNetApiDataSource` powers the home screen with backend data.

### T16.1 — Fetch opportunities on home screen
- [ ] Home screen loads latest 10 opportunities from `GET /api/Projects`
- [ ] Projects display with correct title, company, type

### T16.2 — Fetch announcements
- [ ] Announcements section loads from `GET /api/Announcements` (AllowAnonymous)
- [ ] `AnnouncementModel.fromNetJson` maps: `id`, `title`, `description`, `imageUrl`, `link`, `createdAt`
- [ ] Works even before login (anonymous access)

### T16.3 — Fetch user profile on home
- [ ] Logged-in student's profile picture and name load from `GET /api/students/profile`
- [ ] Shown in home header/avatar

### T16.4 — Search
- [ ] Search bar → type a query
- [ ] `GET /api/Projects?search=<query>` called
- [ ] Results filter to matching projects

---

## T17 — Training Submission (B17): Submit / Check Status / Edit after Rejection

**Goal:** `TrainingSubmissionNetApiDataSource` with the critical ApplicationID repurposing.

### T17.1 — Resolve active ApplicationID
- [ ] Training submission screen loads
- [ ] `getFirstActiveTrainingId` called → `GET /api/Applications/my-applications`
- [ ] Filters to first application with status: `Accepted`, `InProgress`, or `UnderReview`
- [ ] Returns `ApplicationID` (int) stored as `trainingId`

### T17.2 — Upload a training document (two-step)
- [ ] Pick a file (certificate, report, etc.)
- [ ] Step 1: `POST /api/Media/upload?folder=training` → URL returned
- [ ] Step 2: URL included in `POST /api/TrainingSubmissions` body

### T17.3 — Submit training documents
- [ ] Fill at least one document URL
- [ ] `POST /api/TrainingSubmissions` called with `{ applicationId: <ApplicationID>, certificateUrl, reportUrl, ... }`
- [ ] Submission succeeds → status shows as `pending`

### T17.4 — Check existing submission
- [ ] Re-open training screen for a student who already submitted
- [ ] `GET /api/TrainingSubmissions/my` called
- [ ] Latest submission loaded and pre-fills the form

### T17.5 — Status normalization
- [ ] Backend returns `"Pending"` (PascalCase)
- [ ] Stored/displayed as `"pending"` (lowercase) — cubit logic works correctly
- [ ] Status labels: `pending`, `adminApproved`, `companyVerified`, `fullyCompleted`, `rejected`

### T17.6 — Edit mode triggered by Rejected status
- [ ] Simulate a rejected submission (set via admin in Swagger: `PUT /api/TrainingSubmissions/{id}/admin-review` with `IsApproved: false`)
- [ ] Re-open training screen
- [ ] `TrainingSubmissionModel.isRejected(status)` returns `true`
- [ ] Edit mode is activated (NOT by checking adminNotes)
- [ ] `RejectionReason` shown to student as primary message

### T17.7 — Resubmit after rejection
- [ ] In edit mode, update one or more document URLs
- [ ] `POST /api/TrainingSubmissions` called again (backend replaces on resubmit)
- [ ] Status resets to `pending`

---

## T18 — Company Profile (B18): Get Company Profile

**Goal:** `CompanyNetApiDataSource` fetches company profiles by ID.

### T18.1 — Get company profile by ID
- [ ] Navigate to a company profile screen (from opportunity or search)
- [ ] `GET /api/companies/{companyId}` called
- [ ] `CompanyProfileModel.fromNetJson` maps: `companyID`→`id`, `companyName`→`name`, `companyLogo`→`profilePicture`, `website`, `industry`, `description`, `userID`→`userId`

### T18.2 — Company name and logo display
- [ ] Company name and logo appear correctly
- [ ] If `companyLogo` is null, fallback placeholder shows (no crash)

---

## T19 — Cross-Cutting Concerns

### T19.1 — JWT expiry handling
- [ ] Wait 60+ minutes after login (or set a short expiry in backend `appsettings.json` for testing)
- [ ] Make an authenticated request
- [ ] `UnauthorizedException` thrown → user redirected to login screen (or token refresh flow if implemented)

### T19.2 — Network error handling
- [ ] Disable wifi/network
- [ ] Attempt any screen load
- [ ] `NetworkFailure` state shown with user-friendly message (no crash)
- [ ] Re-enable network → retry works

### T19.3 — ServiceResponse failure messages surface correctly
- [ ] Trigger a validation error from the backend (e.g. submit an application twice)
- [ ] Backend returns `{ success: false, message: "Already applied" }` or similar
- [ ] `ValidationException` thrown with that exact message shown in UI

### T19.4 — Role-based access
- [ ] Log in as Company account
- [ ] Student-only endpoints (profile, apply, saved projects) return 403 or redirect
- [ ] Company-only screens show correctly

---

## Test Completion Checklist

Fill in status for each section before running B19:

| Section | Feature | Status | Notes |
|---|---|---|---|
| T1 | Infrastructure: Dio + JWT + Envelope | ⬜ Pass / ⬜ Fail | |
| T2 | Auth: Login / Register / SignOut | ⬜ Pass / ⬜ Fail | |
| T3 | Master Data: Skills / Universities | ⬜ Pass / ⬜ Fail | |
| T4 | Student Profile: CRUD + Uploads | ⬜ Pass / ⬜ Fail | |
| T5 | Opportunities: List / Filter / Modules | ⬜ Pass / ⬜ Fail | |
| T6 | Apply Form: Upload + Submit | ⬜ Pass / ⬜ Fail | |
| T7 | Progress: Active + Completed | ⬜ Pass / ⬜ Fail | |
| T8 | Save Opportunities: Save / Remove | ⬜ Pass / ⬜ Fail | |
| T9 | Chat REST: Conversations + Messages | ⬜ Pass / ⬜ Fail | |
| T10 | Chat SignalR: Real-time Delivery | ⬜ Pass / ⬜ Fail | |
| T11 | Notifications: Fetch + Mark Read | ⬜ Pass / ⬜ Fail | |
| T12 | Settings: Get / Update / Password | ⬜ Pass / ⬜ Fail | |
| T13 | Reviews: Submit + View | ⬜ Pass / ⬜ Fail | |
| T14 | Certificates: List + Verify | ⬜ Pass / ⬜ Fail | |
| T15 | Payments: History | ⬜ Pass / ⬜ Fail | |
| T16 | Home + Announcements | ⬜ Pass / ⬜ Fail | |
| T17 | Training Submission: Full Flow | ⬜ Pass / ⬜ Fail | |
| T18 | Company Profile | ⬜ Pass / ⬜ Fail | |
| T19 | Cross-Cutting: JWT / Network / Roles | ⬜ Pass / ⬜ Fail | |

---

## Known Risks to Watch For

| Risk | Where | Mitigation |
|---|---|---|
| Chat IDs sent as int to String-typed UI fields | T9, T10 | Verify `.toString()` conversion in `ChatModel.fromNetJson` and `MessageModel.fromNetJson` |
| ApplicationID vs OpportunityID confusion | T17 | Confirm `trainingId` field holds ApplicationID after `getFirstActiveTrainingId` resolves |
| SavedID cache miss on DELETE | T8 | Test delete immediately after a fresh app start (cache empty) |
| SignalR JWT auth via `access_token` query param | T10 | Check `skipNegotiation: true` + `HttpConnectionType.WebSockets` in `signalr_service.dart` |
| `isRejected` vs `adminNotes` check for edit mode | T17 | Confirm `TrainingSubmissionCubit.checkSubmissionStatus` checks `status == 'rejected'`, not notes presence |
| Two-step upload — URL not propagated | T4, T6, T17 | Log the URL returned from `/api/Media` before the PUT/POST that uses it |
| `fromNetJson` vs `fromJson` called on wrong payload | All models | If data looks wrong/null, check which factory is being called |

---

## After All Tests Pass → Run B19

When all sections are marked **Pass**, proceed with the B19 cutover prompt from `prompts.md`:

1. Set `_defaultProvider = AuthProviderType.netApi` permanently
2. Remove Supabase registrations from `service_locator.dart`
3. Remove `Supabase.initialize()` from `main.dart`
4. Remove `supabase_flutter` and `cloud_firestore` from `pubspec.yaml`
5. Delete all Supabase datasource files
6. Run `flutter pub get` and `flutter analyze`

**Do not run B19 if any test section is marked Fail.**
