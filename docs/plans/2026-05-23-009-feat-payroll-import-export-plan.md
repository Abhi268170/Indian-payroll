---
title: "feat: Payroll Run Bulk Import/Export"
type: feat
status: active
date: 2026-05-23
---

# feat: Payroll Run Bulk Import/Export

## Overview

Add bulk CSV import and XLS/CSV export to the payroll run Draft page. Admins can upload LOP days, one-time earnings, and expense reimbursements for all employees in one file rather than clicking into each employee's variable inputs panel. They can also export a full payroll summary in CSV or XLS format. Based on a live audit of payroll.zoho.in (2026-05-23).

---

## Problem Frame

Entering variable inputs one employee at a time is impractical for orgs with 20+ employees. Admins need to bulk-upload a prepared spreadsheet and bulk-export payroll data for salary payment or accounting. This is a standard payroll product capability; Zoho Payroll provides five import types and six export report types.

---

## Requirements Trace

- R1. Admin can bulk-import LOP days for all employees in a pay run via CSV upload.
- R2. Admin can bulk-import one-time earnings/deductions (bonus, commission, leave encashment, etc.) via CSV upload.
- R3. Admin can bulk-import expense reimbursements via CSV upload.
- R4. Admin can export pay run data as CSV or XLS (Employee Pay Run Details report).
- R5. Import validates per row; valid rows are applied, invalid rows are returned with reasons — no all-or-nothing rollback.
- R6. Import is only allowed when the run is in Draft state. Export is available in any state.
- R7. Sample CSV templates are downloadable from the Import modal (client-side generated, no backend endpoint required).
- R8. Import/Export is accessible from a dropdown button on the employee summary table (matching Zoho's "Import / Export" button pattern).

---

## Scope Boundaries

- LOP Adjustment Days import: deferred (not selected by user).
- Withhold/Release Salary import: deferred (not selected by user).
- Export report types beyond "Employee Pay Run Details": deferred (TDS Worksheet, LOP Details, Payrun Payment, Reimbursement Details — future iteration).
- Column-mapping UI: out of scope. Fixed template format only.
- XLS import: out of scope. CSV import only.
- File encoding selector: out of scope. UTF-8 (with BOM auto-strip) only.
- Password-protect export: out of scope.
- Export filters (department, location, designation): out of scope for v1.

### Deferred to Follow-Up Work

- Additional export report types (TDS Worksheet, LOP Details, One-Time Earnings, Reimbursements): separate PR.
- XLS import support: separate PR.
- Import step 2 (column mapping UI, matching Zoho's flow): separate PR.

---

## Context & Research

### Zoho Reference (live audit 2026-05-23)

**Import CSV templates (actual downloaded samples):**

| Type | Columns |
|---|---|
| LOP Details | `Employee No, LOP Days` |
| One Time Earnings | `Employee No, Bonus, Commission, Leave Encashment, ...` (wide, one col per component) |
| Expense Reimbursements | `Employee Number, Report Number, Amount To Be Reimbursed` |

Our v1 earnings template diverges from Zoho: we use **tall/normalised format** (`Employee Code, Component Code, Amount`) — one row per earning. Simpler to parse and doesn't require generating a dynamic wide header per org's component set.

**Export modal (live):** Report type dropdown (6 types), format = XLS default, filters (location/dept/designation), password-protect checkbox. Our v1: single report type (Employee Pay Run Details), CSV/XLS format selector, no filters.

### Relevant Code and Patterns

- `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs` — single-employee LOP with full engine recompute. The `internal static RecomputeEmployee` method is reusable by the bulk command.
- `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs` — single-employee one-time earning pattern (breakdown row + GrossPay/NetPay update).
- `src/Payroll.Domain/Interfaces/IEmployeeRepository.cs` — `GetManyByCodesAsync(IEnumerable<string> codes)` exists for bulk employee code → entity lookup.
- `src/Payroll.Domain/Interfaces/ISalaryComponentRepository.cs` — `ListActiveEarningsAsync(tenantId)` loads all active earnings; handler builds a code→component dict in memory.
- `src/Payroll.Domain/Interfaces/IPayrunEmployeeRepository.cs` — `GetByRunIdAsync(runId)` loads all payrun employees at once; no N+1 per CSV row.
- `src/Payroll.Infrastructure/Payroll.Infrastructure.csproj` — **ClosedXML already installed** for XLS generation; no new NuGet packages needed.
- `src/Payroll.Infrastructure/Services/PayslipPdfGenerator.cs` — pattern for Infrastructure export service with Application interface.
- `web/src/pages/payroll/components/VariableInputsPanel.tsx` — existing slide-in panel; ImportModal and ExportModal follow same component pattern.
- `web/src/pages/payroll/PayRunDetailPage.tsx` — page that owns the employee summary table; wire new modals here.

### Institutional Learnings

- No `docs/solutions/` entries relevant to CSV bulk import.

---

## Key Technical Decisions

- **CSV parsing utility in Application layer, not Infrastructure**: Parsing is pure string processing with no I/O. Lives in `src/Payroll.Application/Utilities/CsvParser.cs`. No CsvHelper dependency — the templates are narrow (2-3 columns) and fixed; manual parsing avoids a new transitive dep.
- **Best-effort import (partial apply)**: Valid rows are applied; invalid rows are returned with row-number + reason. No all-or-nothing rollback. This matches Zoho's behaviour and is more useful for HR admins who may have a mix of valid and invalid rows.
- **Batch data loading, not N+1 per row**: Each bulk command loads all employees, payrun employees, and components upfront (one query each), then processes rows against in-memory dicts. Avoids N×3 DB queries for a 200-row CSV.
- **LOP bulk reuses `SetLopCommandHandler.RecomputeEmployee`**: That method is already `internal static`. Bulk handler calls it per valid row, then updates run summary once at the end.
- **Earnings bulk adds breakdowns directly** (same as `AddOneTimeEarningCommand`) without calling the single-row command. Avoids per-employee run summary updates mid-loop; batch the summary update at the end.
- **Reimbursements update `PayrunEmployee.ReimbursementsAmount + NetPay` only** — no breakdown row. The current data model stores reimbursements as an aggregate on `PayrunEmployee`. Adding breakdown rows would require payslip changes to avoid double-counting; defer for a future iteration when the reimbursement entity model is revisited.
- **Export via Application interface + Infrastructure service**: Application defines `IPayrollExportService`; Infrastructure implements with Dapper (for data) + ClosedXML (XLS) / string builder (CSV). Matches `IPayslipPdfGenerator` pattern.
- **Template download is client-side only**: The modal generates a Blob URL from a hardcoded CSV string and triggers a download. No backend endpoint needed. Keeps the template always in sync with the frontend's known format.
- **Controller reads `IFormFile`, converts to string, passes to command**: Keeps IFormFile out of Application layer. The command receives `string csvContent` (not a Stream or IFormFile).
- **`ISalaryComponentRepository` needs no new method**: Bulk earnings handler calls `ListActiveEarningsAsync` once and builds a `code → component` dict in memory.

---

## Open Questions

### Resolved During Planning

- **Should import be all-or-nothing or best-effort?** Best-effort (partial apply). Admins with 200 rows shouldn't need to fix one bad row to get the other 199 applied.
- **Should earnings use Zoho's wide format or a tall format?** Tall format (`Employee Code, Component Code, Amount`). Zoho's wide format requires generating a dynamic header per org's component set — too complex for v1.
- **Where does CSV parsing live?** Application layer as a pure utility. No I/O dep, no framework dep.
- **Should reimbursements add a breakdown row?** No for v1. Aggregate on `PayrunEmployee` is the current model; breakdown rows would require payslip changes to avoid double-counting.
- **New NuGet package for CSV?** No. Manual parsing is sufficient for narrow, fixed-format templates.

### Deferred to Implementation

- **Run summary update ordering for bulk LOP**: whether to update run summary after each row or batch at the end is an implementation-time optimisation decision. Batching is preferred but must be verified against the run summary invariant.
- **Exact XLS column widths / styling in ClosedXML**: implementation-time detail.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification.*

```
POST /{id}/import/lop (multipart, IFormFile)
  │
  ▼ Controller
  reads IFormFile → string csvContent
  sends BulkImportLopCommand(runId, csvContent, actorId)
  │
  ▼ Handler
  CsvParser.Parse(csvContent) → IReadOnlyList<string[]>
  validate run is Draft
  batch-load: employees by code, payrun employees by run, pay schedule, config, YTD
  foreach valid row:
    recompute via SetLopCommandHandler.RecomputeEmployee(...)
    update PayrunEmployee + breakdowns
  update run summary once
  return ImportResult { Applied, Errors[] }

GET /{id}/export?format=csv|xls
  │
  ▼ Controller
  sends ExportPayrollRunQuery(runId, format)
  │
  ▼ Handler → IPayrollExportService.ExportEmployeePayRunDetails(runId, format)
  │
  ▼ Infrastructure
  Dapper: SELECT payrun employees + employee names for runId
  serialize → CSV (StringBuilder) or XLS (ClosedXML Workbook)
  return ExportFileResult { FileName, ContentType, Data: byte[] }
  │
  Controller → FileContentResult(data, contentType, fileName)
```

**Import/Export dropdown (frontend):**

```
[Import / Export ▾]
  ── IMPORT (disabled if not Draft) ──
  LOP Details              → opens ImportModal(type='lop')
  One-Time Earnings        → opens ImportModal(type='earnings')
  Expense Reimbursements   → opens ImportModal(type='reimbursements')
  ── EXPORT ──
  Payroll Data             → opens ExportModal
```

**ImportModal flow:**

```
Step 1 (file select)
  [Download template]  drag-drop zone  [Next]
          ↓ on file select + Next
Step 2 (result)
  Spinner → ImportResult
  Applied: N rows
  Errors:  [row# | employee code | reason]  (if any)
  [Close]
```

---

## Implementation Units

- U1. **CSV parsing utility**

**Goal:** Pure utility that parses UTF-8 CSV text into a list of string arrays, suitable for narrow fixed-format templates.

**Requirements:** R1, R2, R3, R5

**Dependencies:** None

**Files:**
- Create: `src/Payroll.Application/Utilities/CsvParser.cs`
- Test: `tests/Payroll.Application.Tests/Utilities/CsvParserTests.cs`

**Approach:**
- Input: `string csvText` → Output: `IReadOnlyList<string[]>` (data rows only, header excluded)
- Strip UTF-8 BOM (`﻿`) from first line if present
- Skip blank lines
- Handle RFC 4180 quoted fields: field wrapped in double-quotes; embedded double-quote escaped as `""`. A field containing a comma or newline must be quoted.
- Trim leading/trailing whitespace from each cell after unquoting
- Row count includes only data rows (header skipped)

**Test scenarios:**
- Happy path: 3-column CSV with 2 data rows → returns 2 arrays of 3 strings each
- BOM-prefixed file: first header cell has `﻿` prefix → stripped, first data cell is clean
- Quoted field with embedded comma: `"Smith, John"` → single cell value `Smith, John`
- Quoted field with embedded `""`: `"O""Brien"` → `O'Brien`
- Blank rows interspersed → omitted from result
- Whitespace-padded cells: `"  EMP001  "` → `"EMP001"` after trim
- Header-only file (no data rows) → returns empty list
- Single-column CSV → each row is array of length 1

**Verification:** All unit tests pass; no framework or I/O imports in the utility file.

---

- U2. **Bulk import LOP command + endpoint**

**Goal:** Backend command that parses a LOP CSV, validates per row, applies engine recompute for valid rows, and returns applied/error counts.

**Requirements:** R1, R5, R6

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/BulkImportLopCommand.cs`
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` (add POST endpoint)
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/BulkImportLopCommandTests.cs`

**Approach:**
- Command record: `BulkImportLopCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>`
- `ImportResult`: record with `int Applied`, `IReadOnlyList<ImportRowError> Errors` where `ImportRowError` has `int Row, string EmployeeCode, string Reason`
- Handler strategy:
  1. Verify run exists and is Draft — if not, throw `InvalidOperationException` (controller returns 422)
  2. Parse CSV via `CsvParser.Parse` — expected columns: `Employee Code` (index 0), `LOP Days` (index 1)
  3. Batch-load employees: `GetManyByCodesAsync(all employee codes from CSV)`
  4. Load all payrun employees: `GetByRunIdAsync(runId)` → build `employeeId → PayrunEmployee` dict
  5. Load pay schedule, compute salary divisor
  6. Load YTD map, FY openings (same as `SetLopCommandHandler`)
  7. Per row (in order):
     - Validate: employee code found, employee in run, not skipped, LOP days ≥ 0 and < divisor
     - On valid: call `SetLopCommandHandler.RecomputeEmployee(...)`, `payrunEmp.SetLop(...)`, update breakdown amounts
     - On invalid: append to errors list
  8. After all rows: update run summary once using final state of all active payrun employees
  9. `uow.SaveChangesAsync`
- Controller: `POST api/v1/payroll-runs/{id}/import/lop` — `[FromForm] IFormFile file` — reads bytes, decodes as UTF-8, sends command — returns 200 with `ImportResult`
- `SetLopCommandHandler.RecomputeEmployee` is already `internal static` — accessible from the same assembly

**Patterns to follow:**
- `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs` — engine recompute pattern, run summary update
- `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` — batch loading pattern

**Test scenarios:**
- Happy path: 3-row CSV all valid → Applied=3, Errors empty, run summary updated
- Employee code not in org → row error "Employee not found"
- Employee not in this run → row error "Employee not in this payroll run"
- Employee is Skipped status → row error "Employee is skipped"
- LOP days = salary divisor → row error (guard from SetLopCommand)
- LOP days = 0 → valid (clears LOP, engine recomputes)
- Run is Approved (not Draft) → handler throws before processing any row → controller returns 422
- Mixed: 2 valid, 1 invalid → Applied=2, Errors has 1 entry
- Duplicate employee code in CSV → second row for same employee overwrites first (last-wins)
- CSV has extra whitespace in employee code column → trimmed, resolves correctly
- Non-integer LOP days value → row error "LOP Days must be a whole number"

**Verification:** Integration test: upload 3-row valid CSV → verify `GetEmployeeVariableInputsQuery` returns updated LOP for each employee + engine-recomputed gross.

---

- U3. **Bulk import one-time earnings command + endpoint**

**Goal:** Backend command that parses an earnings CSV (`Employee Code, Component Code, Amount`) and bulk-applies one-time earnings to a Draft run.

**Requirements:** R2, R5, R6

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/BulkImportOneTimeEarningsCommand.cs`
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` (add POST endpoint)
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/BulkImportOneTimeEarningsCommandTests.cs`

**Approach:**
- Command: `BulkImportOneTimeEarningsCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>`
- Template format: `Employee Code, Component Code, Amount` (3 columns, tall/normalised)
- Handler strategy:
  1. Verify run is Draft
  2. Parse CSV
  3. Batch-load: employees by code, payrun employees by run, all active earnings via `ListActiveEarningsAsync` → build `code → SalaryComponent` dict (case-insensitive)
  4. Per row:
     - Validate: employee found, in run, not skipped, component code found in active earnings dict, amount > 0
     - On valid: create `PayrunComponentBreakdown` (IsOneTimeEarning=true), call `payrunEmp.UpdateComputedAmounts` with GrossPay += amount and NetPay += amount (mirrors `AddOneTimeEarningCommand` logic)
  5. Batch `breakdownRepo.AddAsync` for all valid breakdowns
  6. Update run summary once
  7. `uow.SaveChangesAsync`
- Controller endpoint: `POST api/v1/payroll-runs/{id}/import/earnings`

**Patterns to follow:**
- `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs` — breakdown creation + gross/net update pattern

**Test scenarios:**
- Happy path: 2 employees, 1 earning each → Applied=2, breakdowns created, GrossPay updated on each
- Component code not found in active earnings → row error "Component not found or not an active earning"
- Component code found but not an earning category → row error (salary components of type Deduction/Benefit must not be accepted)
- Amount = 0 → row error "Amount must be greater than zero"
- Amount negative → row error
- Employee skipped → row error
- Run not Draft → 422
- Same employee, two different earning types in same CSV → both applied (2 breakdown rows)
- Component code lookup is case-insensitive: `bonus` matches component with code `BONUS`

**Verification:** After import, `GetEmployeeVariableInputsQuery` for each affected employee shows the new one-time earnings in the breakdown list.

---

- U4. **Bulk import reimbursements command + endpoint**

**Goal:** Backend command that parses a reimbursements CSV and updates `ReimbursementsAmount` + `NetPay` on affected payrun employees.

**Requirements:** R3, R5, R6

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/BulkImportReimbursementsCommand.cs`
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` (add POST endpoint)
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/BulkImportReimbursementsCommandTests.cs`

**Approach:**
- Command: `BulkImportReimbursementsCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>`
- Template format: `Employee Code, Report Number, Amount` (3 columns — mirrors Zoho's `Employee Number, Report Number, Amount To Be Reimbursed`)
- Handler strategy:
  1. Verify run is Draft
  2. Parse CSV
  3. Batch-load employees by code, payrun employees by run
  4. Per row:
     - Validate: employee found, in run, not skipped, amount > 0
     - On valid: `payrunEmp.UpdateComputedAmounts` with `reimbursementsAmount += amount` and `netPay += amount` (all other fields unchanged)
  5. Update run summary once
  6. `uow.SaveChangesAsync`
- Note: No breakdown row added. `ReimbursementsAmount` is an aggregate on `PayrunEmployee`. Breakdown rows for reimbursements are deferred pending a reimbursement entity model revision.
- Controller endpoint: `POST api/v1/payroll-runs/{id}/import/reimbursements`

**Test scenarios:**
- Happy path: 3 employees with positive amounts → Applied=3, ReimbursementsAmount and NetPay updated on each
- Amount = 0 → row error
- Amount negative → row error
- Employee not in run → row error
- Employee skipped → row error
- Two rows for same employee in CSV → amounts are additive (both applied)
- Run not Draft → 422

**Verification:** `GetPayrollRunEmployeesQuery` response for affected employees shows updated `reimbursementsAmount` and `netPay`.

---

- U5. **Export payroll data — Application interface + Infrastructure service + endpoint**

**Goal:** Export Employee Pay Run Details as CSV or XLS from any run state.

**Requirements:** R4, R6

**Dependencies:** None (independent of U1–U4)

**Files:**
- Create: `src/Payroll.Application/Interfaces/IPayrollExportService.cs`
- Create: `src/Payroll.Infrastructure/Services/PayrollExportService.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (register service)
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` (add GET endpoint)
- Test: `tests/Payroll.Infrastructure.Tests/Services/PayrollExportServiceTests.cs` (integration)

**Approach:**
- Interface: `IPayrollExportService` with method `Task<ExportFileResult> ExportEmployeePayRunDetailsAsync(Guid runId, string format, CancellationToken ct)`
- `ExportFileResult`: record with `string FileName, string ContentType, byte[] Data`
- Infrastructure implementation uses **Dapper** to query payrun employees joined with employees for code + name, then serialises:
  - **CSV** (`format = "csv"`): plain `StringBuilder`, header row + data rows, UTF-8 BOM prefix for Excel compatibility, `text/csv` content-type
  - **XLS** (`format = "xls"`): ClosedXML `XLWorkbook`, `application/vnd.ms-excel` content-type
- CSV/XLS columns: `Employee Code, Employee Name, Paid Days, LOP Days, Gross Pay, Employee PF, Employee ESI, TDS, PT, LWF (Employee), Net Pay, Status`
- Status = "Active" or "Skipped"; skipped employees included with zeroed monetary columns
- Controller: `GET api/v1/payroll-runs/{id}/export?format=csv` — injects `IPayrollExportService`, returns `FileContentResult` with appropriate headers

**Patterns to follow:**
- `src/Payroll.Infrastructure/Services/PayslipPdfGenerator.cs` — Application interface + Infrastructure service registration pattern
- Existing Dapper queries in Infrastructure for data access patterns
- `src/Payroll.Api/Controllers/PayrollRunsController.cs` bank advice download endpoint (`GET /{id}/bank-advice/download`) — `FileContentResult` return pattern

**Test scenarios:**
- Happy path CSV: run with 2 active employees → file bytes decode to UTF-8, first row is header, subsequent rows match employee data, monetary values formatted as plain decimals (no ₹ symbol in file)
- Happy path XLS: same run → content-type is `application/vnd.ms-excel`, file is valid ClosedXML workbook, first row is header
- Skipped employee in run → included in export with Status="Skipped" and zero monetary amounts
- Run not found → 404
- Run in Approved state (not Draft) → export still works (R6)
- `format=xls` query param → XLS content-type
- `format=csv` (default) → CSV content-type
- BOM prefix present in CSV output (so Excel opens it without encoding dialog)

**Verification:** Download endpoint returns 200 with `Content-Disposition: attachment; filename="Payroll_MMMYYYY.csv"` and parseable data. ClosedXML workbook opens without error.

---

- U6. **Frontend: ImportModal component**

**Goal:** Reusable modal for file drag-drop upload of CSV imports, showing result summary with error table.

**Requirements:** R1, R2, R3, R5, R7, R8

**Dependencies:** U2, U3, U4 (backend endpoints must exist)

**Files:**
- Create: `web/src/pages/payroll/components/ImportModal.tsx`

**Approach:**
- Props: `type: 'lop' | 'earnings' | 'reimbursements'`, `runId: string`, `isOpen: boolean`, `onClose: () => void`, `onSuccess: () => void`
- Two internal views controlled by `phase: 'upload' | 'result'`
- **Upload view:**
  - "Download template" link — generates Blob URL from hardcoded CSV string per type:
    - `lop`: header `Employee Code,LOP Days`
    - `earnings`: header `Employee Code,Component Code,Amount`
    - `reimbursements`: header `Employee Code,Report Number,Amount`
  - Drag-drop zone: `<input type="file" accept=".csv">` + styled drop area
  - Selected file name + size shown below zone
  - "Upload & Apply" button (disabled until file selected) — posts `multipart/form-data` to `/api/v1/payroll-runs/{runId}/import/{type}` (`type` maps: `lop` → `lop`, `earnings` → `earnings`, `reimbursements` → `reimbursements`)
  - Spinner overlay while uploading
- **Result view:**
  - "Applied: N rows" in success green
  - If errors > 0: red summary + table with columns `Row #, Employee Code, Reason`
  - "Close" button → calls `onSuccess()` if Applied > 0, else `onClose()`
- Follow design system: `--color-success`, `--color-error`, `var(--radius)`, existing `Modal`, `Button`, `Spinner` components

**Patterns to follow:**
- `web/src/pages/payroll/components/VariableInputsPanel.tsx` — modal/drawer pattern
- `web/src/pages/employees/ImportEmployeesPage.tsx` — existing CSV import UX for reference
- Existing `Modal`, `Button`, `Spinner` UI components

**Test scenarios:**
- Renders with correct title per `type` prop ("Import LOP Details", "Import One-Time Earnings", "Import Expense Reimbursements")
- Template download link generates correct header row for each type
- File input accepts only `.csv` files (accept attribute set)
- Spinner shown while upload mutation is in flight
- On success with no errors: result view shows "Applied: 3 rows", no error table, Close calls `onSuccess`
- On partial result: Applied count + error table rows match API response
- On full failure (Applied=0): Close calls `onClose` (no refresh needed)
- Upload button disabled until file selected

**Verification:** Drag-drop a valid LOP CSV in the running app → result shows applied count → employee table refreshes with updated LOP days.

---

- U7. **Frontend: ExportModal component**

**Goal:** Modal for exporting payroll data in CSV or XLS format, triggering a file download.

**Requirements:** R4, R8

**Dependencies:** U5 (backend endpoint must exist)

**Files:**
- Create: `web/src/pages/payroll/components/ExportModal.tsx`

**Approach:**
- Props: `runId: string`, `isOpen: boolean`, `onClose: () => void`
- Report type: fixed to "Employee Pay Run Details" (v1 — no selector shown)
- Format: radio group or select with two options: "CSV" / "XLS" (default CSV)
- Export button: calls `GET /api/v1/payroll-runs/{runId}/export?format={csv|xls}` via `api.get(url, { responseType: 'blob' })` → constructs a Blob URL → programmatically clicks an `<a>` with `download` attribute → revokes Blob URL
- Show spinner on button while download is in flight
- Cancel closes modal

**Patterns to follow:**
- `web/src/pages/payroll/components/BankAdviceModal.tsx` — blob download pattern (existing)
- Design system: `Modal`, `Button`, `Spinner`

**Test scenarios:**
- Format selector defaults to CSV
- Export button triggers download (URL contains `format=csv` or `format=xls` per selection)
- Spinner shown during download
- Cancel closes without downloading

**Verification:** Click Export (CSV) in running app → browser downloads `Payroll_*.csv` → open in Excel, verify columns and employee data.

---

- U8. **Frontend: Wire Import/Export dropdown into PayRunDetailPage**

**Goal:** Add working "Import / Export" dropdown button to the employee summary area; opens ImportModal or ExportModal per selection; import options disabled when run is not Draft.

**Requirements:** R6, R8

**Dependencies:** U6, U7

**Files:**
- Modify: `web/src/pages/payroll/PayRunDetailPage.tsx`
- Modify: `web/src/pages/payroll/components/EmployeeSummaryTable.tsx` (or add dropdown in PayRunDetailPage above the table — whichever avoids prop-drilling)

**Approach:**
- Add state: `importModalType: 'lop' | 'earnings' | 'reimbursements' | null`, `exportModalOpen: boolean`
- Dropdown button "Import / Export" with chevron (matches Zoho layout: top-right of employee summary table area)
- Dropdown menu structure:
  ```
  IMPORT
    LOP Details          (disabled if run.status !== 'Draft')
    One-Time Earnings    (disabled if run.status !== 'Draft')
    Expense Reimbursements (disabled if run.status !== 'Draft')
  EXPORT
    Payroll Data
  ```
- Selecting an import type: `setImportModalType('lop')` etc. → `<ImportModal type={importModalType} ... isOpen={importModalType !== null} />`
- Selecting Payroll Data: `setExportModalOpen(true)` → `<ExportModal isOpen={exportModalOpen} ... />`
- `onSuccess` of ImportModal: invalidate the `payroll-run-employees` query key so the employee table refreshes
- Use Lucide `Upload`, `Download`, `ChevronDown` icons — consistent with Zoho's icon style

**Patterns to follow:**
- Existing dropdown pattern in codebase (e.g., `SkipEmployeeDialog` trigger pattern)
- Lucide icons already used: `Eye`, `Download` in `EmployeeSummaryTable.tsx`

**Test scenarios:**
- Dropdown opens on button click; closes on outside click or Escape
- Import options have `disabled` class and non-interactive when `run.status !== 'Draft'`
- Import options clickable when `run.status === 'Draft'`
- Export option always enabled regardless of status
- Selecting "LOP Details" opens ImportModal with `type='lop'`
- Selecting "Payroll Data" opens ExportModal

**Verification:** In running app (Draft run): open dropdown → click "LOP Details" → ImportModal appears with correct title and template download link for LOP. Click outside → modal closes. Change to an Approved run → Import options appear disabled.

---

## System-Wide Impact

- **Interaction graph:** Bulk LOP import calls `SetLopCommandHandler.RecomputeEmployee` (internal static) — any future changes to that method affect bulk import behaviour too. Keep them in sync.
- **Error propagation:** Per-row errors return 200 with `ImportResult`. Run-level errors (not Draft, not found) return 4xx. Controller maps `InvalidOperationException` → 422, `NotFoundException` → 404.
- **State lifecycle risks:** Bulk LOP updates many `PayrunEmployee` rows + their component breakdowns in a single transaction. If `SaveChangesAsync` fails mid-import, no partial writes persist (EF Core wraps in a transaction by default). On failure the entire batch is rolled back — this is acceptable given the best-effort per-row validation is pre-commit.
- **API surface parity:** Export endpoint (`GET /{id}/export`) is new. No existing consumer. Frontend is the only caller.
- **Integration coverage:** Bulk LOP engine recompute changes TDS worksheet rows (TDS is recalculated per employee). The existing TDS worksheet upsert logic in `SetLopCommandHandler` must be replicated in the bulk handler.
- **Unchanged invariants:** Single-employee endpoints (`PUT /{id}/employees/{eid}/lop`, `POST /{id}/employees/{eid}/earnings`) remain unchanged. Bulk imports are additive — they do not replace or conflict with per-employee panel updates.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| Bulk LOP misses TDS worksheet upsert (copied from `SetLopCommandHandler` but diverges over time) | Extract `UpsertTdsWorksheet` helper method or document explicitly in U2 test scenarios that TDS worksheet rows are verified |
| Large CSV (500+ rows) causes slow import due to per-row engine recompute | Engine recompute is synchronous pure function — fast per call. 500 rows × ~5ms = ~2.5s, acceptable for v1. Add timeout note in API docs. |
| `RecomputeEmployee` is `internal` to `Payroll.Application` — bulk handler in same assembly can access it | ✓ Both handlers in `Payroll.Application` — no access issue |
| XLS download on iOS Safari may not trigger file save | Out of scope for v1 (desktop-first product). Noted. |
| Frontend Blob URL leak if modal closes before revoke | Revoke Blob URL in `useEffect` cleanup or immediately after `click()` in the download handler |

---

## Sources & References

- Zoho Payroll live audit: `https://payroll.zoho.in/#/payruns` (2026-05-23)
- BA audit doc: `docs/ba-audit/pay-runs/54-create-variable-inputs.md`
- Existing payroll run plan: `docs/plans/2026-05-17-005-feat-payroll-run-module-plan.md` (bulk imports listed as Phase 3 deferred)
- `SetLopCommand`: `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs`
- `AddOneTimeEarningCommand`: `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs`
- Bank advice download (FileContentResult pattern): `src/Payroll.Api/Controllers/PayrollRunsController.cs` lines 247–260
