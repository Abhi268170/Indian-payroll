# Edge Cases Audit — Index

**Org:** lerno (Zoho Payroll India — payroll.zoho.in)
**Audit Period:** May 2026 pay run (completed, Paid status)
**Employees:** EMP001 Arjun Mehta, EMP002 Priya Sharma, EMP003–EMP005 (onboarding incomplete)
**Auditor:** BA Agent
**Session:** Phase 6 (Edge Cases 105–112)

---

## Summary Table

| # | Scenario | Blocker? | Zoho Correct? | Key Finding |
|---|---------|----------|--------------|-------------|
| 105 | New vs Old Regime per employee | No | Mostly correct | Regime-choice via URL param, not explicit toggle; `can_change_tax_regime: true` year-round |
| 106 | IT Declaration full form audit | No | Correct | Zoho already uses Income Tax Code 2025 numbering (Section 123/80C etc.); comprehensive deduction coverage |
| 107 | Annual bonus TDS impact | BLOCKER (EMP003 incomplete) | Untested | Bonus earning type exists; TDS = ₹0 for all May 2026 employees — may be 87A rebate for EMP001 |
| 108 | Reprocess finalised pay run | Partial | Partial | "Delete Recorded Payment" → Approved → Unapprove path exists; no audit trail tab; no mutation warnings on Paid run |
| 109 | Multi-location PT differences | BLOCKER (one location only) | N/A — correct for Kerala | All employees in Kerala; Kerala PT half-yearly (Aug + Feb); May ₹0 PT is correct |
| 110 | PF ceiling cap | BLOCKER (EPF not enabled) | INCORRECT default | `is_employee_restricted_basic_enabled: false` by default — Zoho would compute PF on full salary, not ₹15,000 cap |
| 111 | Mid-month joiner proration | Reframed (EMP002 not May 2026 joiner) | Correct | EMP001 LOP: calendar-day proration (29/31); all components prorated uniformly |
| 112 | Salary revision arrears | Partial | Partial | Revision payrun-bound (not calendar-date); backdated arrears untested; approval workflow present |

---

## Critical Compliance Findings

### Incorrect Default — PF Wage Ceiling
- **File**: `110-pf-ceiling-cap.md`
- **Issue**: `is_employee_restricted_basic_enabled` defaults to `false`. If EPF were enabled, Zoho would compute employee PF on full basic salary (e.g., 12% × ₹80,000 = ₹9,600/month) instead of capped PF wage (12% × ₹15,000 = ₹1,800/month). Admin must manually enable the restriction.
- **Statutory requirement**: Employers must contribute at minimum on ₹15,000. Voluntary higher PF is optional — should not be the default.
- **Impact for our build**: `restrict_pf_wage_to_ceiling` must default to `true` in our `ProvidentFundConfig`.

### Missing Audit Trail on Payrun
- **File**: `108-reprocess-payrun.md`
- **Issue**: No audit log tab on payrun detail page. Cannot see who finalised, approved, deleted payment, or re-opened the run.
- **Statutory concern**: Indian payroll audit trail is legally required for statutory compliance.
- **Impact for our build**: `PayrollRunAuditLog` entity mandatory — log all state transitions.

### TDS = ₹0 for All May 2026 Employees
- **File**: `107-bonus-tds-impact.md`
- **Issue**: Total income tax deducted across May 2026 payrun = ₹0. For EMP001 (income ~₹7.1L), this may be correct if Zoho is applying Section 87A/156(2) rebate (income just above ₹7L threshold, but rebate logic may differ). Unconfirmed whether TDS is misconfigured or correctly applying rebate.
- **Action needed**: Verify with a higher-salary employee or by injecting income above the rebate threshold.

---

## Well-Implemented in Zoho

1. **Income Tax Code 2025 readiness** (106): Dual labeling (Section 123 / 80C) ensures forward compatibility. Well ahead of most payroll vendors.
2. **"Submit and Compare" UX** (106): Employee can compare tax liability under both regimes before committing. Excellent UX for employee experience.
3. **Kerala PT half-yearly deduction** (109): Correctly implements state-specific PT frequency (not monthly). Many payroll systems get this wrong.
4. **EDLI and PF Admin Charges** (110): Pre-configured at 0.50% each. Often missed by payroll systems.
5. **consider_earned_salary_for_epf: true** (110): PF computed on earned (LOP-adjusted) salary, not CTC. Correct.
6. **Calendar-day proration** (111): Industry-standard proration method correctly implemented.
7. **Salary revision approval workflow** (112): Segregation of duties enforced — revision requires explicit approval.

---

## Scenario Blockers Summary

| Blocker | Root Cause | Affects |
|---------|-----------|---------|
| EMP003–EMP005 onboarding incomplete | Missing bank details, work email, UAN | Edge Cases 107, 110 |
| EPF not enabled for org | Admin hasn't registered/enabled EPF | Edge Case 110 |
| Only one work location (Kerala) | No multi-location setup in test org | Edge Case 109 |
| EMP002 DOJ is May 2025, not May 2026 | Scenario brief assumed wrong join date | Edge Case 111 (reframed) |
| June 2026 pay run not created | May 2026 is the latest completed run | Edge Case 112 (partially untested) |

---

## Files in This Audit

| File | Description |
|------|-------------|
| `105-tax-regime-switch.md` | IT Declaration regime selection; `can_change_tax_regime: true` |
| `106-it-declaration-full.md` | Complete IT Declaration form — all sections, fields, limits, Income Tax Code 2025 mapping |
| `107-bonus-tds-impact.md` | Bonus variable pay types; TDS = ₹0 anomaly; annualisation method documented |
| `108-reprocess-payrun.md` | Paid payrun reprocess flow; state machine; audit trail gap |
| `109-pt-multi-location.md` | Kerala PT half-yearly slabs; multi-location test blocked |
| `110-pf-ceiling-cap.md` | EPF not enabled; PF wage ceiling default incorrect; full EPF config schema |
| `111-proration-verification.md` | LOP calendar-day proration (EMP001: 29/31); rounding behaviour |
| `112-salary-revision-arrears.md` | Salary revision approval workflow; payrun-bound effective date; backdated arrears untested |

---

## Next Steps for Our Build

Priority order based on findings:

1. **PF Engine** — implement with `restrict_pf_wage_to_ceiling: true` default; all five PF contribution rates configurable; UAN field mandatory
2. **TDS Engine** — annualisation method; Section 87A/156(2) rebate with marginal relief; TDS = ₹0 edge case documentation
3. **Payroll State Machine + Audit Log** — immutable finalised runs; full state transition log; reprocess via next-run arrears preferred
4. **Multi-state PT** — per-work-location slab config; half-yearly vs monthly frequency per state; Kerala/Maharashtra/Karnataka slabs
5. **Salary Revision** — approval workflow; calendar-date effective (not payrun-bound); arrear computation; Section 157/89(1) relief
6. **IT Declaration** — Income Tax Code 2025 section numbers; 80C combined cap; 80CCD(1B) separate; HRA formula
7. **Proration Engine** — calendar-day; configurable rounding policy; uniform across all components; LOP entry with reason
