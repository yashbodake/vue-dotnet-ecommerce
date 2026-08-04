# S06 — Cart

## Goal
Guest + user cart API; Vue cart page; merge guest into user on login.

## Parity notes
- Cookie `ecommerce.cart_owner`
- Add/update/remove/set quantity; line totals; item count
- Variants optional on add
- Stock checks on mutations

## Owns
- `/api/cart`, `/api/cart/items`, `/api/cart/count`, etc. (match chosen REST shape; document in TASKS)
- Vue `/cart`, add buttons on catalog/detail
- Pinia `cart` store

## Tables
`CartItems` (+ Products/Variants for display)

## Depends on
S05

## Pitfalls
- Cart SQL: explicit JOINs to lines/products; return only needed columns into DTOs
- Merge on JWT login (auth store → cart.merge)
