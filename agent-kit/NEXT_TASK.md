# NEXT TASK — paste into a small-model chat

**Slice:** S03_jwt_auth  
**Queue:** See `EXECUTION_QUEUE.md` — mark T03.3 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S03_jwt_auth/CONTEXT.md`
3. `agent-kit/slices/S03_jwt_auth/FILES.md`
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

### T03.3 — Auth unit/API tests
- Model tier: small
- Goal: Tests for login fail/success, seeder idempotent, me unauthorized
- Allowed write paths: `Ecommerce.Api.Tests/**`
- Acceptance: `dotnet test` auth/seeder tests pass
- Stop condition: Green tests

---

After success: check off T03.3 in `EXECUTION_QUEUE.md` and replace this file's TASK with **S03 ACCEPTANCE** from `slices/S03_jwt_auth/ACCEPTANCE.md`.