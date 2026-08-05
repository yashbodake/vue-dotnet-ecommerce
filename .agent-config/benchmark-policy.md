# Benchmark Policy — performance measurement

How the `efficiency-reviewer` and `repair-agent` measure performance for the Ecommerce-Redo Vue + .NET shop. This project has **no** BenchmarkDotNet harness and none will be added during quality review. All measurement is **per-finding ad-hoc**.

## Core rule

> No performance claim is accepted without a measurement or a defensible complexity analysis.

From `AGENTS.md`: *"Do not claim performance improvement without benchmark evidence."* "Evidence" here means one of the methods below — not a vibe.

## Per-finding measurement methods

Pick the method that fits the finding. State it in the finding's `validation.method`. Record a `baseline` value before the fix and a `target` value the fix must hit.

### 1. Query-count measurement (for N+1 / repeated-query findings)

This repo uses raw `Microsoft.Data.SqlClient`, so round-trips are the key signal.

- **SQL Server Extended Events** or **SQL Server Profiler**: capture `sql_batch_completed` / `rpc_completed` for the duration of one request, count events.
- Or wrap the suspected call path in a test that increments a counter on `SqlConnection.StateChange` (open events).

Record: `baseline = "N queries for M rows"`, `target = "≤ K queries"`.

Example: `PERF-014` → `baseline: "101 queries for 100 rows"`, `target: "≤ 2 queries"`.

### 2. Allocation / GC measurement (for excessive-allocation findings)

- **`dotnet-counters`** (global tool): monitor `System.Runtime[gc-heap-size, gen-0-gc-count]` around a request.

  ```bash
  dotnet tool install -g dotnet-counters   # if missing
  dotnet-counters monitor --process-id <pid> System.Runtime
  ```

- Or **JetBrains dotMemory** (if available) for a precise allocation diff.

Record: `baseline = "X MB allocated / Y Gen-0 GCs per request"`, `target = "≤ Z MB"`.

### 3. Latency measurement (for slow-path / blocking findings)

Use a small `Stopwatch` mini-harness — **no new dependency**. The pattern:

```csharp
// Warmup
for (int i = 0; i < 3; i++) InvokePath();

// Measure
var sw = System.Diagnostics.Stopwatch.StartNew();
const int N = 200;
for (int i = 0; i < N; i++) InvokePath();
sw.Stop();
Console.WriteLine($"avg = {sw.Elapsed.TotalMilliseconds / N:F3} ms over {N} iterations");
```

Requirements for a valid latency number:

- **Warmup** before measuring (≥3 iterations) to exclude JIT.
- **N ≥ 100** iterations (or justify a smaller N if each call is expensive).
- Report **average** and **min/max** (or p95) — single-shot numbers are not accepted.
- Compare **baseline vs after** on the **same machine, same load**.

Record: `baseline = "X ms avg / Y ms p95 over N iterations"`, `target = "≤ Z ms avg"`.

### 4. Bundle / dependency-overhead measurement (frontend)

```bash
cd src/frontend
npm run build
# Read dist/assets/*.js gzip size; compare before/after a dependency change
```

Record the gzip size delta of the produced bundle (`dist/assets/`).

### 5. Complexity analysis (when measurement is impractical)

For algorithmic findings where a runtime measurement would be misleading or too costly, a **defensible complexity analysis** is acceptable **instead** of a measurement:

- State the current complexity (e.g. O(n²) over the product list).
- Show the loop nesting / repeated traversal with file:line evidence.
- State the target complexity (e.g. O(n) with a Dictionary lookup).
- Explain why the input size makes the difference material.

Complexity-only findings must set `confidence ≤ 0.8` and note "complexity analysis, not measured" in `validation.method`.

## Mandatory `validation` object (high / critical findings only)

Every `high` or `critical` efficiency finding MUST include:

```json
"validation": {
  "method": "<one of the above, with the concrete tool/command>",
  "baseline": "<measured or analysed value before the fix>",
  "target":  "<value the fix must achieve>"
}
```

`medium` / `low` findings may omit `target` but must still state `method`.

## What is NOT accepted

- "This looks slow" without a number or a traced complexity argument.
- A single stopwatch reading with no warmup / no iterations.
- Adding caching **without** stating the invalidation rule.
- Adding concurrency **without** limits and failure handling.
- Comparing "after" to a guessed "before" — always measure the real baseline on the current commit.
- Claiming a BenchmarkDotNet run — there is no such project and none should be created during quality review.
