# Unit 11: Reviews and Certificates — Closing the Loop

> **Before reading this unit:** You should have read Unit 10 (Execution — how `CompleteJobAsync` works). This unit explains what happens after a job is completed: mutual reviews and automatic certificate generation.

---

## 11.1 Why Reviews and Certificates Exist

After Ahmad finishes his internship with TechCorp, two things close the loop:

1. **Mutual reviews:** TechCorp rates Ahmad (how was he to work with?), and Ahmad rates TechCorp (how was the company to intern with?). These ratings build trust on the platform — other companies see Ahmad's rating before hiring him, and other students see TechCorp's rating before applying there.

2. **Certificate:** Ahmad gets a formal document he can show his university or add to his CV. The system generates it automatically when the company marks the job complete — no separate action required.

Think of reviews as the **post-trip rating on Uber** (driver and passenger rate each other), and certificates as the **receipt** — proof that the trip happened.

---

## 11.2 Two Separate Review Types

Sha8alny has two distinct review tables because the direction of the review matters:

| Review Type | Who writes it | Who receives it | Table |
|---|---|---|---|
| `StudentReview` | Company | Student | `StudentReviews` |
| `CompanyReview` | Student | Company | `CompanyReviews` |

Both share the same `CreateReviewDto` structure (rating + comment + applicationId), but they are stored separately and update different entities:
- A `StudentReview` updates `student.AverageRating` and `student.TotalReviews`
- A `CompanyReview` updates `company.AverageRating` and `company.TotalReviews`

---

## 11.3 Gate: Reviews Only After Completion

Both `ReviewStudentAsync` and `ReviewCompanyAsync` enforce the same gate before saving anything:

```csharp
if (application.Status != ApplicationStatus.Completed)
{
    return Failure("Cannot review until the job is completed.");
}
```

This prevents reviews during or before the work. The timeline is strictly enforced: Complete first, then review.

Additionally, each review is **one per (reviewer, reviewee, application)** — a company cannot leave two ratings for the same student on the same project, and a student cannot leave two ratings for the same company on the same project. The service checks for duplicates and returns "You have already reviewed..." if a second attempt is made.

---

## 11.4 What Gets Stored in a StudentReview

When TechCorp calls `POST /api/Reviews/student` (rating Ahmad's work):

```csharp
var review = new StudentReview
{
    StudentID = application.StudentID,
    CompanyID = company.CompanyID,
    ProjectID = project.ProjectID,
    ApplicationID = application.ApplicationID,
    Rating = dto.Rating,             // 1–5 stars (from client)
    ReviewText = dto.Comment,        // free text
    Status = ReviewStatus.Approved,  // immediately visible (no moderation)
    IsPublic = true,
    IsVerified = true,               // verified because tied to a real application
    WouldHireAgain = dto.Rating >= 4 // auto-derived: 4 or 5 stars = yes
};
```

`WouldHireAgain` is not asked of the reviewer — it is computed automatically. A rating of 4 or 5 is treated as "would hire again." This gives a quick binary signal alongside the numerical rating.

Similarly, `CompanyReview` has `WouldRecommend = dto.Rating >= 4` and `IsAnonymous = false` (students are not anonymous by default, though the field exists for future use).

---

## 11.5 Rating Recalculation — How AverageRating Stays Accurate

Every time a new `StudentReview` is submitted, `RecalculateStudentRatingAsync` runs immediately:

```csharp
var allReviews = await _unitOfWork.StudentReviews
    .FindAsync(r => r.StudentID == student.StudentID);

student.TotalReviews = reviewList.Count;
student.AverageRating = Math.Round(reviewList.Average(r => r.Rating), 2);
_unitOfWork.Students.Update(student);
await _unitOfWork.SaveAsync();
```

This is a **full recalculation** — it does not just average the new rating with the old average. It loads all reviews for the student and recomputes from scratch. This is correct but slightly expensive for students with thousands of reviews (not a concern for a graduation project scale).

The result is stored as a denormalized field: `student.AverageRating = 4.37`. When a company views Ahmad's profile, they see this pre-computed number directly without any JOIN or aggregation. Same pattern applies for `company.AverageRating`.

The same `RecalculateCompanyRatingAsync` method runs after every `CompanyReview` submission.

---

## 11.6 Review Notification Flow

After saving the review, the service sends both a database notification AND a SignalR real-time push:

**TechCorp reviews Ahmad (StudentReview):**
```
Notification saved → UserID = student.UserID
Title: "New Review Received"
Message: "TechCorp has left you a 5-star review for 'Backend Internship'."
ActionURL: "/profile/reviews"
→ SignalR pushes to Ahmad's app instantly
```

**Ahmad reviews TechCorp (CompanyReview):**
```
Notification saved → UserID = company.UserID
Title: "New Review Received"  
Message: "Ahmad Hassan has left you a 4-star review for 'Backend Internship'."
ActionURL: "/company/reviews"
→ SignalR pushes to TechCorp's dashboard instantly
```

The same `INotifier` + `IUnitOfWork` pattern from Unit 10 (application acceptance) is reused here — consistent across all notification-producing operations.

---

## 11.7 Certificate Generation — What Happens

When `CompleteJobAsync` (Unit 10) runs, it automatically calls `_certificateService.GenerateCertificateAsync(applicationId)` at the end. The student does not request a certificate — it appears automatically.

**`GenerateCertificateAsync` steps:**

```
1. Verify application exists and status == Completed (gate check)

2. Check if certificate already exists for (studentId, projectId):
   → If yes: return the existing certificate (idempotent — calling twice does not create two)
   → If no: continue

3. Load project and company info

4. Generate a unique certificate number:
   var uniqueId = Guid.NewGuid().ToString("N").ToUpper()[..12]
   var certificateNumber = $"CERT-{uniqueId}"
   → Example: "CERT-A3F9B2C1E847"

5. Create the Certificate entity:
   CertificateTitle: "Certificate of Completion - Internship"
   Description: "This certificate is awarded to Ahmad Hassan for successfully 
                completing the internship 'Backend Dev Internship' with TechCorp."
   CertificateURL: "/certificates/verify/CERT-A3F9B2C1E847"
   IssuedAt: DateTime.UtcNow
   ExpiresAt: null (certificates don't expire)

6. SaveAsync() → INSERT INTO Certificates

7. Return CertificateDto with all fields
```

The `CertificateNumber` (`CERT-A3F9B2C1E847`) is the public identifier. It is how third parties verify the certificate.

---

## 11.8 Public Verification — No Login Required

Certificates can be verified by anyone — employers, universities, even random visitors. The endpoint:

```
GET /api/Certificates/verify/{uniqueId}
```

This endpoint has `[AllowAnonymous]` — no JWT token is required. It returns the full certificate details: student name, project name, company name, issue date. This makes the certificate URL shareable: Ahmad can paste it in his CV or LinkedIn and anyone who clicks it can verify it is real.

`GetCertificateByIdentifierAsync` finds the certificate by `CertificateNumber` (not by database ID) — this is important. The public-facing identifier is the `CERT-XXXXXXXXXXXX` string, not the internal integer primary key. Using a GUID-derived code prevents enumeration attacks (you cannot guess certificate #1, #2, #3).

---

## 11.9 The Certificate Entity — What It Stores

```
Certificates table:
CertificateID | StudentID | ProjectID | CompanyID
CertificateNumber | CertificateTitle | Description | CertificateURL
IssuedAt | ExpiresAt
```

The commented-out fields in the entity source code are worth noting:
```csharp
/* Verification
public bool IsVerified { get; set; }
public int? VerifiedBy { get; set; }
*/
```

These fields exist in the code as comments — they represent a future "manual verification by Admin" feature that was designed but not implemented. The current system auto-sets `IsVerified = true` in reviews (through the `IsVerified` field on review entities), but there is no Admin verification step for certificates.

---

## 11.10 Reading Reviews and Certificates

**A student reads their reviews:** `GET /api/Reviews/student/{studentId}`
Returns all `StudentReview` rows for that student, ordered by newest first, with `ReviewerName` (the company name) and `ProjectName` included.

**Company reads their reviews:** `GET /api/Reviews/company/{companyId}`
Returns all `CompanyReview` rows for that company. If `review.IsAnonymous = true`, the response shows `ReviewerName = "Anonymous"` instead of the student's name.

**A student reads their certificates:** `GET /api/Certificates/my`
Requires authentication (JWT). Returns all certificates for the logged-in student.

**Public certificate verification:** `GET /api/Certificates/verify/{uniqueId}`
No authentication required. Returns certificate details or 404.

---

## 11.11 What to Say in Your Defense

- "Reviews are mutual and verified. A company can only review a student (and a student can only review a company) if the application status is `Completed` — no review before the work is done. This prevents fake or early reviews."
- "We have two separate review tables: `StudentReview` (company → student) and `CompanyReview` (student → company). Each one updates the respective entity's `AverageRating` and `TotalReviews` immediately by recalculating from all reviews."
- "`AverageRating` is denormalized — it is pre-computed and stored on `Student` and `Company`. When a profile is loaded, the rating is a direct column read, not a SQL aggregation query. It is recalculated from scratch every time a new review is submitted."
- "`WouldHireAgain` and `WouldRecommend` are automatically set to `true` when the rating is 4 or 5 — the user does not answer this question explicitly."
- "Certificates are generated automatically when a company marks a job complete — the student does not need to request one. The certificate number is a GUID-derived code like `CERT-A3F9B2C1E847` — not an auto-increment integer — so it cannot be guessed or enumerated."
- "Certificate verification is publicly accessible without authentication: anyone with the URL can verify a certificate is real. This makes certificates shareable on CVs and LinkedIn."

---

## 11.12 Self-Check Questions

**Q1: Can a company leave a review before the application status is Completed?**
No. Both `ReviewStudentAsync` and `ReviewCompanyAsync` check `application.Status != ApplicationStatus.Completed` first. If the job is not yet complete, the review is rejected with "Cannot review until the job is completed."

**Q2: How is `student.AverageRating` computed?**
It is recalculated from scratch every time a new `StudentReview` is submitted. The service loads ALL `StudentReview` rows for that student, calls `.Average(r => r.Rating)`, rounds to 2 decimal places, and stores the result directly on `student.AverageRating`. It is not an on-the-fly SQL aggregate.

**Q3: What is `WouldHireAgain` and how is it set?**
A boolean field on `StudentReview`. It is automatically set to `dto.Rating >= 4` — true for 4 or 5 stars, false for 1–3 stars. The reviewer does not answer this question explicitly; it is derived from the rating.

**Q4: Who generates the certificate and when?**
`CertificateService.GenerateCertificateAsync` is called automatically by `ProjectExecutionService.CompleteJobAsync` at the end of the job completion flow. The student does not manually request a certificate — it appears automatically when the company marks the job complete.

**Q5: What is the format of a certificate number?**
`"CERT-"` + first 12 characters of a GUID (uppercase, no hyphens). Example: `CERT-A3F9B2C1E847`. It is unique enough to function as a public identifier without exposing an incrementing database ID.

**Q6: If `GenerateCertificateAsync` is called twice for the same application, what happens?**
The second call finds the existing certificate via `FindSingleAsync(c => c.StudentID == ... && c.ProjectID == ...)` and returns it without creating a duplicate. The method is idempotent.

**Q7: Why is certificate verification publicly accessible without authentication?**
So that anyone — a potential employer, a university administrator — can verify a certificate by visiting the URL on the certificate document. Requiring login would make the certificate impossible to verify without a Sha8alny account, defeating the purpose.
