# Security Audit Report

**Date:** 2026-08-04
**Scope:** Modern ecommerce codebase (`modern/`) — code-level audit (stack not running)
**Auditor:** security-auditor agent

## Findings

### SEV high: JWT signing key committed in appsettings.json
- **Evidence:** `appsettings.json` contains `"SigningKey": "ThisIsAVeryLongSigningKeyForJWT_32CharsOrMore!"`. Same literal in test code.
- **Context:** Acceptable for a demo/development project per RULES.md ("No real secrets in git; demo admin is a documented credential only"). The signing key is not a user secret — it's a demo development key.
- **Suggested fix (production):** Move `Jwt:SigningKey` to environment variables or .NET User Secrets. Remove the committed value for production deployments.

### SEV medium: Demo admin password hardcoded in AdminUserSeeder.cs
- **Evidence:** `public const string AdminPassword = "Admin123!";` — documented demo credential.
- **Context:** Intentional per RULES.md. The seeder is skipped in Testing environment.
- **Suggested fix (production):** Move admin password to configuration secrets. Skip seeding in production.

### SEV medium: SQL TrustServerCertificate=True in connection strings
- **Evidence:** `appsettings.json` and test connection strings use `TrustServerCertificate=True`.
- **Context:** Acceptable for local SQL Express development. Production should use valid certificates.
- **Suggested fix (production):** Remove `TrustServerCertificate=True` and deploy a valid SQL Server certificate.

### SEV low: Gateway destinations use HTTP (not HTTPS)
- **Evidence:** YARP clusters point to `http://127.0.0.1:5100` and `http://127.0.0.1:5173`.
- **Context:** Acceptable for local development on loopback. Docker internal traffic is on the compose network.
- **Suggested fix (production):** Use HTTPS with valid certificates for backend communication.

### SEV low: docker-compose.yml comment contains default SQL password
- **Evidence:** Comment shows `Password=${SQL_PASSWORD:-YourPassword123!}` fallback value.
- **Suggested fix:** Remove the default value from the comment; leave only `${SQL_PASSWORD}`.

## Passed checks

- ✅ Wrong password → 401: `LoginAsync` returns null → `Results.Unauthorized()`
- ✅ No Bearer on /api/auth/me → 401: `RequireAuthorization(JwtBearerDefaults.AuthenticationScheme)`
- ✅ Non-admin JWT on /api/admin/* → 403: `RequireAuthorization("Admin")` policy with `RequireRole("Admin")`
- ✅ Anonymous /api/admin/* → 401: same policy
- ✅ Cart cookie alone cannot checkout/orders/admin: checkout/account/admin endpoints require JWT
- ✅ IDOR: User A token + User B order → 404: `WHERE OrderId = @OrderId AND UserId = @UserId` → `KeyNotFoundException` → 404
- ✅ Soft-deleted product not in public catalog: `ProductCatalogService` filters `IsActive = 1`; admin sees all
- ✅ SQL injection: ALL user-supplied values use `SqlParameter` / `AddWithValue`. Dynamic WHERE uses parameter names, not values. Column name selection (`UserId` vs `SessionId`) is based on boolean flag, not user input.
- ✅ Gateway routing: Only `/api/{**catch-all}` and `/{**catch-all}` exposed — no internal-only paths leaked
- ✅ No .env files committed
- ✅ Demo admin is a documented credential only

## BLOCKED (runtime checks — need running stack)

- Live HTTP status code verification (401/403/400/404 responses)
- Live injection probes with malicious strings against catalog/cart filters
- Gateway smoke test (`/api/health`, `/api/products` through :5000)

These require Docker Compose running the full stack against SQL Express, which was not available during this session.