# Unit 4: The Database Layer — How Sha8alny Talks to SQL Server

> **Before reading this unit:** You should have read Unit 2 (project structure) and Unit 3 (request lifecycle and DI). This unit dives into how data is actually stored and retrieved — the database layer of the Onion Architecture.

---

## 4.1 The Problem with Writing Raw SQL in Your Application Code

You know SQL well. You could write every database operation manually:

```csharp
// ❌ The old way — raw SQL strings in application code
string sql = "SELECT * FROM Projects WHERE Status = 'Active' AND IsVisible = 1";
SqlCommand cmd = new SqlCommand(sql, connection);
SqlDataReader reader = cmd.ExecuteReader();
// ... manually read each row, manually map each column to a property
```

This works for small projects. But imagine 28 different entity types, hundreds of queries, and a team working on this code. The problems compound fast:

- If you rename the table `Projects` to `Opportunities`, you have to find and fix every SQL string in every file.
- If you change the type of a column, your manual mapping code silently breaks at runtime.
- You have to write repetitive mapping code for every query: "column 1 goes to property A, column 2 goes to property B..."
- SQL strings have no compile-time checking — typos only appear as crashes at runtime.

**The solution: an Object-Relational Mapper (ORM).**

---

## 4.2 What Is an ORM? (Entity Framework Core as Your SQL Writer)

An ORM is a tool that lets you work with your database using the same objects and classes you use in your application code — and it writes the SQL for you automatically.

Think of it as a bilingual translator sitting between your C# code and SQL Server:

| What you know (SQL) | What EF Core maps it to (C#) |
|---|---|
| A table (`Projects`) | A C# class (`Project`) |
| A row in the table | One C# object (one `Project` instance) |
| A column (`ProjectName`) | A C# property (`public string ProjectName { get; set; }`) |
| A foreign key (`CompanyID`) | A navigation property (`public Company Company { get; set; }`) |
| `SELECT * FROM Projects WHERE Status = 'Active'` | `FindAsync(p => p.Status == ProjectStatus.Active)` |
| `INSERT INTO Projects (...)` | `AddAsync(project)` + `SaveAsync()` |
| `UPDATE Projects SET ... WHERE ProjectID = 5` | `Update(project)` + `SaveAsync()` |

You write C#. EF Core writes SQL. SQL Server runs it.

**Entity Framework Core (EF Core)** is Microsoft's ORM for .NET. Sha8alny uses EF Core 9 (the latest version) with SQL Server.

---

## 4.3 DbContext — The Connection to the Database

The `DbContext` is EF Core's central object — it represents a session with the database. Think of it as the "open connection" through which all queries flow.

In Sha8alny, the DbContext is called `Sha8lnyDbContext` (note the spelling — the class name has a typo that became permanent). Here is a simplified view:

```csharp
public class Sha8lnyDbContext : DbContext
{
    // Each DbSet<T> = one table in SQL Server
    public DbSet<User> Users { get; set; }       // ← maps to the "Users" table
    public DbSet<Student> Students { get; set; } // ← maps to the "Students" table
    public DbSet<Project> Projects { get; set; } // ← maps to the "Projects" table
    public DbSet<Application> Applications { get; set; }
    // ... 24 more DbSets for the other 24 entity types

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Load all the Fluent API configuration files automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Sha8lnyDbContext).Assembly);
    }
}
```

When you do `context.Projects.FindAsync(...)`, EF Core knows to look in the `Projects` table because the `DbSet<Project>` property establishes that mapping.

The DbContext is registered in `Program.cs` as a Scoped service, so one instance exists per HTTP request — and it tracks all the changes made during that request. This is important: if you add three items and then call `SaveAsync()`, all three are committed in a single database transaction.

---

## 4.4 The Repository Pattern — Why We Wrap EF Core

You could use the DbContext directly in every service:

```csharp
// ❌ Using DbContext directly in a service
public class ProjectService
{
    private readonly Sha8lnyDbContext _context;

    public async Task<Project?> GetProjectAsync(int id)
    {
        return await _context.Projects.FindAsync(id); // ← directly using EF Core
    }
}
```

This works, but it couples the service tightly to EF Core. If you want to test `ProjectService` without a real database, you cannot — because it directly depends on `Sha8lnyDbContext`.

**The Repository Pattern** adds a thin wrapper around EF Core, giving you an abstraction that can be replaced with a fake in tests.

Instead of services using `Sha8lnyDbContext` directly, they use `IGenericRepository<T>`:

```csharp
// ✅ Using the repository abstraction
var project = await _unitOfWork.Projects.FindSingleAsync(p => p.ProjectID == id);
```

The `IGenericRepository<T>` interface provides these operations for any entity type `T`:

| Method | What it does | SQL equivalent |
|--------|-------------|----------------|
| `GetByIdAsync(id)` | Get one entity by its primary key | `SELECT ... WHERE ID = id` |
| `GetAllAsync()` | Get all rows | `SELECT * FROM table` |
| `FindAsync(predicate)` | Get rows matching a condition | `SELECT ... WHERE condition` |
| `FindSingleAsync(predicate)` | Get the first row matching a condition | `SELECT TOP 1 ... WHERE condition` |
| `FindSingleAsync(predicate, includes)` | Same, but include related data | `SELECT ... JOIN ...` |
| `AddAsync(entity)` | Queue an insert | `INSERT INTO ...` *(not committed yet)* |
| `AddRangeAsync(entities)` | Queue multiple inserts | Multiple `INSERT INTO ...` |
| `Update(entity)` | Mark entity as modified | `UPDATE ...` *(not committed yet)* |
| `Remove(entity)` | Queue a delete | `DELETE FROM ...` *(not committed yet)* |
| `AnyAsync(predicate)` | Check if any row matches | `SELECT CASE WHEN EXISTS(...)` |
| `CountAsync(predicate)` | Count matching rows | `SELECT COUNT(*) WHERE condition` |

Notice "not committed yet" — `Add`, `Update`, and `Remove` only queue the change in memory. The actual SQL is only sent to SQL Server when `SaveAsync()` is called. This is the Unit of Work Pattern.

---

## 4.5 The Unit of Work — The "Transaction Wrapper"

Imagine you are buying three items at a checkout counter. You do not pay for each item one by one. You scan everything, and then you pay once at the end. If your card is declined, nothing is charged.

**The Unit of Work pattern works the same way for database operations.**

Multiple repository operations (adds, updates, deletes) are staged in memory. Only when you call `SaveAsync()` does EF Core send them all to SQL Server in a single atomic transaction. Either all of them succeed, or none of them do.

`IUnitOfWork` exposes a repository for every entity type, plus `SaveAsync()`:

```csharp
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Student> Students { get; }
    IGenericRepository<Project> Projects { get; }
    IGenericRepository<Application> Applications { get; }
    // ... all 28 entity repositories

    Task<int> SaveAsync();  // ← commit all queued changes to SQL Server
    
    Task BeginTransactionAsync();    // start explicit DB transaction
    Task CommitTransactionAsync();   // commit it
    Task RollbackTransactionAsync(); // roll it back on error
}
```

In a service, a typical operation looks like:

```csharp
// Example: accepting an application

var application = await _unitOfWork.Applications.FindSingleAsync(a => a.ApplicationID == id);
// ↑ EF Core sends: SELECT * FROM Applications WHERE ApplicationID = id

application.Status = ApplicationStatus.Accepted;  // modify in memory (no SQL yet)
application.ReviewedAt = DateTime.UtcNow;

_unitOfWork.Applications.Update(application);  // mark as changed (no SQL yet)

await _unitOfWork.SaveAsync();
// ↑ NOW EF Core sends: UPDATE Applications SET Status='Accepted', ReviewedAt=... WHERE ApplicationID=id
```

The `UnitOfWork` class uses **lazy initialization** — each repository is only created when first accessed. If a request only touches Projects, the Student and Application repositories are never instantiated.

---

## 4.6 Fluent API Configuration — Teaching EF Core Your Rules

EF Core needs to know the exact structure of your tables: which column is the primary key, which columns are required, what is the maximum length, how foreign key relationships work.

You can teach EF Core this through **Fluent API** — method calls that configure each entity precisely. Sha8alny has 28 configuration files in `Infrastructure/Sh8lny.Persistence/Configurations/`, one per entity.

Here is the configuration for `Project` (simplified and annotated):

```csharp
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");                      // ← map to "Projects" table

        builder.HasKey(p => p.ProjectID);                 // ← primary key

        builder.Property(p => p.ProjectName)
            .IsRequired()                                  // ← NOT NULL in SQL
            .HasMaxLength(200);                           // ← VARCHAR(200)

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()                       // ← store enum as string, not integer
            .HasMaxLength(20)
            .HasDefaultValue(ProjectStatus.Draft);         // ← default value

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETDATE()");              // ← SQL Server fills this automatically

        // Relationship: One Company → Many Projects
        builder.HasOne(p => p.Company)                     // ← Project has one Company
            .WithMany(c => c.Projects)                     // ← Company has many Projects
            .HasForeignKey(p => p.CompanyID)               // ← foreign key column
            .OnDelete(DeleteBehavior.Restrict);             // ← cannot delete a Company if it has Projects
    }
}
```

**Why Fluent API instead of annotations?**

Annotations (like `[Required]`, `[MaxLength(200)]`) are placed directly on the entity class. This means the entity class would need to reference EF Core — and that would violate the Onion Architecture rule: `Sh8lny.Domain` must have zero external dependencies. Fluent API configurations live in `Sh8lny.Persistence`, keeping the domain models clean.

---

## 4.7 Migrations — How the Database Schema Evolves

A migration is a version-controlled database change script — exactly like `ALTER TABLE` in SQL, but generated automatically and stored in your codebase.

**How it works:**
1. A developer modifies a domain entity (e.g., adds a new property to `Student`).
2. The developer runs: `dotnet ef migrations add AddNewStudentField --startup-project ../Sh8lny.Web`
3. EF Core compares the current entity classes to the last migration and generates a new `.cs` file describing what changed.
4. On the next application startup, `context.Database.MigrateAsync()` applies all pending migrations to the actual SQL Server database.

Sha8alny has 8 migrations, representing the evolution of the schema from December 2025 to April 2026:

| Migration | What it added |
|-----------|--------------|
| `20251207020220_InitialCreation` | The initial complete database schema — all tables |
| `20260126215341_UpdateModels` | Model updates and refinements |
| `20260214032853_AddPasswordResetFields` | `PasswordResetToken` and `ResetTokenExpires` on `User` (forgot-password flow) |
| `20260221205827_AlignDashboardMetricsSchema` | Fixed the DashboardMetric table structure |
| `20260328195323_FixPaymentForeignKeysAndDecimalPrecision` | Fixed payment relationships and decimal column precision |
| `20260329161809_AddInternshipDays` | Added `TotalInternshipDays` column to `Student` |
| `20260422183336_SyncPendingModelChanges` | Synced several pending model adjustments |
| `20260423130812_AddSavedProjectsAndReviews` | Added `SavedOpportunity`, `StudentReview`, `CompanyReview` tables |

**Why run `MigrateAsync()` on startup?**
In a traditional deployment, you run migrations manually before deploying a new version. But in Sha8alny's Cloud Run deployment (containerized, ephemeral), there is no manual step — migrations run automatically on startup. If the database schema is already up to date, `MigrateAsync()` does nothing. If there are pending migrations, they are applied immediately.

---

## 4.8 The Golden Rule: Always Use .Include() for Navigation Properties

This is the most common source of bugs in EF Core code, and it is worth understanding deeply.

In SQL, if you want the company name of a project, you write a JOIN:
```sql
SELECT p.ProjectName, c.CompanyName
FROM Projects p
JOIN Companies c ON p.CompanyID = c.CompanyID
WHERE p.ProjectID = 5
```

In EF Core, the equivalent is `.Include()`:
```csharp
// ✅ CORRECT — loads the Company navigation property
var project = await _unitOfWork.Projects.FindSingleAsync(
    p => p.ProjectID == 5,
    p => p.Company  // ← tells EF Core to JOIN the Companies table
);

Console.WriteLine(project.Company.CompanyName); // ✅ works — Company was loaded
```

Without `.Include()`, EF Core performs a simple `SELECT` with no JOIN. The navigation property (`project.Company`) is `null` even though the data exists in the database:

```csharp
// ❌ WRONG — no Include means no JOIN
var project = await _unitOfWork.Projects.GetByIdAsync(5);

Console.WriteLine(project.Company.CompanyName); // 💥 NullReferenceException!
// "Company" is null because EF Core never queried the Companies table
```

This is called **lazy loading** being disabled (which is EF Core's default for safety). You must explicitly declare every related entity you want to load. This is called **eager loading**.

The rule in Sha8alny: **always check which navigation properties your code accesses, and add the corresponding `.Include()` call before you need them.**

For deeply nested relationships, use `.ThenInclude()`:
```csharp
// Loads Student → StudentSkills → each Skill object
var student = await _unitOfWork.GetStudentWithSkillsAsync(userId);
// This calls: context.Students.Include(s => s.StudentSkills).ThenInclude(ss => ss.Skill)...
```

---

## 4.9 Glossary: New Terms in This Unit

**ORM (Object-Relational Mapper)** — A tool that maps between database tables (relational) and programming language objects. EF Core is the ORM used in Sha8alny.

**DbContext** — EF Core's session object. Tracks all entities loaded in a request and coordinates their saving to the database. `Sha8lnyDbContext` is Sha8alny's DbContext.

**DbSet\<T\>** — A property on the DbContext representing one database table. `DbSet<Project>` represents the `Projects` table.

**Repository Pattern** — A design pattern that wraps database operations behind an interface. Makes business logic independent of the specific database technology.

**Unit of Work** — A pattern that groups multiple repository operations into one atomic batch. `SaveAsync()` commits the entire batch. Either all succeed or none do.

**Migration** — A C# file generated by EF Core that describes a schema change (add a column, add a table, change a data type). Applied automatically at startup via `MigrateAsync()`.

**Navigation Property** — A property on an entity that references another entity. `Project.Company` is a navigation property. Must be `.Include()`-d to be loaded from the database.

**Eager Loading** — Explicitly loading a navigation property in the same query using `.Include()`. The opposite of lazy loading (loading on demand, which EF Core does not do by default).

**Fluent API** — EF Core's method-based configuration API. Used in the 28 `*Configuration.cs` files to define table structure, column constraints, and relationships.

---

## 4.10 What to Say in Your Defense

- "We use Entity Framework Core 9 as our ORM. EF Core translates our C# LINQ expressions into SQL queries automatically. We never write raw SQL strings — all queries are written in C# and compiled, so typos are caught at build time, not runtime."
- "We follow the Repository Pattern. All database operations go through `IGenericRepository<T>`, which provides a consistent interface for CRUD operations on any entity. This allows us to test business logic with fake repositories instead of a real database."
- "The Unit of Work Pattern groups all repository operations within a single request into one transaction. `SaveAsync()` commits everything at once — if any part fails, nothing is committed. This prevents partial data corruption."
- "Database schema changes are managed through EF Core Migrations — version-controlled C# files that apply `ALTER TABLE` operations automatically on startup. Our system has 8 migrations covering the schema evolution from December 2025 to April 2026."
- "We use Fluent API configurations (28 files, one per entity) to define table structure, column lengths, constraints, and relationships. This keeps the domain entity classes completely free of EF Core attributes — preserving the Onion Architecture rule that `Sh8lny.Domain` has zero external dependencies."

---

## 4.11 Self-Check Questions

**Q1: What is the purpose of `DbSet<Project>` in the DbContext?**
It maps the C# class `Project` to the `Projects` table in SQL Server. Any query against `context.Projects` will run SQL against that table.

**Q2: Why does calling `_unitOfWork.Applications.Update(application)` not immediately change the database?**
`Update()` only marks the entity as modified in EF Core's change tracker (in memory). The actual `UPDATE` SQL is only sent when `await _unitOfWork.SaveAsync()` is called.

**Q3: What happens if you access `project.Company.CompanyName` without calling `.Include(p => p.Company)` first?**
You get a `NullReferenceException`. EF Core does not load navigation properties unless explicitly told to with `.Include()`. Without it, `project.Company` is `null`.

**Q4: Why are Fluent API configurations in `Sh8lny.Persistence` instead of annotations in `Sh8lny.Domain`?**
Placing annotations directly on domain entities would require `Sh8lny.Domain` to reference EF Core. This violates the Onion Architecture rule that `Sh8lny.Domain` must have zero external dependencies. Fluent API configurations live in `Sh8lny.Persistence`, which already references EF Core.

**Q5: What is a migration and when is it applied?**
A migration is a C# file generated by `dotnet ef migrations add` that describes a database schema change. It is applied automatically when the application starts, via `context.Database.MigrateAsync()`.

**Q6: If you add a new column to the `Student` entity class, what must you do before the change works in the database?**
You must generate a new migration with `dotnet ef migrations add <MigrationName> --startup-project ../Sh8lny.Web`. The next time the app starts, `MigrateAsync()` will run the migration and add the column to the `Students` table in SQL Server.

**Q7: What is the difference between `FindAsync` and `FindSingleAsync` in `IGenericRepository<T>`?**
`FindAsync` returns a list (`IEnumerable<T>`) of all entities matching the condition. `FindSingleAsync` returns only the first match (or `null` if none exist) — equivalent to SQL's `SELECT TOP 1`.
