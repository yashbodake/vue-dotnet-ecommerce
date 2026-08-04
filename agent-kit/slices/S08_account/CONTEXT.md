# S08 — Account (register + orders)

## Goal
Register customer; list/detail own orders; IDOR → 404.

## Parity notes
- Register creates AspNetUsers (no Admin role)
- Orders list only for current user id
- Unknown or others’ order id → **404**

## Owns
- `POST /api/auth/register` (if not in S03 — add here)
- `GET /api/account/orders`, `GET /api/account/orders/{id}`
- Vue `/register`, `/orders`, `/orders/:id`

## Tables
`AspNetUsers`, `Orders`, `OrderItems`

## Depends on
S07

## Pitfalls
- Never reveal existence of others’ orders (404 not 403)
