# S02 ACCEPTANCE

- [ ] `GET /api/health` OK (include active product count if easy)
- [ ] `GET /api/categories` returns categories with active products
- [ ] `GET /api/products` supports page, pageSize, search, sortBy, categoryIds, price, inStockOnly
- [ ] `GET /api/products/{id}` returns detail or 404
- [ ] Catalog reads use **native SQL** only (no EF Core in Api csproj)
- [ ] `dotnet test` catalog-related tests pass
- [ ] DB must be seeded (`.\ecom db` or `db-setup.ps1`) for manual checks
