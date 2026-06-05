# Unit 7: File Uploads — How Sha8alny Handles Profile Pictures, CVs, and Documents

> **Before reading this unit:** You should have read Unit 2 (Onion Architecture — why inner layers cannot reference HTTP concepts) and Unit 3 (request lifecycle). This unit explains the centralized file upload design.

---

## 7.1 The Problem with Accepting Files Everywhere

Imagine if every endpoint in the system accepted file uploads directly:

- `PUT /api/students/profile` accepts a profile picture AND a CV file
- `POST /api/Applications/apply` accepts a resume PDF AND a proposal document
- `POST /api/Projects` accepts a project brief attachment
- `POST /api/Certificates` accepts a certificate image

Every service would need to know how to:
- Validate file types and sizes
- Resize and optimize images
- Save files to disk
- Generate URLs
- Scan for viruses

That is the same code duplicated 6+ times. If you change the storage location, you change 6 files. If a security vulnerability is found in file handling, you have 6 places to fix it. If you want to add virus scanning, you add it 6 times.

**The Sha8alny solution: ALL file uploads go through one endpoint — `/api/Media`.**

---

## 7.2 The Sha8alny Rule: All Files Go Through `/api/Media`

There is one controller (`MediaController`) and one service (`FileService`) responsible for everything related to files. No other controller or service ever accepts a raw file.

**The two-step flow:**

```
Step 1 — Upload the file:
  Client → POST /api/Media/upload?folder=profiles → MediaController → FileService
  → validate → process → save to wwwroot/uploads/profiles/ → return URL string

Step 2 — Use the URL:
  Client → PUT /api/students/profile with body { "profilePictureUrl": "/uploads/profiles/abc123.webp" }
  → StudentsController → StudentService → saves the URL string in Student.ProfilePicture column
```

The client does two requests. The first gets a URL. The second uses that URL as a plain string field. The student profile service never sees a file — only a URL string.

This is directly tied to the Onion Architecture rule from Unit 2: `IFormFile` (the .NET type for an uploaded file in an HTTP request) is an HTTP concept. If a service accepted `IFormFile`, it would depend on ASP.NET Core HTTP types — violating the rule that inner layers must not know about the web layer.

---

## 7.3 What FileService Does to Your File

When a file arrives at `FileService.SaveFileAsync(file, folderName)`, it runs through this pipeline:

**Step 1: Check if the file is there**
If the file is empty or null, throw an error immediately.

**Step 2: Validate the file type**
Only these extensions and MIME types are accepted:
- Images: `.jpg`, `.jpeg`, `.png`, `.gif`
- Documents: `.pdf`

Anything else (`.exe`, `.docx`, `.zip`, `.svg`, etc.) is rejected.

**Step 3: Check the size**
Maximum: **5 MB**. Files larger than this are rejected.

**Step 4: Virus scan**
The file is read into memory and passed to `IVirusScanService.IsFileCleanAsync()`. If the scan returns `false`, the file is rejected. *(Currently the scan always returns `true` — see section 7.5)*

**Step 5: Create the upload directory**
Files are saved to `wwwroot/uploads/{folderName}/`. If this directory does not exist, it is created.

**Step 6: Generate a unique filename**
A new random filename is generated using `Guid.NewGuid()`. This prevents filename collisions and makes it impossible to guess someone else's file URL.

**Step 7: Process images (if image file)**
- **Resize:** If the image is wider than 1920 pixels, it is resized to 1920px wide (maintaining aspect ratio). Images smaller than 1920px are not enlarged.
- **Convert to WebP:** Regardless of the original format (JPEG, PNG, GIF), the image is saved as WebP format with 80% quality. The `.webp` extension replaces the original extension.
- **Thumbnail:** A second copy is saved at 300×300 pixels (square crop) as a thumbnail. The thumbnail filename is the same as the main file but with `_thumb` appended.

**Step 8: Save PDFs as-is**
PDFs skip the image processing step and are saved with their original content.

**Step 9: Return the URL**
The method returns a `FileUploadResult` object containing:
- `FileUrl` — the public URL, e.g., `/uploads/profiles/a1b2c3d4.webp`
- `ThumbnailUrl` — the thumbnail URL (images only), e.g., `/uploads/profiles/a1b2c3d4_thumb.webp`

Because `wwwroot/` is served as static files (from Unit 3's middleware pipeline — `app.UseStaticFiles()`), anyone with the URL can access the file directly without going through a controller.

---

## 7.4 What Is WebP? Why Convert to It?

You probably know JPEG and PNG. **WebP** is a newer image format created by Google. The key advantage:

**Same visual quality as JPEG/PNG, but roughly 25–35% smaller file size.**

For a web and mobile application where thousands of students upload profile pictures, this matters enormously. Smaller images mean faster loading, less bandwidth cost (especially on mobile data), and less storage used on the server.

Think of it like this: WebP is to images what MP3 was to audio in the early 2000s — a more efficient format that became the new standard. All modern browsers and Flutter support WebP natively.

The trade-off: WebP is slightly harder to open on old systems and is not universally supported in very old browsers (before 2019). For Sha8alny's target audience (university students on modern phones), this is not an issue.

---

## 7.5 The Virus Scanner — Why It Exists and Why It's Disabled

The system includes `ClamAvService` — an integration with **ClamAV**, a popular open-source antivirus engine.

The architecture is correct: `FileService` calls `IVirusScanService.IsFileCleanAsync(stream, filename)`. This is an interface, so ClamAV can be replaced or disabled without changing `FileService`.

**Why is it currently disabled?**

`ClamAvService.IsFileCleanAsync()` is a **stub** — it always returns `true` without actually scanning anything. The reason: running a real ClamAV daemon requires a separate service alongside the application container. In Google Cloud Run's current deployment setup, adding a ClamAV sidecar adds complexity that the team deferred. The architecture is ready for it — when the time comes, the stub implementation can be replaced with a real one, and `FileService` will not need to change at all.

This is an example of the Onion Architecture benefit: the inner layer (`FileService`) is decoupled from the implementation detail of which antivirus engine is used.

---

## 7.6 Why IFormFile Is Forbidden Outside the Media Layer

`IFormFile` is a type defined in `Microsoft.AspNetCore.Http` — it is part of ASP.NET Core's HTTP request handling. It represents an uploaded file from an HTTP request body.

If a service in `Sh8lny.Service` accepted `IFormFile` in a method signature:

```csharp
// ❌ WRONG — violates Onion Architecture
public async Task<ServiceResponse<int>> CreateStudentProfileAsync(
    int userId,
    CreateStudentProfileDto dto,
    IFormFile profilePicture  // ← IFormFile is an HTTP concept from Microsoft.AspNetCore.Http
)
```

Then `Sh8lny.Service.csproj` would need a reference to `Microsoft.AspNetCore.App` — meaning the service layer now depends on the web layer. This breaks the dependency rule: services must not know about HTTP.

The correct approach: the client uploads the file to `/api/Media` first, gets a URL string back, and then passes that plain URL string to the profile endpoint. URL strings are plain C# strings — no HTTP dependencies.

```csharp
// ✅ CORRECT — the inner layer only sees a plain string URL
public class CreateStudentProfileDto
{
    public string? ProfilePictureUrl { get; set; }  // just a string
    public string? CvFileUrl { get; set; }          // just a string
}
```

`FileService` is the one place that does accept `IFormFile`, and it lives close to the web layer (even though it is in `Sh8lny.Service`, it has an ASP.NET Core framework reference in its `.csproj` for this specific reason).

---

## 7.7 Walk-Through: Uploading a CV

Ahmad wants to attach his CV to his student profile. Here is the complete flow:

**Step 1: Ahmad uploads the CV file**
```
Ahmad's app → POST /api/Media/upload?folder=cvs
  Body: multipart/form-data with file = "Ahmad_CV.pdf"
```

**Step 2: MediaController receives it**
```csharp
MediaController.Upload(IFormFile file, string folder)
→ calls _fileService.SaveFileAsync(file, "cvs")
```

**Step 3: FileService processes it**
- Validates: extension is `.pdf` ✓, size is 2.3 MB ✓
- Virus scan: returns clean (stub)
- Saves to: `wwwroot/uploads/cvs/7f4a2b8c.pdf`
- Returns: `FileUploadResult { FileUrl = "/uploads/cvs/7f4a2b8c.pdf" }`

**Step 4: MediaController returns the URL**
```json
{ "fileUrl": "/uploads/cvs/7f4a2b8c.pdf", "thumbnailUrl": null }
```

**Step 5: Ahmad's app stores the URL and calls the profile update**
```
Ahmad's app → PUT /api/students/profile
  Body: { "cvFileUrl": "/uploads/cvs/7f4a2b8c.pdf", ... }
```

**Step 6: StudentService saves the URL**
```csharp
student.CvFileUrl = dto.CvFileUrl;  // just storing the string
_unitOfWork.Students.Update(student);
await _unitOfWork.SaveAsync();
```

Now `Student.CvFileUrl = "/uploads/cvs/7f4a2b8c.pdf"` in the database. When a company views Ahmad's profile, they get this URL in the response, and their browser fetches the file directly as a static asset.

---

## 7.8 What to Say in Your Defense

- "All file uploads are centralized through a single endpoint: `POST /api/Media`. No other controller accepts raw files. This ensures consistent validation, virus scanning, image optimization, and URL generation in one place."
- "We use SixLabors.ImageSharp to process uploaded images: resize to a maximum of 1920px wide, convert to WebP format (smaller file size), and generate a 300px thumbnail. PDFs are stored as-is."
- "After uploading a file, the client receives a URL string. This URL is then passed to other endpoints (like profile updates or application submissions) as a plain string field. Inner layers never see `IFormFile` — they only see URL strings."
- "We have ClamAV virus scanning integrated into the file pipeline via the `IVirusScanService` interface. The current implementation is a stub that always returns 'clean' — the architecture is ready to enable real scanning by replacing the stub with a live ClamAV service."
- "`IFormFile` is an ASP.NET Core HTTP type. If services in the inner layers accepted it, they would depend on the web framework — violating the Onion Architecture rule. The URL-string pattern keeps the inner layers framework-agnostic."

---

## 7.9 Self-Check Questions

**Q1: What are the allowed file types in Sha8alny?**
`.jpg`, `.jpeg`, `.png`, `.gif` (images) and `.pdf` (documents). Maximum size: 5 MB.

**Q2: What happens to a JPEG image when it is uploaded?**
It is resized to a maximum width of 1920px (if larger), converted to WebP format at 80% quality, and a 300px thumbnail is also generated and saved. The original JPEG is not stored.

**Q3: Why does the system use a two-step upload process (first upload to /api/Media, then use URL in the profile update)?**
Because `IFormFile` is an HTTP-specific type that inner layers (services) must not reference — it violates Onion Architecture. By separating the upload from the profile update, the service only receives a plain URL string, which has no HTTP dependencies.

**Q4: What is the virus scanner currently doing?**
It is a stub — `ClamAvService.IsFileCleanAsync()` always returns `true` without actually scanning. Real ClamAV integration is architecturally ready but not yet deployed due to complexity of running a ClamAV daemon alongside the API container.

**Q5: Where are uploaded files stored on the server?**
In `wwwroot/uploads/{folder}/`. The `wwwroot/` directory is served as static files, so uploaded files are accessible directly via their URL without going through a controller.

**Q6: What is WebP and why do we use it?**
WebP is a modern image format that produces files 25–35% smaller than JPEG/PNG at equivalent visual quality. Smaller files mean faster loading and less bandwidth cost — important for a mobile-first app used by students on mobile data.

**Q7: If a student tries to upload a `.docx` file (Word document), what happens?**
`FileService.IsValidFileType()` checks the extension against the allowed list. `.docx` is not in the list. The method throws an `ArgumentException` with the message "Invalid file type." The file is rejected before reaching the disk.
