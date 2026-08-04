# S01 TASKS

### T01.1 — Create API + Gateway + Tests projects
- Model tier: small
- Goal: Scaffold .NET 10 Web API, YARP gateway, xUnit test project; add to `Ecommerce.Modern.sln`
- Inputs: none
- Allowed write paths: `modern/Ecommerce.Api/**`, `modern/Ecommerce.Gateway/**`, `modern/Ecommerce.Api.Tests/**`, `modern/Ecommerce.Modern.sln`
- Forbidden: Vue app; legacy projects; Docker files (T01.3)
- Steps:
  1. Ensure `modern/` exists
  2. `dotnet new webapi` / yarp-capable web + xunit (net10.0)
  3. Add projects to sln; Api.Tests references Api
  4. Gateway: package `Yarp.ReverseProxy`; empty reverse proxy section OK
- Acceptance: `$env:PATH="$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"; dotnet build modern/Ecommerce.Modern.sln` exits 0
- Stop condition: Build green; no endpoints beyond template

### T01.2 — Scaffold Vue 3 + Vite + Pinia + Router
- Model tier: small
- Goal: `modern/ecommerce-web` with Vue TS, Pinia, vue-router, basic App shell
- Inputs: T01.1
- Allowed write paths: `modern/ecommerce-web/**`
- Forbidden: API business code; Docker files (T01.3)
- Steps:
  1. npm create vite vue-ts (or equivalent)
  2. Add pinia, vue-router
  3. Placeholder Home view at `/`
- Acceptance: `cd modern/ecommerce-web && npm install && npm run build` succeeds
- Stop condition: SPA builds; no API wiring yet

### T01.3 — Docker Compose + modern-up.ps1
- Model tier: small
- Goal: Containerize Api, Gateway, Vue; launcher wraps compose; **no SQL container**
- Inputs: T01.1, T01.2
- Allowed write paths: `modern/docker-compose.yml`, `modern/.dockerignore`, `modern/**/Dockerfile`, `modern/.env.example`, `tools/modern-up.ps1`
- Forbidden: Changing business app logic; adding `mssql` service
- Steps:
  1. Add Dockerfiles for Api, Gateway, ecommerce-web (multi-stage OK)
  2. `docker-compose.yml`: services `api` (5100), `gateway` (5000), `web` (5173); publish those ports
  3. Wire gateway→api/web with **compose service DNS** (`http://api:8080` etc.), not `localhost`
  4. Ensure `host.docker.internal` available for future DB (S02); document SQL stays on host
  5. `tools/modern-up.ps1`: `docker compose -f modern/docker-compose.yml up --build -d` (+ optional health curls)
  6. `.env.example` for connection string placeholders only
- Acceptance: `docker compose -f modern/docker-compose.yml config` exits 0; script exists; no SQL service in compose
- Stop condition: Compose validates; user can `modern-up` later (DB wiring is S02)
