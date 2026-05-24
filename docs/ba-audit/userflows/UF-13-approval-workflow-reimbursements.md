# UF-13: Approval Workflow — Reimbursements

**Module:** Settings > Setup & Configurations > Claims and Declarations
**Tested:** 2026-05-16
**Mock Data Used:** lerno org, admin user abhijithss2255
**App State Before:** No active reimbursement components configured

## Steps Executed
1. Navigate to `#/settings/preferences/fbp` (Claims and Declarations landing)
2. Navigate to `#/settings/preferences/reimbursement` (Reimbursement Claims tab)
3. Observed empty state — no active reimbursement component

## Page Identity
- **URL:** `#/settings/preferences/reimbursement`
- **Page title:** "Reimbursement Claims Preference | Preferences | Settings | Zoho Payroll"
- **Module path:** Settings > Setup & Configurations > Claims and Declarations > Reimbursement Claims
- **Access:** Admin only

## Claims and Declarations — Tab Structure

The Claims and Declarations section has 4 tabs:
| Tab | URL |
|-----|-----|
| Flexible Benefit Plan | `#/settings/preferences/fbp` |
| Reimbursement Claims | `#/settings/preferences/reimbursement` |
| Income Tax Declaration | `#/settings/preferences/it-declaration` |
| Proof Of Investments | `#/settings/preferences/proof-of-investment` |

## Reimbursement Claims — Current State

**Empty state displayed:**
- Icon + heading: "No Active Reimbursement"
- Message: "Employees can get tax exemptions on producing necessary bills. You can enable a reimbursement component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

This means: **there is no standalone "approval workflow" configuration for reimbursements on this page**. Reimbursement approval configuration is a prerequisite — it flows from salary component setup.

## Reimbursement Approval Flow — Inferred Architecture

Based on the empty-state message and module structure:
1. Admin must first create a Reimbursement component under Settings > Salary Components > Reimbursements
2. That component is associated to an employee's salary
3. Only then does the employee see a claim submission option (from Employee Portal)
4. Approval of reimbursement claims would appear under the Approvals module once claims are submitted

**No direct "approval workflow" radio group like Salary Revisions** was found on this page — approval for reimbursements appears to be implicit (any approver with permission approves), or configured at component level.

## Flexible Benefit Plan (FBP) — Current State

**Empty state displayed:**
- Icon + heading: "No Active FBP component"
- Message: "Your organisation does not have an active FBP component associated to an employee. Mark a reimbursement as FBP component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

FBP is a sub-type of reimbursement where the employee can choose how to allocate a fixed benefit budget across eligible components.

## Pay Run Approval Settings (documented here for completeness)

**URL:** `#/settings/payrun/custom-approval/list`

Identical three-option radio group to Salary Revisions:
- Simple Approval: "any approver with Pay Run approval permission can approve"
- Multi-Level Approval: level-by-level user assignment
- Custom Approval: criteria-based builder

Pay Runs settings page also has additional tabs not present on Salary Revisions:
- **Approvals** (current)
- **Custom Button** → `#/settings/payrun/custom-button/list`
- **Record Locking** → `#/settings/payrun/record-locking`
- **Related List** → `#/settings/payrun/related-list`

## Gaps / Observations
- Reimbursement Claims page shows no approval workflow config — this is a prerequisite gap (no salary component set up)
- There is no standalone approval workflow configuration for reimbursements comparable to salary revisions
- FBP is also unconfigured — lerno org has not set up any flexible benefit components
- The path to enable reimbursements is: Settings > Salary Components > Reimbursements > create component > associate to employee salary > then reimbursement claims become claimable
- No reimbursement claim approval SLA or escalation is visible anywhere in the Settings hierarchy

## Open Questions
- [ ] Is reimbursement claim approval a Simple Approval only (no multi-level option)?
- [ ] Can an approver partially approve a reimbursement claim (approve part of the amount)?
- [ ] Is there a maximum claim amount limit configuration?
