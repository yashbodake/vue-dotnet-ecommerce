# Migration Status

## Overview

Legacy ASP.NET MVC ecommerce application migrated to a modern stack: .NET 9 minimal API, YARP reverse-proxy gateway, and Vue 3 SPA.

## Slices

| Slice | Description | Status |
|-------|-------------|--------|
| S01 | Skeleton: API + Gateway + Tests projects, Vue scaffold, Docker Compose | Complete |
| S02 | Catalog API: `SqlConnection` factory, `ProductCatalogService`, catalog endpoints | Complete |
| S03 | JWT auth + `AdminUserSeeder` | Complete |
| S04 | Vue catalog + login UI | Complete |
| S05 | YARP gateway routes + CORS + health checks | Complete |
| S06 | Cart API + tests + Vue cart UI | Complete |
| S07 | Checkout API + tests + Vue checkout wizard | Complete |
| S08 | Account register + order history + Vue pages | Complete |
| S09 | Admin product CRUD + order status + Vue admin UI | Complete |
| S10 | Polish: legacy redirects, card layout, E2E tests, README + docs | Complete |

## Tests

Approximately 73 integration tests run against real SQL Express on the host:

| Area | Test count |
|------|------------|
| Catalog | 20 |
| Auth | 8 |
| Cart | 15 |
| Checkout | 11 |
| Account | 11 |
| Admin | 16 |

Run with:

```bash
cd modern/Ecommerce.Api.Tests
dotnet test
```

## Notes

- Docker runtime requires Docker Desktop; it was not available during agent development.
- All modern code is build-verified; runtime smoke tests require Docker and a reachable SQL Express instance.
- Heavy parallel test load can exhaust SQL Express named pipes; restart the SQL Express service if tests start timing out.
