# UF-10: Configure PT (Professional Tax)

**Module:** Settings → Setup & Configurations → Statutory Components → Professional Tax
**Tested:** 2026-05-16
**Mock Data Used:** Head Office state: Kerala (not Karnataka — org is registered in Kerala)

## Steps Executed
1. Navigated to `#/settings/statutory-details/list/pt`
2. Observed PT page — no "Enable" step needed; PT is pre-configured based on work location
3. Saw existing PT setup for "Head Office" — State: Kerala, Deduction Cycle: Half Yearly
4. Clicked "View Tax Slabs" → modal opened showing Kerala slab table
5. Noted "Update PT Number" button and "(Revise)" button for slab customization
6. Took screenshot and closed modal

## Key Observation: PT Configured at Work Location Level
PT is NOT org-level — it is automatically determined by each employee's **Work Location** state. The page groups PT entries by work location name. Since all employees are assigned to "Head Office" (Kerala), only one PT entry is shown.

## Fields (Read View — No Edit Form Observed for Slab)

| Field | Value Observed | Notes |
|-------|----------------|-------|
| Work Location | Head Office | Groups PT by location |
| PT Number | (blank — "Update PT Number" button) | Optional; entered via separate button |
| State | Kerala | Derived from work location's state setting |
| Deduction Cycle | Half Yearly | State-specific; Kerala uses biannual cycle |
| PT Slabs | (View via button) | Pre-loaded statutory slabs per state |

## Kerala PT Slabs (Half Yearly basis)

Effective from: 01/04/2026

| Half Yearly Gross Salary (₹) | Half Yearly Tax Amount (₹) |
|------------------------------|---------------------------|
| 1 − 11,999 | 0 |
| 12,000 − 17,999 | 320 |
| 18,000 − 29,999 | 450 |
| 30,000 − 44,999 | 600 |
| 45,000 − 99,999 | 750 |
| 1,00,000 − 1,24,999 | 1,000 |
| 1,25,000 − 9,99,99,999 | 1,250 |

**Note:** Kerala deduction is Half Yearly (April–September and October–March). Maximum annual PT = ₹2,500 (₹1,250 × 2). This is the statutory maximum for Karnataka and Kerala.

**Important discrepancy:** The mock data specifies Karnataka/Bangalore, but the actual org is configured for Kerala/Thiruvananthapuram. Karnataka PT slabs differ:
- Karnataka PT is Monthly deduction
- Karnataka slab: ₹0 up to ₹14,999; ₹200/month for ₹15,000+

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| View Tax Slabs | Button | Work location with PT | Opens modal with slab table; "Okay" to close |
| (Revise) | Button | Work location with PT | Presumably allows slab customization |
| Update PT Number | Button | Work location with PT | Opens field to enter PT registration number |

## UI Patterns
- Page description: "This tax is levied on an employee's income by the State Government. Tax slabs differ in each state."
- Modal title: "Tax Slabs for [Work Location Name]"
- Modal shows: Deduction Cycle, Effective from date, and slab table
- The "(Revise)" label is rendered as a button with parentheses styling — clickable but less prominent than primary actions

## Cross-Module Effects
- PT deduction in pay runs uses the slab for the employee's assigned work location
- Adding a new work location in a different state would auto-create a new PT entry for that state
- PT is always pre-configured (state slabs are built-in to Zoho); only the PT registration number and enablement are admin-managed

## Gaps / Observations
- No explicit "Enable/Disable" toggle for PT — it appears to be always-on once a work location has a state configured
- The "(Revise)" button likely allows overriding state slabs — not explored further
- No field for PT challan details (bank, BSR code) at the settings level

## Screenshots
- [PT overview page](../screenshots/UF-10-PT-overview.png)
- [Kerala PT slabs modal](../screenshots/UF-10-PT-slabs-kerala.png)
