# Ecommerce Modern

Modern migration of the legacy ASP.NET MVC ecommerce app.

## Stack

- **API**: .NET 10 minimal API, native SQL (`Microsoft.Data.SqlClient`), JWT Bearer auth
- **Gateway**: YARP reverse proxy
- **Frontend**: Vue 3 + Vite + Pinia + Vue Router
- **Database**: SQL Express `LegacyEcommerceDb` on the Windows host (not containerized)

## Quick start

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server Express with `LegacyEcommerceDb` (shared memory / TCP enabled)
- Docker Desktop (optional, for the containerized runtime)

### Local dev (no Docker)

1. Start API:
   ```bash
   cd modern/Ecommerce.Api
   dotnet run --urls "http://127.0.0.1:5100"
   ```
2. Start Vue (in a separate terminal):
   ```bash
   cd modern/ecommerce-web
   npm install
   npm run dev
   ```
3. Open http://localhost:5173

The Vite dev server proxies `/api/*` requests to the API on port `5100` (see `vite.config.ts`).

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

Requests enter through the YARP gateway on port `5000`. Paths under `/api/*` are forwarded to the .NET 10 API on port `5100`; all other paths are served by the Vue 3 SPA dev server on port `5173`. The API uses a `SqlConnection` factory to run parameterized queries directly against the host SQL Express `LegacyEcommerceDb`; no EF Core or EF6 is used in the modern code. Authentication is stateless JWT Bearer, with passwords hashed using PBKDF2 in the ASP.NET Identity v3 format.

## API endpoints

### Catalog (public)
- `GET /api/categories` — list categories with active products
- `GET /api/products` — list active products (query: `page`, `pageSize`, `search`, `categoryId`, `sortBy`, `inStock`, `minPrice`, `maxPrice`)
- `GET /api/products/{id}` — product detail with images and variants

### Auth
- `POST /api/auth/login` — obtain JWT
- `POST /api/auth/register` — register new customer (returns JWT)
- `GET /api/auth/me` — current user info (JWT required)

### Cart (guest cookie or JWT)
- `GET /api/cart` — current cart
- `GET /api/cart/count` — total item count
- `POST /api/cart/items` — add item (sets `ecommerce.cart_owner` cookie for guests)
- `PUT /api/cart/items/{cartItemId}` — update quantity
- `DELETE /api/cart/items/{cartItemId}` — remove item
- `POST /api/cart/merge` — merge guest cart into user cart (JWT required)

### Checkout (JWT required)
- `GET /api/checkout/shipping-options` — list Standard/Express options (public)
- `POST /api/checkout/place-order` — place order (transactional stock decrement + cart clear)
- `GET /api/checkout/orders/{orderId}` — order detail (404 for foreign orders)

### Account (JWT required)
- `GET /api/account/orders` — order history for current user
- `GET /api/account/orders/{orderId}` — order detail (returns 404 for foreign orders, not 403)

### Admin (JWT + Admin role required)
- `GET /api/admin/products` — all products including inactive
- `GET /api/admin/products/{id}` — single product
- `POST /api/admin/products` — create product
- `PUT /api/admin/products/{id}` — update product
- `DELETE /api/admin/products/{id}` — soft-delete product (`IsActive = false`)
- `GET /api/admin/categories` — list categories (for product form)
- `GET /api/admin/orders` — all orders (optional `?status=` filter)
- `PUT /api/admin/orders/{orderId}/status` — update order status

## Database

SQL Express `LegacyEcommerceDb` stays on the host and uses the same schema as the legacy application.

- Host connection: `Server=lpc:.\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True`
- Container connection: `Server=host.docker.internal,1433;Database=LegacyEcommerceDb;...` (requires a SQL login; containers cannot use Windows auth)

## Testing

Run the integration test suite against real SQL Express:

```bash
cd modern/Ecommerce.Api.Tests
dotnet test
```

The suite contains 73 integration tests covering catalog, auth, cart, checkout, account, and admin behavior.

### E2E tests (Playwright)

```bash
cd modern/ecommerce-web
npx playwright install chromium
npm run test:e2e
```

## Legacy vs Modern

| | Legacy | Modern |
| --- | --- | --- |
| Location | `Ecommerce.Web/` | `modern/` |
| Framework | ASP.NET MVC 5 | .NET 10 minimal API + YARP + Vue 3 |
| Data access | EF6 | Native SQL (`Microsoft.Data.SqlClient`) |
| Auth | ASP.NET Identity cookie | JWT Bearer |
| Hosting | IIS | Docker Compose |
| Database | SQL Express `LegacyEcommerceDb` | Same SQL Express `LegacyEcommerceDb` |

See `docs/migration/STATUS.md` for the full slice-by-slice migration status.
