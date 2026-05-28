// Shared salary-structure preview math, mirrors backend
// `Payroll.Application.Services.SalaryStructurePreviewCalculator`.
//
// Both the settings builder and the employee hire wizard compute their preview
// client-side (so the operator can see the residual update as they type). This
// module is the single TS implementation — until it existed, each page had its
// own slightly-different copy that drifted from the backend and from each other.
//
// Inputs deliberately match the backend record shape so a future PR can collapse
// both into a single `POST /preview` endpoint without changing call sites.

export type FormulaType = 'Fixed' | 'PercentOfCTC' | 'PercentOfBasic' | 'PercentOfGross' | 'ResidualCTC'

export interface PreviewComponent {
  componentId: string
  code: string
  name: string
  // EarningType.Basic identifies the basic-wage row that drives gratuity accrual
  // and PercentOfBasic computations. Match the backend enum values exactly.
  earningType: string | null
  considerForEpf: boolean
  // Template defaults; overrides applied via the `overrides` map.
  formulaType: FormulaType
  percentage: number | null
  fixedAmount: number | null
  displayOrder: number
}

export interface AddedComponent {
  componentId: string
  code: string
  name: string
  earningType: string | null
  considerForEpf: boolean
  formulaType: FormulaType
  percentage: number | null
  fixedAmount: number | null
}

export interface Override {
  formulaType: FormulaType
  percentage: number | null
  fixedAmount: number | null
}

export interface EmployeeStatutoryFlags {
  epfEnabled: boolean
  esiEnabled: boolean
  ptEnabled: boolean
  lwfEnabled: boolean
  gratuityEnabled: boolean
}

export interface StatutoryOrgFlags {
  epfEnabled: boolean
  epfIncludeEmployerInCtc: boolean
  gratuityIncludedInCtc: boolean
}

export interface StatutoryCaps {
  pfWageCap: number
  epfEmployerRate: number
}

export const DEFAULT_CAPS: StatutoryCaps = {
  pfWageCap: 15_000,
  epfEmployerRate: 0.12,
}

export const DEFAULT_ORG_FLAGS: StatutoryOrgFlags = {
  epfEnabled: true,
  epfIncludeEmployerInCtc: true,
  gratuityIncludedInCtc: true,
}

export const DEFAULT_EMPLOYEE_FLAGS: EmployeeStatutoryFlags = {
  epfEnabled: true,
  esiEnabled: true,
  ptEnabled: true,
  lwfEnabled: true,
  gratuityEnabled: true,
}

export interface PreviewRow {
  componentId: string
  code: string
  name: string
  formulaType: FormulaType
  percentage: number | null
  fixedAmount: number | null
  monthlyAmount: number
  annualAmount: number
  isResidual: boolean
  isAdded: boolean
  isOverride: boolean
}

export interface EmployerContribution {
  code: string
  name: string
  monthlyAmount: number
  annualAmount: number
}

export interface EmployeeDeduction {
  code: string
  name: string
  monthlyAmount: number
  annualAmount: number
}

export interface BenefitRow {
  code: string
  name: string
  monthlyAmount: number
  annualAmount: number
}

export interface PreviewOutput {
  rows: PreviewRow[]
  employerContributions: EmployerContribution[]
  employeeDeductions: EmployeeDeduction[]
  netPayMonthly: number
  benefits: BenefitRow[]
}

export interface BenefitInput {
  componentId: string
  annualAmount: number
}

export interface PreviewInputs {
  annualCtc: number
  templateComponents: PreviewComponent[]
  overrides: Record<string, Override>
  addedComponents: AddedComponent[]
  benefits?: BenefitInput[]
  employeeFlags?: EmployeeStatutoryFlags
  orgFlags?: StatutoryOrgFlags
  caps?: StatutoryCaps
  workStateCode?: string | null
  year?: number
  month?: number
}

function round2(n: number): number {
  return Math.round(n * 100) / 100
}

function evaluateMonthly(
  type: FormulaType,
  pct: number | null,
  fixedAmount: number | null,
  annualCtc: number,
  basicMonthly: number,
  monthlyGross: number,
): number {
  if (type === 'PercentOfCTC' && pct != null) return round2((annualCtc * (pct / 100)) / 12)
  if (type === 'PercentOfBasic' && pct != null) return round2(basicMonthly * (pct / 100))
  if (type === 'PercentOfGross' && pct != null) return round2(monthlyGross * (pct / 100))
  if (type === 'Fixed') return fixedAmount ?? 0
  return 0
}

// API request/response — mirror of backend SalaryStructurePreviewRequest/Dto.
export interface PreviewApiRequest {
  annualCtc: number
  templateComponents: { componentId: string; formulaType: FormulaType; fixedAmount: number | null; percentage: number | null; displayOrder: number }[]
  overrides: { salaryComponentId: string; formulaType: FormulaType; fixedAmount: number | null; percentage: number | null }[]
  addedComponents: { componentId: string; formulaType: FormulaType; fixedAmount: number | null; percentage: number | null }[]
  benefits: { componentId: string; annualAmount: number }[]
  employeeFlags: EmployeeStatutoryFlags & { isPwd?: boolean }
  workStateCode?: string | null
  year?: number
  month?: number
}

export interface PreviewApiResponse {
  rows: PreviewRow[]
  employerContributions: EmployerContribution[]
  employeeDeductions: EmployeeDeduction[]
  netPayMonthly: number
  benefits: BenefitRow[]
}

export function computePreview(inputs: PreviewInputs): PreviewOutput {
  const annualCtc = inputs.annualCtc
  const monthlyGross = annualCtc / 12
  const employeeFlags = inputs.employeeFlags ?? DEFAULT_EMPLOYEE_FLAGS
  const orgFlags = inputs.orgFlags ?? DEFAULT_ORG_FLAGS
  const caps = inputs.caps ?? DEFAULT_CAPS

  const rows: PreviewRow[] = []
  let basicMonthly = 0
  let nonResidualMonthly = 0
  let pfWageMonthly = 0

  const ordered = [...inputs.templateComponents].sort((a, b) => a.displayOrder - b.displayOrder)
  let residual: PreviewComponent | null = null

  for (const comp of ordered) {
    if (comp.formulaType === 'ResidualCTC') {
      residual = comp
      continue
    }
    const ov = inputs.overrides[comp.componentId]
    const type: FormulaType = ov?.formulaType ?? comp.formulaType
    const pct = ov?.percentage ?? comp.percentage
    const fixedAmount = ov?.fixedAmount ?? comp.fixedAmount

    const monthly = evaluateMonthly(type, pct, fixedAmount, annualCtc, basicMonthly, monthlyGross)

    if (comp.earningType === 'Basic') basicMonthly = monthly
    nonResidualMonthly += monthly
    if (comp.considerForEpf) pfWageMonthly += monthly

    rows.push({
      componentId: comp.componentId,
      code: comp.code,
      name: comp.name,
      formulaType: type,
      percentage: pct,
      fixedAmount,
      monthlyAmount: monthly,
      annualAmount: round2(monthly * 12),
      isResidual: false,
      isAdded: false,
      isOverride: ov != null,
    })
  }

  for (const added of inputs.addedComponents) {
    const ov = inputs.overrides[added.componentId]
    const type: FormulaType = ov?.formulaType ?? added.formulaType
    const pct = ov?.percentage ?? added.percentage
    const fixedAmount = ov?.fixedAmount ?? added.fixedAmount
    const monthly = evaluateMonthly(type, pct, fixedAmount, annualCtc, basicMonthly, monthlyGross)
    nonResidualMonthly += monthly
    if (added.considerForEpf) pfWageMonthly += monthly

    rows.push({
      componentId: added.componentId,
      code: added.code,
      name: added.name,
      formulaType: type,
      percentage: pct,
      fixedAmount,
      monthlyAmount: monthly,
      annualAmount: round2(monthly * 12),
      isResidual: false,
      isAdded: true,
      isOverride: true,
    })
  }

  // Employer statutory load — both org and employee must agree before deducting from CTC.
  let employerEpfMonthly = 0
  let gratuityMonthly = 0

  if (employeeFlags.epfEnabled && orgFlags.epfEnabled && orgFlags.epfIncludeEmployerInCtc && pfWageMonthly > 0) {
    const cappedPfWage = Math.min(pfWageMonthly, caps.pfWageCap)
    employerEpfMonthly = round2(cappedPfWage * caps.epfEmployerRate)
  }

  if (employeeFlags.gratuityEnabled && orgFlags.gratuityIncludedInCtc && basicMonthly > 0) {
    gratuityMonthly = round2((basicMonthly * 15) / 26 / 12)
  }

  const employerStatutoryMonthly = employerEpfMonthly + gratuityMonthly

  if (residual) {
    const residualMonthly = Math.max(0, monthlyGross - nonResidualMonthly - employerStatutoryMonthly)
    rows.push({
      componentId: residual.componentId,
      code: residual.code,
      name: residual.name,
      formulaType: 'ResidualCTC',
      percentage: null,
      fixedAmount: null,
      monthlyAmount: round2(residualMonthly),
      annualAmount: round2(residualMonthly * 12),
      isResidual: true,
      isAdded: false,
      isOverride: false,
    })
  }

  const employerContributions: EmployerContribution[] = []
  if (employerEpfMonthly > 0) {
    employerContributions.push({
      code: 'EPF_EMPLOYER',
      name: 'Employer EPF + EPS',
      monthlyAmount: employerEpfMonthly,
      annualAmount: round2(employerEpfMonthly * 12),
    })
  }
  if (gratuityMonthly > 0) {
    employerContributions.push({
      code: 'GRATUITY_ACCRUAL',
      name: 'Gratuity accrual',
      monthlyAmount: gratuityMonthly,
      annualAmount: round2(gratuityMonthly * 12),
    })
  }

  // Local fallback computes earnings + employer-side only. Employee deductions,
  // net pay, and benefits arrive from the backend (state-dependent + delegates to
  // engine calculators that aren't worth porting). Defaults keep callers safe.
  return { rows, employerContributions, employeeDeductions: [], netPayMonthly: 0, benefits: [] }
}
