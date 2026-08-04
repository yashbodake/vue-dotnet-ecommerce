# NEXT TASK

**Slice:** S08_account
**Queue:** See `EXECUTION_QUEUE.md` — mark T08.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S08_account/CONTEXT.md`
3. `agent-kit/slices/S08_account/FILES.md`
4. This file (or paste below)

---

### T08.1 — Register + account order endpoints
- Model tier: small
- Goal: Register + list/detail with IDOR 404
- Allowed write paths: Api auth/account*
- Acceptance: Register new user; place order; list shows it; other user id → 404
- Stop condition: API OK

---

After success: check off T08.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T08.2** from `slices/S08_account/TASKS.md`.