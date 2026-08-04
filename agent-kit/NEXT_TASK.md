# NEXT TASK

**Slice:** S06_cart
**Queue:** See `EXECUTION_QUEUE.md` — mark T06.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S06_cart/CONTEXT.md`
3. `agent-kit/slices/S06_cart/FILES.md`
4. This file (or paste below)

---

### T06.1 — CartService + endpoints + cookie
- Model tier: small
- Goal: Full cart API with owner cookie + optional JWT user id
- Allowed write paths: Api cart* Program
- Acceptance: Add item anonymously (cookie set); GET cart returns line; count endpoint works
- Stop condition: API cart CRUD OK

---

After success: check off T06.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T06.2** from `slices/S06_cart/TASKS.md`.