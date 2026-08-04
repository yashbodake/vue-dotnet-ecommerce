# S02 FILES

## Allowed to read
- `Ecommerce.Services/ProductService.cs`, `Ecommerce.Core/**` (parity)
- `database/00_CreateSchema.sql`
- `Ecommerce.Data/Entities/*` (column/shape reference only — do not copy EF patterns)

## Allowed to write
- `modern/Ecommerce.Api/**` (Data SQL helpers, Services catalog, Endpoints, Program, appsettings)
- `modern/Ecommerce.Api.Tests/**` (catalog tests)

## Forbidden
- Any `Microsoft.EntityFrameworkCore*` package
- Auth/JWT (S03), Vue (S04), Cart (S06)
- Legacy project edits
