# Employees Module — Audit Index

**Audit Date:** 2026-05-15
**Reference App:** Zoho Payroll India (payroll.zoho.in, org: lerno)
**Sessions:** 2 (prior session + this session)
**Status:** COMPLETE — all 18 items + 1 addendum documented

---

## Index of Audit Files

| Item | File | Page / Topic | Key Discoveries |
|---|---|---|---|
| 34 | `34-employee-list-empty.md` | Employee List — Empty State | "Add" button, "Import Employees" strip, Zoho People sync notice, empty state illustration |
| 35 | `35-add-employee-personal.md` | Add Employee Wizard Step 1: Basic Details | 4-step wizard structure; Employee ID auto-suggest; DOJ calendar-click requirement; inline Designation + Department creation modals; Portal Access checkbox |
| 36 | `36-add-employee-employment.md` | Add Employee Wizard Step 3: Personal Details | DOB auto-age calc; Father's Name required for Form 16; PAN optional; Differently Abled 6-option; no Aadhaar field (gap); State dropdown Daman-Diu duplication |
| 37 | `37-add-employee-salary.md` | Add Employee Wizard Step 2: Salary Details | Two-phase save (Statutory → Salary Structure → Benefits); CTC-anchored percentage structure; Fixed Allowance as residual; "Add Earning" inline component selector; Basic auto-50%; Pay Schedule prerequisite |
| 38 | `38-add-employee-bank.md` | Add Employee Wizard Step 4: Payment Information | 4 payment modes; IFSC live validation; account number as password field (masked XXXX+4); 2 payment-method-specific form branches; "Skip" allowed |
| 39 | `39-add-employee-documents.md` | Documents Module (standalone) | NOT a wizard step; org-level document module; Org Folder vs Employee Folder; 1GB/100-emp quota; no per-employee Documents tab |
| 40 | `40-add-employee-tax.md` | Employee Investments Tab (IT Declaration + POI) | Locked by default; `tax_regime=with_exemptions` = OLD regime (red flag); POI separate workflow; admin submit on behalf; FY-scoped declaration |
| 41 | `41-employee-profile-view.md` | Employee Profile — All 5 Tabs | 5 tabs: Overview/Salary Details/Investments/Payslips&Forms/Loans; header Add + kebab dropdowns; Perquisites entity; account masking; Form 16 per-FY; salary certificate print/send |
| 41a | `41a-salary-structure-actions.md` | Salary Structure Actions (Addendum) | Add Earning (inline CTC component); Add Scheduled Earning (extra recurring income); Benefits = pre-tax deductions (`deduction_type=pre-tax`); Deduction = post-tax; Donation = 80G |
| 42 | `42-emp002-midmonth-joiner.md` | EMP002 Priya Sharma (Mid-Month Joiner) | Proration at pay-run time (not creation); ESI spec error (₹22k > ₹21k ceiling); two-phase salary save confirmed; IFSC mock fallback pattern |
| 43 | `43-emp003-midyear-ytd.md` | EMP003 Vikram Nair (Mid-Year + Prior Employer YTD) | Prior employer YTD → IT Declaration → Previous Employment Details; TDS formula with prior YTD; IT Declaration page load failure (incomplete profile); Bangalore location not configured → used Mumbai |
| 44 | `44-emp004-contractor.md` | EMP004 Aisha Khan (Contractor Gap) | NO Employment Type field in Zoho; all employees = permanent salaried; contractor TDS (194C/194J) out of scope for Zoho Payroll V1; Department "Design" created inline |
| 45 | `45-emp005-add.md` | EMP005 Rahul Desai (Standard) | DOJ in prior FY (01/06/2024) works without issue; none of 5 test employees are ESI-eligible; QA Engineer designation created inline |
| 46 | `46-employee-list-populated.md` | Employee List — All 5 Employees | 21 available columns (UAN, PAN, PF A/C, ESI No, Prior Payroll Status, Onboarding Status); 8 pre-built views; Custom View builder; Import/Export dropdown; 3 incomplete profiles warning banner |
| 47 | `47-salary-revision.md` | Salary Revision | Two mechanisms: direct edit (immediate) vs dated revision (route exists, renders blank unless triggered from pay run); Fixed Allowance invariant; dirty-state protection dialog; bulk import via CSV |
| 48 | `48-prior-employer-ytd.md` | Prior Employer YTD Entry | `PriorEmployerYtd` entity design; TDS formula integration; deferred from live investigation (page load failure); bulk import via "Previous Employment Details" import type |
| 49 | `49-employee-exit.md` | Employee Exit Process | Route `/terminate`; fields: Last Working Day, Reason (4 options), F&F timing (regular vs custom date), Personal Email, Notes; employee summary card; portal-disabled warning |
| 50 | `50-full-and-final.md` | Full & Final Settlement | F&F components: prorated salary, leave encashment, gratuity (5-yr rule, ₹20L cap), notice pay; `PayrollRunType.FullAndFinal`; deferred page investigation (exit not submitted to avoid modifying test employee) |
| 51 | `51-bulk-import.md` | Bulk Import | 19 import types across 5 groups; Employee Details / Salary Details / Complete Employee / Exit Details / Investments; "Previous Employment Details" importable; Salary Revision importable via CSV |

---

## Key Cross-Cutting Discoveries

### Statutory Compliance Findings
| Finding | Severity | File |
|---|---|---|
| IT Declaration URL uses `tax_regime=with_exemptions` = OLD regime | Critical | 40 |
| ESI ceiling ₹21,000 — none of 5 test employees are ESI-eligible | High | 42 |
| No Aadhaar field in Zoho Payroll employee entity | Medium | 36 |
| Zoho has no Employment Type (Contractor vs Permanent) field | High | 44 |
| Standard Deduction claimed once per FY across both employers | High | 48 |

### Data Model Discoveries
| Entity | Discovered In |
|---|---|
| Employee (with 21+ attributes including UAN, PF A/C, ESI No) | 46 |
| SalaryComponent (org-level master; once assigned, type locked) | 37, 41a |
| EmployeeSalaryComponent (junction; per-employee % override) | 37, 41a |
| ScheduledEarning (org-level master; recurring extra income) | 41a |
| Deduction + Benefit (pre-tax vs post-tax; same entity) | 41a |
| PriorEmployerYtd (keyed by employee + FY) | 43, 48 |
| EmployeeExit (LWD, reason, F&F mode) | 49 |
| FullAndFinalSettlement (gratuity, leave encashment, prorated salary) | 50 |
| ImportJob + ImportRow (generic import pipeline) | 51 |

### UX Patterns Established
| Pattern | Description | File |
|---|---|---|
| Calendar-click required for DOJ | Ember datepicker requires explicit day cell click; `pressSequentially` alone insufficient | 35 |
| Two-phase salary wizard save | Statutory → unlock Salary Structure → unlock Benefits | 37 |
| Fixed Allowance = residual | Always = Monthly CTC − sum(all other components) | 37, 47 |
| IFSC lookup with graceful fallback | Mock IFSCs fail; Bank Name must be manually entered | 38, 42 |
| Account number = XXXX + last 4 | Masked in all views; "Show A/C No" reveals | 38, 41 |
| Dirty-state navigation protection | Unsaved changes → confirmation dialog on any navigation | 47 |
| Inline entity creation | Department, Designation created inline during wizard | 35 |
| Pre-built + custom employee views | 8 pre-built views + custom view builder | 46 |

### Build Priorities from This Audit
1. **Prior Employer YTD import** — V1 critical for mid-year joiners
2. **IFSC lookup with fallback** — 4 of 5 test employees needed fallback
3. **Proration engine** — mid-month joiners, exiters, and salary revisions
4. **Employee list with 21-column support** — UAN, PAN, PF A/C, ESI No as optional columns
5. **Employment Type field** — add to entity even if V1 behavior is same for all types
6. **`ScheduledEarning` entity** — separate from salary components; recurring extra income
7. **Benefits = pre-tax deductions** — same entity, `is_pre_tax` flag differentiates
8. **4-option Reason for Exit** — drives gratuity eligibility and notice pay logic

---

## Module Completion Status

- [x] Employee List (Empty + Populated)
- [x] Add Employee Wizard (Steps 1–4)
- [x] Employee Profile (All 5 Tabs)
- [x] Salary Structure and Component Actions
- [x] Investments / IT Declaration (partial — IT Declaration form deferred)
- [x] 5 Mock Employees Created (EMP001–EMP005)
- [x] Employee-Specific Scenarios: Mid-Month Join, Mid-Year Join, Contractor Gap, Prior FY DOJ
- [x] Salary Revision (direct edit investigated; dated revision route found but blank)
- [x] Exit Process Form
- [x] F&F Settlement (design-level; page not submitted)
- [x] Bulk Import (modal + all 19 import types documented)
- [ ] IT Declaration Form (full form) — DEFERRED (page load issue; requires complete employee profile)
- [ ] Prior Employer YTD form UI — DEFERRED (same dependency)
- [ ] Salary Revision dated UI — DEFERRED (trigger not found; may require active pay run)
- [ ] F&F Settlement page — DEFERRED (exit not submitted to preserve test employee)

---

## Next Audit Module
**Pay Runs** — the first payroll processing cycle for the 5 test employees.

Recommended order:
1. Create a Pay Run for May 2025 (covering EMP001 full month, EMP002 mid-month)
2. Document Pay Run initiation, variable inputs, review, approval, finalization
3. Revisit Salary Revision (triggered from pay run context)
4. Investigate F&F settlement by exiting EMP005
