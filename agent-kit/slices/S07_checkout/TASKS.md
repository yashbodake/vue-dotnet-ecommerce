# S07 TASKS

### T07.1 — CheckoutService + endpoints
- Model tier: small
- Goal: shipping-options + place-order with validation
- Allowed write paths: Api checkout*
- Acceptance: Authenticated POST place-order with cart creates order; second GET cart empty
- Stop condition: API order created

### T07.2 — Checkout tests
- Model tier: small
- Goal: Unit/API tests for validation + happy path (mocked SQL or host Express)
- Allowed write paths: Api.Tests
- Acceptance: checkout tests pass
- Stop condition: Green

### T07.3 — Vue checkout wizard
- Model tier: small
- Goal: 3-step form + summary; confirmation route
- Allowed write paths: ecommerce-web checkout views/router
- Acceptance: Admin can checkout Standard + demo card; lands on confirmation
- Stop condition: Manual path green (Playwright in S10)
