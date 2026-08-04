# NEXT TASK

**Slice:** S09_admin
**Queue:** See `EXECUTION_QUEUE.md` — mark T09.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S09_admin/CONTEXT.md`
3. `agent-kit/slices/S09_admin/FILES.md`
4. This file (or paste below)

---

### T09.1 — AdminService + endpoints + policy
- Model tier: small
- Goal: Admin-only product/order APIs
- Allowed write paths: Api admin*, Program authorization policy
- Acceptance: Admin token can list products; non-admin JWT → 403; anonymous → 401
- Stop condition: API secured

---

After success: check off T09.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T09.2** from `slices/S09_admin/TASKS.md`.