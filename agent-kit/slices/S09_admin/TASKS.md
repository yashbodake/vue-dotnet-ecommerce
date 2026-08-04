# S09 TASKS

### T09.1 — AdminService + endpoints + policy
- Model tier: small
- Goal: Admin-only product/order APIs
- Allowed write paths: Api admin*, Program authorization policy
- Acceptance: Admin token can list products; non-admin JWT → 403; anonymous → 401
- Stop condition: API secured

### T09.2 — Admin tests
- Model tier: small
- Goal: Soft-delete + status update + authz tests
- Allowed write paths: Api.Tests
- Acceptance: admin tests pass
- Stop condition: Green

### T09.3 — Vue admin UI
- Model tier: small
- Goal: Products table + form + orders status editor
- Allowed write paths: ecommerce-web admin views/router/nav
- Acceptance: Admin user sees Admin nav; can open products/orders
- Stop condition: Manual admin path OK
