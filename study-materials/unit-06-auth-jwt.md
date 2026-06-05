# Unit 6: Authentication and JWT — How Sha8alny Knows Who You Are

> **Before reading this unit:** You should have read Unit 3 (request lifecycle — the Authentication and Authorization middleware steps) and Unit 5 (DTOs and ServiceResponse). This unit explains the full authentication system in depth.

---

## 6.1 The Problem: How Does the Server Know Who Is Making This Request?

HTTP — the protocol that browsers and apps use to talk to servers — is **stateless**. This means every single HTTP request is independent. The server does not remember anything from the previous request.

It is like a bank teller who has amnesia between every customer. You walk up, they help you. You step away, come back one second later — they have no memory of you. Every time you approach, you have to prove who you are from scratch.

In a traditional website with sessions, the server stores your identity in a session and gives you a cookie. Every request includes the cookie, and the server looks up the session to find you.

Sha8alny uses a more modern approach: **JWT tokens**. The server gives you a signed digital credential when you log in, and you send that credential with every request. The server verifies the credential without needing to look anything up in a database.

---

## 6.2 Passwords: Why We Never Store the Real Password

Suppose your database is ever stolen — whether by a hacker or a disgruntled employee. If you stored passwords in plain text, all your users' passwords are immediately exposed.

**BCrypt hashing** solves this. BCrypt is a one-way mathematical function: you give it a password, it gives you a scrambled string. Critically, you **cannot reverse** the scrambling — given the scrambled string, you cannot recover the original password.

When Ahmad registers with password `MySuperPassword123`:

```
BCrypt.HashPassword("MySuperPassword123")
→ "$2a$12$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy"
```

This hash is stored in the `User.PasswordHash` column. The original password is never stored anywhere.

When Ahmad logs in later and types his password:

```csharp
BC.Verify("MySuperPassword123", storedHash)  // ← returns true
BC.Verify("WrongPassword456", storedHash)    // ← returns false
```

BCrypt can verify a password against its hash without knowing the original. Even if someone steals the database and gets all the hashes, they still cannot log in — they would need to try every possible password combination, which takes thousands of years on modern hardware (because BCrypt is deliberately slow).

**The takeaway:** We never store passwords. We store hashes. BCrypt verifies them.

---

## 6.3 What Happens When You Register

Here is the exact flow when Ahmad calls `POST /api/Auth/register`:

```
1. AuthService receives RegisterDto { Email, Password, Role }

2. Check: does this email already exist in the Users table?
   → _unitOfWork.Users.FindSingleAsync(u => u.Email == dto.Email)
   → If yes: return failure "Email already registered."

3. Parse the role:
   → dto.Role = "Student" → UserType.Student (enum)
   → Invalid role = return failure "Invalid role specified."

4. Hash the password:
   → BC.HashPassword(dto.Password) → generates the bcrypt hash

5. Create a new User entity:
   → User { Email, PasswordHash (the hash), UserType, CreatedAt, UpdatedAt }

6. Save to database:
   → _unitOfWork.Users.AddAsync(user)
   → _unitOfWork.SaveAsync()
   → SQL: INSERT INTO Users (Email, PasswordHash, UserType, ...) VALUES (...)

7. Generate a JWT token (see section 6.5)

8. Return AuthResponseDto { IsSuccess=true, Token, Expiration, UserId, Email, Role }
```

Notice that the user is immediately given a JWT token after registration — they are logged in right away. Email verification is separate (see section 6.8) and does not prevent the initial token from being issued.

---

## 6.4 What Happens When You Log In

When Ahmad calls `POST /api/Auth/login` with his email and password:

```
1. AuthService receives LoginDto { Email, Password }

2. Find the user by email:
   → _unitOfWork.Users.FindSingleAsync(u => u.Email == dto.Email)
   → If not found: return failure "Invalid email or password."
   (We give a vague message so hackers cannot determine which emails are registered)

3. Verify the password:
   → BC.Verify(dto.Password, user.PasswordHash)
   → If false: return failure "Invalid email or password."

4. Update last login timestamp:
   → user.LastLoginAt = DateTime.UtcNow
   → _unitOfWork.SaveAsync()

5. Generate a JWT token

6. Return AuthResponseDto { Token, Expiration, UserId, Email, Role }
```

The token is then stored by the client (browser or Flutter app) and included in every subsequent request.

---

## 6.5 What Is a JWT Token?

**JWT** stands for JSON Web Token. It is a compact, URL-safe string that carries information about the user.

A JWT has three parts separated by dots: `header.payload.signature`

**Example (the real token is longer, but conceptually):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9    ← Header (base64 encoded)
.
eyJzdWIiOiI0MiIsImVtYWlsIjoiYWhtYWRAZXhhbXBsZS5jb20iLCJyb2xlIjoiU3R1ZGVudCJ9  ← Payload
.
SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c   ← Signature
```

**The Payload** contains "claims" — pieces of information about the user:

In Sha8alny, every JWT payload includes:
- `sub` (Subject) = UserID (e.g., `42`) — this is how the server knows who you are
- `email` = the user's email address
- `role` = the user's `UserType` as a string (e.g., `"Student"`, `"Company"`, `"Admin"`)
- `jti` = a unique ID for this specific token (for future revocation if needed)
- `iat` = issued-at timestamp

**The Signature** is the critical security piece. It is created by running the header + payload through a cryptographic function using a secret key that only the server knows:

```
Signature = HMAC_SHA256(header + "." + payload, secret_key)
```

Think of it like a **government-stamped wristband** at an event. Anyone can read what is printed on the wristband (the payload). But only the government (the server with the secret key) can issue a wristband with a valid stamp. You cannot forge the stamp without the secret key.

**Lifetime:** Tokens expire after 60 minutes (set in `appsettings.json` as `DurationInMinutes: 60`). After expiry, the server rejects the token and the client must log in again.

---

## 6.6 How the Server Validates a JWT on Every Request

The client includes the token in every request's HTTP header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

The Authentication middleware (configured in `Program.cs`) does this:

1. Extract the token from the `Authorization` header.
2. Split into header, payload, signature.
3. Re-compute what the signature **should be** using the server's secret key.
4. Compare the computed signature to the one in the token.
5. If they match: the token is authentic — nobody tampered with it.
6. Check `expires` claim — if the current time is past the expiry, reject it.
7. If all checks pass: decode the payload, extract the claims, and populate the `User` object in the request context.

The controller can then access the user's identity through claims:

```csharp
private int? GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // ← reads the "sub" claim
    if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        return null;

    return userId; // ← returns 42 (the UserID stored in the JWT)
}
```

The key insight: **the server never stores tokens**. There is no "sessions" table in the database. The token is self-contained — everything the server needs to identify the user is encoded in the token itself. This is why JWT is called "stateless authentication."

---

## 6.7 Role-Based Authorization: [Authorize(Roles = "Company")]

The JWT's `role` claim contains the user's `UserType` as a string: `"Student"`, `"Company"`, or `"Admin"`.

When ASP.NET Core's Authorization middleware sees `[Authorize(Roles = "Company")]` on an endpoint, it reads the `role` claim from the validated JWT and checks if it matches. If it does not match — even if the user is authenticated — the request is rejected with `403 Forbidden`.

```csharp
[HttpPost]
[Authorize(Roles = "Company")]   // ← only Company users can call this
public async Task<ActionResult> CreateProject([FromBody] CreateProjectDto dto)
{
    // If a Student's token is sent, this code never runs — Authorization rejects it
}
```

The role enforcement mapping:

| Endpoint | Required Role | What happens with wrong role |
|---------|--------------|------------------------------|
| `POST /api/Projects` | Company | Student gets 403 |
| `POST /api/Applications/apply` | Student | Company gets 403 |
| `PUT /api/Execution/.../review` | Company | Student gets 403 |
| `GET /api/Admin/stats` | Admin | Student/Company get 403 |
| `GET /api/Projects/{id}` | *(none — anonymous)* | Anyone can call it |

---

## 6.8 Email Verification and Password Reset

### Email Verification Flow

After registration, the user's email is not yet verified (`IsEmailVerified = false`). The system sends a 6-digit OTP (One-Time Password) to their email address. The user must:

1. Call `POST /api/Auth/verify-email` with their email and the OTP code.
2. `AuthService` checks: does the code match `User.VerificationCode`? Is `VerificationCodeExpiry` in the future?
3. If valid: set `IsEmailVerified = true`, clear the verification code.

Some protected operations may check `IsEmailVerified` — unverified users might be blocked from certain actions.

### Forgot Password Flow

If Ahmad forgets his password:

1. `POST /api/Auth/forgot-password` with email → generates a secure random token, stores it in `User.PasswordResetToken` (hashed) and `User.ResetTokenExpires`, sends the token to Ahmad's email.
2. Ahmad opens the link in the email, which includes the token.
3. `POST /api/Auth/reset-password` with { email, token, new password } → verifies the token, verifies it has not expired, hashes the new password, updates `User.PasswordHash`, clears the reset token.

The password reset token is single-use and time-limited — typically 1 hour.

---

## 6.9 The User Entity — What Each Field Stores

| Field | What it stores |
|-------|---------------|
| `UserID` | Primary key — auto-incremented integer |
| `Email` | The login email — unique across the system |
| `PasswordHash` | BCrypt hash of the password — never the original password |
| `UserType` | Enum: `Student`, `Company`, `University`, `Admin` — determines role and which profile type they have |
| `IsEmailVerified` | `false` until the user confirms their email with an OTP |
| `VerificationCode` | The OTP sent to email during registration — cleared after verification |
| `VerificationCodeExpiry` | When the OTP expires — prevents code reuse |
| `PasswordResetToken` | Token sent by email during forgot-password — cleared after use |
| `ResetTokenExpires` | When the reset token expires |
| `IsActive` | `true` by default; set to `false` when an Admin bans the user — login returns failure |
| `LastLoginAt` | Updated every time the user successfully logs in |
| `CreatedAt` / `UpdatedAt` | Standard timestamps |

**Navigation properties:** `User` has optional one-to-one links to `Student?`, `Company?`, `University?`, and `UserSettings?`. One-to-many links to `Notifications`, `ActivityLogs`, `SentMessages`.

---

## 6.10 What to Say in Your Defense

- "We use JWT Bearer token authentication. When a user logs in, the server generates a signed token containing their UserID, email, and role. This token is sent with every subsequent request in the `Authorization` header."
- "Passwords are never stored — only BCrypt hashes. BCrypt is a one-way function: given the hash, you cannot recover the password. Even if the database is stolen, passwords remain safe."
- "JWT is stateless — the server stores no session data. The token itself carries all the information needed to identify the user and their role. The server only needs its secret key to verify the signature."
- "Role-based authorization is enforced at the controller level using `[Authorize(Roles = "Company")]` attributes. If the JWT's role claim does not match, the request is rejected with 403 Forbidden before any business logic runs."
- "We have a full email verification flow (OTP) and a forgot-password flow (time-limited token via email). User accounts can be deactivated by Admin by setting `User.IsActive = false`, which causes all subsequent login attempts to fail."

---

## 6.11 Self-Check Questions

**Q1: Why is BCrypt better than storing a plain password or even a simple hash (like MD5)?**
BCrypt is deliberately slow (computationally expensive) and uses a salt (random data added before hashing). This makes brute-force attacks impractical. MD5 and SHA1 are fast hash functions — an attacker can try billions of passwords per second. BCrypt intentionally slows this to a few per second.

**Q2: What three pieces of information does every Sha8alny JWT token contain?**
UserID (in the `sub` claim), email, and role (UserType as string). Also includes `jti` (unique token ID) and `iat` (issued-at timestamp).

**Q3: Does the server store JWT tokens in the database?**
No. JWT is stateless — no token storage. The server only uses its secret key to verify the token's signature on each request.

**Q4: What happens when a JWT token expires?**
The Authentication middleware reads the `expires` claim and sees the current time is past it. The token is rejected as invalid — the same as if it were tampered with. The user must log in again to get a fresh token.

**Q5: A user has been banned by an Admin (`IsActive = false`). They still have a valid JWT token that has not expired. Can they still use the API?**
Technically, the JWT will pass the signature validation. However, services that check `user.IsActive` (like login, or other sensitive operations) will reject requests. This is a known limitation of stateless JWT — for immediate revocation, you would need a token blacklist (not currently implemented).

**Q6: What does `[AllowAnonymous]` do on an endpoint?**
It explicitly marks the endpoint as accessible without authentication. The Authentication middleware still runs (and populates `User` if a token is present), but the Authorization middleware skips the check. Used for endpoints like `GET /api/Projects` (browse without logging in) and `GET /api/Certificates/verify/{id}` (public verification).

**Q7: Why is the error message for wrong password "Invalid email or password" rather than "Password is incorrect"?**
To prevent user enumeration — if the server said "email not found" for unknown emails and "password incorrect" for known emails, an attacker could use the API to determine which email addresses are registered. The vague message reveals nothing.
