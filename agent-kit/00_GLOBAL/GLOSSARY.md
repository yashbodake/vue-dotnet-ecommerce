# Glossary

| Term | Meaning |
|------|---------|
| **Legacy** | ASP.NET MVC 5 app on `main` (`Ecommerce.Web` + Services/Data/Core), IIS `:44300` |
| **Modern** | .NET 10 API + Vue + YARP under `modern/` (or Ecommerce-Modern repo) |
| **LegacyEcommerceDb** | Host SQL Express DB shared by legacy and modern (not containerized) |
| **Native SQL** | `Microsoft.Data.SqlClient` + parameterized SQL (no EF Core / DbContext) |
| **Docker Compose** | Runs Api + Gateway + Vue; DB remains on host Express |
| **host.docker.internal** | How containers reach SQL Express on the Windows host |
| **AspNetUsers** | Identity user table; JWT validates its `PasswordHash` |
| **JWT** | Bearer token from `/api/auth/login` for Vue `sessionStorage` |
| **Cart owner cookie** | `ecommerce.cart_owner` — anonymous cart key |
| **Soft-delete** | `Products.IsActive = false` |
| **YARP** | Reverse proxy gateway project `Ecommerce.Gateway` |
| **Slice `Sxx`** | Vertical migration increment (see SLICE_INDEX) |
| **Task `Txx.y`** | Small-model work unit inside a slice |
| **IDOR** | Insecure direct object reference — foreign order id must 404 |
| **Strangler Fig** | Route traffic gradually to modern; final product is modern-only |
