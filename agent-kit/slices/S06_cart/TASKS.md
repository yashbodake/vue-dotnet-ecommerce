# S06 TASKS

### T06.1 — CartService + endpoints + cookie
- Model tier: small
- Goal: Full cart API with owner cookie + optional JWT user id
- Allowed write paths: Api cart* Program
- Acceptance: Add item anonymously (cookie set); GET cart returns line; count endpoint works
- Stop condition: API cart CRUD OK

### T06.2 — Cart unit tests
- Model tier: small
- Goal: Cover add, qty, remove, merge, stock error
- Allowed write paths: Api.Tests
- Acceptance: `dotnet test` cart tests pass
- Stop condition: Green

### T06.3 — Vue cart store + CartView + Add buttons
- Model tier: small
- Goal: UI add from catalog/detail; `/cart` page qty/remove; merge after login
- Allowed write paths: ecommerce-web src
- Acceptance: Guest add → see cart; login merges; badge count updates
- Stop condition: Cart UX works via gateway
