# S03 TASKS

### T03.1 — AspNet SQL queries + AuthService login/JWT
- Model tier: small
- Goal: Login against AspNetUsers via native SQL; return access token + user roles
- Allowed write paths: Api Data AspNet*, Services/Auth*, Contracts/Auth*, Endpoints/Auth*, Program JWT
- Forbidden: EF Core
- Acceptance: POST `/api/auth/login` with admin creds returns 200 + token (DB must have user or seed next task)
- Stop condition: Login works for existing hashed user OR ready for seeder

### T03.2 — AdminUserSeeder + /api/auth/me
- Model tier: small
- Goal: Idempotent ensure Admin role/user/password; me endpoint
- Allowed write paths: `AdminUserSeeder.cs`, Program startup call, Auth me endpoint
- Acceptance: Fresh start API → login `admin@legacy.local` / `Admin123!` → roles contains Admin; GET `/api/auth/me` with Bearer OK
- Stop condition: Seed + me verified

### T03.3 — Auth unit/API tests
- Model tier: small
- Goal: Tests for login fail/success, seeder idempotent, me unauthorized
- Allowed write paths: `Ecommerce.Api.Tests/**`
- Acceptance: `dotnet test` auth/seeder tests pass
- Stop condition: Green tests
