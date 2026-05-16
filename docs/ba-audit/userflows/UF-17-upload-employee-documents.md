# UF-17: Upload Employee Documents

**Module:** Documents (global module) — NOT employee profile tab
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001
**App State Before:** May 2026 pay run PAID

## Steps Executed
1. From Arjun Mehta's profile (`#/people/employees/3848927000000032948`), attempted to access `/documents` sub-route
2. Received 403 Unauthorized — route `#/people/employees/{id}/documents` does not exist
3. Found "Documents" as a standalone sidebar module → `#/documents/folder`
4. Observed that employee profile tabs are: Overview | Salary Details | Investments | Payslips & Forms | Loans
5. There is NO "Documents" tab on the employee profile itself

## Key Finding: Document Upload is NOT on Employee Profile

The employee profile in Zoho Payroll does **not** have a dedicated Documents tab. Documents are managed via the global "Documents" module accessible from the left sidebar (`#/documents/folder`), not per-employee.

## Employee Profile Available Tabs

| Tab | URL Pattern |
|-----|-------------|
| Overview | `#/people/employees/{id}` |
| Salary Details | `#/people/employees/{id}/salary-details` |
| Investments | `#/people/employees/{id}/investments-and-proofs` |
| Payslips & Forms | `#/people/employees/{id}/payslips-and-forms` |
| Loans | `#/people/employees/{id}/loans` |

No "Documents" tab exists on the employee profile.

## Global Documents Module

- **URL:** `#/documents/folder`
- **Access:** Sidebar nav item "Documents" with folder icon
- Documents appear to be org-level (not per-employee), functioning as a shared document repository

## Gaps / Observations
- 🔴 **Critical Gap:** There is no per-employee document store in Zoho Payroll's UI. Standard HR document management (offer letters, appointment letters, ID proofs, Form 16, tax declarations) cannot be attached to individual employee records.
- The "Documents" module appears to be a generic file repository, not an employee-specific document manager.
- This is a significant gap vs. typical HRMS/payroll products like GreytHR or Keka, which have per-employee document vaults with document type classification (PAN card, Aadhaar, bank proof, educational certificates, etc.)
- No upload UI was tested directly in this session as the Documents module is a separate scope; it would be covered under a dedicated Documents module audit.

## Open Questions
- [ ] Does the global Documents module support folder-per-employee organization?
- [ ] Can documents be tagged to an employee record (linked via employee ID)?
- [ ] Are payslips stored here, or in a separate payslip store?
- [ ] Is there any document type classification (mandatory vs optional per statutory requirement)?
