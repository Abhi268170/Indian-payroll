# UF-A11: Statutory Bonus

**Module:** Settings > Statutory Components | Pay Runs (Bonus Pay Run type)
**Tested:** 2026-05-16
**Approach:** Navigated to `#/settings/statutory-details/list` and reviewed Pay Run types. Prior session data from UF-53-bonus-payrun.md also referenced.

---

## Findings

### 1. Statutory Bonus — Legal Framework

**Governing law:** The Payment of Bonus Act, 1965 (as amended)

**Eligibility criteria:**
- Employee must complete at least 30 working days in the accounting year
- Employee's salary (basic + DA) must not exceed ₹21,000/month
- Applicable to establishments with 20+ employees (or 10+ in some states)
- Not applicable to managerial/supervisory employees earning > ₹21,000/month

**Bonus rates:**
- Minimum bonus: 8.33% of annual salary or ₹100 (whichever is higher), paid within 8 months of FY close
- Maximum bonus: 20% of annual salary
- If allocable surplus < minimum bonus → minimum bonus still payable
- For employees earning between ₹7,001 and ₹21,000/month: bonus calculated on deemed salary of ₹7,000

**Payment timing:** Must be paid within 8 months of the accounting year closing (i.e., by 30 November following 31 March year-end, or as per set-on/set-off provisions).

---

### 2. Statutory Components in Settings

**Route:** `#/settings/statutory-details/list`
**Prior session findings** (from UF-08 through UF-11):

Components configurable:
| Component | Key Settings |
|-----------|-------------|
| EPF | Employer + Employee contribution rates, wage ceiling |
| ESI | Employer + Employee contribution rates, wage ceiling (₹21,000/month) |
| Professional Tax | State-wise slabs (Kerala: ₹200/month for salary > ₹12,000) |
| LWF | State-wise contribution amounts |

**Statutory Bonus** does NOT appear as a separate configurable statutory component in the Settings > Statutory Components list in this test org. It is handled as a Pay Run type.

---

### 3. Statutory Bonus as a Pay Run Type

**Route to create:** `#/payruns` → "Run Payroll" → select pay run type

**Observed Pay Run types** (from prior session — UF-52 to UF-55):
| Type | Description |
|------|-------------|
| Regular | Monthly salary payroll |
| Off-Cycle | Additional ad-hoc payment |
| Bonus | For bonus payments including statutory bonus |
| Arrears | Arrear salary releases |

**Statutory Bonus flow:**
1. Admin selects "Bonus" pay run type
2. System identifies eligible employees (salary ≤ ₹21,000 basic + DA)
3. Admin enters bonus amount per employee or uses system calculation (8.33% default)
4. Bonus added to net pay for that month's disbursement
5. Payslip shows bonus as a separate line under Earnings

---

### 4. ESI and Bonus

**Important cross-module rule:** Statutory Bonus is exempt from ESI contributions if paid in a lump sum as part of a separate bonus pay run. However, if paid as part of regular monthly salary, it may be included in ESI wages. This distinction must be handled correctly in the engine.

**PF and Bonus:** Bonus is NOT part of PF wages. PF contributions are not deducted on statutory bonus amounts.

**TDS and Bonus:** Bonus IS taxable income. TDS must be computed on the total including bonus. If bonus creates a large spike, TDS recalculation for the year should adjust the monthly TDS accordingly.

---

### 5. Zoho Payroll Handling Assessment

Based on observed UI and prior sessions:

| Aspect | Zoho Approach |
|--------|--------------|
| Eligibility check (≤ ₹21,000) | Expected to be system-enforced (not tested) |
| Bonus rate input | Admin enters amount; system may show 8.33% suggested |
| Separate pay run | Yes — Bonus is a separate pay run type |
| PF exclusion | Expected — bonus pay run type should exclude PF computation |
| ESI exclusion | Expected for separate bonus run |
| TDS inclusion | Yes — bonus taxable income included in annual projection |

---

### 6. Gaps vs. Statutory Requirements

| Statutory Requirement | Zoho Capability | Gap |
|----------------------|-----------------|-----|
| Set-on / Set-off mechanism (multiple year provisions) | Not observed | May be manual outside Zoho |
| 8-month payment deadline tracking | Not observed (Compliance Calendar not fully tested) | Potentially missing |
| Eligibility cutoff at ₹21,000 | Not tested | Unknown |
| Minimum ₹100 floor | Not tested | Unknown |
| Deemed salary ₹7,000 calculation | Not tested | Unknown |

---

## Screenshots / Files

No new screenshots for this artifact — statutory bonus pay run type was documented in prior sessions (UF-53-bonus-payrun.md).

---

## Gaps / Open Questions

- [ ] **Eligibility auto-identification:** Does Zoho automatically identify employees eligible for statutory bonus (salary ≤ ₹21,000)? Or does admin manually select?
- [ ] **8.33% default calculation:** When creating a Bonus pay run, does the system pre-populate 8.33% of annual salary for eligible employees?
- [ ] **Set-on/Set-off:** Is there any provision in Zoho for tracking multi-year set-on/set-off bonus provisions? This is a complex statutory requirement.
- [ ] **₹7,000 deemed salary rule:** For employees earning between ₹7,001–₹21,000, is the bonus computed on actual or deemed ₹7,000?
- [ ] **Compliance calendar:** Is there an alert/reminder for the 8-month payment deadline?
- [ ] **ESI on bonus:** Does the system handle ESI exclusion for lump-sum bonus pay runs?
