# NEXT TASK — paste into a small-model chat

**Slice:** S01_skeleton  
**Queue:** See `EXECUTION_QUEUE.md` — mark T01.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S01_skeleton/CONTEXT.md`
3. `agent-kit/slices/S01_skeleton/FILES.md`
4. This file (or paste below)

---

You are an Executor (small model) for the Legacy → Modern ecommerce migration.

Follow agent-kit/00_GLOBAL/RULES.md strictly.
You may ONLY use the attached slice CONTEXT + FILES + this task card.
Do not expand scope. Do not refactor unrelated files.
Do not read or modify Ecommerce.Web / Ecommerce.Services / Ecommerce.Data / Ecommerce.Core except when FILES.md explicitly allows read-only parity checks.
When acceptance passes, stop and summarize: files changed, commands run, result.
If blocked, report OPEN QUESTION and stop — do not invent cross-slice APIs.

TASK:

### T01.1 — Create API + Gateway + Tests projects
- Model tier: small
- Goal: Scaffold .NET 10 Web API, YARP gateway, xUnit test project; add to `Ecommerce.Modern.sln`
- Inputs: none
- Allowed write paths: `modern/Ecommerce.Api/**`, `modern/Ecommerce.Gateway/**`, `modern/Ecommerce.Api.Tests/**`, `modern/Ecommerce.Modern.sln`
- Forbidden: Vue app; legacy projects
- Steps:
  1. Ensure `modern/` exists
  2. `dotnet new webapi` / yarp-capable web + xunit (net10.0)
  3. Add projects to sln; Api.Tests references Api
  4. Gateway: package `Yarp.ReverseProxy`; empty reverse proxy section OK
- Acceptance: `$env:PATH="$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"; dotnet build modern/Ecommerce.Modern.sln` exits 0
- Stop condition: Build green; no endpoints beyond template

---

After success: check off T01.1 in `EXECUTION_QUEUE.md` and replace this file’s TASK with **T01.2** from `slices/S01_skeleton/TASKS.md`.
