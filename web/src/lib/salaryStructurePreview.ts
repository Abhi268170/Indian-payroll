// Local fallback for the salary-structure preview while the backend
// `POST /api/v1/salary-structure-templates/preview` response is in flight.
//
// The fallback computes EARNINGS ROWS ONLY (percent-of-CTC, percent-of-basic,
// percent-of-gross, fixed). Everything statutory — employer EPF, gratuity,
// the Special Allowance residual, employee deductions, net pay, benefits —
// is server-authoritative and intentionally NOT computed here: the local
// math drifted from the backend (hardcoded caps, unconditional PF cap,
// ignored benefits). Those fields come back as null/empty so consumers can
// render a loading placeholder ("—") until the server preview arrives.

export type FormulaType = 'Fixed' | 'PercentOfCTC' | 'PercentOfBasic' | 'PercentOfGross' | 'ResidualCTC'

export interface PreviewComponent {
  componentId: string
  code: string
  name: string
  // EarningType.Basic identifies the basic-wage row that drives
  // PercentOfBasic computations. Match the backend enum values exactly.
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

export interface PreviewRow {
  componentId: string
  code: string
  name: string
  formulaType: FormulaType
  percentage: number | null
  fixedAmount: number | null
  // null while the server preview is pending (residual row in the local fallback).
  monthlyAmount: number | null
  annualAmount: number | null
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
  workStateCode?: string | null
  year?: number
  month?: number
}

function round2(n: number): number {
  return Math.round((n + Number.EPSILON) * 100) / 100
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

  const rows: PreviewRow[] = []
  let basicMonthly = 0

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

  if (residual) {
    // Residual ("Special Allowance") depends on the employer statutory load —
    // server-only. Emit the row with null amounts so the UI shows a loading
    // placeholder instead of a number that disagrees with the backend.
    rows.push({
      componentId: residual.componentId,
      code: residual.code,
      name: residual.name,
      formulaType: 'ResidualCTC',
      percentage: null,
      fixedAmount: null,
      monthlyAmount: null,
      annualAmount: null,
      isResidual: true,
      isAdded: false,
      isOverride: false,
    })
  }

  // Employer contributions, employee deductions, net pay, and benefits are
  // server-authoritative — empty/zero here means "pending", not "₹0".
  return { rows, employerContributions: [], employeeDeductions: [], netPayMonthly: 0, benefits: [] }
}
