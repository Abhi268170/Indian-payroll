# Settings > Integrations > Zoho Apps

## URL
`#/settings/integrations/zoho`

## Navigation Location
Settings > EXTENSIONS & DEVELOPER DATA > Integrations > Zoho Apps

## Purpose
Connect Zoho Payroll with other Zoho ecosystem applications: Zoho People (HRMS), Zoho Books (accounting), Zoho Expense (expense management), and Zoho Analytics (BI/reporting).

## Page Layout
Single page. Header: "Integrations - Zoho Apps". Body: 4 integration cards arranged in a grid.

---

## Integration Cards

### 1. Zoho People
**Description:** "This integration helps you fetch employee and LOP details directly from Zoho People."
**Button:** Connect
**Status:** Not connected
**Data flow:** Zoho People → Zoho Payroll (employee data, LOP records)

### 2. Zoho Books
**Description:** "This integration helps you sync all payroll transactions with your Zoho Books account automatically."
**Button:** Connect
**Status:** Not connected
**Data flow:** Zoho Payroll → Zoho Books (payroll journal entries, salary payables)

### 3. Zoho Expense
**Description:** "This integration helps your employees to submit expenses for reimbursements easily."
**Button:** Connect
**Status:** Not connected
**Data flow:** Zoho Expense → Zoho Payroll (expense claims for reimbursement processing)

### 4. Zoho Analytics (BETA)
**Description:** "This integration helps to create custom reports and make better business decisions."
**Button:** Connect
**Status:** Not connected; marked "BETA"
**Data flow:** Zoho Payroll → Zoho Analytics (payroll data for custom dashboards and reports)

---

## Business Rules

1. **All integrations are opt-in** — none are pre-connected; all require explicit "Connect" action.
2. **Zoho People integration** — once connected, employee data is managed in Zoho People and synced to Payroll; avoids duplicate employee master management.
3. **Zoho Books integration** — enables automatic accounting: salary payable journals, PF/ESI liability entries, TDS payable entries.
4. **Zoho Expense integration** — employees submit expense claims in Zoho Expense; approved claims flow into Payroll for reimbursement.
5. **Zoho Analytics BETA** — allows custom payroll dashboards beyond the built-in reports.

## Cross-Module Impact
| Integration | Payroll Impact |
|------------|----------------|
| Zoho People | Employee master synced; LOP data imported automatically |
| Zoho Books | Payroll run creates accounting entries in Books automatically |
| Zoho Expense | Expense reimbursement amounts appear in payroll as components |
| Zoho Analytics | Payroll data accessible for custom BI dashboards |

## Observations & Notes
1. **Zoho People → Payroll LOP sync** is a key feature for orgs that track attendance in Zoho People — eliminates manual LOP entry in payroll.
2. **Zoho Books integration** would create: Dr. Salary Expense Cr. Salary Payable; Dr. PF Expense Cr. PF Payable; Dr. TDS Payable Cr. Bank (on payment).
3. **Zoho Analytics BETA** — advanced analytics beyond standard payroll reports; useful for CHROs.
4. **No non-Zoho integrations on this page** — Oracle, SAP, Workday, etc. are not listed. Integration with non-Zoho systems would require API/webhooks from Developer Data.
5. For our build: Accounting integration is critical. When finalising payroll, generate journal entries (salary expense, PF/ESI liability, TDS payable). Integration via REST API to accounting systems. For v1: export journal entries as CSV/Excel for manual import.

## Screenshots
`docs/ba-audit/settings/screenshots/30-integrations-zoho.png`
