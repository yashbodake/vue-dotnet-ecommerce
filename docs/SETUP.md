# Setup — Ecommerce Modern

## Prerequisites

1. SQL Server Express — instance `.\SQLEXPRESS`
2. .NET 10 SDK (`%LOCALAPPDATA%\Microsoft\dotnet` on user install)
3. Node.js 20+
4. Docker Desktop (for full stack)

## Database

Run scripts under `database/` against `.\SQLEXPRESS` (see root README). Admin user is also ensured when **Ecommerce.Api** starts.

## Vue packages

```powershell
cd src\frontend
npm install
```

## Run (Docker)

```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
powershell -ExecutionPolicy Bypass -File tools\modern-up.ps1
```

| URL | Expected |
|-----|----------|
| http://localhost:5000/ | Vue catalog via gateway |
| http://localhost:5000/api/health | Healthy |
| http://localhost:5000/login | JWT login |

**Admin:** `admin@legacy.local` / `Admin123!`

## Connection strings

Host (API / tests):

```
Server=lpc:.\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True
```

Docker API container:

```
Server=host.docker.internal,1433;Database=LegacyEcommerceDb;User Id=...;Password=...;TrustServerCertificate=True
```

See `.env.example`. Enable SQL Express **TCP/IP** for containers.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| API cannot connect | Start SQL Express; check connection string |
| Empty catalog | Re-run `database/05_SeedCatalogFromDummyJson.sql` |
| Admin login fails | Start API once so `AdminUserSeeder` runs |
| Port in use | Stop processes on 5000/5100/5173 |
| Docker cannot reach SQL | Enable TCP/IP + SQL login; use `host.docker.internal` |
