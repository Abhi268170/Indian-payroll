# UF-33: Tax Regime Switch

**Module:** Employee Profile > Investments / IT Declaration / Settings
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta (New Regime, locked declaration)
**App State Before:** IT Declaration locked; "Allow employees to switch tax regimes" = CHECKED

## Steps Executed
1. Observed Settings > IT Declaration Preference
2. Confirmed "Allow employees to switch tax regimes" = CHECKED
3. Observed Arjun's IT Declaration URL containing `?tax_regime=with_exemptions` parameter
4. Documented tax regime switching mechanism

---

## Tax Regime Configuration

### System Setting
URL: `#/settings/preferences/it-declaration`
Setting: **"Allow employees to switch tax regimes" = CHECKED**

This means:
- Employees CAN change their tax regime (new vs old) via the employee portal
- Admin can also change regime on behalf of employee
- Regime choice affects all TDS calculations for the financial year

---

## Regime Options

### New Regime (Section 115BAC) — FY2026-27 Slabs
| Income Slab | Rate |
|-------------|------|
| Up to ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| Above ₹24,00,000 | 30% |

**Deductions available in new regime:**
- Standard Deduction: ₹75,000 (from FY2025-26)
- Employer NPS Contribution: Up to 10% of basic (Section 80CCD(2))
- Rebate u/s 87A: Up to ₹7,00,000 taxable income → tax = ₹0

**NOT available in new regime:**
- HRA exemption (Section 10(13A))
- LTA exemption (Section 10(5))
- 80C deductions (LIC, PPF, ELSS, home loan principal)
- 80D (health insurance)
- 80G (donations)
- Most other Chapter VI-A deductions

### Old Regime (Pre-Section 115BAC)
| Income Slab | Rate |
|-------------|------|
| Up to ₹2,50,000 | 0% |
| ₹2,50,001 – ₹5,00,000 | 5% |
| ₹5,00,001 – ₹10,00,000 | 20% |
| Above ₹10,00,000 | 30% |

**Deductions available:**
- HRA exemption (formula-based under Section 10(13A))
- LTA (Section 10(5))
- Standard Deduction: ₹50,000
- All Chapter VI-A deductions (80C ₹1,50,000, 80D ₹25,000-₹1,00,000, 80G, etc.)
- Section 10 allowances (Conveyance, Medical Reimbursement, etc.)

---

## Regime Switch — When and How

### When Can Employee Switch
- At the beginning of financial year (preferred)
- Mid-year switching allowed if "Allow employees to switch tax regimes" = enabled
- Final choice must be made before the last pay run of the financial year

### How Employee Switches (Expected Portal Flow)
1. Employee logs into portal
2. Navigates to IT Declaration section
3. Sees current regime ("New Tax Regime" or "Old Tax Regime")
4. Clicks "Switch to Old Tax Regime" (or vice versa)
5. System prompts for confirmation: "This will change your TDS calculations"
6. Employee confirms
7. TDS recomputes immediately for remaining months

### How Admin Switches on Behalf of Employee
1. Navigate to `#/people/employees/{id}/investments-and-proofs`
2. Open IT Declaration section
3. Edit the declaration → change regime selection
4. Save — TDS recomputes

---

## Observed Anomaly: `?tax_regime=with_exemptions` Parameter

In Arjun's IT Declaration URL:
`#/people/employees/3848927000000032948/investments-and-proofs?tax_regime=with_exemptions`

The `with_exemptions` parameter value suggests the old regime (which allows exemptions). The new regime would be `without_exemptions` or similar.

**Hypothesis:** Arjun's current declaration may be toggled between regimes and the URL parameter reflects the currently displayed view — not necessarily the confirmed regime. This needs verification.

**Alternative interpretation:** The system allows viewing what the declaration would look like "with exemptions" (old regime) even if the employee is in new regime, for comparison purposes.

---

## TDS Impact of Regime Choice — Arjun Example

| Scenario | Annual Taxable Income | Annual Tax | Monthly TDS |
|----------|-----------------------|------------|-------------|
| New Regime (no deductions) | ₹7,65,000 (₹8,40,000 − ₹75,000 std deduction) | ₹18,250 + cess | ₹1,582/month |
| Old Regime (with 80C ₹1,50,000 + HRA exemption ~₹72,000) | ₹5,18,000 (approx) | ₹13,400 + cess | ₹1,145/month |

**For Arjun's salary level, old regime provides some benefit if they have 80C investments + HRA rent payments.**

---

## Business Rules
1. Only one regime can be active at a time per employee per financial year
2. Switching regime mid-year recalculates TDS from the switch date forward; prior months' TDS is not reversed
3. If employee doesn't choose a regime, default is new regime (FY2024-25 onward budget decision)
4. Admin setting "Allow employees to switch" = checked means employee self-service regime change is permitted
5. "Allow TDS modification to exceed calculated tax amount" = NOT checked → employee cannot request higher TDS deduction than computed

## Gaps / Observations
- Regime switch not tested in UI (IT Declaration locked)
- `?tax_regime=with_exemptions` URL parameter semantics unclear — may indicate old regime
- Old regime support confirmed in settings (despite V1 build plan being "new regime only" — Zoho is a product, not a custom build)
- 🟡 This is significant for the SaaS being built: the "new regime only in V1" constraint means the custom product will not offer this setting

## Open Questions
- [ ] When employee switches regime mid-year, does the system show a tax impact comparison (new regime tax vs old regime tax)?
- [ ] Is the regime choice locked after first pay run of the FY, or can it be changed all year?
- [ ] Does Zoho send a reminder to employees who haven't chosen a regime?
- [ ] In the `?tax_regime=with_exemptions` URL — is this Arjun's current regime or just a view parameter?
