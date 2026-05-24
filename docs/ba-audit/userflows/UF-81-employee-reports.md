# UF-81: Employee and Contractor Reports

**Module:** Reports > Employee / Contractor Reports
**Tested:** 2026-05-16
**Mock Data Used:** Demo org employees: Arjun Mehta (pending salary revision), Priya Sharma, Vikram Nair (skipped), Rahul Verma, Sneha Patel
**App State Before:** Reports Centre accessed; individual report pages not opened

## Steps Executed
1. Identified all 7 employee/contractor reports from Reports Centre overview
2. Cross-referenced with employee data from UF-13 through UF-25
3. Documented expected content

---

## Report 1: Compensation Details

**Purpose:** Current CTC and component breakup per active employee. Point-in-time compensation snapshot.

**Expected Columns:**
| Column | Arjun Mehta |
|--------|------------|
| Employee ID | EMP001 |
| Employee Name | Arjun Mehta |
| Department | Engineering |
| Designation | Senior Engineer |
| Date of Joining | (from profile) |
| Pay Schedule | Monthly |
| Salary Structure | (assigned structure name) |
| Gross CTC | ₹70,000/month (₹8,40,000 annual) |
| Basic | ₹40,000 (57.14%) |
| HRA | ₹16,000 (22.86%) |
| Fixed Allowance | ₹14,000 (20%) |
| EPF (ER) | ₹0 (disabled) |
| Effective From | (salary structure assignment date) |

**Note:** Arjun has a PENDING salary revision to ₹9,45,000/year. This report would show current (₹70,000/month) until revision is approved.

---

## Report 2: Reimbursement Claim Summary

**Purpose:** All reimbursement claims across status (Pending/Approved/Rejected/Paid) with amounts.

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | Claimant |
| Claim Date | When submitted |
| Claim Month | Period the expense was incurred |
| Payout Month | When approved claim is paid |
| Reimbursement Type | Medical / Travel / Fuel / etc. |
| Claimed Amount | ₹ |
| Approved Amount | May differ from claimed (partial approval) |
| Status | Pending / Approved / Rejected / Paid |
| Pay Run | Which pay run included the payout |

**Current Data:** No reimbursement claims exist — report will show empty.

---

## Report 3: Employee Perquisites Summary

**Purpose:** All perquisite values added to taxable income, per employee per month. Covers vehicle, accommodation, club membership, stock options, loans.

**Expected Content (May 2026):**
All perquisites = ₹0 for current demo data:
- Arjun's loan: Perquisite Rate = 0%, likely exempt → ₹0
- No other perquisites configured

**Perquisite Types Expected in Report:**
| Type | Section | Notes |
|------|---------|-------|
| Loan Perquisite | 17(2)(viii) | Interest benefit on employer loan |
| Accommodation | 17(2)(i) | Company-provided housing |
| Vehicle | 17(2) | Company car for personal use |
| Club/Recreation | 17(2)(iv) | Club membership |
| LTA excess | 10(5) | Travel reimbursement exceeding exemption |
| Medical excess | 17(2) | Medical reimbursement > ₹15,000 |

---

## Report 4: Full and Final Settlement Report

**Purpose:** FnF computation for employees who have exited. Shows notice period recovery, leave encashment, gratuity, unpaid salary, and final net settlement.

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | Exited employee |
| Last Working Day | |
| Notice Period Required | As per employment contract |
| Notice Period Served | Actual days |
| Notice Period Recovery | If short-served: salary deducted |
| Unpaid Salary | Days worked × daily rate in last month |
| Leave Encashment | Earned leave × daily rate |
| Gratuity | If eligible (≥ 5 years service) |
| Bonus | Any pending annual bonus |
| Other Deductions | Outstanding loans, recoveries |
| Net FnF Amount | Final payable to employee |

**Current Data:** No exited employees in demo org.

---

## Report 5: Employees' Salary Revisions

**Purpose:** All salary revisions in a financial year — current revisions only (not historical).

**Expected Content (FY2026-27):**
| Employee | Old CTC | New CTC | Revised By | Effective Date | Status |
|----------|---------|---------|------------|----------------|--------|
| Arjun Mehta | ₹8,40,000/yr | ₹9,45,000/yr (₹78,750/mo) | Admin | June 2026 | Pending Approval |

---

## Report 6: Salary Revision History

**Purpose:** Complete historical log of all salary revisions per employee since joining.

**Expected Content for Arjun:**
| Date | Old Salary | New Salary | Revised By | Reason |
|------|------------|------------|------------|--------|
| (Joining date) | — | ₹70,000/mo | Admin (initial) | Initial assignment |
| (Revision date) | ₹70,000/mo | ₹78,750/mo | Admin | (reason if any) |

---

## Report 7: Salary Withhold Report

**Purpose:** Employees whose salary was withheld for a given pay period, with reason.

**Withhold scenarios:**
- Disciplinary action
- Pending compliance documentation
- Admin-initiated hold

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | |
| Month | Pay period |
| Reason | Why salary was withheld |
| Amount Withheld | Full month or partial |
| Released Date | When withheld amount was finally paid |
| Released Pay Run | Which pay run released it |

**Current Data:** No withheld salaries — report will show empty.

---

## Gaps / Observations
- Individual report pages not opened — content inferred
- 🟡 FnF and Gratuity reports cannot be tested without an exited employee
- Arjun's pending revision should appear in Salary Revisions report — verify after approval
- No contractor-specific report visible despite module name including "Contractor"

## Open Questions
- [ ] Are contractors (non-payroll employees) tracked in the Compensation Details report?
- [ ] Does the Salary Revision History include the reason/justification for each revision?
- [ ] FnF Report: Is gratuity calculated automatically or requires admin input?
- [ ] Salary Withhold: Is this a distinct feature or a manual process?
