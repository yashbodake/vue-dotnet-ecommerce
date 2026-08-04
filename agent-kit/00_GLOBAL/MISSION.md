# Mission

## Goals
- Rebuild a **modern-only** eCommerce app (.NET 10 API + Vue 3 + YARP) with **behavioral parity** to Legacy-Ecommerce `main`.
- Reuse **host** SQL Express database **`LegacyEcommerceDb`** via **native SQL** (`Microsoft.Data.SqlClient` / parameterized queries — **no EF Core**). No big-bang schema rewrite. **Do not** put SQL Server in Docker.
- Run the **.NET 10 stack under Docker Compose** (Api + Gateway + Vue). Launch via `tools/modern-up.ps1` → `docker compose up`.
- Structure work so **small models** execute tiny tasks with slice-local context (token-efficient).

## Non-goals
- Saved shipping address book / profile addresses
- Cookie ↔ JWT SSO bridge
- Deleting or “fixing” the legacy MVC repo history
- Rewriting Specs 00–10 as Vue documentation
- Production hardening (secrets vault, real payments, CDN)
- Replacing SQL Express with Linux SQL container / Azure SQL (unless Planner revises)

## Definition of done (whole migration)
- [ ] `tools/modern-up.ps1` brings up **Docker** gateway `:5000`, API `:5100`, Vue `:5173`
- [ ] API container reaches **host** `LegacyEcommerceDb` on SQL Express
- [ ] Catalog, cart, checkout, register/login, orders, admin work via `:5000`
- [ ] JWT auth; demo admin seeded by API: `admin@legacy.local` / `Admin123!`
- [ ] Modern tree has **no** project reference to Ecommerce.Web/Core/Services/Data
- [ ] API tests (host `dotnet test`) + Playwright happy paths green
- [ ] README states old vs new (Legacy `main` vs modern product) and Docker + SQL Express split

## Out of scope (explicit)
| Item | Why |
|------|-----|
| Real payment gateway | Legacy uses demo card validation only |
| Mobile native apps | Web SPA only |
| Multi-tenant / multi-DB | Single SQL Express catalog |
| SQL Server in Docker | Host Express is source of truth |
