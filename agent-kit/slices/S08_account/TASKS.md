# S08 TASKS

### T08.1 — Register + account order endpoints
- Model tier: small
- Goal: Register + list/detail with IDOR 404
- Allowed write paths: Api auth/account*
- Acceptance: Register new user; place order; list shows it; other user id → 404
- Stop condition: API OK

### T08.2 — Account tests
- Model tier: small
- Goal: IDOR + register validation tests
- Allowed write paths: Api.Tests
- Acceptance: tests pass
- Stop condition: Green

### T08.3 — Vue register + orders pages
- Model tier: small
- Goal: UI for register and order history/detail
- Allowed write paths: ecommerce-web
- Acceptance: Manual register + view order
- Stop condition: Nav links work when authenticated
