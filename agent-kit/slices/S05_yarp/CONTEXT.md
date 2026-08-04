# S05 — YARP gateway

## Goal
Browser entry `:5000` routes `/api/*` → API, `/*` → Vue. No legacy cluster required for modern-only product.

## Parity notes
- Final product does not need `/Content` → IIS

## Owns
- `Ecommerce.Gateway` appsettings / compose env routes/clusters
- Docker gateway service; `modern-up.ps1` = compose up
- `/gateway/health`

## Depends on
S04

## Pitfalls
- Inside compose, clusters must target `http://api:...` and `http://web:...`, not `127.0.0.1`
- Route order: api route Order more negative than vue catch-all
- CORS on API allow `http://localhost:5000` and `5173`
