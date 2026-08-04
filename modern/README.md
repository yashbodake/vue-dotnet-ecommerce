# Ecommerce Modern

Modern migration of the legacy ASP.NET MVC ecommerce app.

## Stack

- **API**: .NET 9 minimal API, native SQL (`Microsoft.Data.SqlClient`), JWT Bearer auth
- **Gateway**: YARP reverse proxy
- **Frontend**: Vue 3 + Vite + Pinia + Vue Router
- **Database**: SQL Express `LegacyEcommerceDb` on the Windows host (not containerized)

## Quick start

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- SQL Server Express with `LegacyEcommerceDb` (TCP enabled)
- Docker Desktop (for the containerized runtime)

### Local dev (no Docker)

1. Start API:
   ```bash
   cd modern/Ecommerce.Api
   dotnet run
   ```
2. Start Vue:
   ```bash
   cd modern/ecommerce-web
   npm install
   npm run dev
   ```
3. Open http://localhost:5173

### Docker (full stack)

1. Build and start all services:
   ```bash
   cd modern
   docker compose up --build -d
   ```
2. Open http://localhost:5000

The gateway listens on port `5000`, the API on `5100`, and the Vue dev server on `5173`.

### Demo admin

- **Email**: `admin@legacy.local`
- **Password**: `Admin123!`

## Architecture

Requests enter through the YARP gateway on port `5000`. Paths under `/api/*` are forwarded to the .NET 9 API on port `5100`; all other paths are served by the Vue 3 SPA dev server on port `5173`. The API uses a `SqlConnection` factory to run parameterized queries directly against the host SQL Express `LegacyEcommerceDb`; no EF Core or EF6 is used in the modern code. Authentication is stateless JWT Bearer, with passwords hashed using PBKDF2 in the ASP.NET Identity v3 format.

## API endpoints

### Catalog
- `GET /api/products` — list active products
- `GET /api/products/{id}` — product detail

### Auth
- `POST /api/auth/login` — obtain JWT
- `GET /api/auth/me` — current user info

### Cart
- `GET /api/cart` — current cart
- `POST /api/cart/items` — add item
- `PUT /api/cart/items/{productId}` — update quantity
- `DELETE /api/cart/items/{productId}` — remove item

### Checkout
- `POST /api/checkout` — place order (transactional stock decrement + cart clear)
- `GET /api/checkout/confirmation/{orderId}` — order confirmation

### Account
- `POST /api/account/register` — register new customer
- `GET /api/account/orders` — order history
- `GET /api/account/orders/{orderId}` — order detail (returns 404 for foreign orders)

### Admin
- `GET /api/admin/products` — all products including inactive
- `POST /api/admin/products` — create product
- `PUT /api/admin/products/{id}` — update product
- `DELETE /api/admin/products/{id}` — soft-delete product (`IsActive = false`)
- `GET /api/admin/orders` — all orders
- `PUT /api/admin/orders/{orderId}/status` — update order status

## Database

SQL Express `LegacyEcommerceDb` stays on the host and uses the same schema as the legacy application.

- Host connection: `Server=.\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True`
- Container connection: `Server=host.docker.internal,1433;Database=LegacyEcommerceDb;...` (requires a SQL login; containers cannot use Windows auth)

## Testing

Run the integration test suite against real SQL Express:

```bash
cd modern/Ecommerce.Api.Tests
dotnet test
```

The suite contains approximately 73 integration tests covering catalog, auth, cart, checkout, account, and admin behavior.

## Legacy vs Modern

| | Legacy | Modern |
| --- | --- | --- |
| Location | `Ecommerce.Web/` | `modern/` |
| Framework | ASP.NET MVC 5 | .NET 9 minimal API + YARP + Vue 3 |
| Data access | EF6 | Native SQL (`Microsoft.Data.SqlClient`) |
| Auth | ASP.NET Identity cookie | JWT Bearer |
| Hosting | IIS | Docker Compose |
| Database | SQL Express `LegacyEcommerceDb` | Same SQL Express `LegacyEcommerceDb` |

See `docs/migration/STATUS.md` for the full slice-by-slice migration status.
