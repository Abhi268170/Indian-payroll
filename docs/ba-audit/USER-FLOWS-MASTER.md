# Zoho Payroll — Master User Flow Inventory

**Purpose:** Comprehensive list of every user flow to test with mock data. Each flow must be executed, documented with screenshots/observations, and cross-references to calculations noted.

**Mock Data (actual demo org — differs from original plan):**
- Org: lerno (Kerala, Thrissur — not Karnataka as originally planned)
- Admin: abhijithss2255
- Employees found:
  - Arjun Mehta (EMP001) — Senior Engineer, CTC ₹70,000/mo, EPF disabled
  - Priya Sharma (EMP002) — HR Manager, CTC ₹22,000/mo, EPF disabled
  - Vikram Nair (EMP003) — CTC ₹1,50,000/mo, skipped (onboarding incomplete)
  - Aisha Khan (EMP004) — skipped (onboarding incomplete)
  - Rahul Verma (EMP005?) — unknown status

---

## STATUS TRACKER

| # | Flow | Status | File |
|---|------|--------|------|
| **ONBOARDING** | | | |
| 1 | Complete org onboarding checklist (remaining steps) | ✅ | UF-01-org-onboarding-checklist.md |
| 2 | Explore Getting Started wizard | ✅ | UF-02-getting-started-wizard.md |
| **SETTINGS — SALARY COMPONENTS** | | | |
| 3 | Create custom earnings component (HRA, Special Allowance) | ✅ | UF-03-create-salary-components.md |
| 4 | Create custom deduction component (Meal Card) | ✅ | UF-04-create-deduction-meal-card.md |
| 5 | Create salary structure with Fixed Allowance mechanism | ✅ | UF-05-create-salary-structure.md |
| 6 | Edit salary component — observe immutability constraints | ✅ | UF-06-edit-salary-component.md |
| 7 | Delete salary component — observe constraints | ✅ | UF-07-delete-salary-component.md |
| **SETTINGS — STATUTORY** | | | |
| 8 | Configure EPF (enable, set ceiling, employer/employee rates) | ✅ | UF-08-configure-epf.md |
| 9 | Configure ESI | ✅ | UF-09-configure-esi.md |
| 10 | Configure PT (Kerala slab — org is Kerala, not Karnataka) | ✅ | UF-10-configure-pt.md |
| 11 | Configure LWF (Kerala) | ✅ | UF-11-configure-lwf.md |
| **SETTINGS — APPROVAL WORKFLOWS** | | | |
| 12 | Set up multi-level approval for salary revision | ✅ | UF-12-approval-workflow-salary-revision.md |
| 13 | Set up approval for reimbursements | ✅ | UF-13-approval-workflow-reimbursements.md |
| **EMPLOYEE — CREATION FLOWS** | | | |
| 14 | Add employee — full form (Arjun Mehta, 4-step wizard) | ✅ | UF-14-add-employee-arjun-mehta.md |
| 15 | Assign salary structure to new employee | ✅ | UF-15-employee-salary-details.md |
| 16 | Per-employee statutory settings (EPF/ESI/PT/LWF toggles) | ✅ | UF-16-employee-statutory-settings.md |
| 17 | Upload documents — Documents module (not per-employee tab) | ✅ | UF-17-upload-employee-documents.md |
| 18 | Add employee with mid-month join date (Vikram Nair) | ✅ | UF-18-mid-month-employee-vikram-nair.md |
| 19 | Add intern (Aisha Khan) — EPF not applicable | ✅ | UF-19-intern-pf-not-applicable-aisha-khan.md |
| 20 | Bulk import employees via CSV | ✅ | UF-20-bulk-import-employees.md |
| **EMPLOYEE — UPDATES & REVISIONS** | | | |
| 21 | Salary revision — increment flow (Arjun ₹8.4L → ₹9.45L) | ✅ | UF-21-salary-revision-increment.md |
| 22 | Salary revision — with arrears calculation | ✅ | UF-22-salary-revision-with-arrears.md |
| 23 | Change employee department/designation | ✅ | UF-23-change-department-designation.md |
| 24 | Update bank details (IFSC verification, masked account) | ✅ | UF-24-update-bank-details.md |
| 25 | Add prior employer YTD details | ✅ | UF-25-prior-employer-ytd.md |
| **EMPLOYEE — IT DECLARATION** | | | |
| 26 | Open IT declaration window (admin side) | ✅ | UF-26-it-declaration-admin-settings.md |
| 27 | Submit IT declaration as employee (portal) | ✅ | UF-27-it-declaration-employee-form.md |
| 28 | Lock IT declaration | ✅ | UF-28-lock-it-declaration.md |
| 29 | Open POI window | ✅ | UF-29-open-poi-window.md |
| 30 | Upload proof of investment | ⛔ Not tested — IT Declaration locked; no POI submissions in demo | UF-30-upload-poi.md |
| 31 | Approve/reject POI | ⛔ Not tested — no POI submissions; IT Declaration locked | UF-31-approve-reject-poi.md |
| 32 | Finalise TDS computation | ✅ | UF-32-finalise-tds.md |
| 33 | Tax regime switch | ✅ | UF-33-tax-regime-switch.md |
| **EMPLOYEE — EXIT** | | | |
| 34 | Initiate employee exit / F&F | ⛔ Not tested — no exited employees in demo org | UF-34-employee-exit-fnf.md |
| 35 | Gratuity computation on exit | ⛔ Not tested — no exited employees; manual entry only in Zoho | UF-35-gratuity.md |
| **PAY RUNS — REGULAR** | | | |
| 36 | Regular pay run May 2026 — deep dive | ✅ | UF-36-regular-payrun-may-2026-deep-dive.md |
| 37 | Verify PF calculations | ✅ | UF-37-verify-pf-calculations.md |
| 38 | Verify ESI calculations | ✅ | UF-38-verify-esi-calculations.md |
| 39 | Verify PT deduction | ✅ | UF-39-verify-pt-deduction.md |
| 40 | Verify TDS May run | ✅ | UF-40-verify-tds-may-run.md |
| 41 | Vikram Nair proration / skipped employee | ✅ | UF-41-vikram-nair-proration-skipped.md |
| 42 | LOP in pay run | ✅ | UF-42-lop-in-payrun.md |
| 43 | Variable pay inputs | ✅ | UF-43-variable-pay-inputs.md |
| 44 | Reimbursement in pay run | ✅ | UF-44-reimbursement-in-payrun.md |
| 45 | LOP calculation pay run | ✅ | UF-45-lop-calculation-payrun.md |
| 46 | Pay run review and approve | ✅ | UF-46-payrun-review-approve.md |
| 47 | Mark as paid / Record Payment | ✅ | UF-47-mark-as-paid-payrun.md |
| 48 | New joiner proration | ✅ | UF-48-new-joiner-proration.md |
| 49 | Pay run skipped employees | ✅ | UF-49-payrun-skipped-employees.md |
| 50 | Download payslip (slide-in panel) | ✅ | UF-50-download-payslip.md |
| 51 | Bank advice | ✅ | UF-51-bank-advice.md |
| **PAY RUNS — SPECIAL TYPES** | | | |
| 52 | Off-cycle pay run | ✅ | UF-52-off-cycle-payrun.md |
| 53 | Bonus pay run | ✅ | UF-53-bonus-payrun.md |
| 54 | Arrears pay run | ✅ | UF-54-arrears-payrun.md |
| 55 | One-time payout | ✅ | UF-55-one-time-payout.md |
| 56 | Past pay run (historical view) | ✅ | UF-56-past-payrun.md |
| **PAY RUNS — CORRECTIONS** | | | |
| 57 | Reprocess/revise a finalised pay run | ✅ | UF-57-reprocess-payrun.md |
| 58 | Pay run reversal flow | ✅ | UF-58-payrun-reversal.md |
| **APPROVALS** | | | |
| 59 | Approvals module list (all 3 sub-modules) | ✅ | UF-59-approvals-module-list.md |
| 60 | Reimbursement claim approval | ⛔ Not tested — all reimbursement components inactive; no claims | UF-60-reimbursement-claim-approval.md |
| 61 | Reject an approval — observe behaviour | ⛔ Not tested — no approval items in any queue | UF-61-reject-approval-item.md |
| 62 | Approval history / audit trail | ✅ | UF-62-approval-history-audit-trail.md |
| **LOANS** | | | |
| 63 | Create loan (Arjun LOAN-00001, Vikram LOAN-00002) | ✅ | UF-63-create-loan.md |
| 64 | Loan EMI in pay run | ✅ | UF-64-loan-emi-in-payrun.md |
| 65 | Loan repayment / prepayment | ✅ | UF-65-loan-repayment-prepayment.md |
| 66 | Loan perquisite computation (Rule 15(5)) | ✅ | UF-66-loan-perquisite.md |
| 67 | Loan foreclosure | ✅ | UF-67-loan-foreclosure.md |
| **COMPLIANCE / TAXES & FORMS** | | | |
| 68 | TDS Liabilities page | ✅ | UF-68-tds-liabilities.md |
| 69 | EPF ECR generation | ✅ | UF-69-epf-ecr-generation.md |
| 70 | ESI return / challan | ✅ | UF-70-esi-return-challan.md |
| 71 | PT challan | ✅ | UF-71-pt-challan.md |
| 72 | LWF challan | ✅ | UF-72-lwf-challan.md |
| 73 | Form 24Q | ✅ | UF-73-form-24q.md |
| 74 | Form 16 — prerequisites and Part A | ✅ | UF-74-form-16-prerequisites.md |
| 75 | Form 16 — generate and sign | ✅ | UF-75-form-16-generate-sign.md |
| 76 | Form 16 — publish and email | ✅ | UF-76-form-16-publish-email.md |
| **REPORTS** | | | |
| 77 | Reports Centre overview (39 reports, 9 categories) | ✅ | UF-77-reports-centre-overview.md |
| 78 | Payroll summary report | ✅ | UF-78-payroll-summary-report.md |
| 79 | Statutory reports (EPF, ESI, PT, LWF) | ✅ | UF-79-statutory-reports.md |
| 80 | Loan reports (4 reports) | ✅ | UF-80-loan-reports.md |
| 81 | Employee and contractor reports | ✅ | UF-81-employee-reports.md |
| 82 | Declarations and deduction reports | ✅ | UF-82-declaration-deduction-reports.md |
| 83 | Payroll journal and activity logs | ✅ | UF-83-payroll-journal-activity-logs.md |
| **EMPLOYEE PORTAL** | | | |
| 84 | Employee portal overview and configuration | ✅ | UF-84-employee-portal-overview.md |
| 85 | Employee portal — payslip, declarations, POI | ✅ | UF-85-employee-portal-payslip-declarations.md |
| 86 | Settings — salary components | ✅ | UF-86-salary-components-settings.md |
| 87 | Settings — additional modules | ✅ | UF-87-settings-additional.md |
| 88 | Employee portal — reimbursement submission | ⛔ Not tested — all reimbursement components inactive with ₹0 max | UF-88-employee-portal-reimbursement.md |
| **TAXES & CHALLANS** | | | |
| 89 | TDS challans (ITNS 281 recording and association) | ✅ | UF-89-challans-tds.md |
| 90 | Approvals — Proof of Investments | ✅ | UF-90-approvals-poi.md |
| **SETTINGS** | | | |
| 91 | Settings — Users and Roles (RBAC) | ✅ | UF-91-settings-users-roles.md |
| 92 | Settings — Direct Deposits and Integrations | ✅ | UF-92-settings-direct-deposits-integrations.md |
| 93 | Settings — PDF and Email Templates | ✅ | UF-93-settings-pdf-email-templates.md |
| 94 | Settings — Loans module configuration | ✅ | UF-94-settings-loans-customfields.md |
| 95 | Settings — Tax Details (TAN, PAN, AO Code) | ✅ | UF-95-settings-tax-details.md |
| 96 | Settings — Approval Workflows (Pay Runs, Salary Revisions) | ✅ | UF-96-settings-approval-workflows.md |
| **DESIGN SYSTEM** | | | |
| DS-1 | Design system overview and navigation patterns | ✅ | DS-01-design-system-overview.md |
| DS-2 | Form components and validation patterns | ✅ | DS-02-form-components.md |
| DS-3 | Tables, navigation, state patterns | ✅ | DS-03-table-navigation-patterns.md |
| DS-4 | Modal and drawer patterns | ✅ | DS-04-modal-drawer-patterns.md |
| DS-5 | Toast and notification patterns | ✅ | DS-05-toast-notification-patterns.md |
| DS-6 | Color system and design tokens | ✅ | DS-06-color-system-design-tokens.md |

---

## Summary

| Status | Count |
|--------|-------|
| ✅ Completed | 90 |
| ⛔ Blocked / Not Testable in Demo | 8 |
| Total | 98 |

### Blocked Flows — Reason Summary

| Flow | Reason |
|------|--------|
| UF-30 (Upload POI) | IT Declaration locked; no POI submissions |
| UF-31 (Approve/Reject POI) | No POI submissions in demo |
| UF-34 (Employee Exit FnF) | No exited employees in demo org |
| UF-35 (Gratuity computation) | No exited employees; Zoho requires manual entry |
| UF-60 (Reimbursement claim approval) | All reimbursement components inactive (₹0 max) |
| UF-61 (Reject approval) | No approval items in any queue |
| UF-88 (Portal reimbursement submission) | All reimbursement components inactive |
| Form 16 generation | Tax Deductor (responsible person) not configured |

---

## Output Files

Each flow → individual file in `docs/ba-audit/userflows/UF-{nn}-{slug}.md`

Design system → `docs/ba-audit/userflows/DS-{nn}-{slug}.md`

Master compilation → `docs/ba-audit/USER-FLOWS-COMPILED.md`
