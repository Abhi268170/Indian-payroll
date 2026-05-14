export interface TokenResponse {
  access_token: string
  token_type: string
  expires_in: number
  refresh_token?: string
}

export interface BranchDto {
  id: string
  name: string
  state: string
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
  lastName: string
  fullName: string
  dateOfBirth: string
  gender: string
  dateOfJoining: string
  employmentType: string
  status: string
  workState: string
  departmentId: string
  designationId: string
  branchId: string | null
  costCentreId: string | null
}

export interface TenantDto {
  id: string
  displayName: string
  slug: string
  isActive: boolean
  createdAt: string
}

export interface ApiError {
  error?: string
  errors?: string[]
}
