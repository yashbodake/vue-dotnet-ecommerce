# S01 — Skeleton

## Goal
Create empty modern solution: Api, Gateway, Vue app, tests project, sln, gitignore, **Dockerfiles + compose**, launcher scripts. No business features yet.

## Parity notes
- N/A (infra only)
- Ports reserved: API 5100, Vite 5173, Gateway 5000
- **SQL Express stays on host** — do not add a SQL container

## Owns
- `modern/Ecommerce.Api` (empty health later in S02)
- `modern/Ecommerce.Gateway` (config stub OK)
- `modern/ecommerce-web` (Vite Vue TS scaffold)
- `modern/Ecommerce.Api.Tests`
- `modern/Ecommerce.Modern.sln`
- `modern/docker-compose.yml` + Dockerfiles for api/gateway/web
- `tools/modern-up.ps1` → `docker compose up --build`
- optional `tools/db-setup.ps1` (host SQLCMD only)

## Tables
None

## Depends on
—

## Pitfalls
- Put .NET 10 on PATH for **host** builds/tests: `%LOCALAPPDATA%\Microsoft\dotnet`
- Do not reference legacy csproj from modern sln
- Compose must use `extra_hosts` / `host.docker.internal` so API can reach host SQL later (S02)
- Runtime = Docker; `dotnet build`/`dotnet test` still run on host for CI/executors
