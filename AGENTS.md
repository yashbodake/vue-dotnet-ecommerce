# Automated Quality Review

## Project layout

Vue + .NET shop lives in `src/frontend` + `src/backend`.

- Solution: `Ecommerce.sln` (repo root)
- Backend API: `src/backend/Ecommerce.Api` (.NET 10, raw `Microsoft.Data.SqlClient`, no EF)
- Backend Gateway: `src/backend/Ecommerce.Gateway` (YARP)
- Backend tests: `src/backend/Ecommerce.Api.Tests` (xUnit + Coverlet)
- Frontend: `src/frontend` (Vue 3 + Vite + Pinia + vue-router)
- Frontend tests: `src/frontend/e2e` (Playwright, `workers: 1`)
- DB scripts: `database/*.sql` against `LegacyEcommerceDb` on `.\SQLEXPRESS`

For day-to-day run instructions see `README.md`. Migration kit history (completed): [`agent-kit/`](agent-kit/).

## Project objective

The refactoring and migration work is complete.

The remaining objectives are:

1. Detect and repair security vulnerabilities.
2. Create adversarial tests for edge cases and failure paths.
3. Remove unnecessary code and dependencies.
4. Improve measurable runtime and resource efficiency.

## Agent system

Review/analysis agents are read-only and emit findings to `.agent-results/`. Only the **repair-agent** modifies production code. The **final-verifier** re-runs every gate after repairs and emits a PASS/FAIL verdict.

Agent definitions live at the user level (`~/.zcode/agents/`):

| Agent | Color | Writes | Edits prod code |
|---|---|---|---|
| `security-reviewer` | red | `.agent-results/security-findings.json` | no |
| `adversarial-tester` | red | `.agent-results/adversarial-findings.json` + test files | no (test dirs only) |
| `code-bloat-reviewer` | yellow | `.agent-results/bloat-findings.json` | no |
| `efficiency-reviewer` | yellow | `.agent-results/efficiency-findings.json` | no |
| `repair-agent` | yellow | `.agent-results/repair-log.json` + prod code | **yes** |
| `final-verifier` | yellow | `.agent-results/final-verification.json` | no |

Policies in `.agent-config/` govern all of them:

- `quality-policy.md` — build / test / scan commands (the gate)
- `security-policy.md` — scanning strategy
- `benchmark-policy.md` — accepted performance-measurement methods
- `exclusions.txt` — paths skipped by analysis agents
- `finding-schema.json` — the shared findings output shape

## Global rules

- Preserve all existing external behaviour.
- Do not redesign the architecture.
- Do not modify public API contracts.
- Do not change database schemas.
- Do not remove code unless reachability and tests support removal.
- Do not claim performance improvement without benchmark evidence.
- Do not suppress tests, warnings or scanner findings.
- Do not weaken assertions.
- Do not replace secure code with shorter but less explicit code.
- Avoid unrelated formatting or renaming.
- Keep each repair batch small and independently verifiable.

## Required validation

Run all configured (see `quality-policy.md` for exact commands):

- Build commands
- Unit tests
- Integration / e2e tests
- Static analysis (typecheck)
- Security scans (native dependency scans; optional semgrep/trivy)
- Dependency scans
- Benchmarks (per-finding ad-hoc method per `benchmark-policy.md`)

## Finding requirements

Every finding must include (see `.agent-config/finding-schema.json`):

- Unique ID
- Category
- Severity (`critical | high | medium | low | info`)
- Confidence (`0.0–1.0`)
- File and line range
- Concrete evidence
- Reproduction method
- Recommended change
- Required validation

Vague or theoretical findings without a relevant code path are non-blocking.
