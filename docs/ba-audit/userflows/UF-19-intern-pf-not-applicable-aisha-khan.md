# UF-19: PF-Not-Applicable Employee — Aisha Khan (EMP004)

**Module:** Employees
**Tested:** 2026-05-16
**Mock Data Used:** Aisha Khan EMP004 (ID: 3848927000000034040)
**App State Before:** May 2026 pay run PAID; Aisha SKIPPED with "Onboarding incomplete"

## Steps Executed
1. Navigate to Aisha Khan's Overview: `#/people/employees/3848927000000034040`
2. Reviewed all visible profile sections

## Employee Profile Summary — Aisha Khan

| Field | Value |
|-------|-------|
| Employee ID | EMP004 |
| Name | Aisha Khan |
| Status | Active |
| Designation | UX Consultant |
| Department | Design |
| Work Location | Head Office |
| Date of Joining | 01/05/2025 |
| Gender | Female |
| Email | aisha.khan@lerno.com |
| Mobile | "-" (not filled) |
| Portal Access | Disabled |

## What is MISSING (Causing "Onboarding Incomplete")

Same pattern as Vikram Nair — Personal Information section all blank:

| Missing Field | Value |
|---------------|-------|
| Date of Birth | "-" |
| Father's Name | "-" |
| PAN | "-" |
| Personal Email Address | "-" |
| Residential Address | "-" |

Also: Mobile Number is "-" in Basic Information (unlike Vikram who had a mobile number).

## PF Exemption Status

| Statutory | Status |
|-----------|--------|
| EPF | **Disabled** |
| ESI | **Disabled** |
| Professional Tax | Enabled |
| Labour Welfare Fund | Disabled |

**EPF is disabled for Aisha** — same as all other employees in lerno org. This is a deliberate configuration choice at the employee level (toggle-based). The mechanism for PF exemption is:
- Navigate to Employee Overview → Statutory Information section
- EPF shows "Disabled" with an "(Enable)" inline button
- To exempt an employee from PF: simply leave EPF in "Disabled" state
- There is no separate "PF Exempt" category or reason field — it is a binary Enabled/Disabled toggle

## How PF Exemption Works (at Employee Level)

From the Overview page, Statutory Information section:
- **EPF:** Toggle button — "(Enable)" or "(Disable)"
- **ESI:** Toggle button — "(Enable)" or "(Disable)"
- **Professional Tax:** Toggle button — "(Enable)" or "(Disable)"
- **Labour Welfare Fund:** Toggle button — "(Enable)" or "(Disable)"

Each statutory component can be independently toggled per employee. No justification/reason field is required for disabling. No approval workflow for this change.

This means:
- An employee earning above ₹15,000/month basic can have EPF manually disabled (opt-out for wages above ₹15,000 is permitted under EPF Act for employees who joined on wages > ₹15,000)
- An employee earning below ₹21,000 gross can have ESI manually disabled
- No system validation prevents this — it is trust-based admin configuration

## Why Aisha is SKIPPED

Same reason as Vikram — missing PAN and personal information. DOJ is 01/05/2025 — she would have been a Day 1 employee in May 2025, but has been skipped for 12+ months due to incomplete profile.

## PF Exemption — Key Design Observation

Zoho Payroll treats PF applicability as a simple per-employee toggle, with no:
- Reason for exemption (international worker, high wage opt-out, etc.)
- Date from which exemption applies
- Approval requirement
- Audit trail visible on the profile page

This is a simplified approach compared to statutory requirements which have specific exemption categories under the EPF & MP Act 1952.

## Comparison — EMP001 through EMP005 Statutory Settings

| Employee | EPF | ESI | PT | LWF |
|----------|-----|-----|-----|-----|
| Arjun Mehta (EMP001) | Disabled | Disabled | Enabled | Disabled |
| Priya Sharma (EMP002) | Not observed yet | Not observed | Enabled | Disabled |
| Vikram Nair (EMP003) | Disabled | Disabled | Enabled | Disabled |
| Aisha Khan (EMP004) | Disabled | Disabled | Enabled | Disabled |
| Rahul Desai (EMP005) | Not observed | Not observed | Not observed | Not observed |

**Pattern: All checked employees have EPF=Disabled, ESI=Disabled, PT=Enabled, LWF=Disabled**

This is consistent with the May 2026 pay run showing ₹0 PF and ₹0 ESI across all employees.

## Business Rules
- PF exemption is a simple Enable/Disable toggle — no exemption category required
- An employee can be PF-exempt regardless of salary level — the system does not enforce EPF Act salary threshold rules
- ESI exemption is also a simple toggle — no ₹21,000 ceiling enforcement at employee level
- "Onboarding incomplete" blocks payroll regardless of whether EPF/ESI is configured

## Gaps / Observations
- 🔴 No EPF exemption reason field — cannot report WHY an employee is PF exempt (statutory audit risk)
- 🔴 No ESI wage ceiling validation — employee with gross < ₹21,000 can be manually ESI-disabled without warning
- No EPF UAN field visible on Aisha's profile (consistent with EPF being disabled — UAN only applicable when EPF enabled)
- No warning or alert after 12+ months of "incomplete" status for Aisha
