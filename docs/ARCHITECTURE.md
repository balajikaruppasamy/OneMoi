# OneMoi — Application structure

```
D:\OneMoi
├── run-dev.ps1                    ← starts API + web together
├── database\                      ← SQL you can open in SSMS
│   ├── 01_create_database_schema.sql   (generated from EF migrations)
│   ├── 02_explore_data.sql             (queries to understand the data)
│   └── 03_reset_demo_database.sql      (drop everything → fresh demo data)
├── docs\                          ← MODULE_ANALYSIS, ARCHITECTURE (this), DATABASE
├── ui\  brand\                    ← approved HTML templates & logo
└── src
    ├── backend\   OneMoi.sln      ← .NET 10 (LTS)
    │   ├── OneMoi.Domain          1. ENTITIES  – plain C# classes, no dependencies
    │   ├── OneMoi.Application     2. BUSINESS LOGIC – rules, validation, DTOs (one folder per feature)
    │   ├── OneMoi.Infrastructure  3. DATABASE & TECH – EF Core + PostgreSQL, JWT, OTP sender, Tamil
    │   └── OneMoi.Api             4. HTTP – controllers only call Application services
    └── frontend\onemoi-web        ← Angular 20
        └── src\app
            ├── core\        auth service, HTTP interceptor, guards, API wrapper, models (= API DTOs)
            ├── shared\      icon, logo, <mk-tamil-field> (English → Tamil)
            ├── layout\      shell: sidebar (desktop) / bottom nav (mobile), per-role menu
            └── features\
                ├── auth\        login (OTP / password / operator PIN), OTP, register
                ├── admin\       super-admin console
                ├── vendor\      dashboard, functions, operators & assignment, masters, company profile
                ├── moi\         Moi entry screen + bilingual report (shared by vendor & operator)
                ├── operator\    operator home
                └── me\          individual: My Moi, hosted functions, profile
```

## Layers — who may call whom

```
Angular  ──HTTP/JSON──►  Api (controllers)  ──►  Application (services = business rules)
                                                   │ uses interfaces only
                                                   ▼
                                     Infrastructure (EF Core DbContext, JWT, BCrypt, SMS/e-mail, Tamil)
                                                   │
                                                   ▼
                                     PostgreSQL 18 (database onemoi)
Domain (entities + enums) is shared by all and depends on nothing.
```

* **Controllers have no logic.** One line each: call a service.
* **Business rules live only in `OneMoi.Application/Features/*`**, e.g. `MoiEntryService.CreateAsync` checks that the operator is assigned, the notes add up to the amount, the category highlight is applied, and the person identity is created.
* **Database**: PostgreSQL, connection string in `appsettings.json → ConnectionStrings:OneMoi`. Business code only talks to `IAppDbContext`.

## Logins (4 kinds)

| Login | How | JWT role | Lands on |
|---|---|---|---|
| Super admin (OneMoi owner) | e-mail + password | `SuperAdmin` | `/admin/dashboard` |
| Vendor staff (Owner / Manager / Accountant) | e-mail or mobile + password, or mobile OTP | `TenantUser` + `tid` + `trole` | `/vendor/dashboard` |
| Counter operator | vendor code + operator ID + PIN | `Operator` + `tid` | `/operator/home` |
| Individual / function host | mobile OTP (first OTP login = free sign-up) | `Individual` + `pid` | `/me/moi` |

Tokens: 2-hour access token + 30-day refresh token (stored hashed in `auth.RefreshTokens`, rotated on each refresh).

## Keeping vendors apart (multi-tenancy)

* Every vendor-owned table has `TenantId`.
* `AppDbContext` adds a **global query filter** `TenantId == current user's tenant`. A JD Moi login physically cannot read Meenakshi Moi rows (tested: returns 404).
* `moi.Persons` is **global** (one mobile = one identity). The only cross-vendor read is **My Moi**, and it is filtered by the caller's own `PersonId`.

## Main flows implemented

1. **Function** → English name auto-converted to Tamil (`Ram illa villa → ராம் இல்லா வில்லா`), owner name/phone, location, date/time, other details, counters.
2. **Operators** → created with auto ID + PIN (PIN shown once, stored BCrypt-hashed) → assigned per function/counter on the board (3 × 3 on the same day). Login works only inside the assignment window.
3. **Moi entry** → mobile lookup (how this vendor wrote the name last time), Initial + Name + Tamil, Spouse initial + name + Tamil, work, city (+Tamil), Moi type (★ Thaimaman / Seer highlighted), Cash/UPI/Card/Cheque/Gift-only, **count by notes 500 × 150 + 100 × 250**, gifts, same-name-same-city warning, printable receipt with the vendor's logo.
4. **Expenses during the function** → who took it, relation, purpose, amount → reduces cash in hand.
5. **Reports** → English / Tamil view, print to PDF, CSV for Excel (UTF-8, Tamil-safe).
6. **Individual** → lifetime Moi across all vendors; function hosts see their own function's report.
7. **Company branding** → each vendor uploads its logo and sets receipt header/footer.

## Running locally

```powershell
# 1. API  (creates tables + demo data on first run)  → http://localhost:5080/swagger
cd D:\OneMoi\src\backend
dotnet run --project OneMoi.Api --urls http://localhost:5080

# 2. Web → http://localhost:4200
cd D:\OneMoi\src\frontend\onemoi-web
npx ng serve
```

Or just run `D:\OneMoi\run-dev.ps1`.

### Demo logins (seeded)

| Who | Login | Secret |
|---|---|---|
| Super admin | admin@onemoi.in | Admin@123 |
| JD Moi Tech owner | jd@jdmoi.in | Vendor@123 |
| JD Moi Tech manager | 9000000002 | Vendor@123 |
| Meenakshi Moi owner | meena@meenakshimoi.in | Vendor@123 |
| Sri Murugan (pending approval) | murugan@srimurugan.in | Vendor@123 |
| Operators | JDMOI + OP-101 … OP-109, MEENAMOI + OP-101 … OP-103 | PIN 1234 |
| Individual Ram | 9876543210 | OTP (shown on screen in dev) |
| Function host Manikandan | 9876500001 | OTP |

The login page has a **🧪 Demo logins** panel. One click logs in as any of these.

## Settings to change before hosting

| Setting | Now (dev) | Production |
|---|---|---|
| `App:ExposeOtpInResponse` | `true` (OTP shown on screen) | **false** |
| `Jwt:Key`, `App:OtpSecret` | dev strings | long random secrets from environment variables / Key Vault |
| `ConnectionStrings:OneMoi` | postgres password in appsettings | environment variable / user-secrets, a dedicated app user (not postgres) |
| SMS | simulated (logged in `auth.NotificationLogs`) | MSG91 / Gupshup with DLT templates |
| SMTP | empty = simulated | real SMTP host for e-mail OTP |
| PostgreSQL | local, no SSL | managed PostgreSQL with `SSL Mode=Require` |
