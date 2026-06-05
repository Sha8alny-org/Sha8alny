# Unit 16: Background Services and Infrastructure — The Invisible Workers

> **Before reading this unit:** You should have read Unit 3 (request lifecycle — Program.cs, middleware, startup). This unit explains what the system does automatically, without a user making a request: backups, startup seeding, database migrations, logging infrastructure, and request performance tracking.

---

## 16.1 What "Background Services" Means

Most of the code in Sha8alny runs in response to an HTTP request: a user does something, the server responds. But some work must happen automatically, on a schedule, without a user triggering it.

ASP.NET Core provides `IHostedService` and its simpler subclass `BackgroundService` for exactly this. Classes that extend `BackgroundService` are registered in `Program.cs` and start running when the application starts, independently of any HTTP request.

Sha8alny has one `BackgroundService`: `BackupWorker`.

---

## 16.2 BackupWorker — Automated Database Backups

**What it does:** Creates a full SQL Server database backup every 24 hours and deletes old backups older than 7 days.

**How it runs:**

```csharp
public class BackupWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 2 minutes after app start before first backup
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        using var timer = new PeriodicTimer(_interval);  // default: 24 hours

        do
        {
            try
            {
                await RunBackupCycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BackupWorker error during backup cycle.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

The `PeriodicTimer` fires on each interval (24 hours by default). After each tick, the worker runs one backup cycle. If the backup fails, the error is logged but the loop continues — the worker does not crash, it just logs and waits for the next tick.

**The 2-minute startup delay** gives the application time to fully initialize (run migrations, seed data, start accepting requests) before the first backup runs.

**Configurable via `appsettings.json`:**
```json
{
  "Backup": {
    "IntervalHours": 24,
    "RetentionDays": 7
  }
}
```

---

## 16.3 BackupService — What Actually Happens During a Backup

`BackupWorker` calls `IBackupService.CreateBackupAsync()` which is implemented by `BackupService` in `Sh8lny.Persistence`.

The backup uses SQL Server's native `BACKUP DATABASE` command executed directly via EF Core's raw SQL capabilities:

```sql
BACKUP DATABASE [Sh8lnyDB]
TO DISK = N'/var/opt/mssql/backups/Sh8lnyDB_backup_20260526_020000.bak'
WITH FORMAT,
     INIT,
     NAME = N'Sh8lnyDB Full Backup 20260526_020000',
     COMPRESSION,
     CHECKSUM,
     STATS = 10
```

After creating the backup, the service runs:

```sql
RESTORE VERIFYONLY
FROM DISK = N'/var/opt/mssql/backups/Sh8lnyDB_backup_20260526_020000.bak'
WITH CHECKSUM
```

This verifies the backup file is not corrupted — "backup then verify" is a standard practice. A backup that cannot be restored is useless.

**Where files go:** `/var/opt/mssql/backups/` — the standard backup directory inside the SQL Server container on Google Cloud Run. Files are named `Sh8lnyDB_backup_YYYYMMDD_HHmmss.bak`.

**Scope per cycle:** `BackgroundService` is a Singleton (lives as long as the app). But `IBackupService` and `IDbContext` are Scoped (live only within one request/operation). The `BackupWorker` creates a DI scope for each backup cycle using `_serviceProvider.CreateScope()`, then resolves `IBackupService` from that scope. This is the correct pattern for long-running singletons that need scoped services.

**Purging:** After creating the backup, `BackupService.PurgeOldBackupsAsync(_retentionDays)` queries SQL Server's `msdb` database for backup history and deletes `.bak` files older than the retention policy (7 days default).

---

## 16.4 DbInitializer — Startup Data Seeding

`DbInitializer.SeedAsync(context)` runs once at startup (called from `Program.cs`) and populates the database with initial data if it is empty.

**What it seeds:**

1. **Skills (15 entries):** C#, ASP.NET Core, React, Angular, SQL, Python, JavaScript, TypeScript, Node.js, Flutter, Docker, Azure, Machine Learning, Figma, Git — each with a `SkillCategory`
2. **Universities:** Cairo University (Public), German University in Cairo / GUC (Private), American University in Cairo / AUC (International)
3. **Demo users:** An Admin, a demo Student, and a demo Company — all with password `"Password123!"` (BCrypt hashed)

**Idempotent:** Each section checks before inserting:

```csharp
if (!await context.Skills.AnyAsync())
{
    // Only seed if the Skills table is empty
    await context.Skills.AddRangeAsync(skills);
    await context.SaveChangesAsync();
}
```

Running the seeder twice is safe — it detects existing data and skips. This is important because `Program.cs` calls it every time the application starts, not just on first deploy.

---

## 16.5 Auto-Migrations on Startup

Immediately before seeding, `Program.cs` runs:

```csharp
await context.Database.MigrateAsync();
```

This applies any pending EF Core migrations to the database automatically when the application starts. On a fresh deployment to a new environment:
1. `MigrateAsync()` creates all tables, indexes, and constraints by applying 8 migrations in order
2. `DbInitializer.SeedAsync()` populates the empty tables with starter data

On subsequent startups, both are no-ops if nothing changed.

This "migrate on startup" approach is common for containerized applications where the database might be fresh on every deploy. The alternative — running migrations manually before deploying — is more error-prone in a CI/CD pipeline.

---

## 16.6 Docker — Packaging and Running the Application

### 16.6.1 What Docker Is

Without Docker, a common problem is: "it works on my machine." The developer's laptop has .NET 9, SQL Server, and the right environment variables configured. The production server might have a different OS, a different .NET version, or missing dependencies — and the app breaks.

**Docker** solves this with **containers**. A container is like a **shipping container** for software: it bundles the application, its runtime (.NET 9), its configuration, and all dependencies into one sealed, portable box. That box runs identically on a developer's laptop, a teammate's computer, or a production server — because the environment is packed inside the box, not borrowed from the host machine.

Docker is the tool that builds and runs these containers. A **Docker image** is the blueprint (the sealed box). A **Docker container** is a running instance of that image.

### 16.6.2 The Dockerfile — How the Image Is Built

The `Dockerfile` at the repo root defines how to build the Sha8alny image. It uses a **multi-stage build** — two separate Docker stages that keep the final image small:

```dockerfile
# ── Stage 1: base (slim runtime only) ──────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ── Stage 2: build (full SDK for compiling) ─────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy .csproj files first (layer caching — faster rebuilds)
COPY ["Sh8lny.Web/Sh8lny.Web.csproj", "Sh8lny.Web/"]
COPY ["Core/Sh8lny.Abstraction/Sh8lny.Abstraction.csproj", "Core/Sh8lny.Abstraction/"]
# ... (all 6 .csproj files)

RUN dotnet restore "./Sh8lny.Web/Sh8lny.Web.csproj"

COPY . .    # Copy all source code
RUN dotnet build "./Sh8lny.Web.csproj" -c Release -o /app/build

# ── Stage 3: publish ────────────────────────────────
FROM build AS publish
RUN dotnet publish "./Sh8lny.Web.csproj" -c Release -o /app/publish

# ── Stage 4: final (slim runtime + published output) ─
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sh8lny.Web.dll"]
```

**Why multi-stage?**

The .NET SDK image (used for compiling) is around 700 MB — it contains the compiler, NuGet tools, and build infrastructure. The ASP.NET Core runtime image is around 200 MB — it contains only what is needed to *run* the compiled app, not build it.

If you built in a single stage using the SDK image, every production container would carry 700 MB of build tools that are completely useless at runtime. Multi-stage builds solve this:
- Stage 2 (`build`) uses the big SDK to compile the code
- Stage 4 (`final`) starts fresh from the small runtime image and copies only the compiled output from Stage 3 (`publish`)
- Result: a ~200 MB production image instead of a ~700 MB one

**The `ENTRYPOINT`** — `["dotnet", "Sh8lny.Web.dll"]` — is the command that runs when the container starts. It is equivalent to running `dotnet Sh8lny.Web.dll` in the terminal.

### 16.6.3 docker-compose.yml — Running Everything Together

Running Sha8alny locally requires two things: the API and a SQL Server database. `docker-compose.yml` defines both as **services** and wires them together:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sha8alny-db
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Str0ng!P@ssw0rd2024
    ports:
      - "1433:1433"           # host:container
    volumes:
      - sqlserver_data:/var/opt/mssql
      - ./backups:/var/opt/mssql/backups   # ← backup files land here on host
    healthcheck:
      test: sqlcmd -Q "SELECT 1"
      interval: 10s
      retries: 10

  api:
    build:
      context: .
      dockerfile: Dockerfile   # ← builds the image from the Dockerfile above
    ports:
      - "5000:8080"            # host port 5000 → container port 8080
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=Sh8lnyDB;...
    depends_on:
      sqlserver:
        condition: service_healthy   # ← API waits until SQL Server is ready
    networks:
      - sha8alny-network
    restart: on-failure

networks:
  sha8alny-network:
    driver: bridge
```

**Key design decisions explained:**

**1. The network (`sha8alny-network`):** Both containers join the same Docker bridge network. Within this network, containers can reach each other by **service name** as if it were a hostname. This is why the API's connection string says `Server=sqlserver` — `sqlserver` is not a server address, it is the service name in `docker-compose.yml`, resolved by Docker's internal DNS to the database container's IP address.

**2. The healthcheck + `depends_on`:** SQL Server takes 10–30 seconds to fully start after the container launches. If the API starts immediately, it crashes trying to connect to a database that is not ready yet. The `healthcheck` runs `SELECT 1` every 10 seconds until SQL Server responds. `depends_on: condition: service_healthy` tells Docker to not start the API container until the healthcheck passes.

**3. The volume mapping (`./backups:/var/opt/mssql/backups`):** This maps the host machine's `./backups/` folder to `/var/opt/mssql/backups/` inside the SQL Server container. This is why `BackupService` writes to `/var/opt/mssql/backups/` — it writes inside the SQL Server container, but those files actually land in the `./backups/` folder on your local machine (or the Cloud Run host). This means backup files survive even if the container is destroyed and recreated.

**4. Port mapping (`"5000:8080"`):** The container listens on port 8080 internally (set by `ASPNETCORE_URLS=http://+:8080`). The `"5000:8080"` mapping exposes it as port 5000 on the host, so developers hit `http://localhost:5000` while the container internally uses 8080.

**5. `restart: on-failure`:** If the API crashes (an unhandled exception kills the process), Docker automatically restarts it. This prevents a single crash from taking the service down permanently.

### 16.6.4 One Command to Run Everything

A developer clones the repo and runs:

```bash
docker-compose up --build
```

Docker:
1. Builds the API image from the `Dockerfile`
2. Pulls the SQL Server 2022 image from Microsoft's registry
3. Starts both containers on `sha8alny-network`
4. Waits for SQL Server's healthcheck to pass
5. Starts the API container

Then `Program.cs` runs `MigrateAsync()` and `SeedAsync()` — tables are created and demo data is loaded. The API is live at `http://localhost:5000`.

No manual SQL Server installation, no manual .NET installation, no "works on my machine" problems.

---

## 16.7 Discord Webhook Logging — Seeing Logs Without SSH

**The problem:** In production on Google Cloud Run, you cannot easily read server logs in real-time unless you have GCP access and know where to look.

**The solution:** `DiscordWebhookLogger` — a custom `ILogger` implementation that sends log messages to a Discord channel webhook.

Every time something is logged at `LogLevel.Information` or above, the logger posts a formatted message to a Discord channel:

```
[2026-05-26 03:14:22 UTC] [Information]
Microsoft.Hosting.Lifetime: Application started. Press Ctrl+C to shut down.
```

**How it is built:**

```
DiscordWebhookLoggerProvider (ILoggerProvider)
  → implements ILoggerProvider.CreateLogger(categoryName)
  → returns DiscordWebhookLogger for each category

DiscordWebhookLogger (ILogger)
  → IsEnabled: true if WebhookUrl is set AND logLevel >= Information
  → Log(): formats message, sends HTTP POST to Discord webhook URL
```

The configuration in `appsettings.json`:
```json
{
  "DiscordSettings": {
    "WebhookUrl": "https://discord.com/api/webhooks/..."
  }
}
```

If `WebhookUrl` is not set, `IsEnabled` returns `false` and no Discord messages are sent — the logger gracefully disables itself.

**Discord's 1900-character limit:** Discord messages cannot exceed 2000 characters. The logger trims messages to 1900 characters with `"..."` appended, leaving headroom for the markdown code block formatting.

**Fire-and-forget:** The Discord HTTP call uses `_ = SendAsync(payload)` — it does not await the result. Logging failures are silently swallowed (the empty `catch {}` block). This is intentional: a logging failure should never crash the application.

---

## 16.8 Request Timing Middleware — Performance Monitoring

The request timing middleware lives inline in `Program.cs` (not a separate class):

```csharp
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    await next();
    stopwatch.Stop();
    
    var elapsed = stopwatch.ElapsedMilliseconds;
    var path = context.Request.Path;
    var method = context.Request.Method;
    var statusCode = context.Response.StatusCode;
    
    _logger.LogInformation(
        "{Method} {Path} responded {StatusCode} in {Elapsed}ms",
        method, path, statusCode, elapsed);
});
```

Every request — GET, POST, PUT, DELETE — is timed with a `Stopwatch`. The elapsed time in milliseconds is logged alongside the HTTP method, path, and response status code. This creates a performance log entry for every request:

```
GET /api/Projects responded 200 in 43ms
POST /api/Applications/apply responded 201 in 127ms
GET /api/Students/search responded 200 in 312ms
```

When this flows through the Discord logger, slow requests become visible in the Discord channel in real-time — without any special monitoring infrastructure.

---

## 16.9 What to Say in Your Defense

- "The `BackupWorker` is a `BackgroundService` that automatically creates a full SQL Server backup every 24 hours and purges backups older than 7 days. It waits 2 minutes after startup before the first backup and continues running on a schedule until the application shuts down."
- "Database backups use SQL Server's native `BACKUP DATABASE` T-SQL command with `COMPRESSION` and `CHECKSUM`. After each backup, `RESTORE VERIFYONLY` confirms the file is not corrupted. A backup that cannot be restored is worthless."
- "We auto-apply EF Core migrations on startup with `context.Database.MigrateAsync()`. On a new deployment, this creates all tables. On subsequent startups, it is a no-op if nothing changed."
- "`DbInitializer.SeedAsync` runs at startup and populates empty tables with starter data — skills, universities, and demo users. It checks `AnyAsync()` before inserting, making it safe to call repeatedly."
- "We use Docker to containerize the application. The `Dockerfile` uses a **multi-stage build**: the .NET SDK image (~700 MB) compiles the code, and the final image is based on the slim ASP.NET Core runtime (~200 MB) with only the compiled output copied into it. This keeps the production container lean — no build tools, no source code, just the runnable app."
- "`docker-compose.yml` defines two services: `sqlserver` (SQL Server 2022) and `api` (our application). They share a bridge network called `sha8alny-network`. Within that network, Docker's internal DNS lets the API reach the database using `Server=sqlserver` — the service name resolves to the container's IP address automatically."
- "We use a healthcheck on the `sqlserver` service (`SELECT 1` every 10 seconds) combined with `depends_on: condition: service_healthy` on the `api` service. This prevents the API from starting before SQL Server is fully ready — without it, the API would crash trying to connect to a database that hasn't finished initializing."
- "The volume mapping `./backups:/var/opt/mssql/backups` in `docker-compose.yml` is why backup files survive container restarts. `BackupService` writes to `/var/opt/mssql/backups/` inside the container, but Docker maps that path to the `./backups/` folder on the host machine. If the container is destroyed, the `.bak` files are still there."
- "We built a custom `ILogger` implementation (`DiscordWebhookLogger`) that sends log messages to a Discord channel. This gives us real-time server monitoring without needing SSH access to the production container."
- "Request timing middleware in `Program.cs` uses `Stopwatch` to measure every request's duration and logs `{Method} {Path} {StatusCode} in {elapsed}ms`. Combined with the Discord logger, slow endpoints become visible immediately."

---

## 16.10 Self-Check Questions

**Q1: What is `BackgroundService` and what makes `BackupWorker` one?**
`BackgroundService` is a base class from ASP.NET Core that implements `IHostedService`. It starts automatically when the application starts and runs until shutdown. `BackupWorker` extends it and overrides `ExecuteAsync`, which uses a `PeriodicTimer` to run a backup cycle every 24 hours.

**Q2: Why does `BackupWorker` create a DI scope per backup cycle?**
`BackupWorker` is registered as a Singleton (lives as long as the app). `IBackupService` and `Sha8lnyDbContext` are Scoped (live only within one operation). A Singleton cannot directly inject Scoped services — that would hold the scope open forever. Instead, `BackupWorker` calls `_serviceProvider.CreateScope()` at the start of each backup cycle, resolves the scoped services, uses them, then disposes the scope.

**Q3: What does `RESTORE VERIFYONLY` do after a backup, and why is it important?**
It checks the backup file's checksum and structure to confirm the file is readable and not corrupted — without actually restoring it. A backup that fails this check cannot be used for recovery. Verifying after every backup ensures you always have a known-good backup, not a corrupted file you will only discover when disaster strikes.

**Q4: What is `DbInitializer.SeedAsync`, when does it run, and what prevents duplicate data?**
It is a static method that populates empty tables with starter data (skills, universities, demo users). It runs every time the application starts, called from `Program.cs` after `MigrateAsync`. Duplicate data is prevented by checking `AnyAsync()` before each seed block — if the table already has rows, that block is skipped.

**Q5: What does `DiscordWebhookLogger.IsEnabled` return when `WebhookUrl` is not configured?**
It returns `false` — the logger is disabled. The logger gracefully no-ops if the Discord webhook URL is missing from configuration. No errors, no crashes, just silence.

**Q6: Why does Discord webhook logging use `_ = SendAsync(payload)` instead of `await SendAsync(payload)`?**
To make logging fire-and-forget. If the logger awaited the HTTP call and Discord was down, logging would block request processing. With `_ = SendAsync()`, the HTTP call is started but not awaited — the logger returns immediately. If Discord fails, the exception is silently swallowed in the empty `catch {}`. Logging failures must never crash the application.

**Q7: What information does the request timing middleware log for each request?**
HTTP method (`GET`, `POST`, etc.), the request path (`/api/Projects`), the response HTTP status code (`200`, `400`, etc.), and the elapsed time in milliseconds. Example: `GET /api/Students/search responded 200 in 312ms`.

**Q8: What is Docker, and in one sentence, why do we use it?**
Docker packages the application, its runtime (.NET 9), and its dependencies into a container — a sealed, portable box that runs identically on any machine, eliminating "it works on my machine" problems.

**Q9: Why does the Dockerfile use a multi-stage build instead of a single stage?**
Because the .NET SDK image needed for compilation is ~700 MB, but the ASP.NET Core runtime image needed to *run* the app is only ~200 MB. A single-stage build using the SDK image would ship a 700 MB production container full of build tools that are useless at runtime. Multi-stage builds compile in the big image and copy only the output into the small runtime image, producing a ~200 MB final container.

**Q10: The API's connection string says `Server=sqlserver`. How does the API find the database — there is no machine named "sqlserver"?**
Both the API and SQL Server containers are on the same Docker bridge network (`sha8alny-network`). Docker's internal DNS resolves service names to container IP addresses within that network. So `sqlserver` is not a hostname you configured — Docker automatically makes each service reachable by its service name to other containers on the same network.

**Q11: Why does `docker-compose.yml` have a `healthcheck` on `sqlserver` and a `depends_on: condition: service_healthy` on `api`?**
SQL Server takes 10–30 seconds to finish initializing after the container starts. Without the healthcheck, Docker would start the API immediately, and the API would crash trying to connect to a database that isn't ready yet. The healthcheck runs `SELECT 1` every 10 seconds; `service_healthy` means Docker holds the API container until at least one healthcheck passes.

**Q12: You run `docker-compose up --build` on a fresh clone. Walk through what happens step by step.**
1. Docker builds the API image using the `Dockerfile` (multi-stage: compile with SDK → publish → copy to runtime image).
2. Docker pulls the `mcr.microsoft.com/mssql/server:2022-latest` image from Microsoft's registry.
3. Both containers start on `sha8alny-network`.
4. Docker runs the `sqlserver` healthcheck (`SELECT 1`) every 10 seconds.
5. Once the healthcheck passes, Docker starts the `api` container.
6. The API's `Program.cs` runs `MigrateAsync()` — all 8 migrations create the database schema.
7. `DbInitializer.SeedAsync()` populates skills, universities, and demo users.
8. The API is live at `http://localhost:5000`. No manual SQL Server or .NET installation required.
