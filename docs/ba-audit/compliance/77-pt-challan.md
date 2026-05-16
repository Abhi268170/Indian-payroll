# Compliance > PT — Professional Tax Challan

## URL / Navigation Path
- Settings > Setup & Configurations > Statutory Components > Professional Tax tab
- URL: `#/settings/statutory-details/list/pt`

## Purpose
Configure and view Professional Tax (PT) settings per work location. PT is a state-level tax deducted from employee salaries. Slabs and deduction cycles differ by state. Zoho auto-provisions PT configuration based on the work location's state.

## Architecture: Per Work Location, Not Org-Wide
PT in Zoho is scoped to **work locations**, not to the organisation globally. Each work location inherits PT settings from its state. This was confirmed by:
- The payslip showing "KL Professional Tax (Head Office)" — location-specific label
- PT settings page showing "Head Office" as the work location entity
- Pay Run > Overall Insights > Statutory Summary confirms per-location PT

When the org address was entered (Kerala), Zoho automatically created "Head Office" as a work location and provisioned Kerala PT slabs for it.

## Current State in Test Org
**PT is automatically configured** for "Head Office" (Kerala state).

| Attribute | Value |
|-----------|-------|
| Work Location | Head Office |
| State | Kerala |
| Deduction Cycle | Half Yearly |
| PT Number | Not entered (blank) |
| Status | Active (deductions live) |

## PT Page Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| PT Number | Text | Optional (UI allows blank) | State PT registration number; "Update PT Number" button opens edit form |
| State | Read-only display | N/A | Derived from work location address; cannot change independently |
| Deduction Cycle | Read-only display | N/A | State-mandated: Kerala = Half Yearly |
| PT Slabs | View / Revise buttons | N/A | Opens modal with slab table |

### Actions
| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Update PT Number | Button | — | Opens inline/modal to enter PT registration number |
| View Tax Slabs | Button | — | Opens "Tax Slabs for Head Office" modal |
| (Revise) | Button | — | Likely opens slab revision form (not tested — triggered app crash) |

## Kerala PT Slabs (FY 2026-27)

**Deduction Cycle:** Half Yearly
**Effective from:** 01/04/2026

| Half Yearly Gross Salary (INR) | Half Yearly Tax Amount (INR) |
|---|---|
| 1 — 11,999 | 0 |
| 12,000 — 17,999 | 320 |
| 18,000 — 29,999 | 450 |
| 30,000 — 44,999 | 600 |
| 45,000 — 99,999 | 750 |
| 1,00,000 — 1,24,999 | 1,000 |
| 1,25,000 — 99,99,99,999 | 1,250 |

**Max annual PT for Kerala:** INR 2,500 (1,250 × 2 half-years)

### Half-Yearly Schedule
- H1: April–September (deducted in September payroll)
- H2: October–March (deducted in March payroll)

**Note:** The payslip from Pay Run audit showed "KL Professional Tax (Head Office)" = INR 0 for April 2026 pay run. This is expected — first half-year deduction is in September (or May, depending on Zoho's calculation trigger). Need to verify whether Zoho deducts half-yearly PT in one lump at end of period or splits it monthly.

## Multi-Location PT Scenario
When an organisation has multiple work locations across states:
- Each work location gets its own PT configuration based on its state
- Different states have different slabs and deduction cycles:
  - Maharashtra: Monthly (INR 200/month for income > INR 10,000)
  - Karnataka: Monthly (INR 200/month for specified categories)
  - Kerala: Half-yearly (as above)
  - Gujarat: Monthly
  - Tamil Nadu: Semi-annual
  - Telangana: Monthly
- PT challans are separate per state/location
- No "combined PT challan" across states

## PT Challan / Returns
Zoho does not have a dedicated "PT Challan" screen in the TDS Challans area (which is TDS-only). PT challan filing process:
1. Reports > Statutory Reports > **Professional Tax Summary** — aggregate view
2. Reports > Statutory Reports > **Employee-wise Professional Tax Report** — per-employee detail
3. Reports > Statutory Reports > **Annual Professional Tax Report** — annual aggregate
4. Manual payment to state treasury/bank; challan reference entered manually
5. No direct state PT portal integration observed

## Employee-Level PT Management
Employees can be individually exempt from PT via the Statutory Information edit:
- Toggle: "Professional Tax" checkbox (enabled/disabled per employee)
- Accessible: `#/people/employees/{id}/edit-statutory-details`
- When disabled for an employee, PT deduction skipped in pay run for that employee
- Use case: employees on notice period, trainees exempt by state law, etc.

## Statutory Rules Referenced
- Kerala Panchayat Raj Act / Kerala Municipalities Act — PT provisions
- Maximum PT in India: INR 2,500/year (constitutional ceiling under Article 276)
- Kerala PT cycle: Half-yearly
- Applicable to all salaried employees working in Kerala (subject to salary threshold)

## Cross-Module Dependencies
- Organisation Address → Work Location creation → PT state auto-derivation
- Employee Statutory Information (PT toggle per employee)
- Pay Run > per-employee calculations (PT deducted per applicable cycle)
- Reports > Statutory Reports (PT Summary, Employee-wise PT, Annual PT)
- Payslip: PT line item labelled "KL Professional Tax (Head Office)"

## Key Observations for Our Build
1. **PT entity is per work-location, not per org** — our `PtConfig` or equivalent entity must have a `WorkLocationId` foreign key, not `OrgId`.
2. **PT slabs must be stored in DB config table** — confirm these match Kerala Municipalities Act rates for FY2026. The slabs shown are Zoho's pre-loaded values.
3. **Half-yearly trigger logic**: Zoho deducts in September (end of H1) and March (end of H2). Our engine needs a `DeductionMonth` concept for half-yearly PT — not monthly.
4. **Deduction Cycle is state-mandated** — store as config but derive from state, not user input.
5. **PT Number is optional at time of configuration** — Zoho allows enabling PT without entering a registration number. We should warn but not block.
6. **The "(Revise)" button crashed Zoho's app** — this is a Zoho bug, but we need a slab revision UI to handle mid-year state government rate revisions.
7. **Employee-level PT override** is critical — some employees may be exempt (trainees, part-time, contract staff per state rules).

## Screenshots
- `screenshots/pt-configuration-kerala.png` — PT configuration page for Head Office (Kerala)
- `screenshots/pt-kerala-slabs-modal.png` — Kerala PT slabs modal (full slab table)
