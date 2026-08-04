# S10 TASKS

### T10.1 — Vue legacy path redirects + card layout polish
- Model tier: small
- Goal: Redirects + aligned product cards
- Allowed write paths: router, style.css, ProductCard.vue
- Acceptance: Visiting `/Cart` ends on `/cart`; cards’ Add buttons align across 1–2 line titles
- Stop condition: UX polish done

### T10.2 — Playwright e2e suite
- Model tier: small
- Goal: ≥4 chromium tests covering catalog/cart/checkout/admin
- Allowed write paths: `e2e/**`, playwright.config.ts, package.json scripts
- Acceptance: `npx playwright install chromium` (once); `npm run test:e2e` all pass
- Stop condition: Report path noted

### T10.3 — README old-vs-new + docs status
- Model tier: small
- Goal: Document modern entry, admin creds, link Legacy main as old version
- Allowed write paths: README/SETUP/docs/migration
- Acceptance: README clear; `docs/migration/STATUS.md` lists S01–S10 done when complete
- Stop condition: Docs usable by reviewer
