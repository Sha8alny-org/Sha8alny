# Sha8alny Migration Prompts

> **Strategy:** The mobile frontend is the feature specification. The backend must cover
> everything the mobile needs before the mobile connects to it. Fix backend gaps first (Section A),
> then wire the mobile to the complete backend (Section B).
>
> **Flutter repo (NEW):** `E:\LLM testing\sha8alny-front-end-final\graduation_project`
> **Backend repo:** `E:\LLM testing\Sha8alny`
> **Backend context:** `E:\LLM testing\Sha8alny\context.md`
> **Mobile context:** `E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md`

---

# SECTION A — Backend Gap Prompts
### Run these in order in the BACKEND repo (`E:\LLM testing\Sha8alny`)

---

## A1 — FCM Token (Push Notifications) ✅ DONE

**Status:** Implemented. `User.FcmToken` field added, `PUT /api/Auth/fcm-token` endpoint live.
EF migration `AddFcmTokenToUser` applied.

---

## A2 — App Maintenance Config Endpoint ✅ DONE

**Status:** Implemented. `AppConfig` entity, `GET/PUT /api/Maintenance/config` live.
EF migration `AddAppConfig` applied. Note: `GET /api/Maintenance/config` returns raw `AppConfigDto`
(not ServiceResponse-wrapped).

---

## A3 — Authenticated Password Change ✅ DONE

**Status:** Implemented. `POST /api/Auth/change-password` live, requires `[Authorize]`.

---

## A4 — Chat: Unified User Search + Conversation Creation ✅ DONE

**Status:** Implemented. `GET /api/users/search`, conversation creation confirmed working.

---

## A5 — SignalR: ChatHub for Real-Time Messages ✅ DONE

**Status:** Already fully implemented. No changes needed. Verified all 4 requirements:

1. ✅ `INotifier.SendMessageToUserAsync(userId, messageDto)` exists in `INotifier.cs`
2. ✅ `SignalRNotifier.SendMessageToUserAsync` calls `hub.Clients.User(userId).SendAsync("ReceiveMessage", message)` — non-blocking per Rule 10
3. ✅ `ChatService.SendMessageAsync` calls `_notifier.SendMessageToUserAsync(dto.ReceiverId, messageDto)` after DB save (line 91)
4. ✅ `MessageDto` maps: `ConversationId`, `SenderId`, `Content` (MessageText), `SentAt`

No new Hub class needed — `NotificationHub` at `/hubs/notifications` handles `ReceiveMessage` event.
No changes to `Program.cs` hub mapping required.

---

## A6 — Training Submission + Completion Workflow ✅ ALREADY EXISTS

**Status:** The backend already has a complete, production-ready `TrainingSubmission` implementation.
DO NOT run this prompt — running it would overwrite a richer existing system with a simpler spec.

**What already exists:**
- `TrainingSubmission` entity with `ApplicationID` FK (not opportunityId), full status enum
  (`Pending/AdminApproved/CompanyVerified/FullyCompleted/Rejected`), separate `AdminNotes` +
  `RejectionReason` fields, admin review timestamps, company verification timestamps
- `CompletedOpportunity` entity with full completion record (title, dates, duration, rating, feedback)
- `ITrainingSubmissionService` + `TrainingSubmissionService` — fully implemented
- `TrainingSubmissionsController` with all endpoints: POST submit, GET my, GET by id,
  PUT admin-review, PUT company-verify, GET pending-admin, GET pending-company
- EF configuration + migrations already applied

**Key difference from our earlier spec:**
The backend links submissions to `ApplicationID` (student's application PK),
NOT to `opportunityId` directly. The Flutter B17 migration must resolve `ApplicationID`
via `GET /api/Applications/my-applications` before submitting documents.

---

## A7 — Announcements Feature ✅ DONE

**Status:** Implemented. `Announcement` entity, `AnnouncementService`, `AnnouncementsController` all live.
DTOs in `Sh8lny.Shared/DTOs/Announcements/`. EF migration `AddAnnouncements` applied.

**What was built:**
- `Announcement` entity (Id, Title, Description, ImageUrl, Link, CreatedAt, UpdatedAt)
- `AnnouncementConfiguration` (Title required/200, Description required/2000, ImageUrl max 1000, Link max 1000)
- `DbSet<Announcement>` in `Sha8lnyDbContext`, `IGenericRepository<Announcement>` in `IUnitOfWork`/`UnitOfWork`
- `IAnnouncementService` + `AnnouncementService` (GetAll desc, Create, Update, Delete)
- `AnnouncementsController`: `GET /api/Announcements` [AllowAnonymous], `POST/PUT/DELETE` [Authorize(Admin)]
- `AnnouncementDto` + `CreateAnnouncementDto` DTOs
- Registered in `Program.cs` DI

**Original gap description:**

```
Read E:\LLM testing\Sha8alny\context.md in full before making any changes.

Add an Announcements feature.

Follow Rules 1, 5, 9, 11.

## ENTITY

1. E:\LLM testing\Sha8alny\Core\Sh8lny.Domain\Models\Announcement.cs
   Properties:
     int Id (PK)
     string Title
     string Description
     string? ImageUrl
     string? Link
     DateTime CreatedAt
     DateTime? UpdatedAt

## EF CONFIGURATION

2. Create AnnouncementConfiguration.cs under
   E:\LLM testing\Sha8alny\Infrastructure\Sh8lny.Persistence\Configurations\
   - Title: required, max 200
   - Description: required, max 2000
   - ImageUrl: max 1000
   - Link: max 1000

3. Add DbSet<Announcement> Announcements to Sha8lnyDbContext.
4. Add IGenericRepository<Announcement> to IUnitOfWork and UnitOfWork.

## SERVICES

5. Create IAnnouncementService.cs in Abstraction layer:
     Task<ServiceResponse<List<AnnouncementDto>>> GetAnnouncementsAsync();
     Task<ServiceResponse<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto);
     Task<ServiceResponse<AnnouncementDto>> UpdateAsync(int id, CreateAnnouncementDto dto);
     Task<ServiceResponse<bool>> DeleteAsync(int id);

6. Implement AnnouncementService.cs in Service layer.
   GetAnnouncementsAsync: return all, ordered by CreatedAt desc.

## DTOs

7. E:\LLM testing\Sha8alny\Sh8lny.Shared\DTOs\Announcements\AnnouncementDto.cs
   int Id, string Title, string Description, string? ImageUrl, string? Link, DateTime CreatedAt

8. E:\LLM testing\Sha8alny\Sh8lny.Shared\DTOs\Announcements\CreateAnnouncementDto.cs
   string Title, string Description, string? ImageUrl, string? Link

## CONTROLLER

9. Create AnnouncementsController.cs:
     GET    /api/Announcements           [AllowAnonymous]    — get all (mobile home screen)
     POST   /api/Announcements           [Authorize(Admin)]  — create
     PUT    /api/Announcements/{id}      [Authorize(Admin)]  — update
     DELETE /api/Announcements/{id}      [Authorize(Admin)]  — delete

## MIGRATION

10. Register IAnnouncementService in Program.cs DI.
11. Output the dotnet ef migration command per Rule 4.
    Migration name: AddAnnouncements
```

---

# SECTION B — Frontend Migration Prompts
### Run these in order in the FLUTTER repo (`E:\LLM testing\sha8alny-front-end-final\graduation_project`)
### Only start Section B after all required Section A prompts are complete and the backend is deployed

---

## B1 — Infrastructure: Dio Foundation

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Implement the core Dio infrastructure that all feature migrations depend on.
Do NOT touch any feature files. Only modify core-layer files.

1. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\constants\endpoint_constants.dart
   Replace placeholder baseUrl with EnvConfig.netApiBaseUrl.
   Add route string constants for all backend endpoints listed in context.md §4.

2. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\services\env_config.dart
   Add: static String get netApiBaseUrl => dotenv.env['NET_API_BASE_URL'] ?? '';

3. Add NET_API_BASE_URL=https://REPLACE_WITH_CLOUD_RUN_URL to the .env file

4. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\constants\cache_keys.dart
   Add: static const String authToken = 'auth_token';

5. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\network\interceptors.dart
   In onRequest: read AppPreferences().getData(CacheKeys.authToken) and inject
   Authorization: Bearer <token> header if token is not null. Keep existing logging.

6. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\network\api_consumer.dart
   Add abstract: Future<dynamic> postForm(String path, {required dynamic formData});

7. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\network\service_response.dart
   ServiceResponse<T> class with fromJson(Map, T Function(dynamic)) factory.
   Fields: bool success, String message, T? data, List<String> errors.

8. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\network\dio_consumer.dart
   Update get/post/put/delete to unwrap ServiceResponse envelope:
   - If response has "success" key AND success == true: return data field
   - If response has "success" key AND success == false: throw AppException with message field
   - If response has NO "success" key (e.g. GET /api/Maintenance/config): return raw data as-is
   Implement postForm for multipart/form-data.

9. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\services\service_locator.dart
   Register DioConsumer as LazySingleton implementing ApiConsumer.
   Do NOT change AuthConfig.currentProvider yet.

Do not migrate any feature. Do not remove Supabase.
```

---

## B2 — Auth Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Implement the Auth migration from Supabase to the .NET backend.
The backend Auth endpoints are documented in context.md §4.1.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\auth\data\datasources\net_api\auth_net_api_datasource.dart
   Implements AuthRemoteDataSource using ApiConsumer (Dio).
   - signIn: POST /api/Auth/login → on success, store JWT in AppPreferences(CacheKeys.authToken)
             and serialize UserModel to AppPreferences(CacheKeys.userData)
   - signUp: POST /api/Auth/register with { email, password, userType: 'Student', fullName: name }
   - signOut: clear authToken and userData from AppPreferences
   - resetPassword: POST /api/Auth/forgot-password
   - updatePassword: POST /api/Auth/reset-password (uses the token from the email link)
   - updateFcmToken: PUT /api/Auth/fcm-token (the endpoint added in backend prompt A1)

2. Update UserModel.fromNetJson in:
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\auth\data\models\user_model.dart
   Map: userId→id (int), email, firstName+lastName combined→name, role→role
   Token is stored in shared_preferences, NOT on the model.
   Keep existing fromJson (Supabase path) unchanged.

3. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\auth\domain\entities\user_entity.dart
   Add: String? role
   Keep all existing fields.

4. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\config\auth_config.dart
   Add AuthProviderType.netApi to the enum.
   Add a comment: // TODO: change _defaultProvider to netApi after end-to-end testing

5. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\services\service_locator.dart
   Add netApi case to _getAuthDataSource factory.
   Keep currentProvider = supabase for now.

Do not remove Supabase auth datasource. Do not change any screens or cubits.
```

---

## B3 — Master Data

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Create the Master Data feature (skills, universities, departments).
Backend endpoints: context.md §4.13 (GET /api/MasterData/skills, /universities, /departments).

Create the full Clean Architecture stack under:
E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\master_data\

1. Domain:
   SkillEntity(id, name, category), UniversityEntity(id, name), DepartmentEntity(id, name)
   MasterDataRepository (abstract)
   GetSkillsUseCase, GetUniversitiesUseCase, GetDepartmentsUseCase

2. Data:
   MasterDataRemoteDataSource (abstract) + MasterDataNetApiDataSource (Dio impl)
   SkillModel, UniversityModel, DepartmentModel with fromJson factories

3. Presentation:
   MasterDataCubit
   States: MasterDataInitial, MasterDataLoading, MasterDataLoaded(skills, universities, departments), MasterDataFailure

4. Service locator module:
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\master_data\service\master_data_service_locator.dart
   Call setupMasterDataModule() from the main setup() in service_locator.dart

Do not modify any existing screens. This data will be consumed by profile and opportunities features.
```

---

## B4 — Student Profile Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the student profile feature from Supabase to .NET.
Backend endpoints: context.md §4.2.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\profile\data\datasources\net_api\profile_net_api_datasource.dart
   - getStudentProfile: GET /api/students/profile
   - createStudentProfile: POST /api/students/profile
   - updateStudentProfile: PUT /api/students/profile
   - getStudentSkills: extract from profile DTO's StudentSkills array (not a separate call)
   - addStudentSkill: PUT /api/students/profile with updated skills list
   - removeStudentSkill: PUT /api/students/profile with skill removed
   - uploadResume: two-step — ApiConsumer.postForm to POST /api/Media/upload?folder=cv
                  → take returned URL → PUT /api/students/profile with CvFileUrl
   - uploadProfilePicture: two-step — POST /api/Media/upload?folder=profile
                           → PUT /api/students/profile with ProfilePictureUrl
   - addOrUpdateGithubUrl: PUT /api/students/profile with GitHubProfile field

2. Add StudentProfileModel.fromNetJson factory — map PascalCase backend fields to model.
   Keep existing fromJson unchanged.

3. Register in service_locator.dart guarded by netApi provider.

Do not change ProfileCubit or any screens.
Do not remove the Supabase datasource.
```

---

## B5 — Opportunities / Projects Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the opportunities feature from Supabase to .NET.
Backend endpoints: context.md §4.4.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\opportunities\data\datasources\net_api\opportunities_net_api_datasource.dart
   - fetchOpportunities: GET /api/Projects
   - fetchOpportunitiesByType(isInternship): GET /api/Projects?projectType=Internship or Training
   - fetchOpportunityById(id): GET /api/Projects/{id}
   - fetchCompanyOpportunities(companyId): GET /api/Projects?companyId={companyId}
     (used by the new CompanyOpportunitiesScreen — confirm query param with backend)
   - fetchAppliedOpportunities(studentId): GET /api/Applications/my-applications
   - fetchApplications(opportunityId): GET /api/Applications/project/{projectId}
   - getModules(opportunityId): GET /api/Execution/project/{id}/modules

2. Add OpportunityModel.fromNetJson factory:
   Map: ProjectID→id, ProjectName→title, ProjectType→type, Deadline→deadline,
        Status→status, Company.CompanyName→company,
        ProjectRequiredSkills[].SkillName→skills list
   Keep existing fromJson unchanged.

3. Register in service_locator.dart guarded by netApi provider.

Do not change any cubits or screens.
Do not remove the Supabase datasource.
```

---

## B6 — Apply Form / File Uploads Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the apply form feature from Supabase to .NET.
Backend endpoints: context.md §4.5 and §4.12.
CRITICAL: Files go through /api/Media only. Never upload direct. See context.md Rule 3.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\apply_form\data\datasources\net_api\apply_form_net_api_datasource.dart
   - uploadFile(file, folder): ApiConsumer.postForm → POST /api/Media/upload?folder={folder}
                                returns URL string
   - submitApplication(opportunityId, cvUrl, proposalUrl, coverLetter, bidAmount):
     POST /api/Applications/apply with URL strings (not raw files)
   - hasApplied(opportunityId): GET /api/Applications/my-applications → check if any matches
   - getPreviousApplicationCvs: GET /api/Applications/my-applications → extract StudentCvUrl values
   - getStudentProfileId: GET /api/students/profile → return StudentID

2. Register in service_locator.dart guarded by netApi provider.

Do not change ApplicationCubit or screens.
Do not remove Supabase datasource.
```

---

## B7 — Progress / Execution Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the progress feature from Supabase to .NET.
Backend endpoints: context.md §4.5 and §4.6.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\progress\data\datasources\net_api\progress_net_api_datasource.dart
   - fetchMyProjects: GET /api/Applications/my-applications
                      → filter to active statuses (Accepted, InProgress, UnderReview)
                      → map ApplicationResponseDto to Internship model
   - fetchCompletedProjects: GET /api/Applications/my-applications
                             → filter to Completed status
   - fetchProjectModuleProgress(applicationId): GET /api/Execution/application/{id}/progress

2. Register in service_locator.dart guarded by netApi provider.

Do not change ProgressCubit or HomeCubit.
Do not remove Supabase datasource.
```

---

## B8 — Save Opportunities Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the save/bookmark feature from Supabase to .NET.
Backend endpoints: context.md §4.2 (GET/POST /api/students/saved-projects,
DELETE /api/students/saved-projects/{id}).

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\save_opportunities\data\datasources\net_api\save_opp_net_api_datasource.dart
   - getSavedOpportunities: GET /api/students/saved-projects → map to List<OpportunityModel>
   - isOpportunitySaved(opportunityId): GET /api/students/saved-projects → check if id in list
   - saveOpportunity(opportunityId): POST /api/students/saved-projects with { projectId }
   - removeSavedOpportunity(opportunityId): DELETE /api/students/saved-projects/{id}
     Note: the {id} here is the SavedID (PK of SavedOpportunity), NOT the ProjectID.
     Get the SavedID from the saved projects list response, then use it for deletion.

2. Register in service_locator.dart guarded by netApi provider.

Do not change SaveOppCubit or screens.
Do not remove Supabase datasource.
```

---

## B9 — Chat Migration: Stage 1 REST

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the chat feature from Supabase Realtime to .NET REST.
Backend endpoints: context.md §4.7, plus the new endpoints from backend prompt A4.

IMPORTANT: All chat entity IDs change from String (Supabase UUID) to int.
Update ChatEntity.id and MessageEntity.id to int throughout the domain layer first.

1. Update domain entities:
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\chat\domain\entities\chat_entity.dart
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\chat\domain\entities\message_entity.dart
   Change id type: String → int

2. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\chat\data\datasources\net_api\chat_net_api_datasource.dart
   - getChats: GET /api/Chat/conversations → wrap result in a Stream using BehaviorSubject
               with 5-second periodic refresh (temporary until SignalR in B10)
   - getMessages(chatId): GET /api/Chat/conversations/{id}/messages → BehaviorSubject Stream
   - sendMessage(chatId, content, senderId): POST /api/Chat/send
   - createChat(currentUserId, otherUserId): POST /api/Chat/conversations/direct
     (the endpoint from backend prompt A4)
   - deleteChat: no-op stub (no delete endpoint on backend yet) with TODO comment
   - markMessagesAsRead: no-op stub (no read endpoint on chat yet) with TODO comment
   - Keep FCM push via SendNotificationServices unchanged (uses getOtherUserToken)

3. Update ChatModel and MessageModel with fromNetJson factories (int IDs, camelCase).

4. Register in service_locator.dart guarded by netApi provider.

Do not change ChatCubit or UserSearchCubit.
Do not remove Supabase datasource.
```

---

## B10 — Chat Migration: Stage 2 SignalR

> **Prerequisite:** Backend prompt A5 must be complete and deployed first.

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Replace the 5-second polling from Chat Stage 1 (B9) with a real SignalR connection.
The backend hub is at /hubs/notifications. JWT is passed via access_token query parameter.

1. Add to pubspec.yaml: signalr_netcore (latest)

2. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\services\signalr_service.dart
   - Manages one HubConnection to /hubs/notifications
   - accessTokenProvider: reads AppPreferences(CacheKeys.authToken)
   - Exposes: Stream<MessagePayload> messageStream (from ReceiveMessage event)
              Stream<NotificationPayload> notificationStream (from ReceiveNotification event)
   - Auto-reconnect on disconnect
   - connect() / disconnect() lifecycle methods

3. Update the chat net_api datasource from B9:
   - getChats: keep periodic REST polling (conversations list doesn't need realtime)
   - getMessages(chatId): replace BehaviorSubject polling with SignalRService.messageStream
                          filtered by conversationId

4. Register SignalRService as LazySingleton in service_locator.dart.
   Call signalrService.connect() after successful login in AuthCubit.
   Call signalrService.disconnect() on sign-out.
```

---

## B11 — Notifications Feature

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Implement the Notifications feature fully.
Backend endpoints: context.md §4.8. Real-time: SignalRService.notificationStream from B10
(if B10 is done); else fall back to polling every 30s with a TODO comment.

Full Clean Architecture stack:

1. Domain:
   NotificationEntity(id, title, message, type, isRead, createdAt, relatedProjectId?, relatedApplicationId?, actionUrl?)
   NotificationsRepository (abstract)
   GetNotificationsUseCase, GetUnreadCountUseCase, MarkReadUseCase, MarkAllReadUseCase

2. Data:
   Replace the stub/partial implementation in:
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\notifications\data\datasources\
   Create NotificationsNetApiDataSource (Dio impl)
   NotificationModel with fromJson factory

3. Presentation:
   Replace stub NotificationsCubit with full implementation.
   States: NotificationsInitial, NotificationsLoading,
           NotificationsLoaded(notifications, unreadCount), NotificationsFailure

4. Register in service_locator.dart.

5. Update the badge/count display in the main navigation (if any unread count widget exists).
```

---

## B12 — Settings Migration + Password Change

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Implement Settings backend sync and wire up the ChangePassword screen to the backend.
Backend endpoints: context.md §4.15, and POST /api/Auth/change-password (from A3).

1. Implement SettingRemoteDataSource (replace the stub):
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\setting\data\datasources\setting_remote_datasource.dart
   - getSettings: GET /api/Settings → returns UserSettingsDto
   - updateSettings(language, notificationsEnabled, profileVisibility): PUT /api/Settings

2. Create SettingModel with fromJson + toJson.

3. Update SettingCubit:
   - loadSettings: fetch and apply Language to LocaleCubit
   - saveLanguage: PUT /api/Settings + update local AppPreferences(AppConstants.localeKey)
   - saveNotificationPreference: PUT /api/Settings
   - Keep existing signOut

4. Wire ChangePassword screen to backend:
   File: E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\auth\presentation\screens\reset_password\change_password.dart
   Currently calls Supabase updateUser. Replace with:
   POST /api/Auth/change-password using AuthRepository or a direct Dio call from the screen's cubit.

5. After successful login in AuthCubit, call SettingCubit.loadSettings() to sync
   server-side language to the device locale.

6. Register datasource in service_locator.dart.
```

---

## B13 — Reviews Feature

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Build the Reviews feature (no screens currently exist in the mobile app).
Backend endpoints: context.md §4.9.

Full Clean Architecture stack under
E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\reviews\:

1. Domain:
   StudentReviewEntity + CompanyReviewEntity (fields from context.md §3)
   ReviewsRepository (abstract)
   SubmitStudentReviewUseCase, SubmitCompanyReviewUseCase,
   GetStudentReviewsUseCase, GetCompanyReviewsUseCase

2. Data:
   ReviewsNetApiDataSource (Dio impl)
   StudentReviewModel + CompanyReviewModel with fromJson/toJson factories

3. Presentation:
   ReviewsCubit
   States: ReviewsInitial, ReviewsLoading, ReviewSubmitted, ReviewsLoaded(reviews), ReviewsFailure
   Screens:
   - SubmitReviewScreen — shown after an application reaches Completed status
   - ViewReviewsScreen — shown from profile to display received reviews
   (Match the visual style of existing screens in the repo)

4. Routes:
   Add to E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\routing\routes.dart
   and app_router.dart

5. Register in service_locator.dart.
```

---

## B14 — Certificates Feature

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Build the Certificates feature (no screens currently exist).
Backend endpoints: context.md §4.10.

Full Clean Architecture stack under
E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\certificates\:

1. Domain:
   CertificateEntity(id, title, description, certificateNumber, certificateUrl, issuedAt, expiresAt?, projectId, companyId)
   CertificatesRepository (abstract)
   GetMyCertificatesUseCase, VerifyCertificateUseCase

2. Data:
   CertificatesNetApiDataSource (Dio impl)
   CertificateModel with fromJson factory

3. Presentation:
   CertificatesCubit
   States: CertificatesInitial, CertificatesLoading, CertificatesLoaded(certificates), CertificateVerified(certificate), CertificatesFailure
   Screens:
   - CertificatesScreen — list of earned certificates (accessible from profile/progress)
   - CertificateDetailScreen — full certificate with share button
   - VerifyCertificateScreen — enter a certificate ID to verify (public, no login needed)
   (Match visual style of existing screens)

4. Routes: add to routes.dart and app_router.dart

5. Register in service_locator.dart.
```

---

## B15 — Payments / Wallet Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Replace the local wallet mock with real payment data from the backend.
Current state: CardModel is local-only in shared_preferences; TransactionModel is local-only.
Backend endpoints: context.md §4.11.

Note: POST /api/Payments/pay is authorized for Company role only. The mobile Student
wallet will use GET /api/Payments/history to show incoming payments.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\profile\data\datasources\net_api\payment_net_api_datasource.dart
   - getPaymentHistory: GET /api/Payments/history → returns List<PaymentResponseDto>
   - getPaymentById(id): GET /api/Payments/{id}

2. Create PaymentModel with fromJson.

3. Update the wallet screens under:
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\profile\presentation\pages\wallet\
   - Replace local CardModel + shared_preferences storage with real payment history from the API
   - Show actual transaction list from GET /api/Payments/history
   - Remove the fake "add card" flow (no backend for card storage)
   - Keep the QR scanner widget (may be used later for certificate verification)

4. Wallet Cubit: replace local data with API calls.

5. Register datasource in service_locator.dart.
```

---

## B16 — Home: Announcements Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Prerequisite: Backend prompt A7 (Announcements) must be complete.

Migrate the announcements fetch in the Home feature from Supabase to the .NET backend.
Backend endpoint: GET /api/Announcements (AllowAnonymous).

1. Add fetchAnnouncements to the home net_api datasource
   (or create if one doesn't exist yet):
   E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\home\data\datasources\net_api\home_net_api_datasource.dart
   - fetchOpportunities: GET /api/Projects (latest 10, desc)
   - fetchAnnouncements: GET /api/Announcements
   - fetchUserProfile(userId): GET /api/students/profile (after B4, this just calls the profile endpoint)

2. Add AnnouncementModel.fromNetJson factory:
   Map: Id→id, Title→title, Description→description, ImageUrl→imageUrl, Link→link, CreatedAt→createdAt
   Keep existing fromJson (Supabase) unchanged.

3. Register home net_api datasource in service_locator.dart guarded by netApi provider.

Do not change HomeCubit, HomeState, or screens.
Do not remove Supabase datasource.
```

---

## B17 — Training Submission Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the training_submission feature from Supabase + Storage to the .NET backend.
The backend already has a complete TrainingSubmission implementation — read context-frontend.md
Appendix G carefully before writing any code. The backend structure differs from the Flutter model.

CRITICAL FACTS before you start:
1. The backend links submissions to ApplicationID (student's application PK), NOT to opportunityId.
2. The Flutter cubit currently passes trainingId (= opportunityId). After this migration it must
   pass applicationId instead.
3. File uploads go through POST /api/Media/upload?folder=training first, then URL is sent to
   /api/TrainingSubmissions. Never upload directly to storage.
4. Edit mode (TrainingSubmissionEditMode) is triggered when Status == "Rejected", NOT when
   adminNotes is non-empty (the Supabase logic). After migration check for "Rejected" status.
5. The backend's response dto has both AdminNotes and RejectionReason. Display RejectionReason
   as the primary message to the student (it was set by admin when rejecting).

BACKEND ENDPOINTS (all exist — do not create these):
  POST   /api/TrainingSubmissions         — submit docs (body: SubmitTrainingDocumentsDto)
  GET    /api/TrainingSubmissions/my      — get student's own submissions list
  GET    /api/TrainingSubmissions/{id}    — get single submission

SubmitTrainingDocumentsDto (backend):
  int ApplicationID (required)
  string? CertificateUrl
  string? ReportUrl
  string? PresentationUrl
  string? CompanyEvaluationUrl
  string? StudentSurveyUrl
  int? TrainingDays

TrainingSubmissionResponseDto (backend):
  int TrainingSubmissionID, int ApplicationID, int StudentID,
  string? CertificateUrl, string? ReportUrl, string? PresentationUrl,
  string? CompanyEvaluationUrl, string? StudentSurveyUrl,
  string Status  ("Pending"/"AdminApproved"/"CompanyVerified"/"FullyCompleted"/"Rejected"),
  bool IsAdminApproved, bool IsCompanyVerified,
  string? AdminNotes, string? RejectionReason,
  int? TrainingDays, DateTime SubmittedAt, DateTime UpdatedAt

IMPLEMENTATION STEPS:

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\training_submission\data\datasources\net_api\training_submission_net_api_datasource.dart
   Implements TrainingSubmissionRemoteDataSource using ApiConsumer (Dio).

   uploadFile(File file, String path):
     POST /api/Media/upload?folder=training
     Send file as multipart form-data
     Return the URL string from response

   submitRequirements(TrainingSubmissionModel submission):
     POST /api/TrainingSubmissions
     Body: { applicationId: submission.trainingId, certificateUrl, reportUrl,
             studentSurveyUrl, presentationUrl, companyEvaluationUrl }
     Note: submission.trainingId is repurposed to hold applicationId after B17

   updateRequirements(TrainingSubmissionModel submission):
     POST /api/TrainingSubmissions  (create a new submission — backend replaces on resubmit)
     Same body as above, with updated URLs (unchanged files keep existing URLs)

   getSubmission(int studentProfileId, int? opportunityId):
     GET /api/TrainingSubmissions/my
     Returns the latest submission from the list (null if empty)
     If opportunityId is provided, filter by matching ApplicationID → use after resolving applicationId

   getStudentProfileId(int userId):
     GET /api/students/profile → extract StudentID integer
     (after B4 this can call the same profile datasource)

   getFirstActiveTrainingId(int studentProfileId):
     GET /api/Applications/my-applications
     → find first application with active status (Accepted, InProgress, UnderReview)
     → return its ApplicationID (int) as the "trainingId"
     THIS IS THE KEY CHANGE: returns ApplicationID not opportunityId

   checkSubmissionExists(int studentProfileId, int? opportunityId):
     Derived — call getSubmission and return submission != null
     (Not a separate API call)

   insertCompletionRequest(int studentProfileId, int opportunityId):
     NO-OP stub — the backend automatically handles CompletedOpportunity creation
     when the submission is approved. Add a comment explaining this.

2. Add TrainingSubmissionModel.fromNetJson factory:
   File: E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\training_submission\data\models\training_submission_model.dart

   factory TrainingSubmissionModel.fromNetJson(Map<String, dynamic> json) {
     return TrainingSubmissionModel(
       id: json['trainingSubmissionID'] as int?,
       studentId: json['studentID'] as int,
       trainingId: json['applicationID'] as int?,   // applicationId stored in trainingId field
       certificateUrl: json['certificateUrl'] as String? ?? '',
       reportUrl: json['reportUrl'] as String? ?? '',
       studentSurveyUrl: json['studentSurveyUrl'] as String? ?? '',
       presentationUrl: json['presentationUrl'] as String? ?? '',
       companyEvaluationUrl: json['companyEvaluationUrl'] as String? ?? '',
       status: (json['status'] as String?)?.toLowerCase() ?? 'pending',
       // Combine RejectionReason and AdminNotes — RejectionReason takes priority for student display
       adminNotes: json['rejectionReason'] as String? ?? json['adminNotes'] as String?,
       createdAt: DateTime.parse(json['submittedAt'] as String),
     );
   }

   Status normalization: backend returns "Pending", "AdminApproved", "Rejected", etc. (PascalCase).
   Normalize to lowercase for compatibility with existing cubit logic, BUT also add a helper:
   static bool isRejected(String status) => status.toLowerCase() == 'rejected';

3. Update TrainingSubmissionCubit.checkSubmissionStatus to detect edit mode correctly:
   File: E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\training_submission\presentation\cubit\training_submission_cubit.dart

   In checkSubmissionStatus, change the edit mode trigger from:
     if (submission.adminNotes != null && submission.adminNotes!.trim().isNotEmpty)
   to:
     if (submission.status.toLowerCase() == 'rejected')

   This matches the backend reality: edit mode = rejection, not just having notes.

4. Register the net_api datasource in service_locator.dart guarded by netApi provider.
   Keep the Supabase datasource registered under supabase provider — do not remove it.

Do not change TrainingSubmissionCubit beyond the edit mode trigger fix in step 3.
Do not change any screens.
Do not remove Supabase datasource.
```

---

## B18 — Company Profile Migration

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

Migrate the company feature from Supabase to .NET.
First, check context.md §4 to find the exact endpoint for fetching a company profile by ID.
The expected endpoint is GET /api/companies/{id}/profile or similar.

1. Create E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\features\company\data\datasources\net_api\company_net_api_datasource.dart
   Implements CompanyRemoteDataSource using ApiConsumer (Dio).
   - getCompanyProfile(companyId): GET /api/companies/{companyId}/profile
     Returns: CompanyProfileModel with id, name, profilePicture, website, industry, description, userId

2. Add CompanyProfileModel.fromNetJson factory:
   Map backend DTO fields (PascalCase) to model.
   The Supabase impl joins user table to get name/picture — in the .NET response these should
   be included directly in the company profile DTO. If not, ask the backend to add them.
   Keep existing fromJson (Supabase) unchanged.

3. Register in service_locator.dart guarded by netApi provider.

Do not change CompanyCubit or CompanyOpportunitiesScreen.
Do not remove Supabase datasource.
```

---

## B19 — Cutover: Flip Default Provider & Remove Supabase

> **Only run this after ALL previous prompts are complete and tested end-to-end
> against the live .NET backend. Test the full flow: login → home → opportunity → apply
> → profile → training submission → chat before running this prompt.**

```
Read both of these files in full before making any changes:
  E:\LLM testing\sha8alny-front-end-final\graduation_project\context-frontend.md
  E:\LLM testing\Sha8alny\context.md

The migration is complete. Remove Supabase and flip to the .NET backend as default.

1. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\config\auth_config.dart
   Change _defaultProvider = AuthProviderType.netApi

2. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\services\service_locator.dart
   Remove all Supabase and Firebase Firestore registrations.
   Keep: FirebaseAuth (still needed for FCM), firebase_messaging, firebase_core.

3. E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\main.dart
   Remove: Supabase.initialize(), supabase auth state listener
   Keep: Firebase.initializeApp(), FCM setup, local notifications

4. pubspec.yaml: remove supabase_flutter, cloud_firestore
   Keep: firebase_core, firebase_auth, firebase_messaging

5. Delete all Supabase datasource implementation files across ALL features:
   - features/auth/data/datasources/supabase/
   - features/auth/data/datasources/firebase/
   - features/home/data/datasources/ (supabase impl)
   - features/opportunities/data/datasource/ (supabase impl)
   - features/apply_form/data/datasources/ (supabase impl)
   - features/profile/data/datasources/ (supabase impl)
   - features/progress/data/datasources/ (supabase impl)
   - features/training_submission/data/datasources/ (supabase impl)
   - features/chat/data/datasources/ (supabase impl)
   - features/notifications/data/datasources/ (supabase impl)
   - features/save_opportunities/data/dataSource/ (supabase impl)
   - features/company/data/datasources/ (supabase impl)
   - features/maintenance/ (supabase datasource)

6. Delete E:\LLM testing\sha8alny-front-end-final\graduation_project\lib\core\constants\database\
   (Supabase remote schema constants — no longer needed after cutover)

7. Run flutter pub get and flutter analyze. Fix all import errors.

8. Test every route in app_router.dart end-to-end before releasing.
```

---

## Quick Reference: Gap → Prompt Mapping

| Mobile feature needing this | Backend gap | Backend prompt | Status |
|---|---|---|---|
| Push notifications after auth | No FCM token field/endpoint | A1 | ✅ Done |
| Splash version check | No maintenance config endpoint | A2 | ✅ Done |
| Change password screen | No authenticated password change | A3 | ✅ Done |
| Start new chat, user search | No cross-user search | A4 | ✅ Done |
| Real-time chat messages | No SignalR message push | A5 | Pending |
| Training submission (5 docs) | TrainingSubmission already exists (richer) | A6 | ✅ Already existed |
| Home announcements feed | No Announcements endpoint | A7 | ✅ Done |
