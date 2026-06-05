# Unit 19: CI/CD, Testing & Cloud Deployment

> **Before reading this unit:** You should have read Unit 16 (Docker — containers and docker-compose) and Unit 6 (JWT authentication). This unit explains how code gets from your laptop to production automatically, how the API is tested, and how Google Cloud Run hosts the running application.

---

## 19.1 The Big Picture: What Happens When You Push Code

The moment a developer pushes a commit to the `master` branch on GitHub, a fully automated sequence begins:

```
Developer pushes to master
    ↓
GitHub detects the push
    ↓
GitHub Actions starts a virtual machine (ubuntu-latest)
    ↓
Job 1: Build & Test
  → .NET 9 is installed
  → dotnet restore (downloads NuGet packages)
  → dotnet build (compiles all 7 projects)
  → dotnet test (runs xUnit integration tests)
    ↓ (only if tests pass)
Job 2: Deploy to Cloud Run
  → Authenticate with Google Cloud
  → docker build (creates the API image)
  → docker push (uploads image to Google Container Registry)
  → gcloud deploy (replaces the running Cloud Run service with new version)
    ↓
Production is updated — zero manual steps
```

This is called **CI/CD**: Continuous Integration (build + test automatically on every push) + Continuous Deployment (deploy automatically when tests pass).

---

## 19.2 The CI/CD File — `main-ci-cd.yml`

The entire pipeline is defined in one YAML file at the repo root. This is a GitHub Actions **workflow**.

```yaml
name: Sha8alny API CI/CD Pipeline

on:
  push:
    branches: [ "master", "develop" ]   # ← run on push to these branches
  pull_request:
    branches: [ "master" ]              # ← run on PR targeting master

env:
  PROJECT_ID: sha8alny-grad-project
  REGION: us-central1
  SERVICE_NAME: sha8alny-api
  IMAGE_NAME: gcr.io/sha8alny-grad-project/api-image
```

**Why two triggers?**
- `push` to `master`/`develop`: Run on direct commits — catches issues immediately
- `pull_request` to `master`: Run when someone opens a PR — validates code *before* it merges

The `env` block defines variables used throughout the file so they're not repeated (DRY principle):
- `REGION: us-central1` — Google Cloud's Iowa data center (lowest latency for US traffic)
- `SERVICE_NAME: sha8alny-api` — the Cloud Run service name
- `IMAGE_NAME: gcr.io/...` — the full image path in Google Container Registry

---

## 19.3 Job 1 — Build & Test

```yaml
jobs:
  build-and-test:
    runs-on: ubuntu-latest   # ← uses a fresh Linux VM
    steps:
    - uses: actions/checkout@v4              # download repo code
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'             # install .NET 9 SDK

    - run: dotnet restore Sh8lnySolution.sln # download NuGet packages
    - run: dotnet build Sh8lnySolution.sln --configuration Release --no-restore
    - run: dotnet test Tests/Sh8lny.IntegrationTests/Sh8lny.IntegrationTests.csproj
             --configuration Release
             --no-build
             --verbosity normal
             --logger html                  # produces an HTML test report
```

**Key points:**

1. **`runs-on: ubuntu-latest`** — each job gets a fresh virtual machine. Nothing from a previous run persists. If the build worked last week and fails today, something actually changed.

2. **`--no-restore` and `--no-build`** — these flags chain the steps efficiently. `restore` downloads packages, `build` compiles (skips restore because it was already done), `test` runs (skips build because it was already done). Saves ~2 minutes.

3. **`--configuration Release`** — tests run in Release mode, the same configuration used in production. Debug mode has extra checks that can hide production issues.

4. **`--logger html`** — produces an HTML report file alongside the test results. If a test fails, the file contains the exact failure message.

**What happens if the build fails?** GitHub marks the commit/PR as "failed" with a red ✗. The `deploy-to-cloud-run` job does **not** run — you cannot deploy broken code. The developer gets an email notification.

---

## 19.4 Job 2 — Deploy to Google Cloud Run

```yaml
deploy-to-cloud-run:
  needs: build-and-test          # ← only runs if Job 1 succeeds
  runs-on: ubuntu-latest
  if: github.ref == 'refs/heads/main' && github.event_name == 'push'
  # ↑ Only deploy on direct push to main, not on PRs
```

**The `needs: build-and-test` dependency** creates the guarantee: tests must pass before deployment happens. This is the core of CI/CD — the pipeline gates deployment on quality.

**The `if:` condition** prevents automatic deployment from PRs. When a developer opens a PR against master, Job 1 runs (validate the code) but Job 2 does not (don't deploy the PR to production — it hasn't been merged yet).

**Steps inside the deployment job:**

```yaml
# Step 1: Authenticate with Google Cloud using a service account key
- uses: google-github-actions/auth@v2
  with:
    credentials_json: '${{ secrets.GCP_CREDENTIALS }}'
```

`secrets.GCP_CREDENTIALS` is a GitHub **secret** — a JSON key file for a Google Cloud service account, stored encrypted in the GitHub repository settings. The workflow reads it at runtime but it is never visible in logs or source code.

```yaml
# Step 2: Allow Docker to push to Google Container Registry
- run: gcloud auth configure-docker
```

Google Container Registry (GCR) is Google's private Docker image registry. `configure-docker` sets up credentials so `docker push` can authenticate.

```yaml
# Step 3: Build the Docker image, tagged with the git commit SHA
- run: docker build -t gcr.io/sha8alny-grad-project/api-image:${{ github.sha }} -f Dockerfile .
```

`github.sha` is the full 40-character git commit hash (e.g., `9d8485e...`). Using the commit SHA as the image tag means **every deployment is traceable**: you can look at any running Cloud Run revision and know exactly which commit it came from. Tags like `:latest` lose this traceability.

```yaml
# Step 4: Upload the image to Google Container Registry
- run: docker push gcr.io/sha8alny-grad-project/api-image:${{ github.sha }}
```

This uploads the image from the GitHub Actions VM to GCR. After this step, the image is stored in Google's infrastructure — Cloud Run will pull it from there.

```yaml
# Step 5: Deploy the new image to Cloud Run
- uses: google-github-actions/deploy-cloudrun@v2
  with:
    service: sha8alny-api
    region: us-central1
    image: gcr.io/sha8alny-grad-project/api-image:${{ github.sha }}
    flags: '--allow-unauthenticated --port=8080'
```

`--allow-unauthenticated` — Cloud Run normally requires Google Cloud IAM authentication to call a service. This flag removes that requirement, making the API publicly accessible on the internet. Without it, only other Google Cloud services could call it.

`--port=8080` — tells Cloud Run which port the container listens on. This matches the `EXPOSE 8080` in the Dockerfile and ASP.NET Core's default Kestrel port inside a container.

---

## 19.5 What Google Cloud Run Actually Is

Cloud Run is Google's **serverless container platform**. You give it a Docker image; it handles everything else:

| What Cloud Run manages | What you manage |
|---|---|
| Virtual machines | The Docker image |
| Scaling (0 to N containers) | The app's code and configuration |
| Load balancing | Environment variables and secrets |
| TLS/HTTPS certificates | |
| Health checks and restarts | |

**Serverless** means you do not provision servers. When no requests arrive, Cloud Run scales to zero — no containers running, no compute cost. When a request arrives, a container starts in ~1–2 seconds and handles it. This is much cheaper than a VM running 24/7.

**The container lifecycle:**
1. HTTP request arrives at `sha8alny-api.run.app`
2. Cloud Run starts a container from the stored image (or reuses a warm one)
3. Container runs `dotnet Sh8lny.Web.dll` (the `ENTRYPOINT`)
4. ASP.NET Core runs `MigrateAsync()` and `SeedAsync()` at startup
5. Kestrel starts listening on port 8080
6. The request is routed to the running container
7. After idle period, Cloud Run may scale back to zero

**Where is the database?** SQL Server runs as a second Cloud Run service (with a persistent volume), not on the same container as the API. The connection string points to the database service's internal hostname.

---

## 19.6 The Complete Delivery Pipeline

Here is the full journey from code to production, showing every system involved:

```
Developer's Laptop
    ↓ git push
GitHub Repository (github.com/ninjam5/Sha8alny)
    ↓ webhook triggers workflow
GitHub Actions Runner (ubuntu-latest VM)
    ├── Job 1: Build & Test
    │   ├── checkout code
    │   ├── dotnet restore (NuGet.org → local cache)
    │   ├── dotnet build (all 7 projects → Release binaries)
    │   └── dotnet test (xUnit test runner)
    │       └── [if tests FAIL: stop here, notify developer]
    │
    └── Job 2: Deploy (only if Job 1 passed)
        ├── docker build → creates image tagged with :git-sha
        ├── docker push → uploads to gcr.io/sha8alny-grad-project/
        └── deploy-cloudrun → Cloud Run pulls image, rolls out new revision
                                    ↓
                         Cloud Run Service: sha8alny-api
                         Region: us-central1
                         Port: 8080
                         Zero-downtime rollout (old revision handles requests
                         until new revision is healthy)
                                    ↓
                         Public URL: https://sha8alny-api.run.app
```

---

## 19.7 Zero-Downtime Deployments

Cloud Run performs **zero-downtime deployments** automatically:

1. New revision is deployed and starts accepting a small percentage of traffic
2. Cloud Run monitors the new revision (health checks)
3. If healthy: traffic shifts 100% to new revision
4. If unhealthy: traffic rolls back to the old revision automatically
5. Old revision stays available briefly before being scaled down

This means users never see a "server is restarting" error during deployments.

---

## 19.8 Testing — xUnit and Integration Tests

The CI pipeline runs:
```
dotnet test Tests/Sh8lny.IntegrationTests/Sh8lny.IntegrationTests.csproj
```

**xUnit** is the test framework chosen for Sha8alny (the most common choice in .NET alongside NUnit and MSTest).

**Integration tests vs unit tests:**

| | Unit tests | Integration tests |
|---|---|---|
| What is tested? | One class/method in isolation | Multiple components working together |
| External dependencies? | Mocked/faked | Real (database, HTTP) |
| Speed | Very fast (milliseconds) | Slower (seconds) |
| What they catch | Logic errors in one unit | Wiring errors, SQL errors, config issues |

The test project uses `CustomWebApplicationFactory` — a test fixture that spins up a real ASP.NET Core API in memory (no need for a real server), connected to a real database (or an in-memory database for isolation). Tests can make HTTP calls to the API exactly as the Flutter app would, then assert on the database state.

**Example of what an integration test validates:**
```
1. Register a student → verify HTTP 200 + JWT returned
2. Login with same credentials → verify HTTP 200 + JWT returned
3. Login with wrong password → verify HTTP 401
4. Create a student profile → verify database row exists
5. Apply to a project → verify Application row created + status = Pending
```

These tests catch the kinds of errors that unit tests miss: a middleware not registered in `Program.cs`, a FluentAPI constraint blocking an insert, or a DI registration missing its interface.

---

## 19.9 The `MaintenanceController` — On-Demand Backups

While `BackupWorker` runs automatically every 24 hours, the Admin sometimes needs to trigger a backup manually — before a risky database migration, or before a major demo. That is what `MaintenanceController` is for:

```
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MaintenanceController
```

Three endpoints, all Admin-only:

| Endpoint | What it does |
|---|---|
| `POST /api/Maintenance/backup` | Triggers an immediate `BackupService.CreateBackupAsync()` and returns the filename |
| `GET /api/Maintenance/backups` | Lists all available `.bak` files |
| `DELETE /api/Maintenance/backups/purge?retentionDays=7` | Deletes backup files older than N days |

The `POST /backup` endpoint calls the same `IBackupService.CreateBackupAsync()` that `BackupWorker` calls on schedule — the same T-SQL `BACKUP DATABASE` command with `COMPRESSION` and `CHECKSUM`, followed by `RESTORE VERIFYONLY`. It returns:
```json
{
  "message": "Backup created and verified successfully.",
  "fileName": "Sh8lnyDB_backup_20260526_143215.bak"
}
```

If the backup fails (disk full, SQL Server error), the controller catches the exception and returns HTTP 500 with the error message — unlike `BackupWorker` which swallows errors and continues. The admin needs to know if their manual backup failed.

---

## 19.10 GitHub Secrets — Keeping Credentials Out of the Code

The CI/CD pipeline needs credentials to deploy to Google Cloud. These cannot be in the repository (anyone who clones the repo would have production access).

GitHub Secrets are **encrypted key-value pairs** stored in the repository settings, accessible only during workflow runs:

| Secret | What it contains |
|---|---|
| `GCP_CREDENTIALS` | JSON key file for a Google Cloud service account |

The service account has only the permissions it needs:
- Push to Google Container Registry
- Deploy to Cloud Run

This is the **principle of least privilege** — the CI/CD pipeline cannot do anything beyond what is required to deploy.

In the workflow, the secret is referenced as `${{ secrets.GCP_CREDENTIALS }}`. GitHub injects the value at runtime and masks it in logs — it never appears in plain text.

---

## 19.11 What to Say in Your Defense

- "We have a complete CI/CD pipeline using GitHub Actions. Every push to master triggers two jobs: build-and-test (compiles all 7 projects and runs xUnit integration tests) and deploy-to-cloud-run (builds a Docker image, pushes it to Google Container Registry, and deploys it to Cloud Run). The deploy job only runs if tests pass."
- "Each Docker image is tagged with the git commit SHA — not with a generic `:latest` tag. This means every deployed version is traceable to the exact commit. If a bug is introduced, we know precisely which commit caused it."
- "Google Cloud Run is a serverless container platform. It manages virtual machines, scaling, load balancing, and HTTPS certificates automatically. We only manage the Docker image and configuration. Cloud Run can scale to zero when there's no traffic and scale out automatically under load."
- "We use `--allow-unauthenticated` on Cloud Run because our API uses JWT for authentication — Cloud Run's IAM layer would be a second authentication system on top of ours, which is redundant for a public API."
- "Integration tests use `CustomWebApplicationFactory` to spin up the real ASP.NET Core pipeline in memory and test end-to-end behavior: HTTP request → middleware → controller → service → database → response. This catches wiring errors that unit tests cannot."
- "`MaintenanceController` gives admins three manual backup operations: trigger an immediate backup, list existing backups, and purge backups older than N days. It uses the same `IBackupService` implementation as the automated nightly `BackupWorker` — no duplicated logic."
- "Sensitive credentials (the Google Cloud service account key) are stored as GitHub Secrets, never in source code. The CI/CD workflow reads them at runtime; they are masked in all logs."

---

## 19.12 Self-Check Questions

**Q1: What is CI/CD and how does it apply to Sha8alny?**
CI (Continuous Integration) means every code push is automatically compiled and tested. CD (Continuous Deployment) means passing tests trigger an automatic deployment to production. In Sha8alny, pushing to master triggers GitHub Actions to build all 7 projects, run integration tests, then — if tests pass — build a Docker image, push it to Google Container Registry, and deploy it to Cloud Run. No manual steps are required.

**Q2: Why is the Docker image tagged with `${{ github.sha }}` instead of `:latest`?**
The commit SHA uniquely identifies the exact code state. Using `:latest` means "the most recent build," which changes with every push and provides no traceability. With SHA tags, any running Cloud Run revision can be traced back to the exact commit, making it easy to correlate bugs with code changes and roll back to a specific known-good version.

**Q3: What does `needs: build-and-test` do in the CI/CD workflow?**
It creates a dependency between jobs. The `deploy-to-cloud-run` job will not start unless `build-and-test` completes successfully. This is the key gate: broken code cannot be deployed. If the build fails or any test fails, GitHub marks the commit as failed and no deployment occurs.

**Q4: What is Google Cloud Run and why is it "serverless"?**
Cloud Run runs Docker containers without requiring you to manage virtual machines. "Serverless" means the platform handles provisioning, scaling, load balancing, and HTTPS — you only provide the container image. Cloud Run scales to zero when idle (no traffic = no running containers = no compute cost) and scales out automatically under load. This is more cost-effective than a VM that runs 24/7.

**Q5: What does `--allow-unauthenticated` do in the Cloud Run deployment?**
By default, Cloud Run services require Google Cloud IAM authentication — only Google Cloud services with the right permissions can call them. `--allow-unauthenticated` removes this restriction, making the API publicly accessible on the internet. This is appropriate because the API uses its own JWT authentication system; adding Cloud Run's IAM layer on top would be redundant and would block the Flutter app from connecting.

**Q6: What is the difference between unit tests and integration tests, and why does Sha8alny use integration tests?**
Unit tests test one class or method in isolation, with all dependencies mocked. Integration tests test multiple components together using real dependencies (a real database, real middleware). Sha8alny uses integration tests because the most common failures are wiring errors — middleware not registered, DI not configured, SQL constraint violated — which unit tests cannot detect (they mock everything away). Integration tests catch these by running the actual `Program.cs` pipeline.

**Q7: How does `MaintenanceController` differ from `BackupWorker` in how it handles backup failures?**
`BackupWorker` catches exceptions, logs them, and continues — a failed backup in the nightly cycle should not crash the application. `MaintenanceController` lets the exception propagate and returns HTTP 500 with the error message — an admin who manually triggered a backup needs to know immediately if it failed, not discover it silently.

**Q8: What are GitHub Secrets and why are they used for `GCP_CREDENTIALS`?**
GitHub Secrets are encrypted values stored in repository settings, injected into workflows at runtime. They are never stored in source code and are masked in all CI/CD logs. `GCP_CREDENTIALS` contains the Google Cloud service account key that authorizes pushing images to GCR and deploying to Cloud Run. Storing this in the repo would give anyone who clones the repository production deployment access.
