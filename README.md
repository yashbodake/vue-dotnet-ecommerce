# Northline — Vue + .NET Ecommerce

Simple shop: Vue 3 frontend, .NET 10 API, YARP gateway, SQL Express on the host.

| Service | URL |
|---------|-----|
| Shop (gateway) | http://localhost:5000 |
| API | http://localhost:5100 |
| Vue (direct) | http://localhost:5173 |

## Structure

```text
src/
  frontend/                 # Vue 3 + Vite SPA
  backend/
    Ecommerce.Api/          # REST API (native SQL, no EF)
    Ecommerce.Gateway/      # YARP reverse proxy
    Ecommerce.Api.Tests/    # API tests
database/                   # SQL schema + seed scripts
tools/modern-up.ps1         # Docker Compose up + smoke checks
docker-compose.yml          # api + gateway + web
Ecommerce.sln               # backend solution
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- Docker Desktop (for Compose)
- SQL Server Express (`.\SQLEXPRESS`) with TCP enabled

## Database (once)

```powershell
# Run scripts in order against LegacyEcommerceDb
database\00_CreateSchema.sql
database\01_SeedData.sql
database\02_AspNetIdentity.sql
database\03_SeedAdmin.sql
```

Copy `.env.example` → `.env` and set `SQL_PASSWORD` if containers need SQL auth.

## Run (Docker)

```powershell
powershell -ExecutionPolicy Bypass -File tools\modern-up.ps1
```

Open http://localhost:5000

## Run (local, no Docker)

```powershell
# Terminal 1 — API
cd src\backend\Ecommerce.Api
dotnet run --urls http://127.0.0.1:5100

# Terminal 2 — Vue (proxies /api to :5100)
cd src\frontend
npm install
npm run dev
```

Open http://localhost:5173

## Demo admin

`admin@legacy.local` / `Admin123!`

## Tests

```powershell
dotnet test Ecommerce.sln
cd src\frontend
npm run build
npm run test:e2e
```

## Notes

- UI is Vue only (no Razor/MVC)
- Data access uses `Microsoft.Data.SqlClient` (no EF Core)
- SQL stays on the Windows host; Docker apps reach it via `host.docker.internal`
