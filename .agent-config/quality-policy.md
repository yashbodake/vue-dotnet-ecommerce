# Quality Policy — the gate

The single source of truth for build / test / scan commands. Every agent runs these to validate. The **repair-agent** runs the full gate after each batch; the **final-verifier** re-runs it independently.

## Environment note (.NET 10)

On this machine .NET 10 is a user install. In PowerShell prepend:

```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
```

In Git Bash:

```bash
export PATH="$LOCALAPPDATA/Microsoft/dotnet:$PATH"
```

Confirm with `dotnet --version` before continuing.

All commands assume the working directory is the repo root (`Ecommerce-Redo/`) unless a `cd` is shown.

## 1. Build

```bash
# Backend (all projects in the solution)
dotnet build Ecommerce.sln

# Frontend: typecheck (vue-tsc -b) THEN production build
cd src/frontend && npm run build && cd ../..
```

`npm run build` is `vue-tsc -b && vite build` — the `vue-tsc -b` step is the **typecheck gate**. It failing means the gate fails.

## 2. Unit tests (backend)

```bash
dotnet test Ecommerce.sln --nologo
```

Test project: `src/backend/Ecommerce.Api.Tests` (xUnit). Files: `AccountTests.cs`, `AdminTests.cs`, `AuthTests.cs`, `CartTests.cs`, `CatalogEndpointsTests.cs`, `CheckoutTests.cs`.

### With coverage (Coverlet is wired)

```bash
dotnet test Ecommerce.sln --collect:"XPlat Code Coverage"
```

## 3. End-to-end tests (frontend, Playwright)

One-time browser install on a fresh machine:

```bash
cd src/frontend && npx playwright install chromium && cd ../..
```

The e2e suite **requires the API running** on `http://127.0.0.1:5100` (Playwright's `webServer` only starts the Vue dev server). Start the API first:

```bash
# Terminal 1
cd src/backend/Ecommerce.Api && dotnet run --urls http://127.0.0.1:5100
```

Then run e2e:

```bash
# Terminal 2
cd src/frontend && npm run test:e2e && cd ../..
```

Config: `src/frontend/playwright.config.ts` — `testDir: './e2e'`, single Chromium project, `workers: 1`, `fullyParallel: false`, `reporter: 'html'`.

## 4. Static analysis / typecheck

```bash
# Frontend typecheck (also covered by `npm run build`)
cd src/frontend && npx vue-tsc -b && cd ../..
```

**Known gaps — do not claim these gates exist:**

- No .NET Roslyn analyzers / StyleCop / `dotnet format` configured.
- No ESLint / Prettier / Stylelint on the frontend.
- No `.editorconfig`.

If an agent needs one of these, it must note the gap rather than assert a passing "lint" step.

## 5. Dependency / vulnerability scan (native — always run)

```bash
# Backend — run per project that has package references
dotnet list src/backend/Ecommerce.Api/Ecommerce.Api.csproj --vulnerable
dotnet list src/backend/Ecommerce.Gateway/Ecommerce.Gateway.csproj --vulnerable
dotnet list src/backend/Ecommerce.Api.Tests/Ecommerce.Api.Tests.csproj --vulnerable

# Frontend
cd src/frontend && npm audit --omit=dev && cd ../..
```

These need no install and no network beyond the standard feeds. Treat any reported vulnerability as a lead to verify against the actual code path (see `security-policy.md`).

## 6. Smoke (optional, when the full stack is up)

```bash
# Via Docker Compose helper
powershell -ExecutionPolicy Bypass -File tools/modern-up.ps1
# Smoke GETs: /api/health, /api/products?page=1&pageSize=1, /
```

## Gate summary (the canonical checklist)

A change passes the gate when **all** of these are green on a clean tree:

1. `dotnet build Ecommerce.sln`
2. `dotnet test Ecommerce.sln --nologo`
3. `cd src/frontend && npm run build`
4. `cd src/frontend && npm run test:e2e` (API must be running)
5. `dotnet list ... --vulnerable` (per project) — no high/critical unaddressed
6. `cd src/frontend && npm audit --omit=dev` — no high/critical unaddressed

Any step failing = the change is not ready.
