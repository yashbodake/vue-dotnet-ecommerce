# How to run agents (token-efficient migration)

## Master Prompt v2

Paste targets for Planner (identical content):
- [`prompts/MASTER_PLANNER.md`](./prompts/MASTER_PLANNER.md)
- [`prompts/MASTER_PLANNER_V2.md`](./prompts/MASTER_PLANNER_V2.md)

Archived prior recipe: [`prompts/archive/MASTER_PLANNER.v1.md`](./prompts/archive/MASTER_PLANNER.v1.md).

## Why `agent-kit/` already exists

v2 is the **recipe** for a high-end Planner to generate/overwrite this kit.

This folder was **already generated** (Docker + native SQL + security/ops rules), so you can skip straight to **Executor** tasks.

| Path | When to use |
|------|-------------|
| **Use existing `agent-kit/`** | Default. Run `NEXT_TASK.md` / `EXECUTION_QUEUE.md` with small models. |
| **Re-paste Master Prompt v2** | Only for a **full Planner redo**. That pass should **overwrite** `agent-kit/` completely. |

You do **not** need to paste the Master Prompt again just to create these files.

## Roles

| Role | Model tier | When | Prompt |
|------|------------|------|--------|
| **Planner** | High-end | Once (or regenerating the kit) | `MASTER_PLANNER.md` / `MASTER_PLANNER_V2.md` |
| **Executor** | Small | One task `Txx.y` at a time | `templates/SMALL_MODEL_TASK.md` + RULES + slice |
| **Integrator** | Medium | Cross-slice bugs, `BLOCKED:`, contract changes, compose/SQL/E2E | [`prompts/INTEGRATOR.md`](./prompts/INTEGRATOR.md) |
| **Security Auditor** | Medium/high | After S05+ and again after S10 | [`prompts/SECURITY_AUDITOR.md`](./prompts/SECURITY_AUDITOR.md) |

## Planner (optional full redo)

1. Checkout Legacy-Ecommerce **`main`**.
2. New chat with high-end model.
3. `@` repo root (+ optional specs doc).
4. Paste `prompts/MASTER_PLANNER.md` (v2).
5. Save/overwrite outputs under `agent-kit/`.

## Executor (per task) — start here

1. New chat with **small** model (fresh context).
2. Attach **only**:
   - `agent-kit/00_GLOBAL/RULES.md`
   - `agent-kit/slices/Sxx_*/CONTEXT.md`
   - `agent-kit/slices/Sxx_*/FILES.md`
   - **One** task block from `TASKS.md` (or `NEXT_TASK.md`)
3. Wrap with [`templates/SMALL_MODEL_TASK.md`](./templates/SMALL_MODEL_TASK.md).
4. Do **not** attach other slices or Master Planner / Auditor prompts.
5. Stop when acceptance passes; do not start the next task in the same chat.
6. If stuck out of scope: report `BLOCKED: <reason>` and stop (see RULES).

## Integrator

1. Paste [`prompts/INTEGRATOR.md`](./prompts/INTEGRATOR.md).
2. Attach RULES + failing slice ACCEPTANCE + error output.
3. Fix minimal surface; re-run acceptance.
4. Use for Docker↔SQL Express failures and Playwright flakes after S05+.

## Security Auditor

1. Ensure stack is up (`modern-up` / compose) after **S05+** (smoke) and after **S10** (full).
2. Paste [`prompts/SECURITY_AUDITOR.md`](./prompts/SECURITY_AUDITOR.md).
3. Attach RULES + ARCHITECTURE; run checklist; report findings.
4. Hand clear defects to Integrator for minimal fixes — do not feature-creep.

## Order

Follow [`00_GLOBAL/SLICE_INDEX.md`](./00_GLOBAL/SLICE_INDEX.md). Never start `S0n` until `S0n-1` acceptance is green.

## Target runtime

- **Code:** `modern/` (.NET 10 + Vue + YARP)
- **Process:** Docker Compose (Api + Gateway + Vue)
- **Data:** Host SQL Express `LegacyEcommerceDb` via **native SQL** (no EF Core)
- Legacy `Ecommerce.Web` stays for parity reads only.
