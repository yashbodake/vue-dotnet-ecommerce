# S10 — Polish + E2E + docs

## Goal
Legacy URL redirects in Vue; README old-vs-new; Playwright suite; migration docs package; optional product-card layout polish.

## Parity / UX notes
- Redirect `/Cart`, `/Checkout`, `/Account/*`, `/Admin/*`, `/Product/*`
- README: Legacy `main` = old; this modern tree = new
- E2E: catalog, cart, checkout+orders, admin (≥4 tests)
- Product cards: clamp title 2 lines; pin CTA with flex (alignment)

## Owns
- Router redirects, README/SETUP, `e2e/catalog.spec.ts`, `docs/migration/*` summaries

## Depends on
S09

## Pitfalls
- Playwright selectors: `getByRole('textbox', { name })`
- E2E may start API+Vite; also document gateway smoke
