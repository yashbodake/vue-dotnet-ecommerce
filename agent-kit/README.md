# Agent kit — Legacy → Modern (multi-agent)

Token-efficient redo of the Strangler migration.

| Path | Purpose |
|------|---------|
| [`HOW_TO_RUN_AGENTS.md`](./HOW_TO_RUN_AGENTS.md) | Human workflow (**read first**) |
| [`prompts/MASTER_PLANNER.md`](./prompts/MASTER_PLANNER.md) | **v2** Planner recipe (same as V2 filename) |
| [`prompts/MASTER_PLANNER_V2.md`](./prompts/MASTER_PLANNER_V2.md) | Alias of v2 Planner recipe |
| [`prompts/SECURITY_AUDITOR.md`](./prompts/SECURITY_AUDITOR.md) | Adversarial / security review (after S05+ / S10) |
| [`prompts/INTEGRATOR.md`](./prompts/INTEGRATOR.md) | Cross-slice fixes, `BLOCKED:`, contract changes |
| [`prompts/archive/MASTER_PLANNER.v1.md`](./prompts/archive/MASTER_PLANNER.v1.md) | Archived prior prompt |
| [`00_GLOBAL/`](./00_GLOBAL/) | Mission, architecture, rules, glossary, slice index |
| [`slices/`](./slices/) | Per-slice CONTEXT / FILES / TASKS / ACCEPTANCE |
| [`templates/SMALL_MODEL_TASK.md`](./templates/SMALL_MODEL_TASK.md) | Wrapper for small-model executors |
| [`EXECUTION_QUEUE.md`](./EXECUTION_QUEUE.md) | Ordered task list to run |
| [`NEXT_TASK.md`](./NEXT_TASK.md) | Ready-to-paste current Executor task |

**Already generated:** Skip the Master Prompt unless you want a full Planner overwrite from v2.

**Repo:** This is the **redo workspace** (legacy + kit). Build output goes in `modern/` (created in S01).

**Source of behavior:** Legacy MVC in this repo (`main`)  
**Target:** `modern/` in **Docker** (.NET 10 + Vue + YARP); **DB = host SQL Express** via **native SQL** (no EF Core)
