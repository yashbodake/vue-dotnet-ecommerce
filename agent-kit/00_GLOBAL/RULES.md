# Rules (every Executor must follow)

## Diff hygiene
- Change only files allowed by the current task’s `FILES.md` / task card.
- No drive-by refactors, no formatting unrelated files, no new deps without task approval.
- Prefer small PRs / commits per task or per slice.

## Anti-bloat
- No unused packages; no god services/mega files for a tiny task.
- Do not copy legacy EF/`DbContext` patterns into modern.
- Delete dead code you introduce in the same task if it ends unused.
- Prefer the smallest diff that meets acceptance.

## Stack conventions
- API: .NET 10 minimal APIs or endpoint groups under `modern/Ecommerce.Api`
- DTOs in `Contracts/`; services in `Services/`; SQL access in `Data/` (POCOs + repositories/queries)
- **Data access = native SQL only:** `Microsoft.Data.SqlClient` + parameterized queries. Optional thin mapper: Dapper.
- **Forbidden:** EF Core, EF6, `DbContext`, LINQ-to-Entities, migrations packages
- Vue: Composition API `<script setup>`, Pinia stores, Vue Router
- JSON: camelCase in HTTP; C# DTOs use PascalCase with default System.Text.Json camelCase

## Docker + SQL Express
- **DB stays on host SQL Express** (`LegacyEcommerceDb`). Never add a `mssql` service unless a Planner revises MISSION.
- **Api / Gateway / Vue run in Docker Compose** under `modern/`.
- Container → DB: `host.docker.internal` + TCP (not `lpc:.\\SQLEXPRESS` inside containers).
- Host unit tests / local `dotnet run`: Windows connection to `.\\SQLEXPRESS` is fine.
- `tools/modern-up.ps1` must wrap `docker compose up --build` (not long-lived host `dotnet run` processes for the runtime stack).
- `tools/db-setup.ps1` / SQLCMD run **on the host** against Express only.
- Do not bake real passwords into images; use compose `environment` / `.env` (gitignored) for SQL login.

### SQL Express prerequisites (host)
1. Instance `.\SQLEXPRESS` (or documented equivalent) with database `LegacyEcommerceDb`.
2. **TCP/IP enabled** in SQL Server Configuration Manager; prefer fixed port **1433**.
3. Containers reach DB via `host.docker.internal` — use a **SQL login** in Docker connection string (Trusted_Connection from Linux containers is unreliable).
4. Host `dotnet test`: `Trusted_Connection=True` to `.\\SQLEXPRESS` is OK.
5. If API container cannot connect: check TCP, firewall, login rights, and that Express is running — report `BLOCKED:` rather than inventing a SQL container.

## Security
- Parameterized SQL only — never string-concat user input into queries.
- Guest cart cookie (`ecommerce.cart_owner`) is **not** authentication.
- Checkout / orders / admin require JWT.
- Missing/invalid JWT on protected routes → **401**; authenticated non-Admin on admin APIs → **403**.
- IDOR (foreign order/product access) → **404**, not 403.
- Do not persist PAN / full card numbers.
- No real secrets in git; demo admin is a documented credential only.
- Soft-delete: `IsActive = false` (no hard DELETE when FKs exist).

## Adversarial / negative tests
When a slice owns the behavior, acceptance must include failure cases, not only happy paths:

| Case | Status |
|------|--------|
| Bad login | 401 |
| Missing/invalid JWT on protected route | 401 |
| Authenticated but not Admin | 403 |
| Anonymous admin API | 401 |
| Order/product not found or IDOR | **404** |
| Validation / business rule (checkout, cart stock) | 400 |

## HTTP / errors
| Case | Status |
|------|--------|
| Bad login | 401 |
| Missing/invalid JWT on protected route | 401 |
| Authenticated but not Admin | 403 |
| Order/product not found or IDOR | **404** (not 403) |
| Validation / business rule (checkout, cart stock) | 400 with clear message |

## Cart
- Guest cookie name: `ecommerce.cart_owner` (string owner id)
- Anonymous cart allowed; checkout/orders/admin require JWT
- On login: merge guest cart into user cart (legacy parity)

## Checkout
- Shipping methods exact: `Standard`, `Express`
- Demo card validation only; do not persist PAN
- Order number display: `#OrderId`

## Admin / catalog
- Soft-delete product: `IsActive = false` (never hard delete rows with FKs)
- Admin APIs: `[Authorize]` + role `Admin`

## Auth seed
- Demo: `admin@legacy.local` / `Admin123!`
- Seed from API startup (do not require MVC AdminSeed for modern demo)

## Contract freeze
- After a slice ACCEPTANCE is green, its public endpoints/DTO shapes are **frozen**.
- Later Executors must not silently change those contracts.
- Breaking changes → Integrator only (`prompts/INTEGRATOR.md`).

## Blocked protocol
If you cannot meet acceptance without leaving the task scope:
1. **Stop** — do not invent cross-slice APIs or features.
2. Report: `BLOCKED: <reason>` + commands tried.
3. Hand off to Integrator/human; do not advance the queue in the same chat.

## Tests
- API logic slices: xUnit tests in `Ecommerce.Api.Tests` (run on **host** with `dotnet test`)
- Include negative cases listed above where the slice owns them
- UI slices: Vitest for stores/components when cheap; Playwright for slice acceptance where specified
- Runtime smoke: `docker compose` healthy + `GET http://127.0.0.1:5000/api/health`
- .NET 10 PATH (host builds/tests): `$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"`

## Pitfalls (do not repeat)
- Always parameterize SQL (`@id`, `@ownerId`); never string-concat user input
- Cart/order reads: write explicit JOINs / focused SELECTs — avoid over-fetching wide result sets
- Playwright: prefer `getByRole('textbox', { name: … })` over fragile label locators
- NuGet NU1903 OpenAPI warning: optional follow-up, not a slice blocker

## Secrets
- No real secrets in git. Demo admin password is an intentional documented credential.
