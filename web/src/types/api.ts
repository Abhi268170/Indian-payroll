export interface TokenResponse {
  access_token: string
  token_type: string
  expires_in: number
  refresh_token?: string
}

export interface DepartmentDto {
  id: string
  name: string
  code: string | null
}

export interface DesignationDto {
  id: string
  name: string
}

export interface CostCentreDto {
  id: string
  name: string
  code: string | null
}

export interface EmployeeDto {
  id: string
  employeeCode: string
  firstName: string
  middleName: string | null
  lastName: string
  fullName: string
  workEmail: string
  mobileNumber: string | null
  gender: string
  dateOfJoining: string
  dateOfLeaving: string | null
  employmentType: string
  status: string
  isDirector: boolean
  enablePortalAccess: boolean
  profileComplete: boolean
  departmentId: string
  departmentName: string | null
  designationId: string
  designationName: string | null
  workLocationId: string
  workLocationName: string | null
  businessUnitId: string | null
  costCentreId: string | null
  dateOfBirth: string
  fathersName: string | null
  personalEmail: string | null
  differentlyAbledType: string
  addressLine1: string | null
  addressLine2: string | null
  city: string | null
  residentialState: string | null
  pinCode: string | null
  paymentMode: string
  accountHolderName: string | null
  bankName: string | null
  accountType: string | null
  maskedAccountNumber: string | null
  ifscCode: string | null
  uan: string | null
  esicipNumber: string | null
  epfEnabled: boolean
  esiEnabled: boolean
  ptEnabled: boolean
  lwfEnabled: boolean
  isPWD: boolean
  maskedPAN: string | null
}

export interface EmployeeListItemDto {
  id: string
  employeeCode: string
  fullName: string
  workEmail: string
  mobileNumber: string | null
  status: string
  departmentName: string | null
  designationName: string | null
  workLocationName: string | null
  dateOfJoining: string
  profileComplete: boolean
  enablePortalAccess: boolean
  employmentType: string
}

export interface CreateEmployeeRequest {
  firstName: string
  middleName?: string
  lastName: string
  employeeCode?: string
  workEmail: string
  mobileNumber?: string
  gender: string
  dateOfJoining: string
  dateOfBirth: string
  employmentType: string
  isDirector: boolean
  enablePortalAccess: boolean
  departmentId: string
  designationId: string
  workLocationId: string
  businessUnitId?: string
  costCentreId?: string
}

export interface SalaryStructureTemplateSummaryDto {
  id: string
  name: string
  description: string | null
  isActive: boolean
  componentCount: number
}

export interface SalaryStructureTemplateDetailDto {
  id: string
  name: string
  description: string | null
  isActive: boolean
  components: SalaryStructureComponentDto[]
}

export interface SalaryStructureComponentDto {
  componentId: string
  componentName: string
  componentCode: string
  category: string
  isSystemComponent: boolean
  formulaType: string
  fixedAmount: number | null
  percentage: number | null
  displayOrder: number
}

export interface EmployeeSalaryStructureDto {
  id: string
  annualCTC: number
  monthlyGross: number
  templateId: string | null
  templateName: string | null
  effectiveFrom: string
  components: EmployeeSalaryComponentBreakdownDto[]
}

export interface EmployeeSalaryComponentBreakdownDto {
  componentId: string
  componentName: string
  componentCode: string
  formulaType: string
  percentage: number | null
  monthlyAmount: number
  annualAmount: number
  isResidual: boolean
}

export interface TenantDto {
  id: string
  displayName: string
  slug: string
  isActive: boolean
  createdAt: string
  schema?: string
  adminEmail?: string
}

export interface ApiError {
  error?: string
  errors?: string[]
}

export interface CurrentPayPeriodDto {
  year: number
  month: number
  periodLabel: string
  payDay: string | null
  activeEmployeeCount: number
  hasOutstandingRun: boolean
  outstandingRunId: string | null
  outstandingRunStatus: string | null
}

export interface PayrollRunSummaryDto {
  id: string
  year: number
  month: number
  periodLabel: string
  status: string
  type: string
  payDay: string | null
  payrollCost: number
  totalNetPay: number
  totalEmployerPf: number
  totalEmployerEsi: number
  totalTds: number
  totalPt: number
  employeeCount: number
  createdAt: string
  approvedAt: string | null
  paidAt: string | null
}

export interface PayrollHistoryItemDto {
  id: string
  year: number
  month: number
  periodLabel: string
  totalNetPay: number
  employeeCount: number
  paidAt: string | null
}

export interface PayrunEmployeeDto {
  employeeId: string
  employeeCode: string
  employeeName: string
  department: string
  designation: string
  status: string
  lopDays: number
  baseDays: number
  grossPay: number
  netPay: number
  employeePf: number
  tdsAmount: number
  tdsOverrideAmount: number | null
  skipReason: string | null
}

export interface PendingTaskItemDto {
  employeeId: string
  employeeCode: string
  reason: string
}

export interface PendingTasksDto {
  hardBlocks: PendingTaskItemDto[]
  softWarnings: PendingTaskItemDto[]
  hasAnyHardBlocks: boolean
}

export interface ComponentBreakdownDto {
  id: string
  salaryComponentId: string
  componentCode: string
  componentName: string
  fullAmount: number
  proratedAmount: number
  isOneTimeEarning: boolean
}

export interface EmployeeVariableInputsDto {
  payrollRunId: string
  employeeId: string
  lopDays: number
  baseDays: number
  actualPayableDays: number
  grossPay: number
  netPay: number
  tdsAmount: number
  tdsOverrideAmount: number | null
  tdsOverrideReason: string | null
  components: ComponentBreakdownDto[]
}

export interface PayslipComponentDto {
  componentCode: string
  componentName: string
  amount: number
  ytdAmount: number
  isEarning: boolean
}

export interface PayslipData {
  payrollRunId: string
  employeeId: string
  employeeCode: string
  employeeName: string
  designation: string
  department: string
  companyName: string
  companyAddress: string | null
  payPeriodYear: number
  payPeriodMonth: number
  periodLabel: string
  payDay: string | null
  grossPay: number
  netPay: number
  netPayInWords: string
  employeePf: number
  employerPf: number
  employeeEsi: number
  employerEsi: number
  ptAmount: number
  tdsAmount: number
  ytdGross: number
  ytdNetPay: number
  ytdTds: number
  ytdPf: number
  maskedBankAccount: string
  bankName: string | null
  ifscCode: string | null
  components: PayslipComponentDto[]
}
