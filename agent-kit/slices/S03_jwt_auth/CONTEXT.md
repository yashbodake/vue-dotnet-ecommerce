# S03 — JWT + AdminSeed

## Goal
Login/register-ready auth: JWT issue/validate; `/api/auth/me`; seed Admin role + `admin@legacy.local`.

## Parity notes
- Same password hashes as Identity
- Admin role name exactly `Admin`
- Password min length 6 (legacy register)

## Owns
- `POST /api/auth/login`
- `GET /api/auth/me` (Bearer)
- `AdminUserSeeder` on startup (skip env `Testing`)

## Tables
`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`

## Depends on
S02

## Pitfalls
- SELECT Identity columns used by hasher (PasswordHash, SecurityStamp, …) with SqlClient — no EF
- Jwt SigningKey ≥ 32 chars in appsettings
- WebApplicationFactory tests: use env `Testing` to skip seed against real SQL
