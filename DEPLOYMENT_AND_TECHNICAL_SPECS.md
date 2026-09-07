# Sha8alny (شغلني) — Deployment & Technical Specifications

**Document Version:** 1.0 — September 2026
**Prepared for:** University/Institute IT Department, Training Unit, Advisory Board
**System:** Sha8alny — Freelancing & Field Training Platform (ASP.NET Core Web API)

---

## 1. System Architecture & Cloud Topology

Sha8alny is a **100% on-cloud web application**. All functionality — including the Training Unit's deliverables review, applicant ranking, record filtering, and duration crediting — is accessible through modern browsers (Chrome, Edge, Firefox, Safari). **No on-premise hardware, server rooms, or client-side software installations are required** for staff, students, companies, or administrators.

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLOUD (Google Cloud)                     │
│                                                                 │
│  ┌──────────────┐    ┌─────────────────────┐    ┌────────────┐ │
│  │   Browser    │───▶│  Cloud Run Service  │───▶│  SQL Server│ │
│  │  (any user)  │◀───│  (Dockerized .NET 9 │    │  (cloud-   │ │
│  └──────────────┘    │   Web API, Kestrel) │    │   hosted)  │ │
│                      └─────────┬───────────┘    └────────────┘ │
│                              │  │                              │
│              ┌───────────────┘  └──────────────┐               │
│              ▼                                 ▼               │
│      ┌───────────────┐                 ┌──────────────┐        │
│      │  Static Files │                 │   SignalR    │        │
│      │  (wwwroot/    │                 │  Real-time   │        │
│      │   uploads/)   │                 │  Hub (/hubs) │        │
│      └───────────────┘                 └──────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

**Key properties:**

| Property | Specification |
|---|---|
| Delivery model | Cloud-hosted container (Google Cloud Run), Docker image |
| Access | Modern web browser (API + Swagger UI at site root); mobile apps via REST + SignalR |
| On-premise footprint | **None** — zero client installations for staff/admins |
| Real-time | ASP.NET Core SignalR hub (`/hubs/notifications`) for live notifications and chat delivery |
| API documentation | Swagger / OpenAPI UI served at the application root (`/`) with full XML code documentation |
| Scalability | Cloud Run auto-scaling (container instances scale with demand) |

---

## 2. Platform & Runtime Requirements

| Component | Requirement |
|---|---|
| **Runtime** | .NET 9 (ASP.NET Core Web API), target framework `net9.0` |
| **Web server** | Kestrel (in-container), fronted by a reverse proxy |
| **Reverse proxy** | Nginx (or Cloud Run's built-in HTTPS load balancer / IIS if self-hosted) — terminates SSL/TLS and forwards to Kestrel |
| **SSL/TLS** | TLS termination at the proxy/load balancer; HTTPS redirect enforced in non-development environments; certificates managed by the cloud platform |
| **Containerization** | Docker (multi-stage build), deployed via Google Cloud Run + `gcloud CLI` |
| **Ports** | Kestrel listens on the container's configured port (Cloud Run injects `PORT`); external traffic is 443/HTTPS only |

**Reverse proxy notes (Nginx example):** proxy HTTP traffic to the Kestrel container, forward the `Authorization` header, and allow WebSocket upgrades for the SignalR hub path (`/hubs/notifications`). JWT tokens for WebSocket connections are accepted from the query string (`access_token`) as well as the header.

---

## 3. Database Engine & Specifications

| Item | Specification |
|---|---|
| **Engine** | Microsoft SQL Server 2019 or 2022 |
| **Edition** | Standard Edition for production; Developer Edition acceptable for testing/staging |
| **ORM / schema management** | Entity Framework Core 9 — migrations applied automatically at service startup |
| **Collation** | `SQL_Latin1_General_CP1_CI_AS` (case-insensitive) |
| **Estimated initial storage** | ~250 MB database file (schema + seed data + first academic year of records); provision 2–5 GB to allow comfortable growth |
| **Growth drivers** | Application, training submission, review, and messaging records; media files are **not** stored in the database (see Section 4) |

**Backup strategy (built into the platform):**

- An automated **BackupWorker background service** creates full database backups every **24 hours** with **7-day retention**.
- On-demand backups can be triggered by an Admin via `POST /api/Maintenance/backup`.
- Backups are written to the container's `backups/` volume; for production durability, mount a persistent Cloud Storage bucket or enable the cloud provider's managed SQL backups (daily full + point-in-time log backups) in addition to the built-in worker.

---

## 4. Storage & Media Specifications

All file uploads — including training completion deliverables (PDFs, presentations, company evaluations, surveys, certificates) — are handled exclusively by the **`/api/Media`** endpoints. The database stores only the resulting URL strings.

| Item | Specification |
|---|---|
| **Storage location** | `wwwroot/uploads/{folder}/` on the service's filesystem (mount a persistent volume or Cloud Storage-backed path for production) |
| **Suggested capacity** | 50–100 GB initially (approximately 20,000–40,000 documents at average size) |
| **Allowed file types** | `.jpg`, `.jpeg`, `.png`, `.gif`, `.pdf` |
| **Maximum file size** | 5 MB per file |
| **Image processing** | Automatic resize (max 1920 px wide) and conversion to WebP; 300 px thumbnails generated |
| **Directory permissions** | The service account running the container needs **read/write** on `wwwroot/uploads/` and `backups/`; no execute permission required |
| **Virus scanning** | ClamAV integration is present but currently disabled (stub returns clean); recommended to enable with a containerized ClamAV instance before production go-live |

**Training deliverables:** students upload the 5 required documents (Certificate, Report, Presentation, Company Evaluation, Student Survey) through `/api/Media` first, then submit the returned URLs to `POST /api/TrainingSubmissions/deliverables`. Re-uploads of rejected documents replace only the affected file.

---

## 5. Security & Authorization

| Layer | Implementation |
|---|---|
| **Authentication** | JWT Bearer tokens (symmetric key, HS256); issuer `Sha8alny`, audience `Sha8alnyUsers`, 60-minute token lifetime |
| **Claims** | `NameIdentifier` (UserID), `Email`, `Role` (UserType) |
| **Authorization** | Role-based access control enforced at two levels: `[Authorize(Roles = ...)]` on controller actions **plus** ownership/role re-verification inside the service layer |
| **Roles** | `Student` (apply, submit deliverables, re-upload), `Company` (post opportunities, review applications, verify training), `Admin` / `University` (Training Unit review, duration override, record filtering), plus a planned `University` verification role |
| **Password storage** | BCrypt hashing |
| **CORS** | Configured "AllowAll" policy (any origin, method, header, with credentials). **For production**, restrict `SetIsOriginAllowed` to the university's official domains |
| **Rate limiting** | Not yet enabled. Recommended before public launch: ASP.NET Core rate-limiting middleware on authentication and application-submission endpoints to prevent abuse |
| **Secrets management** | JWT key, SMTP credentials, connection strings, and webhook URLs are injected via environment variables (`cloudrun-env.yaml` for Cloud Run) — never stored in source control |
| **Audit trail** | Per-request timing/HTTP logging middleware plus a Discord webhook logger for errors and significant events |

---

## 6. Training Lifecycle & Deliverables Workflow (Functional Summary)

The platform automates the complete field-training lifecycle from posting to credit:

### 6.1 GPA Eligibility Enforcement
Companies can mark a Training/Internship opportunity as **GPA-required** with a minimum GPA (stored to two decimal places). At application time the system automatically rejects students whose GPA is missing or below the threshold — no manual screening required. Companies and Admins can also view **applicants ranked by GPA** (`GET /api/Projects/{id}/applicants/ranked-by-gpa`) for merit-based selection.

### 6.2 Five-Deliverable Dual-Approval Workflow
1. **Submission** — the student uploads 5 documents (Certificate, Report, Presentation, Company Evaluation, Student Survey) via `/api/Media` and submits them in one request. Five per-file records are created in `Pending` state.
2. **Academic review (Training Unit)** — admins approve or reject **each file individually** via `POST /api/TrainingSubmissions/{id}/review`. Every rejection requires a specific reason (e.g. "Presentation slide 3 is missing").
   - All 5 approved ⇒ submission moves to `AdminApproved` and awaits company verification.
   - Any rejected ⇒ overall status `Rejected`, per-file reasons preserved.
3. **Targeted re-upload** — the student re-submits **only the rejected file** (`PUT /api/TrainingSubmissions/{id}/reupload`); approved documents are never touched or lost. The submission returns to `Pending` for re-review.
4. **Industry verification (Company)** — the company confirms the training took place (`PUT /api/TrainingSubmissions/{id}/company-verify`).
5. **Completion** — when both approvals are in place the submission becomes `FullyCompleted` and training credit is calculated automatically (below).

### 6.3 Duration Credit Calculation (6-Hour Standard Training Day)
Durations may be listed in **days or hours**. Hours are converted using the college's 6-hour standard training day:

```
CalculatedDays = ⌈ TotalHours / 6.0 ⌉
```

At final completion, the credited days are resolved by priority:

1. **Admin override** — `ApprovedDuration` (1–365 days), set by the Training Unit when the certificate proves a duration different from the listing (`PUT /api/TrainingSubmissions/{id}/override-duration`);
2. **Declared days** — the student's declared `TrainingDays`;
3. **Calculated equivalent** — derived from the project listing's duration and unit.

The resolved amount is credited **once** to the student's cumulative `TotalInternshipDays` balance (double-crediting is technically prevented), and the application and project statuses are updated to `Completed`/`Complete` automatically.

### 6.4 Dynamic Record Filtering & Statistics
The Training Unit can query historical training records across **combined or standalone criteria** — company, date range (e.g. 2020–2024), academic department, academic year, and opportunity type (Training vs Internship) — with server-side pagination:

```
GET /api/TrainingSubmissions/records/filter?StartDate=2020-01-01&EndDate=2024-12-31&DepartmentId=3&PageNumber=1&PageSize=10
```

Each record includes the student's academic profile (GPA, department, university), the opportunity details, submission status, and the final credited days — providing the data foundation for institutional reporting and advisory-board statistics.

---

## 7. Operational Notes

| Topic | Detail |
|---|---|
| **Swagger / API reference** | Interactive API documentation with full endpoint descriptions, parameter docs, and response codes is available at the service root URL — no separate documentation portal to maintain |
| **Maintenance mode** | Admins can toggle a platform-wide maintenance screen and a minimum-supported-version gate for the mobile apps via `/api/Maintenance/config` |
| **Announcements** | Platform-wide announcements (with optional image/link) are broadcast to all users via `/api/Announcements` |
| **Monitoring** | Structured application logging; Discord webhook alerts for errors; request timing for latency tracking. Recommended additions for production: health checks endpoint and Cloud Monitoring dashboards |
| **Deployment** | `gcloud run deploy` from the Docker image; environment-specific configuration via `cloudrun-env.yaml`; database migrations apply automatically on startup |

---

> **Summary for the Advisory Board:** Sha8alny requires no institutional hardware or installed software. The university's obligations are limited to (a) providing browser access and user accounts to Training Unit staff, (b) allocating the cloud SQL Server database and file storage, and (c) designating system administrators. All training workflow steps — GPA screening, five-document review with per-file rejection reasons, targeted re-uploads, dual approval, duration conversion, and credit calculation — are fully automated and auditable.
