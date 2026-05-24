---
title: "feat: Background jobs with progress tracking for bulk imports and payroll initiation"
type: feat
status: active
date: 2026-05-24
---

# feat: Background jobs with progress tracking for bulk imports and payroll initiation

## Overview

Bulk CSV imports (LOP, one-time earnings/deductions, reimbursements) and payroll run initiation currently execute synchronously on the HTTP request thread. For orgs with hundreds of employees, these operations risk HTTP timeouts and block the UI. This plan moves them to Hangfire background jobs with Redis-backed progress tracking and a polling endpoint, so the UI remains responsive and the user sees live progress.

**Calculation logic is not touched.** All changes are in the execution layer (when and how commands are dispatched), not in what they compute. `Payroll.Engine` is untouched. Existing command handler business rules are untouched. Per-row validation is untouched.

---

## Problem Frame

Three import endpoints and the payroll initiation endpoint run synchronously and grow linearly with employee count. LOP import is the worst case — it calls `SetLopCommandHandler.RecomputeEmployee` (full engine recompute) for every valid row. 1000-employee org = 1000 engine computations on one HTTP request. Initiation compounds this: it runs engine for every active employee before returning. The current 10-retry Hangfire default is also wrong for import jobs (retry would double-apply rows).

---

## Requirements Trace

- R1. Import operations return `202 Accepted` with a job ID; result is fetched separately once the job completes.
- R2. Payroll run initiation returns `202 Accepted` with a job ID; the resulting run ID is delivered in the completed job's result payload.
- R3. Bulk import processing is chunked at 500 rows per chunk; each chunk is committed independently to bound transaction size and memory.
- R4. A polling endpoint returns live job progress (`status`, `processed`, `total`) and the final result once complete.
- R5. Import jobs must not auto-retry — retrying a half-applied import would double-apply rows.
- R6. Calculation logic, engine code, and per-row business rules remain identical to today.
- R7. Job progress is tenant-scoped; one tenant cannot read another's job state.
- R8. Job results expire automatically; no manual cleanup is needed.

---

## Scope Boundaries

- No changes to `Payroll.Engine` — the pure calculation library is untouched.
- No changes to existing command handler business rules or per-row validation logic.
- Import jobs only cover the three CSV import types. Export is synchronous and fast; no change needed.
- No real-time push (SignalR/WebSocket) — polling is sufficient for this operation class.
- No admin job-management UI beyond the existing Hangfire dashboard.
- `ReEvaluateSkippedCommand` is not backgrounded in this plan — it operates only on skipped employees and is typically fast.

### Deferred to Follow-Up Work

- **InitiatePayrollRunCommand internal chunking**: this plan backgrounds the command as-is (all employees in one job execution). Chunking the employee loop inside the handler to cap per-job transaction size is a follow-up once the job infrastructure is in place.
- **MinIO-backed CSV staging**: large CSVs (> 5 MB) could overflow the Hangfire `job` table column. Storing the CSV in MinIO and passing only a storage key is the long-term solution; for v1 the serialized CSV string parameter is acceptable (payroll-scale CSVs are typically < 300 KB).
- **Progress persistence**: this plan uses Redis with a 24-hour TTL. Durable result history (for audit) would require a `BackgroundJobResult` DB table — deferred.

---

## Context & Research

### Relevant Code and Patterns

- **Tenant setup in jobs**: `src/Payroll.Infrastructure/Jobs/GeneratePayslipsJob.cs` — accepts `Guid tenantId` as job parameter, calls `SetupTenantContextAsync(tenantId)` which loads from `PlatformDbContext` and calls `tenantContext.SetTenant(...)`. All new jobs must replicate this pattern exactly.
- **Job dispatch pattern**: `src/Payroll.Infrastructure/Jobs/HangfirePayrollJobDispatcher.cs` — uses `BackgroundJob.Enqueue<TJob>(j => j.Execute(...))` (static API, not `IBackgroundJobClient`).
- **Existing queues**: Hangfire server declares `["payroll", "reports", "notifications", "default"]` in `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs`. The `payroll` queue is declared but unused. New jobs should use `[Queue("payroll")]`.
- **Redis access**: `IConnectionMultiplexer` is registered as singleton but not yet consumed directly. All new Redis access for job progress uses `IConnectionMultiplexer.GetDatabase()` for `StringGet/SetAsync`.
- **CSV parsing**: `src/Payroll.Application/Utilities/CsvParser.cs` — `CsvParser.Parse(csvText)` returns `IReadOnlyList<string[]>` (header row stripped). The utility needs a `SplitIntoChunks` method added.
- **Import commands**: `src/Payroll.Application/Commands/PayrollRuns/BulkImportLopCommand.cs`, `BulkImportOneTimeEarningsCommand.cs`, `BulkImportReimbursementsCommand.cs` — all accept `(Guid RunId, string CsvContent, Guid ActorId)`, return `ImportResult(int Applied, IReadOnlyList<ImportRowError> Errors)`.
- **Import controllers**: `src/Payroll.Api/Controllers/PayrollRunsController.cs` — three `POST` endpoints that read `IFormFile`, convert to `string`, dispatch via `ISender`, return `200 OK` with `ImportResult`.
- **Initiation command**: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` — `IRequest<PayrollRunSummaryDto>`. Returns full summary including run `Id`.
- **DI scoping**: `IUnitOfWork` is `Scoped`. Hangfire creates a new DI scope per job — scoped services resolve correctly inside jobs.

### Institutional Learnings

- No prior background-job learnings documented in `docs/solutions/`. This plan should produce a `docs/solutions/background-jobs/` entry on completion capturing the tenant-setup pattern, Redis key schema, and chunking approach.

### External References

- Hangfire `[AutomaticRetry(Attempts = 0)]` attribute — disables all retries for a job class.
- Hangfire `[Queue("name")]` attribute — routes jobs to a named queue.
- StackExchange.Redis `IDatabase.StringSetAsync(key, value, expiry)` — atomic set with TTL.

---

## Key Technical Decisions

- **Redis over DB for job progress**: No migration needed. 24-hour TTL auto-cleans results. Import results are transient (the actual data is in breakdown rows). `IConnectionMultiplexer` singleton already registered.
- **Tenant-scoped Redis keys** (`payroll:job:{tenantId}:{jobId}`): Prevents cross-tenant reads without an extra authorization check at the status endpoint.
- **Chunk at job level, not handler level**: Import background jobs parse the full CSV once, split into 500-row batches, reconstruct a mini-CSV (header + batch rows) per chunk, and dispatch the existing MediatR command for each chunk. Existing command handlers are not modified. The same validation and business rules run per chunk as they would run per row today.
- **`[AutomaticRetry(Attempts = 0)]` on all import jobs**: Retrying a partial import would re-apply already-applied rows. The user must re-upload a corrected CSV if a job fails partway.
- **InitiatePayrollRun: background without chunking (v1)**: The command runs in one job execution (as today, just off the HTTP thread). Chunking the employee loop is deferred until the infrastructure proves out.
- **CSV as Hangfire job parameter**: Hangfire serializes job parameters as JSON into PostgreSQL (`hangfire.job`). Payroll-scale CSVs (500 employees × ~30 bytes/row ≈ 15 KB) are well within acceptable size. 5 MB threshold documented as upgrade trigger for MinIO staging.
- **Static `BackgroundJob.Enqueue<T>`**: Matches existing dispatcher pattern. `IBackgroundJobClient` injection is an acceptable alternative if DI is preferred; this plan uses static API to stay consistent.
- **Mini-CSV reconstruction**: Each chunk is reconstructed as `"header\nrow1\nrow2\n..."` so existing command handlers receive valid CSV strings and their internal `CsvParser.Parse` calls work unchanged.

---

## Open Questions

### Resolved During Planning

- **Should import jobs be retried?** No — retry would double-apply rows. `[AutomaticRetry(Attempts = 0)]` on all import job classes.
- **Where to store job results?** Redis with 24h TTL. Acceptable: actual imported data lives in DB; job result is UI feedback only.
- **Should we chunk payroll initiation in this plan?** No — background without chunking first. Chunking the initiation handler is a known follow-up; the infrastructure built here supports it.
- **Which queue?** `"payroll"` — already declared in Hangfire server options, semantically correct.
- **How does the frontend get the run ID after async initiation?** The completed job result includes `{ runId }`. The frontend reads it from the job status response once status is `completed`, then navigates.

### Deferred to Implementation

- Exact Redis serialisation format for `ImportResult` (JSON via `System.Text.Json` is the obvious choice but exact type shape may be refined).
- Whether to add a `CancellationToken` propagation pattern to background jobs (currently `GeneratePayslipsJob` uses no cancellation — follow existing convention).
- How to surface a failed initiation job to the user in the frontend — design the error state in `PayRunsPage` during implementation.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

### Import flow (new)

```
POST /api/v1/payroll-runs/{id}/import/lop
  Controller reads IFormFile → string csvContent
  BackgroundJob.Enqueue<BulkImportBackgroundJob>(ImportType.Lop, runId, csvContent, actorId, tenantId)
  ──→ 202 Accepted { jobId }

  [payroll queue] BulkImportBackgroundJob.Execute(ImportType.Lop, runId, csvContent, actorId, tenantId)
    SetupTenantContextAsync(tenantId)           ← same pattern as GeneratePayslipsJob
    rows = CsvParser.Parse(csvContent)
    IJobProgressService.Initialize(jobId, total: rows.Count)
    foreach chunk in CsvParser.SplitIntoChunks(rows, 500):
      chunkCsv = ReconstructMiniCsv(header, chunk)
      result = ISender.Send(BulkImportLopCommand(runId, chunkCsv, actorId))
      aggregatedApplied += result.Applied
      aggregatedErrors += result.Errors
      IJobProgressService.Update(jobId, processed: cumulativeRows)
    IJobProgressService.Complete(jobId, new ImportResult(aggregatedApplied, aggregatedErrors))

GET /api/v1/jobs/{jobId}/status
  IJobProgressService.Get(tenantId, jobId)
  ──→ { status, processed, total, result? }
```

### Initiation flow (new)

```
POST /api/v1/payroll-runs/initiate
  BackgroundJob.Enqueue<InitiatePayrollRunBackgroundJob>(actorId, tenantId)
  ──→ 202 Accepted { jobId }

  [payroll queue] InitiatePayrollRunBackgroundJob.Execute(actorId, tenantId)
    SetupTenantContextAsync(tenantId)
    IJobProgressService.Initialize(jobId, total: 1)   ← single-unit progress
    summary = ISender.Send(InitiatePayrollRunCommand(actorId))
    IJobProgressService.Complete(jobId, { runId: summary.Id })

Frontend:
  POST /initiate → { jobId }
  Poll GET /jobs/{jobId}/status every 2s
  On completed → navigate to /payroll/runs/{result.runId}
```

### Redis key schema

```
payroll:job:{tenantId}:{jobId}   TTL 24h
Value (JSON):
  {
    "status":    "pending" | "running" | "completed" | "failed",
    "processed": 0..N,
    "total":     N,
    "result":    null | <ImportResult or { runId }>,
    "error":     null | string
  }
```

---

## Implementation Units

- U1. **Job progress service — interface and DTOs**

**Goal:** Define the `IJobProgressService` contract and `JobProgressDto` in the Application layer.

**Requirements:** R1, R2, R4, R7, R8

**Dependencies:** None

**Files:**
- Create: `src/Payroll.Application/Interfaces/IJobProgressService.cs`
- Create: `src/Payroll.Application/DTOs/JobProgressDto.cs`

**Approach:**
- `JobProgressDto` carries: `string JobId`, `string Status` (pending/running/completed/failed), `int Processed`, `int Total`, `string? ResultJson`, `string? Error`.
- `IJobProgressService` methods: `InitializeAsync(string jobId, int total)`, `UpdateAsync(string jobId, int processed)`, `CompleteAsync(string jobId, object result)`, `FailAsync(string jobId, string error)`, `GetAsync(Guid tenantId, string jobId) → JobProgressDto?`.
- All methods are `async Task` to match the Redis async API.
- The interface lives in Application; the key schema (`payroll:job:{tenantId}:{jobId}`) is an implementation detail — do not encode it in the interface.

**Patterns to follow:**
- `src/Payroll.Application/Interfaces/IPayrollExportService.cs` for interface style.

**Test scenarios:**
- Test expectation: none — interface + DTO only, no behavior.

**Verification:**
- Interface and DTO compile in Application project with no Infrastructure references.

---

- U2. **RedisJobProgressService**

**Goal:** Implement `IJobProgressService` using `IConnectionMultiplexer`.

**Requirements:** R4, R7, R8

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Infrastructure/Services/RedisJobProgressService.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (register as `AddScoped<IJobProgressService, RedisJobProgressService>`)
- Test: `tests/Payroll.Infrastructure.Tests/RedisJobProgressServiceTests.cs`

**Approach:**
- Key: `payroll:job:{tenantId}:{jobId}`. `tenantId` scopes keys at the storage level, not just at the API level.
- Value: JSON-serialized `JobProgressDto` via `System.Text.Json`.
- TTL: 24 hours on every write (each update resets TTL).
- `InitializeAsync`: writes `{ status: "pending", processed: 0, total: N }`.
- `UpdateAsync`: reads current value, updates `processed`, writes back. Accepts partial progress (some chunks may process 0 rows if all skipped).
- `CompleteAsync`: serializes `result` as `ResultJson`, sets `status: "completed"`.
- `FailAsync`: sets `status: "failed"`, stores `error` message.
- `GetAsync`: reads and deserializes. Returns `null` if key missing (expired or unknown jobId).
- Inject `IConnectionMultiplexer` — singleton, safe to hold in scoped service.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Services/RedisTenantResolver.cs` for `IConnectionMultiplexer` usage pattern.

**Test scenarios:**
- Happy path: Initialize → Update(250) → Complete(result) → Get returns status=completed, processed=250, result deserializes correctly.
- Edge case: Get on expired/unknown key returns null.
- Edge case: Update called with 0 processed does not corrupt state.
- Error path: FailAsync sets status=failed and error message; subsequent Get reflects this.
- Integration: Uses real Redis via Testcontainers or Docker-compose Redis; key includes correct tenantId prefix.

**Verification:**
- Integration test hits real Redis, verifies key TTL resets on each write, verifies tenant key isolation.

---

- U3. **Job status API endpoint**

**Goal:** Expose `GET /api/v1/jobs/{jobId}/status` so the frontend can poll job progress.

**Requirements:** R4, R7

**Dependencies:** U1, U2

**Files:**
- Create: `src/Payroll.Api/Controllers/JobsController.cs`
- Test: `tests/Payroll.Api.Tests/JobsControllerTests.cs`

**Approach:**
- Route: `GET /api/v1/jobs/{jobId}/status`.
- Requires `[Authorize]` (standard JWT auth — tenant from claim, no special role needed).
- Reads `tenantId` from `ITenantContext` (same pattern as other controllers).
- Calls `IJobProgressService.GetAsync(tenantId, jobId)`.
- Returns `200 OK` with `JobProgressDto` when found.
- Returns `404` when not found (expired, unknown, or wrong tenant).
- Does not return job parameters or CSV content — only progress state.
- `JobId` in the URL is a GUID string (unguessable), providing adequate access control within the tenant scope.

**Patterns to follow:**
- `src/Payroll.Api/Controllers/PayrollRunsController.cs` for controller style and `ITenantContext` injection.

**Test scenarios:**
- Happy path: known jobId in running state returns 200 with correct dto.
- Happy path: completed job returns result in dto.
- Edge case: unknown/expired jobId returns 404.
- Error path: jobId belonging to a different tenantId (key miss because key is tenant-scoped) returns 404.

**Verification:**
- Controller resolves without errors; 200/404 responses match expected shapes.

---

- U4. **CSV chunk utility**

**Goal:** Add `SplitIntoChunks` to `CsvParser` so background jobs can split parsed rows into fixed-size batches and reconstruct valid mini-CSV strings per batch.

**Requirements:** R3

**Dependencies:** None

**Files:**
- Modify: `src/Payroll.Application/Utilities/CsvParser.cs`
- Test: `tests/Payroll.Application.Tests/CsvParserTests.cs` (extend existing)

**Approach:**
- `SplitIntoChunks(IReadOnlyList<string[]> rows, int chunkSize)` → `IEnumerable<IReadOnlyList<string[]>>`. Pure split, no CSV string reconstruction here.
- `ReconstructCsv(string headerLine, IReadOnlyList<string[]> rows)` → `string`. Writes header + re-joins each row with commas. Quoted fields with embedded commas must be re-quoted (round-trip safe).
- Background jobs call `Parse` once on the full CSV, then `SplitIntoChunks` + `ReconstructCsv` per batch.
- The header line is the original first line of the CSV string (passed separately by the background job, not stored in the parsed rows since `Parse` already strips it).
- Alternative: store the original header when parsing. Decide during implementation which is cleaner.

**Patterns to follow:**
- Existing `CsvParser.Parse` implementation for quoting/escaping conventions.

**Test scenarios:**
- Happy path: 1200 rows split into chunks of 500 → 3 chunks (500, 500, 200).
- Edge case: rows count exactly divisible by chunkSize → no empty trailing chunk.
- Edge case: rows count < chunkSize → single chunk.
- Edge case: empty rows list → empty enumerable.
- Happy path: `ReconstructCsv` round-trips — `Parse(ReconstructCsv(header, rows))` equals original rows.
- Edge case: fields with commas are re-quoted correctly in `ReconstructCsv`.

**Verification:**
- Unit tests pass; `Parse(ReconstructCsv(header, rows))` == input rows for all test cases.

---

- U5. **BulkImportBackgroundJob**

**Goal:** Background job that processes all three import types in 500-row chunks, dispatching the existing MediatR commands unchanged, and writing progress to Redis after each chunk.

**Requirements:** R1, R3, R5, R6

**Dependencies:** U1, U2, U4

**Files:**
- Create: `src/Payroll.Infrastructure/Jobs/BulkImportBackgroundJob.cs`
- Test: `tests/Payroll.Infrastructure.Tests/BulkImportBackgroundJobTests.cs`

**Approach:**
- Constructor-injected: `ITenantContext`, `PlatformDbContext`, `ISender`, `IJobProgressService`.
- Method signature: `Execute(string jobId, string importType, Guid runId, string csvContent, Guid actorId, Guid tenantId)`. `importType` is a string enum value ("lop", "earnings", "reimbursements") — strings serialize cleanly in Hangfire parameters.
- Job attributes: `[AutomaticRetry(Attempts = 0)]`, `[Queue("payroll")]`.
- Execution flow:
  1. `SetupTenantContextAsync(tenantId)` — identical to `GeneratePayslipsJob`.
  2. `rows = CsvParser.Parse(csvContent)`.
  3. `IJobProgressService.InitializeAsync(jobId, rows.Count)`.
  4. For each chunk (500 rows): reconstruct mini-CSV, dispatch correct command via `ISender.Send(...)`, accumulate `ImportResult`, call `IJobProgressService.UpdateAsync(jobId, cumulativeProcessed)`.
  5. `IJobProgressService.CompleteAsync(jobId, aggregatedResult)`.
  6. On any unhandled exception: `IJobProgressService.FailAsync(jobId, ex.Message)` then rethrow (Hangfire marks job as failed in its own tables; Redis reflects failure for the UI).
- `importType` dispatches to: `BulkImportLopCommand`, `BulkImportOneTimeEarningsCommand`, or `BulkImportReimbursementsCommand` — all existing, unchanged.
- Aggregated `ImportResult`: sum all `Applied` counts, concatenate all `Errors` lists (with row numbers offset per chunk so they refer back to the original CSV row numbers). Row offset = `chunkIndex * 500 + 2` (accounting for header row).

**Patterns to follow:**
- `src/Payroll.Infrastructure/Jobs/GeneratePayslipsJob.cs` for tenant setup, constructor injection, and `ISender` usage.

**Test scenarios:**
- Happy path: 600 LOP rows → 2 chunks dispatched; applied = sum, errors = merged list; final status = completed.
- Happy path: earnings job dispatches `BulkImportOneTimeEarningsCommand`, not `BulkImportLopCommand`.
- Edge case: 0 applied rows across all chunks → status = completed, applied = 0, errors populated.
- Error path: `ISender.Send` throws on chunk 2 → `FailAsync` called; job rethrows for Hangfire to mark failed.
- Integration: full job execution against real DB and Redis via test containers; imported rows appear in payrun_component_breakdowns after completion.

**Verification:**
- Unit tests with substituted `ISender` confirm correct command type dispatched per import type and correct row-offset applied to error row numbers.
- Integration test confirms real data appears in DB after job completion.

---

- U6. **Import controllers → 202 Accepted**

**Goal:** Change the three import endpoints to enqueue `BulkImportBackgroundJob` and return `202 Accepted { jobId }` instead of blocking.

**Requirements:** R1

**Dependencies:** U5

**Files:**
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs`
- Create: `src/Payroll.Application/DTOs/JobAcceptedDto.cs` (or inline record)
- Test: `tests/Payroll.Api.Tests/PayrollRunsImportControllerTests.cs`

**Approach:**
- Each import endpoint:
  1. Reads `IFormFile` → `string csvContent` (unchanged).
  2. Generates `jobId = Guid.NewGuid().ToString()`.
  3. Calls `BackgroundJob.Enqueue<BulkImportBackgroundJob>(j => j.Execute(jobId, importType, runId, csvContent, actorId, tenantId))`.
  4. Returns `Accepted(new { jobId })`.
- `tenantId` from `ITenantContext.TenantId`. `actorId` from JWT `sub` claim.
- Response body: `{ "jobId": "..." }` (matches what the frontend will poll with).
- Retain the `422` guard for non-Draft runs before enqueuing (run status check moves to the controller layer; the background job will also fail gracefully if run status changes between enqueue and execute, but the fast 422 is better UX).

**Patterns to follow:**
- Existing import endpoints in `src/Payroll.Api/Controllers/PayrollRunsController.cs`.

**Test scenarios:**
- Happy path: valid file → 202 with jobId GUID.
- Error path: non-Draft run → 422 before enqueue (no job created).
- Error path: empty file → 400 before enqueue.
- Edge case: `tenantId` from tenant context is passed correctly in job parameters.

**Verification:**
- Endpoints return 202; Hangfire job appears enqueued (observable via `IMonitoringApi` in test or Hangfire test utilities).

---

- U7. **InitiatePayrollRunBackgroundJob**

**Goal:** Wrap `InitiatePayrollRunCommand` in a background job so payroll initiation does not block the HTTP request.

**Requirements:** R2, R6

**Dependencies:** U1, U2

**Files:**
- Create: `src/Payroll.Infrastructure/Jobs/InitiatePayrollRunBackgroundJob.cs`
- Test: `tests/Payroll.Infrastructure.Tests/InitiatePayrollRunBackgroundJobTests.cs`

**Approach:**
- Constructor-injected: `ITenantContext`, `PlatformDbContext`, `ISender`, `IJobProgressService`.
- Attributes: `[AutomaticRetry(Attempts = 0)]`, `[Queue("payroll")]`.
- Method: `Execute(string jobId, Guid actorId, Guid tenantId)`.
- Execution:
  1. `SetupTenantContextAsync(tenantId)`.
  2. `IJobProgressService.InitializeAsync(jobId, total: 1)`.
  3. `summary = await ISender.Send(new InitiatePayrollRunCommand(actorId))`.
  4. `IJobProgressService.CompleteAsync(jobId, new { runId = summary.Id })`.
  5. On exception: `FailAsync`, rethrow.
- `InitiatePayrollRunCommand` returns `PayrollRunSummaryDto` — the `Id` field is the new run's GUID, needed for frontend navigation.
- Auto-retry disabled: initiation is not idempotent in its current form (it calculates the next pay period by looking at the latest paid run). Re-running could create a duplicate run.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Jobs/GeneratePayslipsJob.cs`.

**Test scenarios:**
- Happy path: job executes successfully → `CompleteAsync` called with `{ runId: <guid> }`.
- Error path: `ISender.Send` throws `DomainException` (e.g., no pay schedule) → `FailAsync` called with exception message.
- Integration: job creates a payroll run visible via the repository after completion.

**Verification:**
- Job completes; `IJobProgressService.GetAsync` returns `status = completed` and `result.runId` is a valid run GUID in the DB.

---

- U8. **Initiation controller → 202 Accepted**

**Goal:** Change `POST /api/v1/payroll-runs/initiate` to enqueue `InitiatePayrollRunBackgroundJob` and return `202 { jobId }`.

**Requirements:** R2

**Dependencies:** U7

**Files:**
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs`
- Test: extend `tests/Payroll.Api.Tests/PayrollRunsControllerTests.cs`

**Approach:**
- Generate `jobId`, enqueue `InitiatePayrollRunBackgroundJob`, return `Accepted(new { jobId })`.
- Any pre-flight validations that were done in the command handler before touching the DB should stay in the controller (e.g., return `422` if an outstanding Draft run already exists — check `CurrentPayPeriodDto.HasOutstandingRun` pattern).
- No `[AutomaticRetry]` attribute needed on the controller side; that is set on the job class.

**Patterns to follow:**
- Import endpoint pattern from U6.

**Test scenarios:**
- Happy path: initiate → 202 with jobId.
- Error path: outstanding draft run exists → 422 before enqueue.

**Verification:**
- Returns 202; job visible in Hangfire storage.

---

- U9. **Frontend: ImportModal polling flow**

**Goal:** Update `ImportModal` to handle the new `202 Accepted` response by polling `/jobs/{jobId}/status` until complete, then rendering the existing result UI.

**Requirements:** R1, R4

**Dependencies:** U3, U6

**Files:**
- Modify: `web/src/pages/payroll/components/ImportModal.tsx`
- Test: `web/src/__tests__/ImportModal.test.tsx` (add polling scenarios)

**Approach:**
- On `202` response: extract `jobId`, switch modal state to `polling`.
- Poll `GET /api/v1/jobs/{jobId}/status` every 2 seconds using `setInterval` (or `useInterval` hook). Use `api` axios instance (not `fetch`) to maintain correct auth headers.
- Progress view: show a spinner + `"Processing... {processed} / {total} rows"` text.
- On `status === "completed"`: clear interval, set `result` state (parse `resultJson` from the `JobProgressDto`), render existing result UI (applied count + error table). Call `onSuccess()` if `applied > 0`.
- On `status === "failed"`: clear interval, show error banner with the `error` field from the DTO.
- On `status === "running" | "pending"`: continue polling.
- Add `componentWillUnmount` / `useEffect` cleanup to clear the interval when the modal closes during polling.
- `JobProgressDto` type in `web/src/types/api.ts`.

**Patterns to follow:**
- Existing `ImportModal.tsx` for state management, error banner, and result UI.

**Test scenarios:**
- Happy path: upload → 202 → polling shows progress → completed → result rendered with applied count.
- Edge case: modal closed during polling → interval cleared, no further requests made.
- Error path: job fails → error banner shown with message from `error` field.
- Edge case: `status = "pending"` on first poll → continues polling without flipping to result.
- Integration: upload a real CSV file in Playwright E2E; result appears after polling completes.

**Verification:**
- TypeScript compiles; ESLint clean; modal correctly transitions through pending → running → completed → result-display states.

---

- U10. **Frontend: Payroll run initiation polling**

**Goal:** Update the payroll initiation trigger to handle `202 Accepted`, poll for completion, and navigate to the new run's detail page once the job reports success.

**Requirements:** R2, R4

**Dependencies:** U3, U8

**Files:**
- Modify: `web/src/pages/payroll/PayRunsPage.tsx` (or whichever component calls `/api/v1/payroll-runs/initiate`)
- Test: extend existing Vitest or Playwright tests for the payroll initiation flow

**Approach:**
- Current flow: `POST /initiate` → receive `PayrollRunSummaryDto` → navigate to `/payroll/runs/{id}`.
- New flow: `POST /initiate` → receive `{ jobId }` → show an in-page progress indicator → poll `/jobs/{jobId}/status` every 2 seconds → on `status === "completed"`, parse `resultJson` for `runId`, navigate to `/payroll/runs/{runId}` → on `status === "failed"`, show error toast/banner.
- Progress indicator: a modal or inline loading state on the "Process Payroll" button (disabled + spinner while job is pending/running).
- The polling interval must be cleared on component unmount.
- `JobProgressDto` already added to `web/src/types/api.ts` in U9.

**Patterns to follow:**
- Polling pattern established in U9 (`ImportModal`).

**Test scenarios:**
- Happy path: initiate → polling → completed → navigate to run detail page with correct runId.
- Error path: job fails → error shown on page; user can retry initiation.
- Edge case: user navigates away during polling → interval cleared.

**Verification:**
- TypeScript and ESLint clean; user lands on the correct run detail page after job completes.

---

## System-Wide Impact

- **Interaction graph:** The three import endpoints and the initiation endpoint now return 202. Any client (script, integration, test) calling these endpoints must be updated to poll instead of reading the synchronous response. Existing Playwright E2E tests for import and initiation flows will break and need updating.
- **Error propagation:** Import handler exceptions inside background jobs are caught by the job, stored in Redis via `FailAsync`, and rethrown for Hangfire to record. The user sees the failure message via the poll endpoint. `[AutomaticRetry(Attempts = 0)]` ensures no silent second attempt.
- **State lifecycle risks:** If `BulkImportBackgroundJob` fails mid-way (e.g., after chunk 1 but before chunk 2), chunk 1 rows are already committed to the DB — there is no rollback across chunks. The user sees the partial result in the job error and must re-upload a corrected file covering only the unprocessed rows. This is acceptable and matches the existing partial-success semantics of single-shot imports.
- **API surface parity:** No agent-facing API changes beyond the 202 response shape. The frontend is the only consumer of these endpoints today.
- **Integration coverage:** The existing MediatR `ValidationBehaviour` and `TenantValidationBehaviour` run inside `ISender.Send` even from within background jobs — tenant context is set before dispatch, so the TenantValidation pipeline pass will succeed. The run-status guard (`Draft` check inside the command handlers) still fires per-chunk.
- **Unchanged invariants:** `Payroll.Engine.PayrollEngine.Compute()` is called identically to today — one call per employee, same inputs, same outputs. `SetLopCommandHandler.RecomputeEmployee` is called identically per row. No calculation result changes.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| Hangfire default retry (10x) was active before; disabling globally would break email jobs | `[AutomaticRetry(Attempts = 0)]` is set **per job class** on import and initiation jobs only. Email jobs are unchanged. |
| CSV string parameter size in Hangfire's PostgreSQL storage | Document 5 MB as the upgrade trigger. Payroll-scale files are << 1 MB. |
| Partial import on mid-job failure leaves DB in partially applied state | This is the same risk as today's single-shot import. Acceptable; user re-uploads remaining rows. Document clearly in UI error message. |
| `InitiatePayrollRunCommand` is not idempotent — retry would create duplicate run | `[AutomaticRetry(Attempts = 0)]` on the job. UI must not re-submit while job is pending/running. |
| Redis TTL expires before the frontend polls the result (user leaves and returns 25h later) | 404 from status endpoint; user retries the import. 24h TTL is generous for the use case. |
| Existing E2E tests assert synchronous import responses | All import and initiation E2E tests must be updated to the polling model. Plan this as part of U9/U10 test work. |
| Background jobs run in a worker process or worker-only mode (`Hangfire:WorkerOnly` flag) | `IJobProgressService` (Redis) and all repositories are registered in both API and worker modes — verify DI registration applies to both. |

---

## Documentation / Operational Notes

- After landing, create `docs/solutions/background-jobs/tenant-setup-and-progress-tracking.md` documenting: the tenant-setup pattern for Hangfire jobs, the Redis key schema, the chunk-and-dispatch pattern, and the `[AutomaticRetry(Attempts = 0)]` import policy.
- Hangfire dashboard (`/hangfire`) remains accessible to SuperAdmins for monitoring job state.
- Redis key expiry (24h) means job progress is not durable across Redis restarts if persistence is not configured. In production, configure Redis AOF or RDB persistence, or switch the result store to a DB table.

---

## Sources & References

- Related code: `src/Payroll.Infrastructure/Jobs/GeneratePayslipsJob.cs`
- Related code: `src/Payroll.Infrastructure/Jobs/HangfirePayrollJobDispatcher.cs`
- Related code: `src/Payroll.Application/Utilities/CsvParser.cs`
- Related code: `src/Payroll.Application/Commands/PayrollRuns/BulkImportLopCommand.cs`
- Related code: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Related code: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
