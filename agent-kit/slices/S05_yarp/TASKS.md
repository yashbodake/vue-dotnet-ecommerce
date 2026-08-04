# S05 TASKS

### T05.1 — Configure YARP routes + health
- Model tier: small
- Goal: api-route + vue-route clusters; gateway health JSON
- Allowed write paths: `Ecommerce.Gateway/**`
- Acceptance: With api+vite running, `GET http://127.0.0.1:5000/api/health` and `GET http://127.0.0.1:5000/` succeed via `modern-up` or manual
- Stop condition: Gateway proxies both

### T05.2 — CORS + modern-up health checks
- Model tier: small
- Goal: API allows gateway origin; `modern-up.ps1` (compose) prints OK for health/products
- Allowed write paths: Api Program CORS, `tools/modern-up.ps1`, compose healthchecks if needed
- Acceptance: After `modern-up`, `GET http://127.0.0.1:5000/api/health` OK; login from page on `:5000` works
- Stop condition: Documented demo URL `:5000` via Docker
