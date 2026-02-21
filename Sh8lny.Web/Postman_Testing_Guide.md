# Sha8alny API — Postman Testing Guide

> **Base URL (Docker):** `http://localhost:5000`
> **Base URL (Local):** `http://localhost:5065`
> **Swagger UI:** `{Base URL}/swagger/index.html`

---

## Table of Contents

1. [Setup Instructions](#1-setup-instructions)
2. [Auth Endpoints](#2-auth-endpoints)
3. [Student Profile Endpoints](#3-student-profile-endpoints)
4. [Company Profile Endpoints](#4-company-profile-endpoints)
5. [Master Data Endpoints](#5-master-data-endpoints)
6. [Projects Endpoints](#6-projects-endpoints)
7. [Applications Endpoints](#7-applications-endpoints)
8. [Execution Endpoints](#8-execution-endpoints)
9. [Payments Endpoints](#9-payments-endpoints)
10. [Reviews Endpoints](#10-reviews-endpoints)
11. [Certificates Endpoints](#11-certificates-endpoints)
12. [Chat Endpoints](#12-chat-endpoints)
13. [Notifications Endpoints](#13-notifications-endpoints)
14. [Media Endpoints](#14-media-endpoints)
15. [Settings Endpoints](#15-settings-endpoints)
16. [Admin Endpoints](#16-admin-endpoints)
17. [SignalR Hub (Notifications)](#17-signalr-hub-notifications)
18. [Recommended Test Flow](#18-recommended-test-flow)

---

## 1. Setup Instructions

### Postman Environment Variables

Create a Postman environment called **"Sha8alny Local"** with:

| Variable | Initial Value | Description |
|----------|--------------|-------------|
| `baseUrl` | `http://localhost:5000` | API base URL |
| `token` | *(empty)* | JWT access token (auto-set by login script) |
| `adminToken` | *(empty)* | Admin JWT token |
| `studentToken` | *(empty)* | Student JWT token |
| `companyToken` | *(empty)* | Company JWT token |
| `studentUserId` | *(empty)* | Student's user ID |
| `companyUserId` | *(empty)* | Company's user ID |
| `projectId` | *(empty)* | Created project ID |
| `applicationId` | *(empty)* | Created application ID |

### Auth Setup (Collection-Level)

In your Postman **Collection > Authorization tab**, set:
- **Type:** Bearer Token
- **Token:** `{{token}}`

This applies the JWT to all requests unless overridden.

### Auto-Save Token Script

Add this **Post-response script** to each login/register request to auto-save the token:

```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    if (json.token) {
        pm.environment.set("token", json.token);
        pm.environment.set("userId", json.userId);
    }
}
```

---

## 2. Auth Endpoints

### 2.1 Register (Student)

```
POST {{baseUrl}}/api/Auth/register
```

**Auth:** None
**Headers:** `Content-Type: application/json`
**Body (raw JSON):**
```json
{
    "email": "student@test.com",
    "password": "Test@1234",
    "role": "Student"
}
```

**Post-response script:**
```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    pm.environment.set("studentToken", json.token);
    pm.environment.set("studentUserId", json.userId);
    pm.environment.set("token", json.token);
}
```

### 2.2 Register (Company)

```
POST {{baseUrl}}/api/Auth/register
```

**Auth:** None
**Body (raw JSON):**
```json
{
    "email": "company@test.com",
    "password": "Test@1234",
    "role": "Company"
}
```

**Post-response script:**
```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    pm.environment.set("companyToken", json.token);
    pm.environment.set("companyUserId", json.userId);
    pm.environment.set("token", json.token);
}
```

### 2.3 Login

```
POST {{baseUrl}}/api/Auth/login
```

**Auth:** None
**Body (raw JSON):**
```json
{
    "email": "student@test.com",
    "password": "Test@1234"
}
```

**Post-response script:**
```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    pm.environment.set("token", json.token);
}
```

**Expected Response (200):**
```json
{
    "isSuccess": true,
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiration": "2026-02-21T22:00:00Z",
    "userId": 4,
    "email": "student@test.com",
    "role": "Student",
    "message": "Login successful"
}
```

### 2.4 Login as Admin (Seeded)

```
POST {{baseUrl}}/api/Auth/login
```

**Body (raw JSON):**
```json
{
    "email": "admin@sha8alny.com",
    "password": "Admin@123"
}
```

**Post-response script:**
```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    pm.environment.set("adminToken", json.token);
    pm.environment.set("token", json.token);
}
```

### 2.5 Login as Seeded Company

```
POST {{baseUrl}}/api/Auth/login
```

**Body (raw JSON):**
```json
{
    "email": "techcorp@sha8alny.com",
    "password": "Company@123"
}
```

### 2.6 Login as Seeded Student

```
POST {{baseUrl}}/api/Auth/login
```

**Body (raw JSON):**
```json
{
    "email": "john.doe@sha8alny.com",
    "password": "Student@123"
}
```

### 2.7 Get Current User

```
GET {{baseUrl}}/api/Auth/me
```

**Auth:** Bearer Token `{{token}}`

### 2.8 Forgot Password

```
POST {{baseUrl}}/api/Auth/forgot-password
```

**Auth:** None
**Body (raw JSON):**
```json
{
    "email": "student@test.com"
}
```

### 2.9 Reset Password

```
POST {{baseUrl}}/api/Auth/reset-password
```

**Auth:** None
**Body (raw JSON):**
```json
{
    "email": "student@test.com",
    "token": "<paste-token-from-email-or-database>",
    "newPassword": "NewPass@1234"
}
```

---

## 3. Student Profile Endpoints

### 3.1 Create Student Profile

```
POST {{baseUrl}}/api/student/profile
```

**Auth:** Bearer Token `{{studentToken}}`
**Body (raw JSON):**
```json
{
    "fullName": "Test Student",
    "bio": "A passionate software developer",
    "phone": "+962791234567",
    "profilePicture": null,
    "gitHubProfile": "https://github.com/teststudent",
    "city": "Amman",
    "state": "Amman",
    "country": "Jordan",
    "educations": [
        {
            "universityName": "University of Jordan",
            "degree": "Bachelor",
            "fieldOfStudy": "Computer Science",
            "startYear": 2022,
            "endYear": 2026,
            "description": "Dean's list student"
        }
    ],
    "experiences": [
        {
            "companyName": "Tech Startup",
            "role": "Junior Developer",
            "location": "Amman",
            "startDate": "2025-06-01",
            "endDate": null,
            "isCurrent": true,
            "description": "Working on full-stack web applications"
        }
    ],
    "skillIds": [1, 2, 3]
}
```

> **Tip:** First call `GET /api/MasterData/skills` to get valid skill IDs.

---

## 4. Company Profile Endpoints

### 4.1 Create/Update Company Profile

```
POST {{baseUrl}}/api/company/profile
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "companyName": "Test Corp",
    "description": "An innovative technology company",
    "industry": "Information Technology",
    "websiteUrl": "https://testcorp.com",
    "address": "123 Tech Street",
    "city": "Amman",
    "state": "Amman",
    "country": "Jordan",
    "contactEmail": "hr@testcorp.com",
    "contactPhone": "+962791234567",
    "logoUrl": null
}
```

### 4.2 Get Company Profile

```
GET {{baseUrl}}/api/company/profile
```

**Auth:** Bearer Token `{{companyToken}}`

---

## 5. Master Data Endpoints

> No authentication required.

### 5.1 Get All Skills

```
GET {{baseUrl}}/api/MasterData/skills
```

### 5.2 Get All Departments

```
GET {{baseUrl}}/api/MasterData/departments
```

### 5.3 Get All Universities

```
GET {{baseUrl}}/api/MasterData/universities
```

---

## 6. Projects Endpoints

### 6.1 Create Project

```
POST {{baseUrl}}/api/Projects
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "title": "E-commerce Mobile App",
    "description": "Build a cross-platform mobile app for our e-commerce platform using React Native",
    "projectType": "Mobile Development",
    "startDate": "2026-03-01",
    "endDate": "2026-06-01",
    "deadline": "2026-02-28",
    "duration": "3 months",
    "requiredSkillIds": [1, 2, 5],
    "minAcademicYear": 3,
    "maxApplicants": 10,
    "isVisible": true
}
```

**Post-response script:**
```javascript
if (pm.response.code === 201) {
    var json = pm.response.json();
    pm.environment.set("projectId", json.data);
}
```

### 6.2 Update Project

```
PUT {{baseUrl}}/api/Projects/{{projectId}}
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "title": "E-commerce Mobile App (Updated)",
    "description": "Updated description with better scope",
    "projectType": "Mobile Development",
    "startDate": "2026-03-15",
    "endDate": "2026-07-01",
    "deadline": "2026-03-10",
    "duration": "3.5 months",
    "requiredSkillIds": [1, 2, 3, 5],
    "minAcademicYear": 2,
    "maxApplicants": 15,
    "isVisible": true,
    "status": "Active"
}
```

### 6.3 Get Project by ID

```
GET {{baseUrl}}/api/Projects/{{projectId}}
```

**Auth:** None (public)

### 6.4 Get My Projects (Company)

```
GET {{baseUrl}}/api/Projects/my-projects
```

**Auth:** Bearer Token `{{companyToken}}`

### 6.5 Search Projects

```
GET {{baseUrl}}/api/Projects/search?Keyword=mobile&ProjectType=Mobile Development&PageNumber=1&PageSize=10&SortBy=newest
```

**Auth:** None (public)

**Available Query Params:**
| Param | Type | Example | Description |
|-------|------|---------|-------------|
| `Keyword` | string | `mobile` | Search in title/description |
| `ProjectType` | string | `Web Development` | Filter by type |
| `Status` | string | `Active` | Filter by status |
| `CompanyId` | int | `2` | Filter by company |
| `DeadlineBefore` | datetime | `2026-06-01` | Deadline upper bound |
| `DeadlineAfter` | datetime | `2026-01-01` | Deadline lower bound |
| `SkillIds` | int[] | `1&SkillIds=2` | Filter by required skills |
| `IsVisible` | bool | `true` | Visibility filter |
| `SortBy` | string | `newest` | Sort order |
| `PageNumber` | int | `1` | Pagination page |
| `PageSize` | int | `10` | Items per page |

### 6.6 Delete Project

```
DELETE {{baseUrl}}/api/Projects/{{projectId}}
```

**Auth:** Bearer Token `{{companyToken}}`

---

## 7. Applications Endpoints

### 7.1 Apply for Project (Student)

```
POST {{baseUrl}}/api/Applications/apply
```

**Auth:** Bearer Token `{{studentToken}}`
**Body (raw JSON):**
```json
{
    "projectId": 1,
    "proposal": "I am excited to work on this project. I have experience with React Native and Node.js.",
    "duration": "2 months",
    "bidAmount": 500.00
}
```

**Post-response script:**
```javascript
if (pm.response.code === 200 || pm.response.code === 201) {
    var json = pm.response.json();
    pm.environment.set("applicationId", json.data);
}
```

### 7.2 Get My Applications (Student)

```
GET {{baseUrl}}/api/Applications/my-applications
```

**Auth:** Bearer Token `{{studentToken}}`

### 7.3 Get Applicants for Project (Company)

```
GET {{baseUrl}}/api/Applications/project/{{projectId}}
```

**Auth:** Bearer Token `{{companyToken}}`

### 7.4 Update Application Status (Company)

```
PUT {{baseUrl}}/api/Applications/{{applicationId}}/status
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "status": "Reviewed",
    "reviewNotes": "Strong technical background, scheduling interview"
}
```

### 7.5 Review Application — Accept/Reject (Company)

```
PUT {{baseUrl}}/api/Applications/review
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "applicationId": 1,
    "status": "Accepted",
    "note": "Welcome aboard! Great proposal."
}
```

> **Valid statuses:** `"Accepted"`, `"Rejected"`

### 7.6 Withdraw Application (Student)

```
POST {{baseUrl}}/api/Applications/{{applicationId}}/withdraw
```

**Auth:** Bearer Token `{{studentToken}}`

---

## 8. Execution Endpoints

### 8.1 Create Project Module (Company)

```
POST {{baseUrl}}/api/Execution/project/{{projectId}}/modules
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "name": "UI Design Phase",
    "description": "Design all screens and user flows",
    "weight": 25.0,
    "estimatedDuration": "2 weeks"
}
```

> Create multiple modules for a project (weights should sum to 100).

### 8.2 Get Project Modules

```
GET {{baseUrl}}/api/Execution/project/{{projectId}}/modules
```

**Auth:** Bearer Token `{{token}}` (any authenticated user)

### 8.3 Delete Module (Company)

```
DELETE {{baseUrl}}/api/Execution/modules/{moduleId}
```

**Auth:** Bearer Token `{{companyToken}}`

### 8.4 Update Progress (Student)

```
PUT {{baseUrl}}/api/Execution/application/{{applicationId}}/progress
```

**Auth:** Bearer Token `{{studentToken}}`
**Body (raw JSON):**
```json
{
    "moduleId": 1,
    "progressPercentage": 75,
    "note": "Completed wireframes, finalizing high-fidelity mockups"
}
```

> `progressPercentage`: integer from 0 to 100

### 8.5 Get Application Progress

```
GET {{baseUrl}}/api/Execution/application/{{applicationId}}/progress
```

**Auth:** Bearer Token `{{token}}` (any authenticated user)

### 8.6 Complete Job (Company)

```
POST {{baseUrl}}/api/Execution/complete
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "applicationId": 1,
    "companyFeedbackNote": "Excellent work, delivered ahead of schedule!",
    "finalDeliverableUrl": "https://drive.google.com/final-deliverable"
}
```

---

## 9. Payments Endpoints

### 9.1 Process Payment (Company)

```
POST {{baseUrl}}/api/Payments/pay
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "applicationId": 1,
    "paymentMethod": "Credit Card"
}
```

---

## 10. Reviews Endpoints

### 10.1 Company Reviews Student

```
POST {{baseUrl}}/api/Reviews/student
```

**Auth:** Bearer Token `{{companyToken}}`
**Body (raw JSON):**
```json
{
    "applicationId": 1,
    "rating": 5,
    "comment": "Outstanding work! Highly recommended developer."
}
```

> `rating`: integer from 1 to 5

### 10.2 Student Reviews Company

```
POST {{baseUrl}}/api/Reviews/company
```

**Auth:** Bearer Token `{{studentToken}}`
**Body (raw JSON):**
```json
{
    "applicationId": 1,
    "rating": 4,
    "comment": "Great company to work with, clear requirements."
}
```

### 10.3 Get Reviews for Student (Public)

```
GET {{baseUrl}}/api/Reviews/student/{studentId}
```

**Auth:** None

### 10.4 Get Reviews for Company (Public)

```
GET {{baseUrl}}/api/Reviews/company/{companyId}
```

**Auth:** None

---

## 11. Certificates Endpoints

### 11.1 Get My Certificates (Student)

```
GET {{baseUrl}}/api/Certificates/my-certificates
```

**Auth:** Bearer Token `{{studentToken}}`

### 11.2 Verify Certificate (Public)

```
GET {{baseUrl}}/api/Certificates/verify/{uniqueId}
```

**Auth:** None
> Replace `{uniqueId}` with the certificate's unique verification ID (e.g., `CERT-ABC123`).

### 11.3 Generate Certificate (Company)

```
POST {{baseUrl}}/api/Certificates/generate/{{applicationId}}
```

**Auth:** Bearer Token `{{companyToken}}`

---

## 12. Chat Endpoints

### 12.1 Send Message

```
POST {{baseUrl}}/api/Chat/send
```

**Auth:** Bearer Token `{{token}}`
**Body (raw JSON):**
```json
{
    "receiverId": 3,
    "content": "Hi! I'd like to discuss the project requirements."
}
```

> `receiverId`: the other user's UserID

### 12.2 Get My Conversations

```
GET {{baseUrl}}/api/Chat/conversations
```

**Auth:** Bearer Token `{{token}}`

### 12.3 Get Messages in Conversation

```
GET {{baseUrl}}/api/Chat/conversations/{conversationId}/messages
```

**Auth:** Bearer Token `{{token}}`

### 12.4 Mark Conversation as Read

```
PUT {{baseUrl}}/api/Chat/conversations/{conversationId}/read
```

**Auth:** Bearer Token `{{token}}`

---

## 13. Notifications Endpoints

### 13.1 Get All Notifications

```
GET {{baseUrl}}/api/Notifications
```

**Auth:** Bearer Token `{{token}}`

### 13.2 Get Unread Count

```
GET {{baseUrl}}/api/Notifications/unread-count
```

**Auth:** Bearer Token `{{token}}`

### 13.3 Mark One as Read

```
PUT {{baseUrl}}/api/Notifications/{notificationId}/read
```

**Auth:** Bearer Token `{{token}}`

### 13.4 Mark All as Read

```
PUT {{baseUrl}}/api/Notifications/read-all
```

**Auth:** Bearer Token `{{token}}`

---

## 14. Media Endpoints

> All media endpoints use **form-data** (not JSON).

### 14.1 Upload Profile Picture

```
POST {{baseUrl}}/api/Media/upload/profile
```

**Auth:** Bearer Token `{{token}}`
**Body:** form-data

| Key | Type | Value |
|-----|------|-------|
| `file` | File | Select a `.jpg`/`.png` file |

### 14.2 Upload Company Logo

```
POST {{baseUrl}}/api/Media/upload/logo
```

**Auth:** Bearer Token `{{token}}`
**Body:** form-data

| Key | Type | Value |
|-----|------|-------|
| `file` | File | Select a `.jpg`/`.png` file |

### 14.3 Upload Project Attachment

```
POST {{baseUrl}}/api/Media/upload/project
```

**Auth:** Bearer Token `{{token}}`
**Body:** form-data

| Key | Type | Value |
|-----|------|-------|
| `file` | File | Select any file (PDF, image, etc.) |

### 14.4 Upload Certificate File

```
POST {{baseUrl}}/api/Media/upload/certificate
```

**Auth:** Bearer Token `{{token}}`
**Body:** form-data

| Key | Type | Value |
|-----|------|-------|
| `file` | File | Select a certificate file |

### 14.5 Generic Upload

```
POST {{baseUrl}}/api/Media/upload?folder=documents
```

**Auth:** Bearer Token `{{token}}`
**Body:** form-data

| Key | Type | Value |
|-----|------|-------|
| `file` | File | Select any file |

**Query Param:** `folder` (string, default: `"general"`)

### 14.6 Delete File

```
DELETE {{baseUrl}}/api/Media?filePath=uploads/profile/image.jpg
```

**Auth:** Bearer Token `{{token}}`
**Query Param:** `filePath` — relative path returned from an upload response

---

## 15. Settings Endpoints

### 15.1 Get User Settings

```
GET {{baseUrl}}/api/Settings
```

**Auth:** Bearer Token `{{token}}`

### 15.2 Update User Settings

```
PUT {{baseUrl}}/api/Settings
```

**Auth:** Bearer Token `{{token}}`
**Body (raw JSON):**
```json
{
    "enableEmailNotifications": true,
    "enablePushNotifications": false,
    "enableMessageNotifications": true,
    "enableApplicationNotifications": true,
    "language": "en",
    "timezone": "Asia/Amman",
    "profileVisibility": "Public"
}
```

---

## 16. Admin Endpoints

> **All admin endpoints require the Admin role.** Use `{{adminToken}}`.

### 16.1 Get Dashboard Stats

```
GET {{baseUrl}}/api/Admin/stats
```

**Auth:** Bearer Token `{{adminToken}}`

**Expected Response (200):**
```json
{
    "isSuccess": true,
    "data": {
        "totalStudents": 5,
        "totalCompanies": 3,
        "totalUsers": 10,
        "activeUsers": 9,
        "bannedUsers": 1,
        "totalProjects": 12,
        "activeProjects": 8,
        "closedProjects": 4,
        "totalApplications": 25,
        "completedApplications": 10,
        "totalTransactionVolume": 15000.00,
        "totalTransactions": 10,
        "newUsersLast30Days": 3,
        "newProjectsLast30Days": 5
    },
    "message": null,
    "errors": []
}
```

### 16.2 Get Metric History

```
GET {{baseUrl}}/api/Admin/metrics/history?days=30
```

**Auth:** Bearer Token `{{adminToken}}`
**Query Param:** `days` (int, default: 30) — how many days of history to return

### 16.3 Record Daily Snapshot

```
POST {{baseUrl}}/api/Admin/metrics/snapshot
```

**Auth:** Bearer Token `{{adminToken}}`

> Idempotent — safe to call multiple times per day.

### 16.4 Get All Users

```
GET {{baseUrl}}/api/Admin/users
```

**Auth:** Bearer Token `{{adminToken}}`

### 16.5 Get User by ID

```
GET {{baseUrl}}/api/Admin/users/{userId}
```

**Auth:** Bearer Token `{{adminToken}}`

### 16.6 Toggle User Ban

```
PUT {{baseUrl}}/api/Admin/users/{userId}/ban
```

**Auth:** Bearer Token `{{adminToken}}`

> Toggles the user's `IsActive` status. Cannot ban Admin users.

### 16.7 Get All Projects (Admin)

```
GET {{baseUrl}}/api/Admin/projects
```

**Auth:** Bearer Token `{{adminToken}}`

### 16.8 Force Delete Project

```
DELETE {{baseUrl}}/api/Admin/projects/{projectId}
```

**Auth:** Bearer Token `{{adminToken}}`

> Bypasses ownership checks. Cascades to modules, applications, skills.

---

## 17. SignalR Hub (Notifications)

> SignalR cannot be tested directly in Postman. Use the **Postman WebSocket** feature or a separate tool.

**Connection URL:**
```
ws://localhost:5000/hubs/notifications?access_token={{token}}
```

**Available Client Methods (listen for):**
- `ReceiveNotification` — real-time notification push

**Available Server Methods (invoke):**
- `JoinGroup(groupName)` — join a notification group
- `LeaveGroup(groupName)` — leave a notification group

---

## 18. Recommended Test Flow

Follow this order for a complete end-to-end test:

### Phase 1: Setup
```
1. POST /api/Auth/login (admin@sha8alny.com / Admin@123) → save adminToken
2. POST /api/Auth/login (techcorp@sha8alny.com / Company@123) → save companyToken
3. POST /api/Auth/login (john.doe@sha8alny.com / Student@123) → save studentToken
4. GET  /api/MasterData/skills → note skill IDs
```

### Phase 2: Profiles
```
5. POST /api/student/profile (studentToken) → create student profile
6. POST /api/company/profile (companyToken) → create company profile
7. GET  /api/company/profile (companyToken) → verify profile
```

### Phase 3: Projects
```
8.  POST /api/Projects (companyToken) → create project, save projectId
9.  GET  /api/Projects/{projectId} → verify project (no auth needed)
10. GET  /api/Projects/search?Keyword=... → search works
11. GET  /api/Projects/my-projects (companyToken) → company sees own projects
```

### Phase 4: Applications
```
12. POST /api/Applications/apply (studentToken) → apply, save applicationId
13. GET  /api/Applications/my-applications (studentToken) → student sees apps
14. GET  /api/Applications/project/{projectId} (companyToken) → company sees applicants
15. PUT  /api/Applications/review (companyToken) → accept application
```

### Phase 5: Execution
```
16. POST /api/Execution/project/{projectId}/modules (companyToken) → create module 1
17. POST /api/Execution/project/{projectId}/modules (companyToken) → create module 2
18. GET  /api/Execution/project/{projectId}/modules → verify modules
19. PUT  /api/Execution/application/{applicationId}/progress (studentToken) → update progress
20. GET  /api/Execution/application/{applicationId}/progress → verify progress
21. POST /api/Execution/complete (companyToken) → complete the job
```

### Phase 6: Payment & Reviews
```
22. POST /api/Payments/pay (companyToken) → process payment
23. POST /api/Reviews/student (companyToken) → company reviews student
24. POST /api/Reviews/company (studentToken) → student reviews company
25. GET  /api/Reviews/student/{studentId} → public review check
```

### Phase 7: Certificates
```
26. GET /api/Certificates/my-certificates (studentToken) → see generated cert
27. GET /api/Certificates/verify/{uniqueId} → public verify
```

### Phase 8: Chat
```
28. POST /api/Chat/send (studentToken) → send message to company
29. GET  /api/Chat/conversations (studentToken) → see conversation
30. GET  /api/Chat/conversations/{id}/messages (companyToken) → read messages
31. PUT  /api/Chat/conversations/{id}/read (companyToken) → mark read
```

### Phase 9: Notifications & Settings
```
32. GET  /api/Notifications (studentToken) → check notifications generated
33. GET  /api/Notifications/unread-count (studentToken)
34. PUT  /api/Notifications/read-all (studentToken)
35. GET  /api/Settings (studentToken)
36. PUT  /api/Settings (studentToken) → update preferences
```

### Phase 10: Admin
```
37. GET  /api/Admin/stats (adminToken) → dashboard stats (auto-records snapshot)
38. GET  /api/Admin/metrics/history?days=30 (adminToken) → view history
39. POST /api/Admin/metrics/snapshot (adminToken) → manual snapshot
40. GET  /api/Admin/users (adminToken) → all users
41. PUT  /api/Admin/users/{userId}/ban (adminToken) → ban a user
42. GET  /api/Admin/projects (adminToken)
43. DELETE /api/Admin/projects/{projectId} (adminToken) → force delete
```

---

### Seeded Test Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@sha8alny.com` | `Admin@123` |
| Company | `techcorp@sha8alny.com` | `Company@123` |
| Student | `john.doe@sha8alny.com` | `Student@123` |

---

### Common Error Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created (Projects POST) |
| 400 | Bad Request / Validation failure |
| 401 | Unauthorized — missing or expired token |
| 403 | Forbidden — wrong role |
| 404 | Not Found |
| 500 | Server error |
