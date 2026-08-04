# S04 — Vue catalog + login

## Goal
Vue UI: product grid with filters, product detail, JWT login storing token.

## Parity notes
- Filters mirror API query params
- Show price, stock, category; thumbnail or placeholder
- Login form email/password → call API → persist token (sessionStorage)

## Owns
- Routes: `/`, `/products/:id`, `/login`
- Pinia: `catalog`, `auth`
- API client fetch helpers

## Tables
None directly (HTTP only)

## Depends on
S03

## Pitfalls
- Vite proxy `/api` → `http://127.0.0.1:5100` for dev
- Brand text: product name e.g. “Ecommerce Modern” (not required legacy chrome)
