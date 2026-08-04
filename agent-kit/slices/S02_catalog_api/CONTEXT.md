# S02 — Catalog API

## Goal
Native SQL access to existing catalog tables; expose health, categories, products list/detail with filter/search/sort/paging parity to legacy `ProductService`.

## Parity notes
- Page size default ~12
- Filters: search, categoryIds, min/max price, inStockOnly, sortBy (`name`, `price_asc`, `price_desc`, `newest`)
- Only active products for public catalog (`IsActive`)
- Categories: those with active products
- Detail includes images + variants

## Owns
- `GET /api/health`
- `GET /api/categories`
- `GET /api/products`
- `GET /api/products/{id}`
- `SqlConnection` factory / catalog query helpers (no EF)

## Tables
`Products`, `Categories`, `ProductImages`, `ProductVariants`

## Depends on
S01

## Pitfalls
- Match existing column names; do not rewrite schema
- **No EF Core / DbContext / migrations**
- **Host tests:** `Server=.\SQLEXPRESS;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True`
- **Docker API:** `Server=host.docker.internal,1433;...` + SQL login (TCP enabled on Express)
- Parameterize all filters (`@search`, `@page`, etc.)
