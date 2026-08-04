# Migration Status

## Overview

Legacy ASP.NET MVC ecommerce app was migrated to **Vue 3 + .NET 10 API + YARP**. Razor/MVC projects have been **removed**. Product code lives under `src/frontend` and `src/backend`.

## Slices

| Slice | Description | Status |
|-------|-------------|--------|
| S01 | Skeleton: API + Gateway + Tests, Vue scaffold, Docker Compose | Complete |
| S02 | Catalog API: `SqlConnection` factory, catalog endpoints | Complete |
| S03 | JWT auth + `AdminUserSeeder` | Complete |
| S04 | Vue catalog + login UI | Complete |
| S05 | YARP gateway routes + CORS + health checks | Complete |
| S06 | Cart API + tests + Vue cart UI | Complete |
| S07 | Checkout API + tests + Vue checkout wizard | Complete |
| S08 | Account register + order history + Vue pages | Complete |
| S09 | Admin product CRUD + order status + Vue admin UI | Complete |
| S10 | Polish: redirects, E2E tests, README + docs | Complete |
| Post | Restructure to `src/frontend` + `src/backend`; delete MVC/Razor | Complete |

## Layout (final)

```text
src/frontend/                 # Vue 3 SPA
src/backend/Ecommerce.Api/
src/backend/Ecommerce.Gateway/
src/backend/Ecommerce.Api.Tests/
Ecommerce.sln
docker-compose.yml
```

## Tests

```powershell
dotnet test Ecommerce.sln
cd src\frontend
npm run test:e2e
```

## Runtime note

Full Docker stack needs Docker Desktop + SQL Express TCP for `host.docker.internal`. Host `dotnet test` uses Trusted_Connection to `.\SQLEXPRESS`.
