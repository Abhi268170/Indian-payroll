---
title: "refactor: Employee Pay Breakdown — side drawer → inline accordion"
type: refactor
status: active
date: 2026-05-31
---

# refactor: Employee Pay Breakdown — side drawer → inline accordion

## Overview

The employee pay breakdown on the payroll run detail page currently lives in a 480px right-side drawer (`VariableInputsPanel`). This refactor converts it to an inline accordion row inside `EmployeeSummaryTable`, matching the pattern already established in `PayRunTaxesTab`'s `WorksheetRow`. Four bugs discovered during review are fixed alongside the UX change.

---

## Problem Frame

Three issues stem from the drawer architecture:

1. **Draft-only visibility** — the Eye icon that opens the drawer is hidden for Approved/Paid runs, so payroll managers have no way to inspect a finalised employee breakdown without downloading the PDF payslip.
2. **Drawer vs table context mismatch** — opening a panel that overlays the table breaks the flow. An inline expand keeps focus in the table where the user already is.
3. **Naming confusion** — the tooltip says "View details" but the panel is a full editing surface.

Two additional bugs exist inside the current drawer component:

4. **Misleading deductions figure** — the summary strip shows `grossPay - netPay`, which equals `deductions − reimbursements`, understating deductions when reimbursements are present.
5. **Fragile LOP edit sync** — a `prevDataRef` ref-comparison trick resets `lopDaysEdit` to `null` on every server response, silently wiping unsaved LOP edits when background queries refetch.

---

## Requirements Trace

- R1. Every employee row in the summary table must be expandable to show pay breakdown, regardless of run status.
- R2. Draft-status expanded rows expose all existing edit controls: LOP days, TDS override, add/remove one-time earnings/deductions.
- R3. Approved/Paid expanded rows show a read-only breakdown with no mutation controls.
- R4. The deductions figure in the summary strip must equal the explicit sum of statutory and one-time deductions, not `grossPay − netPay`.
- R5. Editing LOP days must survive background query refetches while the user is mid-edit.
- R6. Only one expanded row may be open at a time (single-expand).

---

## Scope Boundaries

- No changes to the API layer or `EmployeeVariableInputsDto` shape.
- `AddOneTimeEntryModal` stays structurally the same; it just moves to live inside the new component.
- `PayslipPanel` (Approved/Paid download flow) is not changed.
- `SkipEmployeeDialog`, `ImportModal`, `ExportModal`, and all other dialogs in `PayRunDetailPage` are not touched.
- The `PayRunTaxesTab` accordion is not changed.
- No TDS override read-only display is added for Approved/Paid (the TDS figure already appears in the table row; the expanded view shows it in context).

---

## Context & Research

### Relevant Code and Patterns

- `web/src/pages/payroll/tabs/PayRunTaxesTab.tsx` — `WorksheetRow` component: `<>` fragment of two `<tr>` siblings, local `useState(false)`, chevron icon toggles, `colSpan` on detail row, `var(--color-surface)` background.
- `web/src/pages/payroll/components/VariableInputsPanel.tsx` — source component: fetch, mutations, section layout, `prevDataRef` sync, `AddOneTimeEntryModal` mount.
- `web/src/pages/payroll/components/EmployeeSummaryTable.tsx` — pre-refactor: 7-col Draft / 5-col Approved/Paid table (post-refactor adds chevron col → 8/6); Eye icon opens drawer; Skip button; Download button.
- `web/src/pages/payroll/PayRunDetailPage.tsx` — sole importer of `VariableInputsPanel`; holds `variableInputs` state and `onOpenVariableInputs` callback.
- `web/src/types/api.ts` — `EmployeeVariableInputsDto`, `ComponentBreakdownDto`, `PayrunEmployeeDto`.

### Institutional Learnings

No relevant docs/solutions entries. New patterns established here should be captured post-merge.

---

## Key Technical Decisions

- **Single-expand via parent state**: `EmployeeSummaryTable` holds `expandedRowId: string | null`. Each row receives `isExpanded` and `onToggle` as props. Clicking a closed row expands it and implicitly collapses any other. Rationale: prevents concurrent LOP/TDS edits across multiple rows which would confuse which save applies to which employee.

- **Subcomponent owns its query**: `EmployeePayBreakdown` is only mounted when `isExpanded === true` (conditional render, not `display: none`). It calls `useQuery` unconditionally on mount. React Query caches the result by `['variable-inputs', runId, employeeId]` — re-opening is instant. Rationale: same key as existing invalidations, no new fetch coordination in the parent table.

- **`readOnly` prop gates all mutations**: `EmployeePayBreakdown` accepts `readOnly: boolean`. When true: LOP days renders as a read-only `<div>` not an `<input>`, no TDS override form, no "Add Earning/Deduction" buttons, no trash-can remove buttons on one-time entries. Rationale: one component, two modes, no duplication.

- **Explicit deductions calculation**: Summary strip deductions = `employeePf + employeeEsi + ptAmount + lwfEmployeeAmount + effectiveTds + oneTimeDeductions.reduce(sum, 0)`. Reimbursements are visible in the Earnings section and not folded into the deductions figure. Summary strip remains 3-column (Gross / Deductions / Net).

- **`isDirtyLopRef` replaces `prevDataRef`**: `useEffect([data])` syncs `lopDaysEdit` to `null` (server value) only when `isDirtyLopRef.current === false`. `isDirtyLopRef` (a `useRef<boolean>`) becomes `true` on first LOP `onChange`, resets to `false` in `lopMutation.onSuccess`. Using a ref — not state — avoids adding it to the `useEffect` dependency array, which would trigger the `react-hooks/exhaustive-deps` ESLint error (enforced as an error in this repo). TDS override syncs similarly only when `!editingTds`.

- **Column counts after adding chevron**: Draft = 8 cols (chevron + Employee + Gross + Deductions + Taxes + Net + LOP + Actions). Approved/Paid = 6 cols (chevron + Employee + Net + TDS + PF + Actions). Expanded detail row uses matching colSpan per mode.

---

## Open Questions

### Resolved During Planning

- **Can `/inputs` endpoint be called for Approved/Paid runs?** Yes — the endpoint is a read-model query; mutations are separate PUT/DELETE calls that won't be triggered in readOnly mode. The breakdown data is available for any run status.
- **Does `AddOneTimeEntryModal` need to move?** No move required. It mounts inside `EmployeePayBreakdown` with `fixed inset-0 z-50` positioning — renders above the page regardless of DOM nesting depth.
- **Multiple vs single expand?** Single-expand (see Key Technical Decisions).

### Deferred to Implementation

- Whether to add `aria-expanded` / `aria-controls` attributes for accessibility — can be done during U2 if time allows but not blocking.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

```
EmployeeSummaryTable
  state: expandedRowId: string | null

  <thead>  [chevron col] [Employee] [status cols…] [Actions]

  {employees.map(emp => (
    <>
      <tr onClick=toggle(emp.employeeId)>
        <td> ChevronDown | ChevronRight </td>
        <td> name / code / designation </td>
        ... status columns ...
        <td> [Skip] [Download] </td>
      </tr>

      {expandedRowId === emp.employeeId && (
        <tr bg=color-surface>
          <td />   {/* chevron column spacer */}
          <td colSpan={N}>
            <EmployeePayBreakdown
              runId={runId}
              employeeId={emp.employeeId}
              employeeName={emp.employeeName}
              readOnly={!isDraft}
            />
          </td>
        </tr>
      )}
    </>
  ))}
```

`EmployeePayBreakdown` (formerly `VariableInputsPanel`, drawer chrome stripped):

```
props: runId, employeeId, employeeName, readOnly

useQuery(['variable-inputs', runId, employeeId]) → EmployeeVariableInputsDto

summary strip: Gross | explicitDeductions | Net

Attendance section:
  baseDays (read-only)
  lopDays:  readOnly → <div>  |  !readOnly → <input onBlur=save>
  payableDays (computed)

Earnings section:
  salary components (read-only rows)
  one-time earnings (readOnly → no trash | !readOnly → trash icon)
  reimbursements (readOnly → no trash | !readOnly → trash icon)
  total gross row
  !readOnly → "Add Earning" button

Deductions section:
  PF / ESI / PT / LWF / TDS rows
  one-time deductions (readOnly → no trash | !readOnly → trash icon)
  total deductions row
  !readOnly → "Override TDS" + "Add Deduction" buttons

Benefits section (employer cost, always read-only)

{addModalCategory && <AddOneTimeEntryModal ... />}
```

---

## Implementation Units

- U1. **Create `EmployeePayBreakdown.tsx` — content component**

**Goal:** New version of the pay breakdown component: drawer chrome removed, `readOnly` prop added, deductions fixed (R3, R4), LOP sync fixed (R5).

**Requirements:** R2, R3, R4, R5

**Dependencies:** None

**Files:**
- Create: `web/src/pages/payroll/components/EmployeePayBreakdown.tsx`
- Test: `web/src/__tests__/payroll/EmployeePayBreakdown.test.tsx`

**Approach:**
- Copy `VariableInputsPanel.tsx` as starting point; remove the `fixed inset-0 z-50` overlay wrapper, the `relative w-[480px] h-full shadow-2xl` drawer container, and the header close button. Root element becomes a plain `<div>` block.
- Add `readOnly: boolean` prop. Gate all mutation-triggering UI: replace LOP `<input>` with a `<div>` read-only display when `readOnly`, hide Add Earning/Deduction buttons, hide trash-can remove buttons on one-time entries and reimbursements, hide TDS override and Override TDS button.
- **Bug 2 fix**: In the summary strip, compute `explicitDeductions` = `data.employeePf + data.employeeEsi + data.ptAmount + data.lwfEmployeeAmount + effectiveTds + oneTimeDeductions.reduce((s,c) => s + c.fullAmount, 0)`. Use `explicitDeductions` in the "Deductions" cell, not `grossPay - netPay`.
- **Bug 4 fix**: Delete the `prevDataRef` + `dataKey` comparison block. Replace with `useEffect([data])` that runs when `data` changes: if `isDirtyLopRef.current === false`, call `setLopDaysEdit(null)`; if `!editingTds`, sync `tdsOverride` from `data`. `isDirtyLopRef` is a `useRef<boolean>(false)` — set `isDirtyLopRef.current = true` on LOP input `onChange`, reset to `false` in `lopMutation.onSuccess`. A ref (not state) is mandatory here: adding a boolean state to the `[data]` dep array would cause the effect to re-run on flag change (not just data change), and ESLint `react-hooks/exhaustive-deps` (error-level in this repo) requires all variables used inside the effect to be in the array.
- No external wrapper — the component outputs an inline block suitable for embedding in a `<td>`. The `AddOneTimeEntryModal` stays inside as-is (fixed positioning works regardless of DOM nesting).
- All existing query keys, mutation endpoints, and invalidation patterns are preserved verbatim.

**Patterns to follow:**
- `web/src/pages/payroll/components/VariableInputsPanel.tsx` — full existing logic (source to copy)
- `web/src/pages/payroll/tabs/PayRunTaxesTab.tsx` — `WorksheetRow` (accordion structure) and `WorksheetLine` (read-only key-value field layout)

**Test scenarios:**
- Happy path: component fetches `['variable-inputs', runId, employeeId]`; renders Gross / Deductions / Net summary strip.
- Deductions accuracy: when `data` has `employeePf=500, employeeEsi=200, ptAmount=200, lwfEmployeeAmount=10, tdsAmount=1000` and a one-time deduction of `300`, summary "Deductions" shows `2210` not `grossPay - netPay`.
- readOnly=true: LOP input is not an `<input>` element; no Add Earning button; no Add Deduction button; no trash icons; no Override TDS button; no TDS form.
- readOnly=false: LOP `<input>` is present; Add Earning, Add Deduction, trash icons, Override TDS button are all rendered.
- LOP dirty flag: simulate `data` refetch while `isDirtyLopRef.current === true`; `lopDaysEdit` must not be reset to `null`.
- LOP clean sync: with `isDirtyLopRef.current === false`, a data refetch with updated `lopDays` propagates to the displayed payable days.
- lopMutation success: `isDirtyLopRef.current` resets to `false`; LOP input shows server value after round-trip.
- TDS override: entering amount + reason and clicking Save calls `PUT .../tds-override`; `editingTds` closes on success.
- AddOneTimeEntryModal: clicking "Add Earning" mounts the modal; `onClose` unmounts it.

**Verification:**
- Component renders without errors in both `readOnly=true` and `readOnly=false` modes.
- Deductions figure in summary strip equals explicit sum (not `grossPay - netPay`).
- LOP dirty flag prevents sync; flag resets after successful save.
- All existing mutations (LOP, TDS override, remove earning) reach their API endpoints unchanged.

---

- U2. **Refactor `EmployeeSummaryTable.tsx` — accordion rows**

**Goal:** Replace Eye-icon + drawer-open pattern with inline chevron accordion. Add read-only expand for Approved/Paid. Single-expand behaviour enforced at table level. (R1, R2, R3, R6)

**Requirements:** R1, R2, R3, R6

**Dependencies:** U1

**Files:**
- Modify: `web/src/pages/payroll/components/EmployeeSummaryTable.tsx`
- Test: `web/src/__tests__/payroll/EmployeeSummaryTable.test.tsx`

**Approach:**
- Add `expandedRowId: string | null` and `setExpandedRowId` state. Toggle: `setExpandedRowId(id === expandedRowId ? null : id)`.
- Remove `onOpenVariableInputs` from the component's props interface.
- Add a chevron column as the **first** column in `<thead>` and in every data row. Use `ChevronDown` when `expandedRowId === emp.employeeId`, `ChevronRight` otherwise. **Chevron `<td>` only is clickable** (w-6, `cursor-pointer`, `onClick` on the `<td>`) — not the whole row. Rationale: matches `WorksheetRow` in `PayRunTaxesTab` where only the row is the target, and avoids accidental expand on Skip/Download button clicks in the Actions column.
- After each employee `<tr>`, render the expand row conditionally. Wrap both `<tr>` elements in `<React.Fragment key={emp.employeeId}>` so they are siblings in `<tbody>` — identical to `WorksheetRow`. The detail `<tr>` gets `bg-[var(--color-surface)]`; first `<td />` is the chevron-column spacer; second `<td colSpan={N}>` contains `<EmployeePayBreakdown ... />`.
- **`divide-y` removal**: Current `<tbody>` uses `divide-y` which inserts a border between every direct child element — this would draw a divider between the summary row and its own expand row. Drop `divide-y divide-[var(--color-border)]` from `<tbody>` and add `border-b border-[var(--color-border)]` directly to each summary `<tr>` className. Match `WorksheetRow`'s per-row `border-b` pattern exactly.
- **Column counts**: Draft `colSpan={7}` (8 cols total minus chevron col = 7 data cols). Approved/Paid `colSpan={5}` (6 cols total minus chevron col = 5 data cols). Empty-state row keeps `colSpan={10}`.
- **Stale expand ID reset**: Add a `useEffect` that depends on `[employees, filter]` (the `filter` state already exists in the component; `employees` is the prop). Inside the effect, `visible` is the derived list of employees shown after filtering — compute it inline or use the existing `visible` constant already in scope. If `expandedRowId` is not among the IDs in `visible`, call `setExpandedRowId(null)`. This handles both filter-tab changes and pagination page changes (when the parent changes the `employees` prop on page turn).
- Remove Eye icon buttons from the Actions column. Draft Active: keep Skip button only. Draft Skipped: Actions cell is empty (chevron is the only affordance). Approved/Paid: keep Download payslip button only.
- Import `EmployeePayBreakdown` and `React` (for `Fragment`).
- `VariableInputsPanel` import is never added here — `EmployeePayBreakdown` is the new import.
- **Approved and Paid are treated identically**: both receive `readOnly={true}`. No distinction needed between the two statuses for the breakdown view.

**Patterns to follow:**
- `web/src/pages/payroll/tabs/PayRunTaxesTab.tsx` — `WorksheetRow` structure: `<>` fragment, two `<tr>` siblings, chevron column, `colSpan`, `var(--color-surface)` on detail row.

**Test scenarios:**
- Happy path: clicking chevron on a Draft Active row expands it and mounts `EmployeePayBreakdown` with `readOnly=false`.
- Single-expand: expanding row A then row B collapses A and expands B.
- Collapse: clicking chevron on already-expanded row collapses it.
- Approved/Paid expand: clicking chevron on an Approved row mounts `EmployeePayBreakdown` with `readOnly=true`.
- Draft Skipped row: renders chevron; no Eye icon; no Skip button in Actions.
- Draft Active row: renders chevron + Skip button; no Eye icon.
- Approved/Paid row: renders chevron + Download button; no Eye icon.
- Eye icon (`<Eye>`) must not appear anywhere in the rendered output.
- Filter tabs (All / Active / Skipped): when the expanded employee is filtered out, `expandedRowId` must reset to `null` — it will NOT reset automatically (state persists across renders). The `useEffect([employees, filter])` described in Approach handles this.
- Pagination: when parent changes page (`employees` prop changes to a different set), `expandedRowId` from the previous page must reset — same `useEffect` handles this case too.

**Verification:**
- No `VariableInputsPanel` import in `EmployeeSummaryTable.tsx`.
- No Eye icon button in the rendered table.
- Expand/collapse works for Draft and Approved/Paid rows.
- `readOnly` prop matches run status.

---

- U3. **Update `PayRunDetailPage.tsx` — remove drawer state**

**Goal:** Remove `variableInputs` state, `VariableInputsPanel` import, and `onOpenVariableInputs` prop now that the accordion handles everything. (Bug 3 — naming mismatch is fully resolved once the Eye button and drawer are gone.)

**Requirements:** R1 (cleanup enabling correct wiring)

**Dependencies:** U1, U2

**Files:**
- Modify: `web/src/pages/payroll/PayRunDetailPage.tsx`

**Approach:**
- Delete `import VariableInputsPanel from './components/VariableInputsPanel'`.
- Delete the `VariableInputsState` interface and `variableInputs` state declaration.
- Remove `onOpenVariableInputs` prop from the `<EmployeeSummaryTable>` JSX call.
- Remove the `{variableInputs && <VariableInputsPanel .../>}` conditional render block.
- `VariableInputsPanel.tsx` source file: rename to `EmployeePayBreakdown.tsx` (this is the create in U1 — the old file can be deleted once U3 confirms no remaining imports).

**Test scenarios:**
- Test expectation: none — this is a pure deletion/cleanup unit. TypeScript compiler (`npm run typecheck`) confirms no remaining references to `VariableInputsPanel` or `variableInputs`.

**Verification:**
- `npm run typecheck` passes with zero errors.
- `VariableInputsPanel` is not referenced anywhere in the codebase.
- `PayRunDetailPage` renders without `variableInputs` state.

---

## System-Wide Impact

- **Interaction graph:** `AddOneTimeEntryModal` stays embedded in `EmployeePayBreakdown`. Its mutations invalidate `['variable-inputs', runId, employeeId]`, `['run-employees', runId]`, `['payroll-run', runId]` — unchanged.
- **Error propagation:** `EmployeePayBreakdown` shows inline loading/error states (spinner on load, error text on mutation failure) — identical to current panel behaviour.
- **State lifecycle risks:** Because `EmployeePayBreakdown` is conditionally mounted (not always in DOM), its local state (`lopDaysEdit`, `editingTds`, `tdsOverride`) resets to initial values each time the row is collapsed and re-expanded. This is acceptable behaviour; the server is the source of truth.
- **API surface parity:** No API changes. All existing endpoints, query keys, and mutation payloads are preserved.
- **Integration coverage:** Expanding a row, editing LOP, saving, and verifying the `run-employees` table row updates its LOP column — this cross-layer scenario should be tested in E2E or manual smoke test.
- **Unchanged invariants:** `PayslipPanel`, `SkipEmployeeDialog`, `ApprovePayrollDialog`, `RecordPaymentDialog`, `DeletePaymentDialog`, `BankAdviceModal`, `ImportModal`, `ExportModal` are all untouched.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| `AddOneTimeEntryModal` uses `fixed inset-0 z-50` inside a `<td>` — a CSS transform ancestor could break `fixed` stacking context | Confirmed safe: no `transform`/`will-change`/`filter` on any `<td>`, `<tr>`, `<tbody>`, `<table>` ancestor in this codebase. `overflow-hidden` alone does not create a containing block for fixed-position descendants. |
| Filter tab change or pagination page change leaves stale `expandedRowId` pointing to an employee not in current `visible` list | `useEffect([employees, filter])` checks membership and resets `expandedRowId` to `null` if the expanded employee is absent. Covers both filter changes and page turns. |
| `divide-y` on `<tbody>` draws a divider between a summary row and its own expand row | Drop `divide-y` from `<tbody>`; add `border-b border-[var(--color-border)]` to each summary `<tr>`. See U2 Approach. |
| `colSpan` mismatch breaks layout if column counts change in future | Define named constants `DRAFT_TOTAL_COLS = 8` / `APPROVED_TOTAL_COLS = 6` and derive colSpan from them. |

---

## Sources & References

- Related code: `web/src/pages/payroll/tabs/PayRunTaxesTab.tsx` (accordion reference)
- Related code: `web/src/pages/payroll/components/VariableInputsPanel.tsx` (source component)
- Related code: `web/src/pages/payroll/components/EmployeeSummaryTable.tsx` (table being modified)
