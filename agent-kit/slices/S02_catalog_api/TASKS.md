# S02 TASKS

### T02.1 — SqlConnection factory + catalog POCOs
- Model tier: small
- Goal: Wire `Microsoft.Data.SqlClient` to `LegacyEcommerceDb`; add row/POCO types for Products/Categories/Images/Variants
- Allowed write paths: `modern/Ecommerce.Api/Data/**`, `appsettings.json` / Docker connection settings, Program DI for connection string
- Forbidden: EF packages; endpoints beyond a smoke query; business filter endpoints (T02.2)
- Steps:
  1. Add package `Microsoft.Data.SqlClient` only (optional: Dapper). **Do not** add EF Core
  2. `ISqlConnectionFactory` (or similar) returning open `SqlConnection`
  3. Simple POCOs matching table columns (not EF entities)
  4. One smoke query e.g. `SELECT COUNT(*) FROM Products` in a tiny health/test helper
- Acceptance: `dotnet build` OK; smoke query returns against seeded DB
- Stop condition: Native SQL connects; zero EF references in csproj

### T02.2 — ProductCatalogService + endpoints
- Model tier: small
- Goal: Implement filter/search/sort/paging with **parameterized SQL** + HTTP endpoints
- Inputs: T02.1
- Allowed write paths: `Services/ProductCatalog*`, `Contracts/Catalog*`, `Endpoints/Catalog*`, `Data/**` queries, Program maps
- Steps:
  1. Port filter logic from legacy ProductService into SQL WHERE/ORDER BY + OFFSET/FETCH
  2. Map GET health/categories/products/products/{id}
- Acceptance: `Invoke-RestMethod http://127.0.0.1:5100/api/products?page=1&pageSize=1` returns items when DB seeded (host run or Docker)
- Stop condition: Read API works manually

### T02.3 — Catalog unit tests
- Model tier: small
- Goal: xUnit tests for filter/sort/paging (fake connection factory or integration against Express)
- Allowed write paths: `modern/Ecommerce.Api.Tests/**`
- Acceptance: `dotnet test modern/Ecommerce.Api.Tests` — catalog tests pass
- Stop condition: Tests green; no EF InMemory packages
