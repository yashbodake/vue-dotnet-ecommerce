# S09 — Admin

## Goal
Admin JWT role APIs + Vue admin for products (CRUD soft-delete) and order status.

## Parity notes
- Non-admin → 403 on admin APIs
- Soft-delete: IsActive=false
- Order statuses: Pending, Processing, Shipped, Delivered, Cancelled
- Nav link Admin only if roles include Admin

## Owns
- `/api/admin/products`, `/products/{id}`, POST/PUT/DELETE
- `/api/admin/categories`, `/api/admin/orders`, PUT status
- Vue `/admin/products`, create/edit, `/admin/orders`

## Tables
Products, Categories, Orders

## Depends on
S08

## Pitfalls
- Keep admin Save buttons compact (not full-width cell stretch)
