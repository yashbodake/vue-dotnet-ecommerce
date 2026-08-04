# Slice index

Execute **in order**. Do not start a slice until the previous `ACCEPTANCE.md` is green.

| ID | Name | Depends on | Primary deliverable | Verify |
|----|------|------------|---------------------|--------|
| **S01** | Skeleton | — | `modern/` Api+Gateway+Vue sln, Dockerfiles, compose, `modern-up.ps1` | `dotnet build` + `docker compose config` |
| **S02** | Catalog API | S01 | Native SQL data access + `/api/health` + products/categories | `dotnet test` catalog tests; GET products |
| **S03** | JWT + AdminSeed | S02 | `/api/auth/login`, `/me`, startup admin seed | Login returns token + Admin role |
| **S04** | Vue catalog + login | S03 | Home, filters, detail, login page | Manual/UI: list products, login |
| **S05** | YARP | S04 | Gateway `:5000` → api + vue | `GET :5000/api/health`, `:5000/` |
| **S06** | Cart | S05 | `/api/cart*` + Vue `/cart` | Add line as guest; merge on login |
| **S07** | Checkout | S06 | Place-order API + Vue wizard | Place order as admin; confirmation |
| **S08** | Account | S07 | Register + `/orders` IDOR→404 | Register user; foreign order 404 |
| **S09** | Admin | S08 | `/api/admin/*` + Vue `/admin/*` | Soft-delete + status update |
| **S10** | Polish + E2E | S09 | Redirects, README, Playwright suite, docs | `npm run test:e2e` 4+ tests green |

## Model tiers
- Tasks inside slices → **small**
- Slice acceptance failures spanning files → **medium** Integrator
- Changing this index / RULES → **high** Planner only
