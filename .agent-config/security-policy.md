# Security Policy — scanning strategy

How the `security-reviewer` agent (and the `final-verifier` when re-checking) approach scanning for the Ecommerce-Redo Vue + .NET shop.

## Baseline (always run, no install)

Native dependency/vuln scans are the always-on baseline:

```bash
dotnet list src/backend/Ecommerce.Api/Ecommerce.Api.csproj --vulnerable
dotnet list src/backend/Ecommerce.Gateway/Ecommerce.Gateway.csproj --vulnerable
dotnet list src/backend/Ecommerce.Api.Tests/Ecommerce.Api.Tests.csproj --vulnerable

cd src/frontend && npm audit --omit=dev && cd ../..
```

These cover **known-vulnerable dependencies** and (for npm) some license/advisory issues. They do **not** cover application logic, SAST, secrets-in-code, or misconfig.

## Optional SAST / misconfig / secret scanners

These are **not** installed by default and are not required. Run them **only if present on PATH**, or if the parent explicitly authorizes an install.

### Semgrep (SAST — application code)

Install (Python):

```bash
pip install semgrep
```

Run:

```bash
semgrep scan --config auto --json --output .agent-results/semgrep.json .
```

Semgrep supports local and CI scanning, including diff-aware CI operation.

### Trivy (filesystem: vuln + secret + misconfig)

Install: see <https://aquasecurity.github.io/trivy/latest/getting-started/installation/>

Run:

```bash
trivy fs --scanners vuln,secret,misconfig --format json --output .agent-results/trivy.json .
```

Trivy can scan repositories and filesystems for vulnerabilities, secrets, and configuration problems.

## Interpretation rules

1. **A scanner hit is a lead, not a confirmed finding.** The security agent must trace each hit to a real, reachable, security-sensitive code path before promoting it to a finding. Unverified hits go in the report with `confidence < 0.5` and `status: "unconfirmed"`.
2. **Separate exploitable findings from defence-in-depth.** Only exploitable, application-specific issues should be `severity: high` or `critical`. Generic hardening suggestions are `low`/`info`.
3. **Do not dismiss a scanner finding without a recorded reason.** If a hit is a false positive, the finding (or note) must say exactly why (e.g. "input is bounded to integers upstream at line X").
4. **Native-scan advisories** (npm audit / dotnet list --vulnerable): for each `high`/`critical` advisory, record the package, CVE/GHSA, fixed version, and whether the vulnerable code path is actually reachable. A dependency listed ≠ exploited.
5. The security agent inspects security **logic** that static scanners cannot understand: auth flows, JWT validation, ownership/IDOR checks, parameterized SQL usage, checkout/stock races.

## Scope of review

The reviewer covers the whole application, prioritising:

- Authentication bypass & JWT handling
- Authorization and per-user ownership checks (IDOR)
- Injection (SQL via `Microsoft.Data.SqlClient`, command, LDAP, XPath)
- SSRF, path traversal, unsafe file ops
- Deserialization, command execution
- Secret exposure (committed creds, `.env`, `appsettings*.json`)
- Sensitive-data logging
- Cryptographic misuse
- Session and token handling
- Rate-limit bypass
- Security-relevant race conditions (stock decrement, cart)
- Dependency vulnerabilities
- Insecure defaults
- Error information leakage

## What this policy does NOT mandate

- No requirement to install semgrep or trivy. Absence of those tools is not a gate failure.
- No DAST scanner is configured. (The `adversarial-tester` agent provides behaviour-level attack coverage instead.)
- No web firewall / rate-limit product is in scope; rate-limit findings are code-level.
