# Legacy Ecommerce

ASP.NET MVC 5 · .NET Framework 4.7.2 · EF6 · SQL Server Express

Layered solution: `Ecommerce.Web → Ecommerce.Services → Ecommerce.Data → Ecommerce.Core`

## Specs completed
- Spec 00 — Database schema + seed (`database/`)
- Spec 01 — Solution skeleton + Unity DI
- Spec 02 — Core domain models + service interfaces

## Setup
1. Ensure SQL Server Express is running (`.\SQLEXPRESS`)
2. Run `database/00_CreateSchema.sql` then `database/01_SeedData.sql`
3. Open `Ecommerce.sln` in Visual Studio 2022
4. Restore NuGet packages and run `Ecommerce.Web`
5. Smoke test: `/Test` should return product count
