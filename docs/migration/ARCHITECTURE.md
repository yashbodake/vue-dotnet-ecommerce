# Migration Architecture

## Overview

The modern ecommerce application replaces the legacy ASP.NET MVC 5 monolith with three cooperating services: a .NET 9 minimal API, a YARP reverse-proxy gateway, and a Vue 3 single-page application.

## Service topology

```
User
 |
 |  http://localhost:5000
 v
Ecommerce.Gateway (YARP) :5000
 |-- /api/*  --> Ecommerce.Api :5100
 |-- /*      --> ecommerce-web (Vue/Vite) :5173
 |
 |  host.docker.internal:1433
 v
SQL Express LegacyEcommerceDb (host)
```

- **Gateway** is the single browser entry point. It routes API calls to the backend and all other requests to the Vue dev server.
- **API** contains all business logic and data access. It is stateless and exposes REST endpoints under `/api`.
- **Vue SPA** is a client-side rendered frontend that talks to the API through the gateway.
- **SQL Express** remains on the Windows host. Containers reach it via `host.docker.internal` over TCP.

## Data access

The modern API uses native SQL only. Each service class receives an `IDbConnectionFactory` that creates and opens `Microsoft.Data.SqlClient.SqlConnection` instances. All queries are parameterized; no string concatenation of user input is used. EF Core, EF6, `DbContext`, EF migrations, and SQL Server containers are intentionally absent from the modern codebase.

## Authentication

- Login endpoint verifies the email and password using PBKDF2 hashing compatible with ASP.NET Identity v3.
- Successful login returns a JWT Bearer token signed with a configurable key.
- The Vue app stores the token and sends it in the `Authorization: Bearer <token>` header.
- `[Authorize]` endpoints reject missing or invalid tokens with 401.
- Admin endpoints require both authentication and an `Admin` role claim; authenticated non-admin users receive 403.

## Cart

- Guest carts are tracked by a signed browser cookie (`cart_id`) containing a GUID.
- Authenticated users have their cart owner stored as their user ID.
- On login, any guest cart is merged into the user's cart.
- Cart endpoints return 400 for validation failures and invalid stock.

## Checkout

Placing an order is transactional:

1. Validate cart contents and stock availability.
2. Open a SQL transaction.
3. Decrement product stock for each line item.
4. Create the order and order lines.
5. Clear the cart.
6. Commit the transaction.

Stock failures return 400; the order is not created.

## Account and order access

- `POST /api/account/register` creates a new customer with a hashed password.
- `GET /api/account/orders` returns the authenticated user's order history.
- `GET /api/account/orders/{orderId}` returns order details only for orders owned by the caller. Accessing another user's order returns 404 to prevent IDOR leaks.

## Admin authorization

An `Admin` authorization policy checks the `role` claim for `Admin`. Admin endpoints include:

- Product CRUD (creation, update, soft-delete via `IsActive = false`; hard deletion is not used).
- Order status updates.
- Listing all orders.

## Docker Compose

The `modern/docker-compose.yml` brings up:

- `api` on internal port 5100
- `gateway` on host port 5000
- `web` on host port 5173

SQL Express is not containerized; the API uses `host.docker.internal,1433` and must authenticate with a SQL login because Windows authentication does not work from Linux containers.

## Legacy relationship

`Ecommerce.Web/` is the original ASP.NET MVC 5 codebase and remains in the repo as a read-only parity reference. The modern stack re-implements the same domain and uses the same `LegacyEcommerceDb` schema, allowing a side-by-side migration path.
