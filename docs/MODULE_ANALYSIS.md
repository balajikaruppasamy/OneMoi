# OneMoi — Module Analysis

> **ஒரே Mobile Number — உங்கள் முழு மொய் வரலாறு.**
> One Mobile Number. One Moi Identity. A Lifetime of Moi History — across every Moi vendor.

Source: *Digital_Moi_SaaS_Product_Business_Concept.docx* + the vendor → function → operator flow described in chat.

---

## 1. Product name & brand

| Item | Decision |
|---|---|
| **Name** | **OneMoi** (Tamil sub-line: ஒரே மொய் அடையாளம் — "one Moi identity") |
| Why | The name *is* the product promise: one mobile number → one Moi identity across every vendor and function. Short, bilingual, easy to say at a counter ("OneMoi-la paarunga"). |
| Availability (checked 2026-10-05) | No existing app/company found. `onemoi.in`, `onemoi.co.in`, `onemoi.app`, `getonemoi.com` unregistered. `onemoi.com` parked for sale (HugeDomains, $995) — use `onemoi.in` as primary; buy .com later. Still to do: IP India trademark search classes 9 / 36 / 42, Play Store package `in.onemoi.app`. |
| Tagline (EN) | One Mobile. One Moi Identity. Every Function. |
| Tagline (TA) | ஒரே Mobile — முழு மொய் வரலாறு |
| Colours | Kumkum Maroon `#8E1B3A` (trust, auspicious), Turmeric Gold `#F2A516` (celebration, money), Banana-leaf Green `#1F8A5B` (success / paid), Ivory `#FFF9F0` background |
| Logo idea | A **Moi cover (envelope)** whose flap forms an **"M"**, sealed with a gold **₹ coin**. Files in `brand/`. |
| Fallback names (.com + .in both free) | MoiSetu, MoiThadam, Moiyal, MoiNest |

---

## 2. The real business model (as clarified)

The concept doc is written as if *function organisers* buy the product. Your actual model is a **B2B2C marketplace of Moi vendors**:

```
                  ┌──────────────────────── OneMoi Platform (Super Admin) ────────────────────────┐
                  │                                                                                     │
   Tenant ──►  JD Moi Tech            Meenakshi Moi             Sri Murugan Moi Services   … (paying SaaS customers)
                  │                         │
   Functions ──►  ├─ Manikandan Keda Vettu  ├─ Friend's Marriage (Kalyana Mandapam)
                  ├─ Surya Marriage         └─ …
                  └─ Kumar Housewarming
                  │
   Counters/      ├─ Function A: Counter 1 (Op Arun), Counter 2 (Op Bala), Counter 3 (Op Chitra)
   Operators ──►  ├─ Function B: Counter 1 (Op Dinesh), Counter 2 (Op Esther), Counter 3 (Op Faizal)
                  └─ Function C: Counter 1 (Op Ganesh), Counter 2 (Op Hari), Counter 3 (Op Indhu)

   Person (global, NOT tenant-owned) ──►  Ram  (+91 98xxx xx210)
        ├─ ₹1,000  at Manikandan Keda Vettu  via JD Moi Tech
        └─ ₹5,000  at Friend's Marriage       via Meenakshi Moi
        = ₹6,000 lifetime — visible to Ram in ONE login
```

**The single most important architectural rule:**
`Tenant`, `Function`, `Operator`, `Transaction` are **tenant-scoped**. `Person` (mobile identity) is **global**. That is what lets Ram see Moi given through *any* vendor, while JD Moi can never see what Ram gave through Meenakshi Moi.

---

## 3. Roles & permissions

| Role | Scope | Logs in with | Can do | Cannot do |
|---|---|---|---|---|
| **Super Admin** | Platform | Email + password + OTP (2FA) | Approve/suspend tenants, plans & billing, global audit, support | Edit any Moi amount |
| **Tenant Owner** (JD Moi) | One tenant | Mobile + OTP (+ optional password) | Create functions, operators, counters, assign operators, reports, branding, subscription | See a person's Moi outside own tenant |
| **Tenant Manager** | One tenant | Mobile + OTP | Same as owner minus billing / deleting tenant | Billing |
| **Function Owner (host family)** — optional, invited by tenant | One function | Mobile + OTP | Live dashboard & report of *their* function only | Edit entries |
| **Moi Operator** | Assigned function(s) + counter for a time window | Operator ID + 4–6 digit PIN (device bound) **or** mobile OTP | Search mobile, add guest, record Moi, print receipt, correct own entry within N minutes | See other functions, reports, delete |
| **Viewer / Accountant** | Tenant or function | Mobile + OTP | Read reports, export | Write anything |
| **Individual (Guest)** | Self | Mobile + OTP | Profile, lifetime Moi history across all tenants, personal QR, pay by function QR | See anyone else's data |

> Operators are *temporary staff*. Assignment = `(operator, function, counter, valid_from, valid_to)`. Access auto-expires after the function ends — this is what implements "3 persons in function A, 3 in B, 3 in C on the same day".

---

## 4. Modules (Modular Monolith — ASP.NET Core)

Each module = own folder, own DbContext schema, talks to others only through interfaces/events. Later any module can be split into a service.

| # | Module | Responsibility | Key entities | MVP? |
|---|---|---|---|---|
| 1 | **Identity & Auth** | OTP login, PIN login for operators, JWT + refresh tokens, device binding, rate limiting | `UserAccount`, `OtpRequest`, `RefreshToken`, `Device` | ✅ |
| 2 | **Tenants** | Vendor onboarding, KYC, branding (logo, receipt header), settings | `Tenant`, `TenantSetting`, `TenantMember` | ✅ |
| 3 | **Subscriptions & Billing** | Plans (per-function / monthly / annual), counter packs, invoices, usage metering | `Plan`, `Subscription`, `Invoice`, `UsageRecord` | ✅ (basic) |
| 4 | **Functions (Events)** | Create function, type, date, venue, host family, Function Code + QR, status (Draft → Live → Closed → Archived) | `Function`, `FunctionHost`, `FunctionType` | ✅ |
| 5 | **Counters & Operators** | Operator pool per tenant, counters per function, **assignment with time window**, shift handover | `Operator`, `Counter`, `OperatorAssignment` | ✅ |
| 6 | **People (Moi Identity)** | Global person by mobile; tenant-local alias (name/place spelled differently per vendor); merge/claim | `Person`, `PersonAlias`, `PersonClaim` | ✅ |
| 7 | **Moi Transactions** | Cash/UPI entry, correction (never hard-delete — reversal entry), receipt number, idempotency key for offline sync | `MoiTransaction`, `TransactionCorrection` | ✅ |
| 8 | **Payments** | UPI/PG order create, webhook verification, settlement status, reconciliation | `PaymentOrder`, `PaymentWebhookLog`, `Settlement` | ✅ (one PG) |
| 9 | **QR Management** | Function QR, personal QR (signed token, rotating), counter QR | `QrToken` | ✅ function QR / v2 personal |
| 10 | **Moi History (Personal Ledger)** | Read-model across tenants for the individual: totals, by year, by vendor, by function, "received vs given" later | `PersonLedgerView` (projection) | ✅ |
| 11 | **Reports & Exports** | Function / counter / operator / cash / digital / guest-wise, Excel & PDF | report queries, `ExportJob` | ✅ basic |
| 12 | **Notifications** | SMS OTP, WhatsApp/SMS receipt "Your ₹1,000 Moi recorded at …", push | `NotificationTemplate`, `NotificationLog` | OTP ✅ / rest v2 |
| 13 | **Offline Sync** | Counter local queue (IndexedDB / SQLite), conflict rules, sync status | `SyncBatch` | v2 (design now) |
| 14 | **Audit & Compliance** | Who changed what, consent log, data-export / delete requests (DPDP Act 2023) | `AuditLog`, `Consent` | ✅ |
| 15 | **Platform Admin** | Tenant approval, plan management, support impersonation (audited) | — | ✅ basic |

---

## 5. Core data model (tenant-aware)

```
Person            (GLOBAL)      PersonId, Mobile(E.164, unique), Name, Place, Work, Verified(bool), CreatedAt
UserAccount       (GLOBAL)      UserId, PersonId?, Mobile, PasswordHash?, Status
Tenant                          TenantId, Name, Slug, Logo, Gstin?, Status, PlanId
TenantMember                    TenantId, UserId, Role(Owner|Manager|Viewer)
Function                        FunctionId, TenantId, Name, Type, Date, Venue, HostName, HostMobile,
                                FunctionCode(6 char, unique), QrToken, Status
Counter                         CounterId, FunctionId, Name("Counter 1"), Printer?
Operator                        OperatorId, TenantId, Name, Mobile, PinHash, Status
OperatorAssignment              Id, OperatorId, FunctionId, CounterId, ValidFrom, ValidTo
PersonAlias                     TenantId, PersonId, LocalName, LocalPlace   ← how *this* vendor spells Ram
MoiTransaction                  TxnId, TenantId, FunctionId, CounterId, OperatorId, PersonId,
                                Amount, Mode(Cash|UPI|Card), Status(Pending|Success|Failed|Reversed),
                                ReceiptNo, ClientRef(idempotency), CreatedAt, SyncedAt
PaymentOrder                    OrderId, TxnId, Provider, ProviderRef, Status, RawWebhook
```

**Row-level security:** every tenant-scoped table has `TenantId`; EF Core global query filter + SQL Server RLS policy as a second wall.
**The only cross-tenant query** in the whole system is `GET /me/moi-history` — and it is filtered by `PersonId = currentUser.PersonId`.

---

## 6. Key flows

### 6.1 Vendor's day with 3 functions
1. Tenant owner creates Function A, B, C (date, venue, host) → system issues Function Code + QR each.
2. Creates/reuses operators from tenant pool (Arun … Indhu).
3. **Assign screen:** drag 3 operators into each function, each gets a counter. Assignment window e.g. 06:00–14:00.
4. Operator opens app → enters Operator ID + PIN → sees *only* their assigned function/counter for today → starts entry.
5. Live dashboard per function + combined tenant dashboard.
6. Function closed → assignments auto-expire → report sent to host family.

### 6.2 Counter entry (target: < 8 seconds per guest)
Mobile number (10 digits) → auto lookup → existing: name/place pre-filled from *this tenant's* alias, else global name → amount (quick chips ₹101 / ₹501 / ₹1001 …) → mode → **Save & Print** → SMS/WhatsApp "₹1,001 Moi recorded at Surya weds Priya via JD Moi Tech. See all your Moi: onemoi.in". Keyboard-only on desktop (Enter to move fields).

### 6.3 Ram sees his history
Ram opens app/web → mobile + OTP → **Person is matched by mobile** → ledger shows every transaction across JD Moi, Meenakshi Moi … grouped by function, with vendor badge, totals by year. If a vendor typed his name as "Raman" that's just a `PersonAlias`; history still unifies by mobile.

### 6.4 Guest digital Moi (no app install)
Scan Function QR → web page `onemoi.in/f/AB12CD` → function details → mobile + OTP (light) → amount → UPI intent → **webhook-verified** → transaction created → receipt + "Create free account to see lifetime history".

### 6.5 Privacy rules
- Vendor sees: name, place, amount *for their functions only*.
- Host family sees: their function only.
- Person sees: everything they gave, everywhere.
- Mobile is masked in vendor reports by default (`98xxx xx210`) except during live counter entry.
- An unverified person record (created by operator) becomes "claimed" only after that mobile verifies by OTP.

---

## 7. Gaps / risks spotted in the concept

| Gap | Recommendation |
|---|---|
| Doc assumes organiser = customer; real customer = Moi vendor | Model `Tenant` as vendor; host family is an optional invited role. |
| Same guest typed differently by vendors | `PersonAlias` per tenant; global identity only by mobile. |
| Guest without mobile (elders) | Allow "no mobile" entries — tenant-local only, never in global ledger. |
| Shared family mobile (husband gives on wife's phone) | v2 Family accounts; v1 show "given by name" field in transaction. |
| Operator fraud / mistaken entry | No delete, only reversal with reason; corrections after 10 min need manager approval; audit log. |
| Holding guest money (RBI rules) | Never hold funds — route UPI directly to host/vendor via PG split/route; platform only records. |
| Offline at mandapam | Design `ClientRef` idempotency from day 1 even if offline ships in v2. |
| DPDP Act 2023 consent | Consent checkbox at registration; SMS to new person includes opt-out. |
| OTP cost & abuse | Rate limit per mobile/IP, 30s resend timer, max 5/hour, captcha after 3 failures. |

---

## 8. Recommended stack (aligned with your doc)

| Layer | Choice |
|---|---|
| Web (tenant admin, operator counter, guest web pay) | Angular 18+ (standalone components, signals), PWA for counter offline |
| Mobile (individual + operator) | Flutter (single codebase Android/iOS) |
| API | ASP.NET Core 8, modular monolith, MediatR, EF Core |
| DB | SQL Server / Azure SQL with RLS |
| Cache / OTP | Redis |
| SMS / WhatsApp | MSG91 / Gupshup (DLT registered templates) |
| Payments | Razorpay / Cashfree (Route/Split) |
| Hosting | Azure App Service + Azure Front Door; Blob for receipts/exports |

---

## 9. Screen inventory (templates in `ui/`)

| Area | Screen | File |
|---|---|---|
| Common | Splash / loading logo | `splash.html` |
| Common | Login (mobile + OTP, operator PIN tab) | `login.html` |
| Common | OTP verification | `otp.html` |
| Common | Register (Individual / Moi Vendor) | `register.html` |
| Common | States: empty, offline, error, success, 404 | `states.html` |
| Individual | My Moi history (cross-vendor ledger) | `my-moi.html` |
| Individual / Guest | Scan QR & pay Moi | `pay.html` |
| Vendor | Tenant dashboard (today's functions) | `vendor-dashboard.html` |
| Vendor | Functions list + create function | `functions.html` |
| Vendor | Operators & assignment (3×3 flow) | `operators.html` |
| Operator | Counter entry | `counter.html` |
| Vendor | Reports | `reports.html` |
| Platform | Super admin — tenants | `admin.html` |

All screens are responsive: sidebar layout ≥ 960px, bottom navigation on mobile, safe for Android/iOS WebView (no hover-only actions, 44px touch targets, `viewport-fit=cover`, safe-area insets).
