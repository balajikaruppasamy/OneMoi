# OneMoi — Security design

Four layers protect every request:

```
Browser ──► 1 AUTHENTICATION      who are you?          JWT access token (2 h) + rotating refresh token (30 d)
        ──► 2 SESSION CHECK       still allowed now?    SessionValidationMiddleware (every request)
        ──► 3 AUTHORIZATION       may you do this?      [HasPermission(...)] on every endpoint
        ──► 4 TENANT ISOLATION    is this your data?    EF Core global filter TenantId = your vendor
```

The Angular app hides menus and buttons the user can't use, but that is only for a clean UI. **The server checks everything.**

---

## 1. Authentication (proving who you are)

| Login | Method | Protection |
|---|---|---|
| Super admin, vendor staff | e-mail / mobile + password | BCrypt hash · **locked 15 min after 5 wrong passwords** · forgot-password by OTP (also unlocks) |
| Vendor staff (alternative) | mobile OTP | — |
| Individual / function host | mobile OTP (first login = sign-up) | OTP stored only as HMAC hash · 5-minute expiry · 5 tries · max 10 per hour |
| Counter operator | vendor code + operator ID + PIN | BCrypt hash · **locked 15 min after 5 wrong PINs** · vendor must be Active |

**Password rule:** at least 8 characters, with letters and numbers.

**Sessions**
* The access token is a JWT. It carries the user id, user type, vendor, role and a **security stamp**.
* The refresh token is random, stored only as a hash, and **rotates on every use**. All tokens from one login form a *family*. If an already-used refresh token is presented again (a stolen copy), the **whole family is revoked**.
* `POST /api/auth/logout-all` creates a new security stamp and revokes every refresh token, so every device is signed out.
* Changing or resetting a password also signs out all other devices.

**Rate limiting (per IP):** 30 login attempts per minute and 10 OTP sends per minute. The limits are set in `appsettings.json → RateLimit`. They are higher than usual because many operators at a mandapam share one Wi-Fi connection; per-account lockout stops password guessing.

## 2. Session check on every request

`OneMoi.Api/Security/SessionValidationMiddleware.cs` uses a 30-second cache (`ISessionStateStore`). Changes made in the app clear the cache, so they apply immediately.

| Situation | Result |
|---|---|
| Login disabled by owner / admin, or operator disabled | 401 `SESSION_REVOKED` |
| Password changed / "sign out everywhere" (security stamp differs) | 401 `SESSION_REVOKED` |
| Owner changed this staff member's role or removed them | 401 (the app refreshes and gets the new role) |
| Vendor suspended by OneMoi | 403 `TENANT_SUSPENDED` → signed out |

## 3. Role-based authorization (permissions)

The **single source of truth** is `OneMoi.Application/Common/Security/Permissions.cs`. To change who can do what, edit `RolePermissions.Matrix` there; the Angular copy of the names is in `src/app/core/permissions.ts`.

| Permission | Owner | Manager | Accountant | Operator | Individual | Super admin |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| tenant.dashboard | ✔ | ✔ | ✔ | | | |
| tenant.profile.view | ✔ | ✔ | ✔ | | | |
| tenant.profile.manage (company, logo) | ✔ | | | | | |
| tenant.staff.view | ✔ | ✔ | ✔ | | | |
| tenant.staff.manage (add, role, disable, reset pwd) | ✔ | | | | | |
| masters.view | ✔ | ✔ | ✔ | ✔ | | ✔ |
| masters.manage (vendor's own rows) | ✔ | ✔ | | | | |
| functions.view | ✔ | ✔ | ✔ | ✔ (assigned only) | | |
| functions.manage | ✔ | ✔ | | | | |
| operators.view | ✔ | ✔ | ✔ | | | |
| operators.manage (add, PIN) | ✔ | ✔ | | | | |
| operators.assign | ✔ | ✔ | | | | |
| moi.view | ✔ | ✔ | ✔ | ✔ (own entries) | | |
| moi.entry | ✔ | ✔ | | ✔ (assigned counter, time window) | | |
| moi.reverse.any | ✔ | ✔ | | | | |
| moi.reverse.own (10 min) | | | | ✔ | | |
| expenses.view / expenses.create | ✔ | ✔ | ✔ | ✔ | | |
| expenses.delete | ✔ | ✔ | | | | |
| reports.view | ✔ | ✔ | ✔ | | ✔ (own hosted functions) | ✔ |
| reports.export | ✔ | ✔ | ✔ | | ✔ (own hosted functions) | |
| me.moi.view / me.profile.manage / me.hosted.view | | | | | ✔ | |
| platform.* (vendors, users, logs, system masters) | | | | | | ✔ |
| account.manage (change own password) | ✔ | ✔ | ✔ | | ✔ | ✔ |

**How it is enforced**
* Controller: `[HasPermission(Permissions.FunctionsManage)]` (see `OneMoi.Api/Security/PermissionAuthorization.cs`).
* Service (defence in depth): `current.Demand(Permissions.FunctionsManage)`.
* UI: `*mkCan="P.functionsManage"` directive, `permissionGuard` on routes, menus filtered by permission.

## 4. Tenant authorization (vendor isolation)

* Every vendor-owned table has `tenant_id`. `AppDbContext` adds the filter `tenant_id = caller's vendor` to **every query**, so another vendor's row comes back as 404 Not Found.
* **Pending vendors** (not yet approved by OneMoi) can set up profile, logo, masters, operators and staff. Money-changing permissions (`functions.manage`, `operators.assign`, `moi.entry`, `moi.reverse.*`, `expenses.create/delete`) return 403 `TENANT_PENDING` until approval.
* **Suspended vendors**: every open session and every operator login is blocked immediately.
* **Operators** additionally see and write only the functions they are assigned to, and only inside the assignment time window.
* **Individuals** read across vendors only for their *own* PersonId (My Moi), or the report of a function whose owner mobile is theirs.

## Audit trail (`auth.audit_logs`)
Logged events: LOGIN_*, LOGIN_FAILED, ACCOUNT_LOCKED, OPERATOR_LOCKED, PASSWORD_CHANGED, PASSWORD_RESET, LOGOUT_ALL, TOKEN_REUSE_DETECTED, STAFF_ADDED, STAFF_CHANGED, STAFF_PASSWORD_RESET, USER_ENABLED/DISABLED/UNLOCKED, TENANT_ACTIVE/SUSPENDED, MOI_REVERSED.

## Other hardening
* Security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
* Logo uploads: PNG / JPG / WEBP only (SVG is refused because it can contain scripts), max 2 MB.
* Swagger is on only in Development (or `Swagger:Enabled = true`).
* No hard deletes of money records; corrections are reversals with a reason.

## Before going live
| Item | Action |
|---|---|
| `App:ExposeOtpInResponse` | set to **false** |
| `Jwt:Key`, `App:OtpSecret`, DB password | move to environment variables / a secret store |
| HTTPS | serve the API and the web app only over HTTPS (`app.UseHttpsRedirection()` + HSTS) |
| CORS | `Cors:Origins` = your real web domain only |
| SMS / e-mail | connect a real provider (MSG91 / Gupshup with DLT, SMTP) |
| Token storage | for higher security, move the refresh token to an HttpOnly cookie |
