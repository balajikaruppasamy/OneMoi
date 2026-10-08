# OneMoi

**One Mobile. One Moi Identity. Every Function.** · ஒரே Mobile — முழு மொய் வரலாறு

A multi-tenant SaaS for Moi vendors (JD Moi Tech, Meenakshi Moi, …). Each person also gets one Moi history that spans every vendor.

## Quick start

```powershell
D:\OneMoi\run-dev.ps1
```
* Web: http://localhost:4200 (the login page has a **🧪 Demo logins** panel)
* API: http://localhost:5080/swagger
* Database: PostgreSQL `onemoi` on localhost:5432. Tables and demo data are created automatically on the first API start.

## Folders

| Folder | What |
|---|---|
| `src/backend` | .NET 10 API: Domain → Application (business logic) → Infrastructure (database) → Api |
| `src/frontend/onemoi-web` | Angular 20 web app, responsive for desktop, mobile and WebView |
| `database` | PostgreSQL schema script, pgAdmin exploration queries, reset script |
| `docs` | [MODULE_ANALYSIS](docs/MODULE_ANALYSIS.md) · [ARCHITECTURE](docs/ARCHITECTURE.md) · [DATABASE](docs/DATABASE.md) · [SECURITY](docs/SECURITY.md) · [DEPLOYMENT](docs/DEPLOYMENT.md) |
| `ui`, `brand` | approved HTML templates, logo and loader |
