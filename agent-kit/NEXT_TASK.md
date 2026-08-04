# NEXT TASK — paste into a small-model chat

**Slice:** S04_vue_catalog  
**Queue:** See `EXECUTION_QUEUE.md` — mark T04.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S04_vue_catalog/CONTEXT.md`
3. `agent-kit/slices/S04_vue_catalog/FILES.md`
4. This file (or paste below)

---

You are an Executor (small model) for the Legacy → Modern ecommerce migration.

Follow agent-kit/00_GLOBAL/RULES.md strictly.
You may ONLY use the attached slice CONTEXT + FILES + this task card.
Do not expand scope. Do not refactor unrelated files.
Do not read or modify Ecommerce.Web / Ecommerce.Services / Ecommerce.Data / Ecommerce.Core except when FILES.md explicitly allows read-only parity checks.
When acceptance passes, stop and summarize: files changed, commands run, result.
If blocked, report OPEN QUESTION and stop — do not invent cross-slice APIs.

---

### T04.1 — API client + auth store + LoginView
- Model tier: small
- Goal: login works from Vue against API
- Allowed write paths: `src/api/**`, `src/stores/auth.ts`, `src/views/LoginView.vue`, router
- Acceptance: Manual login as admin shows authenticated nav state
- Stop condition: Token stored; me hydration optional

---

After success: check off T04.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T04.2** from `slices/S04_vue_catalog/TASKS.md`.