# S07 — Checkout

## Goal
JWT place-order; Vue wizard Address → Shipping → Payment → Confirmation.

## Parity notes
- Shipping: `Standard` | `Express` only
- Required address fields; demo card fields validated not stored
- Creates Order + OrderItems; clears cart; decrements stock (legacy rules)
- Confirmation shows `#orderId` + ship-to summary

## Owns
- `GET /api/checkout/shipping-options`
- `POST /api/checkout/place-order`
- Vue `/checkout`, `/checkout/confirmation/:orderId`

## Tables
`Orders`, `OrderItems`, `CartItems`, `Products` stock

## Depends on
S06

## Pitfalls
- Empty cart → cannot checkout
- Unauthenticated → redirect login with return URL
