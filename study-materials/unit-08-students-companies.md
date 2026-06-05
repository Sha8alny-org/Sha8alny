# Unit 8: Students and Companies — Profile Management

> **Before reading this unit:** You should have read Unit 5 (DTOs, ServiceResponse), Unit 6 (authentication — how User and UserType work), and Unit 7 (the file upload pattern — how profile pictures and CVs arrive as URL strings).

---

## 8.1 Why Is There a User Entity AND a Student Entity?

When Ahmad registers, the system creates one row in the `Users` table. But a `User` only stores the login credentials — email, password hash, role, and verification status. There is no place in `User` for a bio, a GitHub link, a list of skills, or a CV.

This is intentional. Think of the difference between your **university ID card** and your **academic transcript**:

- Your **ID card** (`User`) proves who you are: name, ID number, expiry date. It does not say what courses you took.
- Your **academic transcript** (`Student`) is your full professional record: courses, grades, specialization, achievements.

| `User` stores... | `Student` stores... |
|---|---|
| Email (login identity) | First name, last name, bio |
| Password hash | GitHub profile, CV file URL |
| UserType (Student/Company/Admin) | University, department, academic year |
| Verification status | Skills, education history, work experience |
| IsActive (can they log in?) | Average rating, total reviews |
| LastLoginAt | Total internship days accumulated |

Every `Student` has exactly one `User` (linked by `UserID` foreign key). And every `User` with `UserType = Student` should have exactly one `Student` row — but only after they explicitly create their profile. A newly registered student has a `User` row but no `Student` row yet.

The same pattern applies to `Company`: every Company user has a `User` row (authentication) and a separate `Company` row (business profile).

---

## 8.2 The Student Profile — What It Contains

Let us walk through every meaningful field in the `Student` entity:

**Identity & Contact**
- `FirstName`, `LastName` — denormalized from `User.FirstName/LastName` for quick access
- `Bio` — free-text introduction, up to a few paragraphs
- `Phone` — optional contact number
- `GitHubProfile` — link to their GitHub account
- `ProfilePicture` — URL string (uploaded via `/api/Media` — see Unit 7)
- `CvFileUrl` — URL string pointing to their uploaded CV PDF

**Academic Details**
- `UniversityID` — FK to the `University` lookup table (from Master Data — Unit 15)
- `DepartmentID` — FK to the `Department` lookup table
- `AcademicYear` — enum: `FirstYear`, `SecondYear`, `ThirdYear`, `FourthYear`, `Graduate`
- `StudentIDNumber` — their university student ID number (for verification purposes)
- `City`, `State`, `Country` — location fields

**Profile Health**
- `ProfileCompleteness` — an integer from 0 to 100. The system calculates this based on how many optional fields are filled. A complete profile gets more visibility and trust from companies. It incentivizes students to fill in everything.
- `Status` — enum: `Active`, `Inactive`, `Suspended`, `Graduated`

**Ratings & Achievement**
- `AverageRating` — decimal, computed from all `StudentReview` ratings received from companies. Updated by `ReviewService` every time a new review is submitted.
- `TotalReviews` — count of received reviews
- `TotalInternshipDays` — cumulative total of days spent on completed projects (internships, training, etc.). Updated by `ProjectExecutionService` when a job is marked complete. Relevant to Egyptian graduation requirements.

**Computed property** (not a database column):
```csharp
public string FullName => $"{FirstName} {LastName}";
// → "Ahmad Hassan" — convenient shortcut, no need to concatenate in every service
```

---

## 8.3 Student Sub-Records: Education, Experience, Skills

A student's profile is not just the `Student` row — it is a cluster of related rows in separate tables.

**StudentSkill** (join table between `Student` and `Skill`)
Ahmad has skills like "React", "Python", "SQL." Instead of storing a comma-separated list in the `Student` row (bad practice), each skill is a separate row in `StudentSkills`:

```
StudentSkills table:
StudentID=42, SkillID=7   (React)
StudentID=42, SkillID=12  (Python)
StudentID=42, SkillID=3   (SQL)
```

In SQL terms, this is a many-to-many JOIN table. The `Skill` lookup table is maintained by Admin and contains all skills available on the platform. Students select from this list — no free-text skill names, which prevents "React.js" vs "ReactJS" vs "react" confusion.

**Education** (one-to-many — a student can have multiple education records)
Each row in `Education` represents one degree or qualification: university name, degree, field of study, start year, end year (or "in progress"). A student might list their bachelor's degree AND a prior diploma.

**Experience** (one-to-many — a student can have multiple experience records)
Similar to Education but for work experience: company name, role, description, dates. This is self-reported, separate from the completed opportunities tracked by the platform itself.

Why normalize these into separate tables instead of one big JSON blob? Because you can query and filter on them. "Find students who have Python skills AND are in their 3rd or 4th year" requires proper SQL rows, not parsing text strings.

---

## 8.4 Bookmarking Projects — SavedOpportunity

Ahmad found an interesting project but is not ready to apply yet. He saves it with a bookmark.

`SavedOpportunity` is a simple join table:

```
SavedOpportunities table:
SavedID | StudentID | ProjectID | SavedAt
1       | 42        | 15        | 2026-05-01 10:30:00
2       | 42        | 23        | 2026-05-03 14:15:00
```

**API endpoints for bookmarking:**
- `POST /api/students/saved-projects` with `{ projectId: 15 }` → adds a row
- `GET /api/students/saved-projects` → returns the list of saved projects (with project details via `.Include()`)
- `DELETE /api/students/saved-projects/{projectId}` → removes a row

This is implemented directly on the `IUnitOfWork` with a specialized method `GetSavedOpportunitiesWithProjectAsync` that includes the related `Project` and `Company` data in one query — so the response includes the project name, type, and company name, not just raw IDs.

---

## 8.5 The Company Profile — What It Contains

`Company` follows the same split pattern as `Student`:

| `User` stores... | `Company` stores... |
|---|---|
| Email, password, UserType=Company | CompanyName, CompanyLogo (URL) |
| IsEmailVerified, IsActive | ContactEmail, ContactPhone |
| | Website, Address, Industry, Description |
| | AverageRating, TotalReviews |

Key fields in `Company`:
- `CompanyLogo` — URL string from `/api/Media/upload/logo`
- `Industry` — free text: "Software", "Education", "Marketing", etc.
- `Description` — company description for student browsing
- `AverageRating` — computed from `CompanyReview` entries (students rate companies)
- `TotalReviews` — count of student reviews

**Upsert pattern:** Company profile creation uses an upsert — "create or update." Instead of separate `POST` (create) and `PUT` (update) endpoints, `CompanyService.CreateOrUpdateProfileAsync()` checks if a company row already exists for this user. If yes, it updates. If no, it creates. This simplifies the client: it always calls the same endpoint, regardless of whether this is the first time or an update.

---

## 8.6 Profile Search

Both students and companies can be searched and filtered.

**Searching students (`GET /api/students/search`):**
Companies can search for students by:
- Keyword (matches against name, bio, skills)
- Academic year
- University / department
- Country / city
- Minimum rating

**Searching companies (`GET /api/companies/search`):**
Students can search for companies by:
- Keyword (matches against name, industry, description)
- Industry
- Country / city
- Minimum rating

Search is implemented using EF Core's `FindAsync` or a LINQ `Where` chain — building a query step by step and only sending it to SQL when all filters are applied. This is efficient because EF Core only generates one SQL `SELECT` with multiple `WHERE` conditions, not multiple separate queries.

---

## 8.7 What to Say in Your Defense

- "We separate authentication identity (`User`) from professional profiles (`Student`, `Company`). This is a deliberate design: `User` handles login and security; `Student`/`Company` handles professional data. A student can log in even before completing their profile."
- "`Student.AverageRating` is a denormalized computed field — it is updated by `ReviewService` every time a new review is submitted, so the student's rating is always fast to read. We do not compute it on-the-fly from all reviews every time the profile is loaded."
- "`Student.TotalInternshipDays` accumulates automatically each time a project is marked complete by a company. This gives students a real metric to show in their graduation documentation."
- "Skills are not free-text — they reference a centralized `Skill` lookup table managed by Admin. This ensures consistency: 'React' and 'ReactJS' and 'react.js' are not three different skills."
- "Profile completeness scoring (0–100) incentivizes students to fill out their profiles completely. A complete profile is more likely to be noticed by companies browsing student profiles."

---

## 8.8 Self-Check Questions

**Q1: What is the relationship between `User` and `Student`?**
One-to-one. Every `Student` row has a `UserID` foreign key pointing to one `User` row. The `User` stores authentication data; the `Student` stores professional profile data.

**Q2: What does `Student.AverageRating` represent, and who updates it?**
The average star rating from all company reviews received by the student. It is updated by `ReviewService` whenever a new `StudentReview` is submitted — maintaining a pre-computed value rather than recalculating from all reviews on each request.

**Q3: Why are skills stored in a separate `StudentSkill` join table instead of as a comma-separated list in the `Student` row?**
Normalized tables allow proper SQL filtering ("find students with React skills"). A comma-separated string cannot be efficiently queried or joined. It also ensures consistency: skills reference the canonical `Skill` table rather than free-text entries.

**Q4: What is a `SavedOpportunity` and what does it store?**
A join-table row that records which student bookmarked which project, with a timestamp. It has StudentID (FK), ProjectID (FK), and SavedAt. No other data — just the relationship.

**Q5: What is the "upsert" pattern used in company profile creation?**
A single endpoint that creates the company profile if it does not exist yet, or updates it if it does. This simplifies the client — it always calls the same endpoint without needing to check whether it is a first-time creation or an update.

**Q6: What does `Student.TotalInternshipDays` track and when is it updated?**
The cumulative total number of days the student has spent on completed projects (internships, training, part-time work, etc.). It is updated by `ProjectExecutionService.MarkJobCompleteAsync()` when a company marks a project as fully complete.

**Q7: What is `ProfileCompleteness` and what purpose does it serve?**
An integer (0–100) that measures how completely the student has filled out their profile. Computed fields like bio, GitHub link, CV, and skills contribute to a higher score. It incentivizes students to build complete profiles and signals to companies which candidates have put in the effort.
