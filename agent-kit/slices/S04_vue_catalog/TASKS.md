# S04 TASKS

### T04.1 — API client + auth store + LoginView
- Model tier: small
- Goal: login works from Vue against API
- Allowed write paths: `src/api/**`, `src/stores/auth.ts`, `src/views/LoginView.vue`, router
- Acceptance: Manual login as admin shows authenticated nav state
- Stop condition: Token stored; me hydration optional

### T04.2 — Catalog store + HomeView filters + ProductCard
- Model tier: small
- Goal: List products with search/sort/category/inStock + pagination
- Allowed write paths: `stores/catalog.ts`, `views/HomeView.vue`, `components/ProductCard.vue`, styles
- Acceptance: Home shows products from API; filter changes refetch
- Stop condition: Grid usable

### T04.3 — ProductDetailView
- Model tier: small
- Goal: Detail page with images/variants/price
- Allowed write paths: `views/ProductDetailView.vue`, router
- Acceptance: Navigate from card to detail; 404 handling basic
- Stop condition: Detail renders
