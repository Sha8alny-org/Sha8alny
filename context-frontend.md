# Sha8alny — Flutter Mobile Architectural Context Document

> **Purpose:** This document is the mobile-side source of truth for the Sha8alny platform, the
> exact parallel of `context-backend.md`. Every AI agent working on the Flutter codebase **MUST**
> read this file before making changes. Cross-reference with the backend context at
> `E:\LLM testing\Sha8alny\context-backend.md`.
>
> **Mobile repo root:** `E:\LLM testing\Sha8alny-front-end`
> **Backend repo root:** `E:\LLM testing\Sha8alny`
> **Flutter SDK (pinned):** 3.32.6
> **Last audited:** June 2026

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Matrix & Layer Dependency Flow](#2-technology-matrix--layer-dependency-flow)
3. [Network, Serialization & Error-Handling Lifecycle](#3-network-serialization--error-handling-lifecycle)
4. [Local Persistence & Caching](#4-local-persistence--caching)
5. [Authentication Architecture (Duality)](#5-authentication-architecture-duality)
6. [Feature-by-Feature State Mapping](#6-feature-by-feature-state-mapping)
7. [Cross-Platform Feature Parity Matrix](#7-cross-platform-feature-parity-matrix)
8. [Integration Debt Register](#8-integration-debt-register)
9. [Migration Roadmap (.NET Cutover)](#9-migration-roadmap-net-cutover)
10. [Appendices](#10-appendices)

---

## 1. Project Overview

| Property            | Value                                                                 |
|---------------------|-----------------------------------------------------------------------|
| **App Name**        | Sha8alny (شغلني)                                                      |
| **Type**            | Flutter mobile app (Freelancing & Field Training Platform)           |
| **Current Backend** | **Supabase** (default) + Firebase Auth (alternate) — NOT the .NET API |
| **Target Backend**  | ASP.NET Core 9 Web API at `E:\LLM testing\Sha8alny`                 |
| **Flutter SDK**     | 3.32.6                                                               |
| **Dart SDK Range**  | `>=3.0.0 <4.0.0`                                                     |
| **pubspec.yaml**    | `E:\LLM testing\Sha8alny-front-end\pubspec.yaml`                    |

### Current Backing Reality

The mobile app is **not connected to the .NET backend**. All live feature data is fetched directly
from a **Supabase project** via the Supabase Flutter SDK. A fully-structured Dio HTTP client exists
in the codebase (`E:\LLM testing\Sha8alny-front-end\lib\core\network\dio_consumer.dart`) but is
**completely dormant** — its `baseUrl` is a placeholder and no feature uses it.

Firebase is wired for:
- Alternative authentication (login/signup via Firebase Auth)
- Push notifications (FCM) — active regardless of Supabase/Firebase auth choice

The .NET backend's `context-backend.md` documents 16 fully-working API controller groups
plus a SignalR hub. **Zero of these are currently consumed by the mobile app.**

---

## 2. Technology Matrix & Layer Dependency Flow

### 2.1 Dependency Catalogue

| Package | Version (pubspec) | Role | Migration Relevance |
|---|---|---|---|
| `flutter_bloc` | latest | Cubit-based state management (all features) | Keep — wire to .NET responses |
| `dio` | latest | HTTP client (built, dormant) | **Activate** — point at .NET base URL |
| `shared_preferences` | latest | Local key-value persistence (only local store) | Keep — store JWT token here |
| `easy_localization` | ^3.0.8 | i18n (en/ar) — saved in `shared_preferences` | Sync with backend `UserSettings.Language` |
| `equatable` | latest | Value equality in states/entities | Keep |
| `get_it` | latest | Service locator / DI container | Keep |
| `dartz` | latest | `Either<Failure, T>` functional error model | Keep — works with Dio errors |
| `freezed_annotation` + `freezed` | latest | Code-gen for immutable models | Keep |
| `json_annotation` + `json_serializable` | latest | JSON serialization code-gen | Keep — map .NET DTOs |
| `supabase_flutter` | ^2.10.1 | **Active data backend (to be replaced)** | Remove per feature as each is migrated |
| `firebase_core` | ^4.1.0 | Firebase initialization | Keep for FCM push |
| `firebase_auth` | ^6.0.2 | Firebase authentication (alternate) | Decommission auth path; keep FCM |
| `cloud_firestore` | ^6.0.1 | Firestore DB (alternate) | Decommission as features migrate |
| `firebase_messaging` | ^16.0.4 | FCM push notifications | Keep — no mobile push in .NET yet |
| `flutter_local_notifications` | ^19.5.0 | Local notification display | Keep |
| `googleapis_auth` | ^2.0.0 | FCM v1 HTTP API access token | Keep |
| `flutter_dotenv` | ^6.0.0 | `.env` file (Supabase/Firebase keys) | Add .NET base URL + JWT secrets here |
| `file_picker` | ^10.3.7 | File selection (CV, proposals) | Redirect uploads to `/api/Media` |
| `connectivity_plus` | ^6.1.5 | Network connectivity monitoring | Keep |
| `app_links` | ^6.4.1 | Deep link handling (`sha8lny://`, `sha8lny.com`) | Keep |
| `mobile_scanner` | ^7.1.3 | QR code scanner (wallet feature) | Keep |
| `cached_network_image` | ^3.4.1 | Image caching | Keep |
| `flutter_screenutil` | ^5.9.3 | Responsive sizing | Keep |
| `package_info_plus` | ^9.0.0 | App version (maintenance version check) | Keep |
| `bloc_test` + `mockito` | dev | Testing | Keep |

### 2.2 Clean Architecture Layering

The codebase follows strict Clean Architecture. The dependency rule is respected:

```
Presentation  ──→  Domain  ←──  Data
(Cubits/Pages)    (Entities,    (RemoteDataSources,
                  UseCases,     Models,
                  Repositories) RepositoryImpls)
```

Each feature (`lib/features/<name>/`) has three sub-packages:
```
<feature>/
├── data/
│   ├── datasources/   ← Supabase SDK calls (to be replaced with Dio)
│   ├── models/        ← JSON deserialization from Supabase/backend
│   └── repositories/  ← RepositoryImpl (wraps datasource, returns Either)
├── domain/
│   ├── entities/      ← Pure Dart classes, no external dependencies
│   ├── repositories/  ← Abstract interface
│   └── usecases/      ← Single-responsibility business operations
└── presentation/
    ├── cubit/         ← State management (Cubit + State classes)
    ├── pages/ (or screens/)
    └── widgets/
```

### 2.3 Dependency Injection

Global GetIt instance: `E:\LLM testing\Sha8alny-front-end\lib\core\services\service_locator.dart`

Pattern: `final sl = GetIt.instance;`
- External SDKs registered as `LazySingleton`: `FirebaseAuth.instance`, `FirebaseFirestore.instance`, `Supabase.instance.client`
- DataSources registered as `LazySingleton`
- Repositories registered as `LazySingleton`
- UseCases registered as `LazySingleton`
- Cubits registered as `factory` (new instance per route push)

Each feature also has its own service locator called by `setup()`:
- `setupHomeModule()` — `E:\LLM testing\Sha8alny-front-end\lib\features\home\service\home_service_locator.dart`
- `setProfileService()` — `E:\LLM testing\Sha8alny-front-end\lib\features\profile\service\profile_service_locator.dart`
- `setupProgressModule()` — `E:\LLM testing\Sha8alny-front-end\lib\features\progress\service\progress_sl.dart`
- `setupOpportunitiesModule()` — `E:\LLM testing\Sha8alny-front-end\lib\features\opportunities\service\service_locator.dart`
- `setupSaveOpportunitiesModule()` — `E:\LLM testing\Sha8alny-front-end\lib\features\save_opportunities\service\save_opp_service_locator.dart`
- `setupSettingServiceLocator()` — `E:\LLM testing\Sha8alny-front-end\lib\features\setting\service\setting_sevice_locator.dart`

Chat is wired directly in `setup()` (no dedicated module file).

### 2.4 App Initialization Order

File: `E:\LLM testing\Sha8alny-front-end\lib\main.dart`

```
1. dotenv.load('.env')                       // load Supabase + Firebase keys
2. Supabase.initialize(url, anonKey)         // from EnvConfig (.env)
3. Supabase auth state listener              // handles password-recovery deep link
4. Firebase.initializeApp(options)           // from firebase_options.dart
5. LocalNotificationConfig.instance.configNotifications()
6. FirebaseMessagingConfig.instance.configNotifications()  // FCM permission + topic
7. EasyLocalization.ensureInitialized()
8. DeepLinkService.instance.init()           // app_links stream
9. Bloc.observer = AppBlocObserver()
10. AppPreferences().init()                  // SharedPreferences singleton
11. setup()                                  // GetIt DI
12. runApp(EasyLocalization(
      child: MultiBlocProvider([
        BlocProvider(LocaleCubit),
        BlocProvider(ThemeCubit),
        BlocProvider(InternetCubit),
      ], child: MyApp(AppRouter()))))
```

Global root Cubits (always alive): `LocaleCubit`, `ThemeCubit`, `InternetCubit`.

### 2.5 Navigation

Router: `E:\LLM testing\Sha8alny-front-end\lib\core\routing\app_router.dart`
Routes constants: `E:\LLM testing\Sha8alny-front-end\lib\core\routing\routes.dart`

Navigation style: named routes with `PageRouteBuilder` fade transitions (400 ms).
Global navigator key: `AppRouter.navigatorKey` (used by FCM + deep link handlers).
Initial route: `/splashScreen`.

### 2.6 Global Cross-Cutting Concerns

| Concern | Implementation | Files |
|---|---|---|
| Theme | `ThemeCubit` — reads/writes `AppConstants.themeKey` to `shared_preferences`. `ThemeMode` only; dark theme commented out in `MyApp`. | `E:\LLM testing\Sha8alny-front-end\lib\core\cubit\theme\theme_cubit.dart` |
| Locale | `LocaleCubit` + `LocaleService` — reads/writes `AppConstants.localeKey`. `easy_localization` drives translations in `assets/lang/`. Supports `en` and `ar`. | `E:\LLM testing\Sha8alny-front-end\lib\core\cubit\locale\locale_cubit.dart` |
| Internet | `InternetCubit` — wraps `connectivity_plus`. Optimistic (shows No-Internet overlay but doesn't block network calls). | `E:\LLM testing\Sha8alny-front-end\lib\core\cubit\internet\internet_cubit.dart` |
| Deep Links | `DeepLinkService` — handles `sha8lny://opportunity/<id>` and `https://sha8lny.com/opportunity/<id>`. Navigates to `Routes.deepLinkLoadingScreen`. | `E:\LLM testing\Sha8alny-front-end\lib\core\services\deep_link_service.dart` |
| Push (FCM) | `FirebaseMessagingConfig` — foreground messages → `LocalNotificationConfig.showNotification`. Tapped notifications → navigate to `chatConversationScreen` if payload has `chatId`. | `E:\LLM testing\Sha8alny-front-end\lib\core\config\firebase_messaging_config.dart` |
| Push (local) | `LocalNotificationConfig` — `flutter_local_notifications`. `onDidReceiveNotificationResponse` parses `chatId` payload → navigate to chat. | `E:\LLM testing\Sha8alny-front-end\lib\core\config\local_notification_config.dart` |
| FCM send | `SendNotificationServices` — uses `googleapis_auth` + Dio (standalone, not `DioConsumer`) to call FCM v1 HTTP API with a service account JSON in `assets/notifications_key/`. | `E:\LLM testing\Sha8alny-front-end\lib\core\services\send_notification_services.dart` |
| BLoC observer | `AppBlocObserver` — logs all state transitions. | `E:\LLM testing\Sha8alny-front-end\lib\app_bloc_observer.dart` |

---

## 3. Network, Serialization & Error-Handling Lifecycle

### 3.1 The Dormant Dio Scaffold

A complete HTTP client scaffold exists but is **not used by any feature**:

| File | Absolute Path | Status |
|---|---|---|
| `ApiConsumer` (abstract) | `E:\LLM testing\Sha8alny-front-end\lib\core\network\api_consumer.dart` | Defined; unused |
| `DioConsumer` | `E:\LLM testing\Sha8alny-front-end\lib\core\network\dio_consumer.dart` | Implemented; unused |
| `AppInterceptors` | `E:\LLM testing\Sha8alny-front-end\lib\core\network\interceptors.dart` | Logs only; no JWT injection |
| `EndpointConstants` | `E:\LLM testing\Sha8alny-front-end\lib\core\constants\endpoint_constants.dart` | `baseUrl = 'https://api.example.com/v1'` — **placeholder** |
| `StatusCode` | `E:\LLM testing\Sha8alny-front-end\lib\core\network\status_code.dart` | HTTP codes + Firebase error strings |

`DioConsumer` provides `get`, `post`, `put`, `delete`. It catches `DioException` and maps
to domain exceptions via `_handleDioError`. **Critical gap:** the interceptor only logs
requests/responses — there is no `Authorization: Bearer <token>` header injection.

### 3.2 Error Model (Two-Layer)

The exception→failure→Either pipeline is fully designed and working, used by all repositories:

```
DioException (Dio layer)
    ↓ _handleDioError()
AppException (lib/core/errors/exceptions.dart)
    ↓ handleException() (lib/core/errors/handle_exceptions.dart)
Failure (lib/core/errors/failures.dart)
    ↓
Either<Failure, T>  (returned by all repositories)
    ↓
Cubit.fold(left → emit(FailureState), right → emit(SuccessState))
```

**Exceptions defined:**
`ServerException`, `CacheException`, `NetworkException`, `UnauthorizedException`,
`NotFoundException`, `AuthenticationException`, `ValidationException`, `ConflictException`,
`UnknownException`

**Failures defined (1:1 mapping):**
`ServerFailure`, `CacheFailure`, `NetworkFailure`, `UnauthorizedFailure`, `NotFoundFailure`,
`AuthenticationFailure`, `ValidationFailure`, `ConflictFailure`, `UnknownFailure`

**`handleException()` function:**
`E:\LLM testing\Sha8alny-front-end\lib\core\errors\handle_exceptions.dart` — central dispatcher
called in repository catch blocks.

### 3.3 Critical Gaps for .NET Integration

1. **No `ServiceResponse<T>` parsing.** The .NET backend wraps all responses in:
   `{ "success": bool, "message": string, "data": T, "errors": [...] }`.
   `DioConsumer` currently reads `error.response?.data['message']` on errors only.
   It has no logic to unwrap the `data` field on success, or to surface `errors` array on 400s.

2. **No token interceptor.** `AppInterceptors.onRequest` only calls `log(...)`. It does not read
   a JWT from storage or inject `Authorization: Bearer` headers.

3. **`ApiConsumer` has no `multipart/form-data` method.** The interface only has
   `get`, `post`, `put`, `delete` with `Map<String, dynamic>? body`. A `postForm` or
   `uploadFile` method must be added for `/api/Media` uploads.

---

## 4. Local Persistence & Caching

### 4.1 Storage Engine

**There is no embedded database.** The only local storage is `shared_preferences`.
- Package: `shared_preferences` (latest)
- Wrapper: `AppPreferences` (singleton)
  — `E:\LLM testing\Sha8alny-front-end\lib\core\utils\app_shared_preferences.dart`

**There is no HydratedBloc.** State is not auto-persisted. All Cubits start from initial state
on app restart and re-fetch from remote on demand.

### 4.2 `core/constants/database/*_table.dart` — NOT a Local DB

The 18 files under `E:\LLM testing\Sha8alny-front-end\lib\core\constants\database\` are
**Supabase remote schema constants**, NOT a local database schema:

```dart
// Example from user_table.dart
class UserTable {
  String get tableName => 'user';
  String get columnId => 'id';
  String get columnAuthId => 'auth_id';   // Supabase UUID
  String get columnFcmToken => 'fcm_token';
  // ...
}
```

All columns are snake_case strings, consumed via `supabaseClient.from(tableName)` calls.
The global `RemoteDatabaseTables` singleton in `tables.dart` provides instances of each.

### 4.3 SharedPreferences Keys

| Key | Source | Type | Purpose |
|---|---|---|---|
| `'user_data'` | `CacheKeys.userData` | `String` (JSON) | Serialized `UserModel` from last login |
| `'is_first_run'` | `CacheKeys.isFirstRun` | `bool` | Controls onboarding gate in splash |
| `'recent_searches'` | `CacheKeys.recentSearchKey` | `List<String>` | Recent search queries (max 10) |
| `'savedCard'` | `CacheKeys.cardKey` | `String` (JSON) | `CardModel` for wallet (local only) |
| `'app_locale'` | `AppConstants.localeKey` | `String` | `'en'` or `'ar'` |
| `'app_theme'` | `AppConstants.themeKey` | `String` | `'light'`, `'dark'`, or `'system'` |
| `'saved_email'` | hardcoded | `String` | Credential persistence (remember me) |
| `'saved_password'` | hardcoded | `String` | Credential persistence (remember me) |
| `'remember_me'` | hardcoded | `bool` | Remember-me flag |

`E:\LLM testing\Sha8alny-front-end\lib\core\constants\cache_keys.dart`

---

## 5. Authentication Architecture (Duality)

### 5.1 Provider Switch

File: `E:\LLM testing\Sha8alny-front-end\lib\core\config\auth_config.dart`

```dart
enum AuthProviderType { firebase, supabase }

class AuthConfig {
  static const AuthProviderType _defaultProvider = AuthProviderType.supabase;
  static AuthProviderType currentProvider = _defaultProvider;  // ← default = Supabase
}
```

The switch is a **compile-time-mutable static field**, not a user-facing toggle. The factory
methods in `service_locator.dart` (`_getAuthDataSource`, `_getUserDataSource`) read this field
at DI setup time and wire the correct implementation.

**Currently active:** Supabase (`AuthProviderType.supabase`)

### 5.2 Auth Data Sources

| Provider | Auth DataSource | User DataSource |
|---|---|---|
| **Supabase (default)** | `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\supabase\supa_base_auth_datasource.dart` | `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\supabase\user_supabase_datasource.dart` |
| Firebase (alternate) | `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\firebase\auth_firebase_data_source.dart` | `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\firebase\user_firestore_datasource.dart` |

Both implement the same abstract interfaces:
- `AuthRemoteDataSource` — `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\auth_remote_datasource.dart`
- `UserRemoteDataSource` — `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\user_remote_datasource.dart`

### 5.3 Auth Operations (Supabase path)

| Operation | Supabase call | .NET equivalent |
|---|---|---|
| Sign up | `_client.auth.signUp(email, password)` + insert user row | `POST /api/Auth/register` |
| Sign in | `_client.auth.signInWithPassword(email, password)` | `POST /api/Auth/login` |
| Sign out | `_client.auth.signOut()` | Clear local JWT token |
| Reset password | `_client.auth.resetPasswordForEmail(email)` | `POST /api/Auth/forgot-password` |
| Update password | `_client.auth.updateUser(UserAttributes(password: ...))` | `POST /api/Auth/reset-password` |
| Update FCM token | `_userDataSource.updateFcmToken(userId, token)` | No equivalent yet (backend `User` has no FCM token field) |

### 5.4 User Entity & Model

Entity: `E:\LLM testing\Sha8alny-front-end\lib\features\auth\domain\entities\user_entity.dart`
```dart
class UserEntity {
  final int?    id;       // Integer primary key (matches backend UserID)
  final String  email;
  final String? fcmToken;
  final String  name;
  final String? authId;   // Supabase UUID — has no meaning on the .NET backend
}
```

Model: `E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\models\user_model.dart`
- `fromJson` reads: `'id'`, `'email'`, `'full_name'`/`'name'`, `'password'`, `'auth_id'`, `'fcm_token'`

**Key migration divergences:**
- Backend returns `FirstName` + `LastName` separately; mobile expects `full_name` (single field)
- `authId` (Supabase UUID) has no place in the backend's JWT claims
- `UserEntity` has no `role`/`UserType` field — mobile is currently unaware of Student/Company/Admin distinction
- Backend JWT claims: `NameIdentifier` (UserID as int), `Email`, `Role` (UserType string)

### 5.5 Auth Cubit

File: `E:\LLM testing\Sha8alny-front-end\lib\features\auth\presentation\cubit\auth_cubit.dart`

| State | Trigger |
|---|---|
| `AuthInitial` | App start or logged-out state |
| `AuthLoading` | Any auth operation in progress |
| `AuthSignInSuccess(UserEntity)` | Successful login |
| `AuthSignUpSuccess(UserEntity)` | Successful registration |
| `AuthFailure(String error)` | Any auth error |
| `AuthLoggedOut` | Successful sign-out |
| `UserCached(UserEntity)` | Cached session restored on app resume |
| `PasswordResetEmailSent` | Reset password email dispatched |
| `PasswordUpdateSuccess` | Password change confirmed |

Post-login side effect: `_syncFcmToken(userId)` fetches current FCM token and stores it in
the user's Supabase row. This is **fire-and-forget** (fails silently).

---

## 6. Feature-by-Feature State Mapping

### 6.1 Splash

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\splash\`
**Data source:** None (uses `MaintenanceRepository` + `GetCachedUserUseCase` + `AppPreferences`)
**State:** Stateless screen — uses `Timer` callbacks, no Cubit.

**Decision tree (after 3-second animation):**
1. Fetch `app_config` from Supabase (`maintenanceRepo.getMaintenanceStatus()`)
2. If `isMaintenanceMode` → push `/maintenance`
3. If app version < `minSupportedVersion` → push `/update`
4. Else: `isFirstRun()` → push `/onBoarding`; else `isLoggedIn()` → push `/home`; else push `/login`

**Migration note:** Maintenance config is Supabase-only (`app_config` table). The .NET backend
has no maintenance endpoint. Either keep Supabase for this or add `GET /api/Maintenance/config`
to the backend.

---

### 6.2 Onboarding

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\onboarding\`
**Data source:** None (static `OnboardingPageModel` list)
**State:** Stateless `OnboardingScreen`; page indicator via `smooth_page_indicator`.
**Post-onboarding:** Sets `CacheKeys.isFirstRun = false`, pushes to `/login`.

---

### 6.3 Auth

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\auth\`
**Data source:** Supabase (default) or Firebase (alternate)
**Cubit:** `AuthCubit` (see §5.5)
**Screens:** `LoginScreen`, `SignUpScreen`, `ResetPassword`, `ChangePassword`, `VerificationScreen`

**Operations with screens:**
- Login → `AuthCubit.signInWithEmailAndPassword` → `AuthSignInSuccess` → navigate `/home`
- Sign up → `AuthCubit.signUpWithEmailAndPassword` → `AuthSignUpSuccess` → navigate `/home`
- Reset password → `AuthCubit.resetPassword(email)` → `PasswordResetEmailSent`
- Change password → `AuthCubit.updatePassword(password)` → `PasswordUpdateSuccess`
- Sign out (from settings) → `SettingCubit.signOut()` → delegates to `AuthUseCase.signOut()`

**Migration target:** Replace Supabase auth calls with REST calls to `/api/Auth/*`. The
`AuthConfig.currentProvider` switch can be extended to a third value, or the Supabase
implementations can be replaced directly once .NET auth is stable.

---

### 6.4 Home

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\home\`
**Data source:** `HomeRemoteDataSourceImpl` — `E:\LLM testing\Sha8alny-front-end\lib\features\home\data\datasources\home_remote_data_source_impl.dart`
**Backend:** Supabase (`opportunity` table, `student_profile` table)
**Cubits:** `HomeCubit`, `SearchCubit`

#### `HomeCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\home\presentation\cubit\home_cubit.dart`

| State | Contents |
|---|---|
| `HomeInitial` | — |
| `HomeLoading` | — |
| `HomeSuccess` | `List<OpportunityModel> opportunities`, `StudentProfileModel? userProfile`, `List<Internship>? myProjects` |
| `HomeFailure` | `String error` |

`fetchOpportunities()` performs three parallel calls:
1. `getCachedUserUseCase()` — get logged-in user
2. `useCase.fetchUserProfile(userId)` — Supabase `student_profile` table
3. `useCase.fetchOpportunities()` + `progressUseCase.fetchMyProjects(studentId)` — Supabase

#### `SearchCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\home\presentation\cubit\search_cubit.dart`

Manages local suggestion filtering + Supabase `ilike` search on `opportunity.title`.
Recent searches persisted in `shared_preferences` via `AppPreferences.saveRecentSearch()`.

**Migration target:** Replace Supabase calls with `GET /api/Projects?search=<query>`.

---

### 6.5 Opportunities

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\opportunities\`
**Data source:** `OpportunitiesRemoteDatasourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\features\opportunities\data\datasourse\opportunities_remote_datasource.dart`
*(Note: directory is misspelled `datasourse` in the repo)*
**Backend:** Supabase (`opportunity`, `modules`, `application` tables)
**Cubits:** `OpportunitiesCubit`, `AppliedOpportunitiesCubit`, `OpportunityStatusCubit`

#### `OpportunitiesCubit`
| State | Contents |
|---|---|
| `OpportunitiesInitial` | — |
| `OpportunitiesLoading` | — |
| `OpportunitiesSuccess` | `List<ModuleModel> modules`, `List<OpportunityModel> opportunities` |
| `OpportunitiesFailure` | `String message` |

`fetchOpportunities()` fetches all opportunities then calls `filterOpportunities(isInternship: bool)`.
`getOpportunitiesModules(opportunityId)` fetches modules from Supabase `modules` table.

#### `AppliedOpportunitiesCubit`
`fetchAppliedOpportunities(studentId)` — fetches application records from Supabase then joins
opportunity data.

#### `OpportunityStatusCubit`
`checkStatus(opportunityId)` — delegates to `CheckApplicationStatusUseCase` which checks if
the student has an `application` row for the given opportunity in Supabase.

**Migration target:** Replace with `GET /api/Projects` (list/filter), `GET /api/Projects/{id}`,
`GET /api/Applications/my-applications`.

---

### 6.6 Apply Form

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\apply_form\`
**Data source:** `ApplyFormRemoteDataSourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\features\apply_form\data\datasources\apply_form_remote_datasource_impl.dart`
**Backend:** Supabase Storage (`applications` bucket for file uploads) + Supabase `application` table

#### File Upload (Current)
```dart
await _client.storage.from('applications').upload(fullPath, file);
final publicUrl = _client.storage.from('applications').getPublicUrl(fullPath);
```

#### `ApplicationCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\apply_form\presentation\cubit\application_cubit.dart`

| State | Contents |
|---|---|
| `ApplicationInitial` | — |
| `ApplicationLoading` | — |
| `ApplicationSuccess` | — |
| `ApplicationFailure` | `String error` |
| `ApplicationResumesLoaded` | `List<String> resumeUrls`, `File? cvFile`, `File? proposalFile`, `String? selectedCvUrl` |

`submitApplication({opportunityId, notes})` — uploads CV/proposal to Supabase Storage,
then inserts `ApplicationModel` into Supabase `application` table.

**Migration target (two-step per backend Rule 3):**
1. Upload `File` to `POST /api/Media/upload?folder=applications` → get `cvUrl` string
2. `POST /api/Applications/apply` with `{ projectId, cvUrl, coverLetter, ... }`

---

### 6.7 Profile

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\profile\`
**Data source:** `ProfileRemoteDataSourceImplSupabase`
— `E:\LLM testing\Sha8alny-front-end\lib\features\profile\data\datasources\profile_remote_data_source_impl_supabase.dart`
**Backend:** Supabase (`student_profile`, `user`, `student_skills` tables) + Supabase Storage (`resumes` bucket)

#### Operations
- `getStudentProfile(userId)` — Supabase `student_profile` filtered by `user_id`
- `createStudentProfile(userId, profile)` — Supabase insert
- `updateStudentProfile(userId, profile)` — Supabase update
- `getStudentSkills(profileId)` — Supabase `student_skills`
- `addStudentSkill` / `removeStudentSkill` — Supabase insert/delete
- `uploadResume(userId, filePath)` — **Supabase Storage `resumes` bucket**; updates `cv_url` in `student_profile`
- `uploadProfilePicture` — **commented out** (no-op stub)
- `addOrUpdateGithubUrl` — Supabase update `student_profile.github_url`

#### `ProfileCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\profile\presentation\cubit\profile_cubit.dart`

| State | Contents |
|---|---|
| `ProfileInitial` | — |
| `ProfileLoading` | — |
| `ProfileLoaded` | `StudentProfileModel? studentProfileEntity`, `UserEntity? userEntity`, `List<StudentSkillModel>? skills` |
| `ProfileFailure` | `String errorMessage`, `int errorCode` |

#### Wallet Sub-Feature
Files: `E:\LLM testing\Sha8alny-front-end\lib\features\profile\presentation\pages\wallet\`

**Wallet is entirely local and disconnected from any backend.** `CardModel` is stored in
`shared_preferences` under `CacheKeys.cardKey`. `TransactionModel` is a local-only model.
The QR scanner (`mobile_scanner`) has no backend integration. There is no Paymob integration
in the mobile wallet UI.

**Migration target:** Profile → `GET/PUT /api/students/profile`; CV upload → `POST /api/Media/upload?folder=cv`; skills → `GET/POST/DELETE` on `/api/students/profile` via the skills array in the student DTO; Wallet → needs full Paymob + `/api/Payments` integration.

---

### 6.8 Progress

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\progress\`
**Data source:** `ProgressRemoteDataSourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\features\progress\data\datasources\progress_remote_datasource.dart`
**Backend:** Supabase (`assignment`, `completed_opportunity`, `opportunity` tables)

#### Operations
- `fetchMyAssignmentsIds(studentId)` — reads `assignment` Supabase table
- `fetchCompletedOpportunityIds(studentId)` — reads `completed_opportunity` Supabase table
- `fetchProjectsByIds(studentId, ids, isCompleted)` — fetches `opportunity` rows by IDs, maps to `Internship` model

#### `ProgressCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\progress\presentation\cubit\progress_cubit.dart`

| State | Contents |
|---|---|
| `ProgressInitial` | — |
| `ProgressLoading` | — |
| `ProgressSuccess` | `List<Internship>? myProjects` |
| `ProgressFailure` | `String error` |

Methods: `fetchMyProjects()` (ongoing), `fetchCompletedProjects()`.

**Note:** `HomeCubit.fetchOpportunities()` also calls `progressUseCase.fetchMyProjects()` and
includes `List<Internship>? myProjects` in `HomeSuccess` — progress data is fetched on home load.

**Migration target:** `GET /api/Applications/my-applications` (ongoing), `GET /api/Execution/application/{id}/progress`, completed via `CompletedOpportunity` endpoint (not yet in backend).

---

### 6.9 Chat

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\chat\`
**Data source:** `ChatRemoteDataSourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\features\chat\data\datasources\chat_remote_data_source.dart`
**Backend:** **Pure Supabase Postgres + Realtime streams.** No SignalR. No REST HTTP.

#### Architecture
- `getChats(userId)` → `supabaseClient.from('chats').stream(primaryKey: ['id'])` → `Stream<List<ChatModel>>`
- `getMessages(chatId, userId)` → `supabaseClient.from('messages').stream(...)` → `Stream<List<MessageModel>>`
- `sendMessage(chatId, content, senderId)` → Supabase insert
- `createChat(userId, otherUserId)` — checks for existing/orphaned chat; creates new if needed
- `deleteChat(chatId, userId)` — removes user from `participants` array; hard-deletes if empty
- `markMessagesAsRead(chatId, userId)` — bulk update `is_read = true`
- `getOtherUserToken(chatId, currentUserId)` — fetches FCM token for push notifications

Push for offline delivery: `SendNotificationServices.sendNotification(token, title, body, data)`
sends FCM via HTTP v1 API. Payload includes `chatId` and `senderId` for deep-link on tap.

#### `ChatCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\chat\presentation\cubit\chat_cubit.dart`

| State | Contents |
|---|---|
| `ChatInitial` | — |
| `ChatLoading` | — |
| `ChatLoaded` | `Stream<List<ChatEntity>> chatsStream` |
| `ChatMessagesLoading` | — |
| `ChatMessagesLoaded` | `Stream<List<MessageEntity>> messagesStream` |
| `ChatError` | `String message` |
| `ChatCreated` | `ChatEntity chat` |

Also: `UserSearchCubit` — searches Supabase `user` table by name for starting new chats.

**Migration target (complex — two stages):**
- **Stage 1:** Migrate to REST `/api/Chat/*` endpoints (send, conversations, messages)
- **Stage 2:** Replace Supabase Realtime streams with SignalR `/hubs/notifications`
  (`ReceiveMessage` event) once the backend `ChatHub` is built (per `context-backend.md` §5.1)

---

### 6.10 Notifications

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\notifications\`
**Data source:** `NotificationsRemoteDataSource` — **STUB** (empty abstract class)
**Cubit:** `NotificationsCubit` — **STUB** (only `doSomething()` placeholder)

Files:
- `E:\LLM testing\Sha8alny-front-end\lib\features\notifications\data\datasources\notifications_remote_datasource.dart` — empty abstract class
- `E:\LLM testing\Sha8alny-front-end\lib\features\notifications\presentation\cubit\notifications_cubit.dart` — placeholder

**The notifications feature scaffold exists but has zero implementation.**
Push notifications are currently delivered via FCM directly (see §2.6).

**Migration target:** Implement `NotificationsRemoteDataSource` and `NotificationsCubit` against:
- `GET /api/Notifications` — fetch notification list
- `GET /api/Notifications/unread-count`
- `PUT /api/Notifications/{id}/read`
- `PUT /api/Notifications/read-all`
- SignalR `ReceiveNotification` event for real-time push

---

### 6.11 Save Opportunities (Bookmarks)

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\save_opportunities\`
*(Note: domain sub-package is misspelled `domin` in the repo)*
**Data source:** `SaveOppDataSourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\features\save_opportunities\data\dataSource\save_opp_datasource.dart`
**Backend:** Supabase `saved_opportunities` table

#### `SaveOppCubit`
File: `E:\LLM testing\Sha8alny-front-end\lib\features\save_opportunities\presentation\cubit\save_opp_cubit.dart`

| State | Contents |
|---|---|
| `SaveOppState.initial()` | `isSaved = false` |
| `SaveOppLoading` | `isSaved` (preserved) |
| `SaveOppSuccess` | `bool isSaved`, `List<OpportunityModel>? opportunities` |
| `SaveOppFailure` | `String errorMessage`, `bool isSaved` |

Operations: `checkIfSaved`, `toggleSaveOpportunity`, `getSavedOpportunities`.

**Migration target:** `GET/POST /api/students/saved-projects`, `DELETE /api/students/saved-projects/{id}`

---

### 6.12 Setting

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\features\setting\`
**Data source:** `SettingRemoteDataSource` — **STUB** (empty abstract class)
**Backend:** None (sign-out delegates to `AuthUseCase`; locale/theme are local-only)

`SettingCubit` only wraps `AuthUseCase.signOut()`. UI offers screens for language/theme (local),
FAQ, Help & Support (local/static), and About.

**Migration target:** Implement `SettingRemoteDataSource` and wire to `GET/PUT /api/Settings`
for server-side `UserSettings` (language, notification preferences, privacy).

---

### 6.13 Maintenance (Core)

**Directory:** `E:\LLM testing\Sha8alny-front-end\lib\core\maintenance\`
**Data source:** `MaintenanceRemoteDataSourceImpl`
— `E:\LLM testing\Sha8alny-front-end\lib\core\maintenance\data\datasources\maintenance_remote_data_source.dart`
**Backend:** Supabase `app_config` table

Fetches `isMaintenanceMode`, `maintenanceMessage`, `maintenanceTitle`, `minSupportedVersion`.
Used by `SplashScreen` to gate app entry.

**Migration note:** The .NET backend has `POST /api/Maintenance/backup` (admin-only) but no
`GET /api/Maintenance/config` equivalent. This Supabase dependency should remain or a new
backend endpoint should be created.

---

## 7. Cross-Platform Feature Parity Matrix

Backend capabilities from `context-backend.md` §4 vs. mobile implementation status:

| Backend Capability | Endpoint(s) | Mobile Status | Notes |
|---|---|---|---|
| Register | `POST /api/Auth/register` | ❌ Not connected | Auth is Supabase-based |
| Login (JWT) | `POST /api/Auth/login` | ❌ Not connected | Auth is Supabase-based |
| Get current user | `GET /api/Auth/me` | ❌ Not connected | Uses Supabase user row |
| Forgot password | `POST /api/Auth/forgot-password` | ❌ Not connected | Supabase `resetPasswordForEmail` |
| Reset password | `POST /api/Auth/reset-password` | ❌ Not connected | Supabase `updateUser` |
| Email verification | `POST /api/Auth/verify-email` | ❌ Not connected | Supabase handles natively |
| Student profile CRUD | `GET/POST/PUT /api/students/profile` | ⚠️ Supabase | Profile data in Supabase `student_profile` |
| Get student by ID | `GET /api/students/{id}` | ❌ Not connected | No cross-student lookup on mobile |
| Search students | `GET /api/students/search` | ❌ Not connected | — |
| Saved projects (bookmarks) | `GET/POST /api/students/saved-projects` | ⚠️ Supabase | `saved_opportunities` Supabase table |
| Delete saved project | `DELETE /api/students/saved-projects/{id}` | ⚠️ Supabase | — |
| Company profile CRUD | `GET/POST /api/companies/profile` | ❌ Not connected | No company profile screen on mobile |
| Get company by ID | `GET /api/companies/{id}` | ❌ Not connected | — |
| Search companies | `GET /api/companies/search` | ❌ Not connected | — |
| List/search projects | `GET /api/Projects` | ⚠️ Supabase | Supabase `opportunity` table |
| Get project by ID | `GET /api/Projects/{id}` | ⚠️ Supabase | — |
| Create project | `POST /api/Projects` | ❌ Not connected | No company-facing project creation screen |
| Update/delete project | `PUT/DELETE /api/Projects/{id}` | ❌ Not connected | — |
| Company's own projects | `GET /api/Projects/my-projects` | ❌ Not connected | — |
| Submit application | `POST /api/Applications/apply` | ⚠️ Supabase | Files → Supabase Storage; record → Supabase `application` |
| Get application | `GET /api/Applications/{id}` | ❌ Not connected | — |
| My applications | `GET /api/Applications/my-applications` | ⚠️ Supabase | Via `AppliedOpportunitiesCubit` |
| Review application | `PUT /api/Applications/{id}/review` | ❌ Not connected | No company review screen |
| Update application status | `PUT /api/Applications/{id}/status` | ❌ Not connected | — |
| Project modules CRUD | `GET/POST/DELETE /api/Execution/project/{id}/modules` | ⚠️ Supabase | Modules fetched from Supabase `modules` table |
| Update module progress | `PUT /api/Execution/modules/{id}/progress` | ❌ Not connected | No progress-update action in UI |
| Review module | `PUT /api/Execution/modules/{id}/review` | ❌ Not connected | — |
| Application progress | `GET /api/Execution/application/{id}/progress` | ❌ Not connected | Progress cubit reads Supabase |
| Mark job complete | `POST /api/Execution/application/{id}/complete` | ❌ Not connected | — |
| Completion summary | `GET /api/Execution/application/{id}/summary` | ❌ Not connected | — |
| Send message | `POST /api/Chat/send` | ⚠️ Supabase | Supabase insert |
| Get conversations | `GET /api/Chat/conversations` | ⚠️ Supabase | Supabase Realtime stream |
| Get conversation | `GET /api/Chat/conversations/{id}` | ⚠️ Supabase | — |
| Get paginated messages | `GET /api/Chat/conversations/{id}/messages` | ⚠️ Supabase | Supabase Realtime stream |
| Get notifications | `GET /api/Notifications` | ❌ STUB | `NotificationsCubit` is empty |
| Unread count | `GET /api/Notifications/unread-count` | ❌ STUB | — |
| Mark read | `PUT /api/Notifications/{id}/read` | ❌ STUB | — |
| Mark all read | `PUT /api/Notifications/read-all` | ❌ STUB | — |
| Review student | `POST /api/Reviews/student` | ❌ Not connected | No review screens |
| Review company | `POST /api/Reviews/company` | ❌ Not connected | — |
| Get student reviews | `GET /api/Reviews/student/{id}` | ❌ Not connected | — |
| Get company reviews | `GET /api/Reviews/company/{id}` | ❌ Not connected | — |
| My certificates | `GET /api/Certificates/my-certificates` | ❌ Not connected | No certificates feature |
| Verify certificate | `GET /api/Certificates/verify/{uniqueId}` | ❌ Not connected | — |
| Process payment | `POST /api/Payments/pay` | ❌ Not connected | Wallet is local mock only |
| Payment history | `GET /api/Payments/history` | ❌ Not connected | — |
| Payment details | `GET /api/Payments/{id}` | ❌ Not connected | — |
| Upload media | `POST /api/Media/upload/*` | ❌ Not connected | Files go to Supabase Storage |
| Delete media | `DELETE /api/Media` | ❌ Not connected | — |
| Master data: skills | `GET /api/MasterData/skills` | ❌ Not connected | No dynamic skills list from backend |
| Master data: universities | `GET /api/MasterData/universities` | ❌ Not connected | — |
| Master data: departments | `GET /api/MasterData/departments` | ❌ Not connected | — |
| Admin operations | `GET/PUT /api/Admin/*` | ❌ Not connected | No admin screens |
| User settings | `GET/PUT /api/Settings` | ❌ STUB | `SettingRemoteDataSource` is empty |
| Database backup | `POST /api/Maintenance/backup` | ❌ Not connected | — |
| SignalR (notifications) | `/hubs/notifications` — `ReceiveNotification`, `ReceiveMessage` | ❌ Not connected | Uses FCM instead |

**Legend:** ✅ Connected | ⚠️ Supabase (feature works but on wrong backend) | ❌ Missing/Not connected/Stub

---

## 8. Integration Debt Register

### 8.1 ID Type Mismatch

| Layer | ID type | Example |
|---|---|---|
| .NET Backend | `int` (auto-increment) | `UserID`, `StudentID`, `ProjectID` |
| Mobile (domain entities) | `int` or `int?` | `UserEntity.id`, `StudentProfileModel.id` |
| Supabase Auth | `String` (UUID) | `UserEntity.authId` |
| Chat datasource | `String` | `ChatEntity.id`, `MessageEntity.id` |

**Debt:** Chat and user-search cubits pass `String` IDs throughout (Supabase UUIDs). When
migrating to the .NET backend, these must be changed to `int` to match `UserID`, `ConversationID`,
`MessageID`.

### 8.2 User Schema Mismatch

| Field | Supabase column | .NET backend field |
|---|---|---|
| Display name | `full_name` (single field) | `FirstName` + `LastName` (separate) |
| Auth token | Supabase session token | JWT Bearer token (60 min, RS key) |
| User identifier | `auth_id` (Supabase UUID) | `UserID` (int) in JWT `NameIdentifier` claim |
| Role | `role` (string, informal) | `UserType` enum: `Student`, `Company`, `University`, `Admin` |
| FCM token | `fcm_token` column in `user` table | ❌ No FCM token field on backend `User` entity |

**Debt:** `UserEntity` must gain a `UserType` / `role` field. `name` must be split or the backend
DTO must be asked to return a `FullName` projection. `authId` becomes irrelevant. FCM token
storage needs a new backend endpoint or the mobile must send it at login time.

### 8.3 File Upload Model Mismatch

| Current (Supabase) | Required (.NET) |
|---|---|
| `supabaseClient.storage.from('bucket').upload(path, file)` | `POST /api/Media/upload?folder=<folder>` with `multipart/form-data` |
| Returns public URL directly | Returns URL string in `ServiceResponse<string>` |
| Files stored in Supabase Storage buckets | Files stored in `wwwroot/uploads/{folder}/` |
| Allowed: any extension | Allowed: `.jpg .jpeg .png .gif .pdf` only, max 5 MB |

**Debt:** Every upload site (`apply_form`, `profile` CV, profile picture) must be redirected to
`/api/Media`. `ApiConsumer` needs a new `postForm(path, FormData)` method added.

### 8.4 Column Naming Convention

Mobile code reads **snake_case** columns from Supabase (`opportunity_id`, `student_id`,
`created_at`). The .NET backend DTOs use **camelCase / PascalCase** JSON serialization
(e.g., `projectId`, `studentId`, `createdAt`). All `fromJson` model factories must be
updated when migrating each feature to the .NET responses.

### 8.5 ServiceResponse<T> Envelope

The .NET backend wraps all responses:
```json
{ "success": true, "message": "...", "data": { ... }, "errors": [] }
```
The current `DioConsumer` does **not** unwrap this. A `ServiceResponse<T>` parser must be
added (see Migration Roadmap Step 3).

### 8.6 Notifications & Realtime

| Current | Required |
|---|---|
| FCM push only (for chat) | SignalR `/hubs/notifications` for `ReceiveNotification` + `ReceiveMessage` |
| Supabase Realtime streams for chat | SignalR connection + channel subscription |
| Chat IDs are Supabase UUIDs (String) | Chat IDs will be `ConversationID` (int) |
| `NotificationsCubit` is a stub | Full implementation needed |

### 8.7 Missing Features on Mobile (No Screen / No Cubit)

| Backend capability | Mobile status |
|---|---|
| Company profile (create/view/edit) | **No screen exists** |
| Company project creation/management | **No screen exists** |
| Application review (company side) | **No screen exists** |
| Certificates (list/verify) | **No screen exists** |
| Reviews (student ↔ company) | **No screen exists** |
| Module progress update (student action) | **No update action** (modules displayed in progress cubit only) |
| Admin panel | **No screens** |
| Email verification OTP screen | `VerificationScreen` exists (`screens/reset_password/verification_screen.dart`) — wired to Supabase; needs rewiring to `POST /api/Auth/verify-email` |

### 8.8 `EndpointConstants` — Placeholder

`E:\LLM testing\Sha8alny-front-end\lib\core\constants\endpoint_constants.dart`:
```dart
static const String baseUrl = 'https://api.example.com/v1';
```
This must be replaced with the actual .NET API base URL (e.g., the Google Cloud Run endpoint)
before Dio calls will reach the backend.

---

## 9. Migration Roadmap (.NET Cutover)

This roadmap converts the mobile app from Supabase/Firebase to the .NET API. Execute in order —
each step depends on the previous.

### Step 1 — Point `EndpointConstants` at the .NET API

**File:** `E:\LLM testing\Sha8alny-front-end\lib\core\constants\endpoint_constants.dart`

```dart
class EndpointConstants {
  // Replace placeholder with actual deployed URL from Google Cloud Run
  static const String baseUrl = 'https://<your-cloud-run-url>';

  // Define all backend route constants here (matching context-backend.md §4):
  static const String login             = '/api/Auth/login';
  static const String register          = '/api/Auth/register';
  static const String forgotPassword    = '/api/Auth/forgot-password';
  static const String resetPassword     = '/api/Auth/reset-password';
  static const String verifyEmail       = '/api/Auth/verify-email';
  static const String getMe             = '/api/Auth/me';
  static const String studentProfile    = '/api/students/profile';
  static const String projects          = '/api/Projects';
  static const String applications      = '/api/Applications';
  static const String mediaUpload       = '/api/Media/upload';
  // ... add remaining routes as features are migrated
}
```

Add the env variable to `.env`: `NET_API_BASE_URL=https://<cloud-run-url>`
Then in `EnvConfig`: `static String get netApiBaseUrl => dotenv.env['NET_API_BASE_URL'] ?? '';`

---

### Step 2 — Add JWT Token Interceptor

**File to modify:** `E:\LLM testing\Sha8alny-front-end\lib\core\network\interceptors.dart`

The interceptor must inject the JWT returned by `/api/Auth/login` (stored in `shared_preferences`)
into every authorized request:

```dart
import 'package:graduation_project/core/constants/cache_keys.dart';
import 'package:graduation_project/core/utils/app_shared_preferences.dart';

class AppInterceptors extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final token = AppPreferences().getData(CacheKeys.authToken); // new key to add
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    log('REQUEST[${options.method}] => PATH: ${options.path}');
    super.onRequest(options, handler);
  }
  // ... (keep existing onResponse/onError logging)
}
```

Add a new key to `CacheKeys`: `static const String authToken = 'auth_token';`
Store the JWT string received from `/api/Auth/login` response under this key.

---

### Step 3 — Add `ServiceResponse<T>` Envelope Parser

The .NET API always returns `{ "success": bool, "message": string, "data": T }`.
`DioConsumer` must unwrap this. Add a helper class and update `DioConsumer`:

```dart
// New file: lib/core/network/service_response.dart
class ServiceResponse<T> {
  final bool success;
  final String message;
  final T? data;
  final List<String> errors;

  ServiceResponse({
    required this.success,
    required this.message,
    this.data,
    this.errors = const [],
  });

  factory ServiceResponse.fromJson(
      Map<String, dynamic> json, T Function(dynamic) fromData) {
    return ServiceResponse(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      data: json['data'] != null ? fromData(json['data']) : null,
      errors: List<String>.from(json['errors'] ?? []),
    );
  }
}
```

Update `DioConsumer.get / post / put / delete` to return the unwrapped `data` field:
```dart
@override
Future<dynamic> post(String path, {Map<String, dynamic>? body}) async {
  try {
    final response = await client.post(path, data: body);
    // Unwrap ServiceResponse envelope
    if (response.data is Map && response.data.containsKey('success')) {
      if (response.data['success'] == true) {
        return response.data['data'];    // Return unwrapped data
      } else {
        throw ValidationException(response.data['message'] ?? 'Request failed');
      }
    }
    return response.data;  // Fallback for non-envelope responses
  } on DioException catch (error) {
    _handleDioError(error);
  }
}
```

Also add `postForm` method for multipart uploads:
```dart
@override
Future<dynamic> postForm(String path, {required FormData formData}) async {
  try {
    final response = await client.post(path, data: formData,
      options: Options(contentType: 'multipart/form-data'));
    return response.data['data'];
  } on DioException catch (error) {
    _handleDioError(error);
  }
}
```
And add `postForm` to `ApiConsumer` abstract class.

---

### Step 4 — Migrate Auth Feature

**Target:** Replace Supabase auth with `POST /api/Auth/*` + JWT storage.

Create new datasource:
`E:\LLM testing\Sha8alny-front-end\lib\features\auth\data\datasources\net_api\auth_net_api_datasource.dart`

```dart
class AuthNetApiDataSource implements AuthRemoteDataSource {
  final ApiConsumer _apiConsumer;
  AuthNetApiDataSource(this._apiConsumer);

  @override
  Future<UserModel> signInWithEmailAndPassword({
    required String email, required String password
  }) async {
    final data = await _apiConsumer.post(EndpointConstants.login,
      body: {'email': email, 'password': password});
    // data = { "userId": int, "email": str, "fullName": str, "role": str, "token": str }
    final token = data['token'] as String;
    await AppPreferences().setData(CacheKeys.authToken, token);
    return UserModel.fromNetJson(data);
  }

  @override
  Future<UserModel> signUpWithEmailAndPassword({
    required String email, required String password, required String name
  }) async {
    final data = await _apiConsumer.post(EndpointConstants.register,
      body: {'email': email, 'password': password, 'fullName': name, 'userType': 'Student'});
    return UserModel.fromNetJson(data);
    // After register, user must verify email — no JWT yet
  }

  @override
  Future<void> signOut() async {
    await AppPreferences().removeData(CacheKeys.authToken);
    await AppPreferences().removeData(CacheKeys.userData);
  }
  // ... implement remaining methods
}
```

Update `UserModel` with a `fromNetJson` factory:
```dart
factory UserModel.fromNetJson(Map<String, dynamic> json) {
  return UserModel(
    id: json['userId'] as int?,
    email: json['email'] ?? '',
    name: json['fullName'] ?? '${json['firstName'] ?? ''} ${json['lastName'] ?? ''}'.trim(),
    password: '',     // Never stored from backend
    authId: null,     // No Supabase UUID on backend
    fcmToken: null,   // No FCM on backend User entity
    role: json['role'],  // Add 'role' field to UserEntity
  );
}
```

Add `role` / `userType` field to `UserEntity`:
```dart
class UserEntity {
  final int?    id;
  final String  email;
  final String  name;
  final String? fcmToken;
  final String? authId;   // Keep for Supabase path; null on .NET path
  final String? role;     // 'Student', 'Company', 'Admin', 'University'
}
```

Register in `service_locator.dart` by adding `AuthProviderType.netApi` to the enum and
its factory case.

---

### Step 5 — Migrate Master Data (Skills / Universities / Departments)

These are prerequisite lookups consumed by profile creation and opportunity filtering.

```dart
// Profile creation screen and opportunity filter need skills list from:
// GET /api/MasterData/skills
// GET /api/MasterData/universities
// GET /api/MasterData/departments
```

Create `MasterDataRemoteDataSource` and `MasterDataCubit`. The skills list replaces the
free-text `StudentSkillsTable` in Supabase — students will now select from `Skill` entities
with canonical `SkillID` values that match `ProjectRequiredSkill` rows.

---

### Step 6 — Migrate Student Profile

**Target:** `GET/POST/PUT /api/students/profile`

The backend `StudentProfileResponseDto` will have:
`StudentID`, `FirstName`, `LastName`, `Bio`, `Phone`, `ProfilePicture` (URL), `CvFileUrl`,
`UniversityID`, `DepartmentID`, `AcademicYear`, `City`, `Country`, `GitHubProfile`,
`ProfileCompleteness`, `StudentSkills` (list).

Update `StudentProfileModel.fromNetJson` to map these. **Key change:** `skills` are now
`List<{skillId, skillName, proficiency}>` instead of free-text strings.

---

### Step 7 — Migrate Opportunities / Projects

**Target:** `GET /api/Projects`, `GET /api/Projects/{id}`

Map `ProjectResponseDto` → `OpportunityModel` (or rename to `ProjectModel`):
- `ProjectID` → `id` (int)
- `ProjectName` → `title`
- `ProjectType` → `type` (enum string: `Internship`, `Training`, etc.)
- `Deadline` → `deadline`
- `Status` → `status`
- `Company.CompanyName` → `company`
- `ProjectRequiredSkills[].SkillName` → `skills`

Filter by `ProjectType` on the client (or use backend query param `?projectType=Internship`).

---

### Step 8 — Migrate Application Flow

**Two-step per backend Rule 3:**
1. `ApplicationCubit.pickCv()` / `pickProposal()` → then **upload** via `ApiConsumer.postForm`
   to `POST /api/Media/upload?folder=cv` and `POST /api/Media/upload?folder=proposals`
   → store returned URL strings
2. `ApplicationCubit.submitApplication()` → `POST /api/Applications/apply` with:
   ```json
   { "projectId": 123, "coverLetter": "...", "bidAmount": 0.0, "studentCvUrl": "<url>", "proposalFileUrl": "<url>" }
   ```

---

### Step 9 — Migrate Progress / Execution

**Target:** `GET /api/Applications/my-applications` + `GET /api/Execution/application/{id}/progress`

Map `ApplicationResponseDto.Status` (enum: `Pending`, `Accepted`, `InProgress`, `Completed`...)
to the progress display. Module progress comes from `ApplicationModuleProgress` array on the
application detail DTO.

---

### Step 10 — Migrate Chat

**Stage 1 (REST):** Replace Supabase chat with `/api/Chat/*`. Change all IDs from `String`
(Supabase UUID) to `int`. `ChatEntity.id` → `int`. `MessageEntity.id` → `int`.

**Stage 2 (SignalR):** Once the backend adds `ChatHub` (per `context-backend.md §5.1`):
- Add `signalr_core` or `signalr_netcore` Flutter package
- Connect: `HubConnection hubConn = HubConnectionBuilder().withUrl('/hubs/notifications', options: HttpConnectionOptions(accessTokenProvider: () async => token)).build();`
- Listen: `hubConn.on('ReceiveMessage', (List<Object?>? args) { ... })`
- Replace Supabase `stream(...)` subscriptions with SignalR event handlers

---

### Step 11 — Migrate Notifications, Settings, Reviews, Certificates, Payments

Implement the stub features in order of user-facing priority:
1. **Notifications** — wire `NotificationsCubit` to `/api/Notifications/*` + SignalR `ReceiveNotification`
2. **Settings** — wire `SettingCubit` to `GET/PUT /api/Settings` (sync locale/theme to `UserSettings.Language`)
3. **Reviews** — new `ReviewCubit` + `POST /api/Reviews/*` + `GET /api/Reviews/*`
4. **Certificates** — new `CertificateCubit` + `GET /api/Certificates/my-certificates`
5. **Payments** — full Paymob integration via `POST /api/Payments/pay` (replace local wallet mock)

---

## 10. Appendices

### Appendix A: Full Feature Inventory & Directory Map

| # | Feature | Directory (absolute) | Sub-package misspellings |
|---|---|---|---|
| 1 | Splash | `E:\LLM testing\Sha8alny-front-end\lib\features\splash\` | — |
| 2 | Onboarding | `E:\LLM testing\Sha8alny-front-end\lib\features\onboarding\` | — |
| 3 | Auth | `E:\LLM testing\Sha8alny-front-end\lib\features\auth\` | — |
| 4 | Home | `E:\LLM testing\Sha8alny-front-end\lib\features\home\` | — |
| 5 | Opportunities | `E:\LLM testing\Sha8alny-front-end\lib\features\opportunities\` | `datasourse` (misspelled) |
| 6 | Apply Form | `E:\LLM testing\Sha8alny-front-end\lib\features\apply_form\` | — |
| 7 | Profile | `E:\LLM testing\Sha8alny-front-end\lib\features\profile\` | — |
| 8 | Progress | `E:\LLM testing\Sha8alny-front-end\lib\features\progress\` | — |
| 9 | Chat | `E:\LLM testing\Sha8alny-front-end\lib\features\chat\` | — |
| 10 | Notifications | `E:\LLM testing\Sha8alny-front-end\lib\features\notifications\` | — |
| 11 | Save Opportunities | `E:\LLM testing\Sha8alny-front-end\lib\features\save_opportunities\` | `domin` (misspelled) |
| 12 | Setting | `E:\LLM testing\Sha8alny-front-end\lib\features\setting\` | — |
| 13 | Maintenance (core) | `E:\LLM testing\Sha8alny-front-end\lib\core\maintenance\` | — |

### Appendix B: Every Cubit/State

| Feature | Cubit | States |
|---|---|---|
| Auth | `AuthCubit` | `AuthInitial`, `AuthLoading`, `AuthSignInSuccess`, `AuthSignUpSuccess`, `AuthFailure`, `AuthLoggedOut`, `UserCached`, `PasswordResetEmailSent`, `PasswordUpdateSuccess` |
| Home | `HomeCubit` | `HomeInitial`, `HomeLoading`, `HomeSuccess`, `HomeFailure` |
| Home Search | `SearchCubit` | `SearchInitial`, `SearchLoading`, `SearchUpdated`, `SearchSuccess`, `SearchFailure` |
| Opportunities | `OpportunitiesCubit` | `OpportunitiesInitial`, `OpportunitiesLoading`, `OpportunitiesSuccess`, `OpportunitiesFailure` |
| Opportunities | `AppliedOpportunitiesCubit` | `AppliedOpportunitiesInitial`, `AppliedOpportunitiesLoading`, `AppliedOpportunitiesLoaded`, `AppliedOpportunitiesError` |
| Opportunities | `OpportunityStatusCubit` | `OpportunityStatusInitial`, `OpportunityStatusLoading`, `OpportunityStatusLoaded`, `OpportunityStatusError` |
| Apply Form | `ApplicationCubit` | `ApplicationInitial`, `ApplicationLoading`, `ApplicationSuccess`, `ApplicationFailure`, `ApplicationResumesLoaded` |
| Profile | `ProfileCubit` | `ProfileInitial`, `ProfileLoading`, `ProfileLoaded`, `ProfileFailure` |
| Progress | `ProgressCubit` | `ProgressInitial`, `ProgressLoading`, `ProgressSuccess`, `ProgressFailure` |
| Chat | `ChatCubit` | `ChatInitial`, `ChatLoading`, `ChatLoaded`, `ChatMessagesLoading`, `ChatMessagesLoaded`, `ChatError`, `ChatCreated` |
| Chat Search | `UserSearchCubit` | (search-specific states) |
| Notifications | `NotificationsCubit` | `NotificationsInitial`, `NotificationsLoading`, `NotificationsSuccess`, `NotificationsFailure` (**STUB**) |
| Save Opps | `SaveOppCubit` | `SaveOppState.initial()`, `SaveOppLoading`, `SaveOppSuccess`, `SaveOppFailure` |
| Setting | `SettingCubit` | `SettingInitial`, `SettingLoading`, `SettingSuccess`, `SettingFailure`, `SettingSignedOut` |
| Global | `LocaleCubit` | `LocaleState(Locale)` |
| Global | `ThemeCubit` | `ThemeState(ThemeMode)` |
| Global | `InternetCubit` | `InternetState(InternetStatus.connected/disconnected)` |

### Appendix C: Every Data Source & Its Current Backend

| Feature | DataSource Class | Current Backend | Status |
|---|---|---|---|
| Auth (Supabase) | `SupaBaseAuthDatasource` | Supabase Auth SDK | Active default |
| Auth (Firebase) | `AuthFirebaseDataSource` | Firebase Auth | Alternate |
| Auth user (Supabase) | `UserSupabaseDatasource` | Supabase `user` table | Active default |
| Auth user (Firebase) | `UserFirestoreDataSource` | Firestore | Alternate |
| Auth local | `UserSharedPrefsDataSource` | `shared_preferences` | Active (session cache) |
| Home | `HomeRemoteDataSourceImpl` | Supabase `opportunity` + `student_profile` | Active |
| Opportunities | `OpportunitiesRemoteDatasourceImpl` | Supabase `opportunity`, `modules`, `application` | Active |
| Apply Form | `ApplyFormRemoteDataSourceImpl` | Supabase Storage (`applications`) + `application` table | Active |
| Profile | `ProfileRemoteDataSourceImplSupabase` | Supabase `student_profile`, `student_skills`, Storage (`resumes`) | Active |
| Progress | `ProgressRemoteDataSourceImpl` | Supabase `assignment`, `completed_opportunity`, `opportunity` | Active |
| Chat | `ChatRemoteDataSourceImpl` | Supabase `chats`, `messages` tables + Realtime | Active |
| Notifications | `NotificationsRemoteDataSource` (abstract) | **STUB — no implementation** | ❌ Dead |
| Save Opps | `SaveOppDataSourceImpl` | Supabase `saved_opportunities` | Active |
| Setting | `SettingRemoteDataSource` (abstract) | **STUB — no implementation** | ❌ Dead |
| Maintenance | `MaintenanceRemoteDataSourceImpl` | Supabase `app_config` | Active |

### Appendix D: Package / Dependency Matrix with Migration Relevance

| Package | Purpose | Action on .NET migration |
|---|---|---|
| `flutter_bloc` | Cubit state management | **Keep** |
| `dio` | Dormant HTTP client | **Activate** — set baseUrl, add JWT interceptor |
| `shared_preferences` | Local key-value store | **Keep** — add `authToken` key |
| `dartz` | Functional Either type | **Keep** |
| `get_it` | DI container | **Keep** |
| `equatable` | Value equality | **Keep** |
| `freezed` / `json_serializable` | Code gen | **Keep** — generate .NET DTO models |
| `easy_localization` | i18n | **Keep** — sync locale to `/api/Settings` |
| `supabase_flutter` | Live data backend | **Remove per-feature** as each migrates |
| `firebase_core` | Firebase init | **Keep** (needed for FCM) |
| `firebase_auth` | Firebase auth | **Remove** once auth migrates |
| `cloud_firestore` | Firestore | **Remove** once features migrate |
| `firebase_messaging` | FCM push | **Keep** (backend has no mobile push endpoint yet) |
| `flutter_local_notifications` | Local notification display | **Keep** |
| `googleapis_auth` + Dio | FCM v1 HTTP send | **Keep** |
| `flutter_dotenv` | `.env` config | **Keep** — add `NET_API_BASE_URL` |
| `file_picker` | File selection | **Keep** — redirect uploads to `/api/Media` |
| `connectivity_plus` | Network state | **Keep** |
| `app_links` | Deep links | **Keep** — deep link IDs are already `int` |
| `mobile_scanner` | QR scanner | **Keep** (wallet QR) |
| `cached_network_image` | Image cache | **Keep** |
| `syncfusion_flutter_pdfviewer` | PDF viewer (CV) | **Keep** |
| `url_launcher` | Open URLs | **Keep** |
| `share_plus` | Share content | **Keep** |
| `mask_text_input_formatter` | Input masking | **Keep** |
| `pinput` | OTP input | **Keep** — for email verification screen |
| `smooth_page_indicator` | Onboarding pager | **Keep** |
| `curved_navigation_bar` | Bottom nav | **Keep** |
| `flutter_screenutil` | Responsive sizing | **Keep** |
| `flutter_animate` / `animate_do` / `lottie` / `animator` | Animation | **Keep** |
| `google_fonts` | Typography | **Keep** |
| `timeago` | Relative time display | **Keep** |
| `package_info_plus` | App version | **Keep** |
| `device_info_plus` | Device info | **Keep** |
| `fluttertoast` | Toast messages | **Keep** |
| `percent_indicator` | Progress indicators | **Keep** |
| `timer_button` | Resend OTP button | **Keep** |

### Appendix E: Known Naming / Schema Mismatches Quick-Reference

| Concern | Mobile (Supabase) | Backend (.NET) |
|---|---|---|
| User display name | `full_name` (single column) | `FirstName` + `LastName` (separate) |
| User identifier in auth | `auth_id` (Supabase UUID string) | `UserID` (int) in JWT `NameIdentifier` claim |
| User role field | Absent from `UserEntity` | `UserType` enum: `Student/Company/University/Admin` |
| FCM token storage | Supabase `user.fcm_token` | Not on backend `User` entity |
| Opportunity ID type | `int` | `ProjectID` (int) — ✅ matches |
| Conversation ID type | `String` (Supabase UUID) | `ConversationID` (int) — ❌ mismatch |
| Message ID type | `String` (Supabase UUID) | `MessageID` (int) — ❌ mismatch |
| Chat participants | `participants: String[]` (UUID array) | `ConversationParticipant` join table — ❌ structural difference |
| Project type field | `type` (free-text string) | `ProjectType` enum (camelCase JSON) |
| Application status | Custom Supabase strings | `ApplicationStatus` enum: `Submit/Pending/UnderReview/Accepted/InProgress/Completed/Rejected/Withdrawn` |
| Module model | `ModuleModel` (Supabase `modules`) | `ProjectModule` with `Weight`, `OrderIndex` |
| Skills on student | `StudentSkillModel` (free-text name) | `StudentSkill` with FK to `Skill` table |
| Media/file uploads | Supabase Storage buckets | `wwwroot/uploads/{folder}/` via `/api/Media` exclusively |
| Response envelope | Raw JSON | `ServiceResponse<T>` `{ success, message, data, errors }` |
| Column case | `snake_case` | `camelCase` (System.Text.Json default) |
| Primary key naming | `id` | `{EntityName}ID` (e.g., `studentID`) |

---

> **Last Updated:** June 2026
> **Audited from:** `E:\LLM testing\Sha8alny-front-end` (Flutter 3.32.6)
> **Cross-reference:** `E:\LLM testing\Sha8alny\context-backend.md`
> **Maintainer:** Sha8alny Development Team
