# Architecture

## Overview

Browser → YARP Gateway → Vue SPA / .NET 10 API → host SQL Express.

Legacy Razor/MVC is **not** in the product tree.

## Topology

```text
User
 |
 |  http://localhost:5000
 v
Ecommerce.Gateway (YARP) :5000
 |-- /api/*  --> Ecommerce.Api :5100
 |-- /*      --> src/frontend (Vue) :5173 / nginx:80 in Docker
 |
 |  host.docker.internal:1433
 v
SQL Express LegacyEcommerceDb (host — not containerized)
```

| Path | Role |
|------|------|
| `src/frontend` | Vue 3 + Vite + Pinia storefront + admin |
| `src/backend/Ecommerce.Api` | REST + JWT + native SQL |
| `src/backend/Ecommerce.Gateway` | YARP browser entry |
| `src/backend/Ecommerce.Api.Tests` | xUnit against host SQL |
| `database/` | Schema + seed scripts |

## Data access

Native SQL only (`Microsoft.Data.SqlClient`, parameterized). No EF Core / `DbContext` / SQL container.

## Auth

JWT Bearer; passwords compatible with ASP.NET Identity hasher; `AdminUserSeeder` on API startup (`admin@legacy.local` / `Admin123!`).

## Cart / checkout / admin

- Guest cookie `ecommerce.cart_owner`; merge on login
- Shipping: `Standard` / `Express`
- Order IDOR → 404
- Soft-delete products: `IsActive = false`
- Admin role required for `/api/admin/*`
