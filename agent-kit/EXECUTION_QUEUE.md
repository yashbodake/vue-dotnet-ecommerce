# Execution queue

Run **one** task per small-model chat. Check off when acceptance passes.

Attach always: `00_GLOBAL/RULES.md` + slice `CONTEXT.md` + `FILES.md` + template wrapper.

## S01 Skeleton
- [x] T01.1 Create API + Gateway + Tests projects
- [x] T01.2 Scaffold Vue 3 + Vite + Pinia + Router
- [x] T01.3 Docker Compose + modern-up.ps1  

→ then `slices/S01_skeleton/ACCEPTANCE.md`

## S02 Catalog API
- [x] T02.1 SqlConnection factory + catalog POCOs
- [x] T02.2 ProductCatalogService + endpoints
- [x] T02.3 Catalog unit tests  
→ S02 ACCEPTANCE (complete)

## S03 JWT + AdminSeed
- [x] T03.1 AspNet entities + AuthService login/JWT
- [x] T03.2 AdminUserSeeder + /api/auth/me
- [x] T03.3 Auth unit/API tests  
→ S03 ACCEPTANCE (complete)

## S04 Vue catalog + login
- [x] T04.1 API client + auth store + LoginView
- [x] T04.2 Catalog store + HomeView + ProductCard
- [x] T04.3 ProductDetailView  
→ S04 ACCEPTANCE (complete)

## S05 YARP
- [ ] T05.1 Configure YARP routes + health
- [ ] T05.2 CORS + modern-up health checks  
→ S05 ACCEPTANCE

## S06 Cart
- [ ] T06.1 CartService + endpoints + cookie
- [ ] T06.2 Cart unit tests
- [ ] T06.3 Vue cart store + CartView + Add buttons  
→ S06 ACCEPTANCE

## S07 Checkout
- [ ] T07.1 CheckoutService + endpoints
- [ ] T07.2 Checkout tests
- [ ] T07.3 Vue checkout wizard  
→ S07 ACCEPTANCE

## S08 Account
- [ ] T08.1 Register + account order endpoints
- [ ] T08.2 Account tests
- [ ] T08.3 Vue register + orders pages  
→ S08 ACCEPTANCE

## S09 Admin
- [ ] T09.1 AdminService + endpoints + policy
- [ ] T09.2 Admin tests
- [ ] T09.3 Vue admin UI  
→ S09 ACCEPTANCE

## S10 Polish + E2E
- [ ] T10.1 Redirects + card layout polish
- [ ] T10.2 Playwright e2e suite
- [ ] T10.3 README old-vs-new + docs status  
→ S10 ACCEPTANCE → Mission DoD

## Next task ready now
**T01.3** — use [`NEXT_TASK.md`](./NEXT_TASK.md) (regenerate that file when advancing the queue).
