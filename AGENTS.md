# AGENTS — start the migration redo here

This repo is the **redo workspace**: legacy MVC code + multi-agent kit. It is **not** the finished modern app.

| Repo | Purpose |
|------|---------|
| **This repo** | Legacy `main` + `agent-kit/` → build fresh `modern/` via Executor tasks |
| [Legacy-Ecommerce](https://github.com/yashbodake/Legacy-Ecommerce) | Original legacy product (unchanged reference) |
| [Ecommerce-Modern](https://github.com/yashbodake/Ecommerce-Modern) | **Previous** completed migration (for reference only — do not copy blindly) |

## Start here

1. Read [`agent-kit/HOW_TO_RUN_AGENTS.md`](agent-kit/HOW_TO_RUN_AGENTS.md)
2. Run [`agent-kit/NEXT_TASK.md`](agent-kit/NEXT_TASK.md) in a **new small-model chat** (T01.1)
3. Track progress in [`agent-kit/EXECUTION_QUEUE.md`](agent-kit/EXECUTION_QUEUE.md)

## Attach per Executor chat

- `agent-kit/00_GLOBAL/RULES.md`
- Current slice `CONTEXT.md` + `FILES.md`
- One task from `NEXT_TASK.md` or `TASKS.md`

## Other prompts

| Role | File |
|------|------|
| Planner (optional redo of kit) | `agent-kit/prompts/MASTER_PLANNER.md` |
| Integrator | `agent-kit/prompts/INTEGRATOR.md` |
| Security Auditor | `agent-kit/prompts/SECURITY_AUDITOR.md` |

## Target you are building

- **Code:** empty `modern/` (created in S01) — Docker Api + Gateway + Vue
- **DB:** host SQL Express `LegacyEcommerceDb` (same scripts under `database/`)
- **Data access:** native SQL only (no EF Core)

Legacy MVC in this repo stays for **parity reads** only. Executors must not edit `Ecommerce.Web` unless a task allows read-only checks.
