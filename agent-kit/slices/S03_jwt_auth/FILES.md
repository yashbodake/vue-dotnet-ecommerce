# S03 FILES

## Allowed to read
- `Ecommerce.Web/App_Start/AdminSeed.cs`, Identity models
- `database/02_AspNetIdentity.sql`, `03_SeedAdmin.sql`

## Allowed to write
- `modern/Ecommerce.Api/**` auth services, endpoints, JWT config, Program auth
- `modern/Ecommerce.Api.Tests/**` auth tests

## Forbidden
- Vue login UI (S04)
- Cart merge (S06)
