import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { api } from '@/lib/api'

export type OnboardingStepId =
  | 'org-profile'
  | 'tax-details'
  | 'work-locations'
  | 'org-structure'
  | 'pay-schedule'
  | 'statutory'
  | 'salary-structure'
  | 'deductor-employee'
  | 'first-employee'

export interface OnboardingStepDto {
  id: OnboardingStepId
  complete: boolean
  required: boolean
  skippable: boolean
  details?: Record<string, unknown>
}

export interface NavGateDto {
  enabled: boolean
  missing: string[]
}

export interface OnboardingStatusDto {
  setupComplete: boolean
  steps: OnboardingStepDto[]
  navGates: Record<'people' | 'payRuns', NavGateDto>
}

export interface PreflightBlockerDto {
  code: string
  message: string
  fixUrl: string
  count?: number
}

export interface PreflightWarningDto {
  code: string
  message: string
  fixUrl: string
}

export interface PayrollRunPreflightDto {
  ready: boolean
  blockers: PreflightBlockerDto[]
  warnings: PreflightWarningDto[]
}

export function useOnboardingStatus(): UseQueryResult<OnboardingStatusDto> {
  return useQuery<OnboardingStatusDto>({
    queryKey: ['onboarding-status'],
    queryFn: () => api.get<OnboardingStatusDto>('/api/v1/onboarding/status').then(r => r.data),
    // Cached briefly so settings mutations that don't explicitly invalidate
    // still reflect within a few seconds.
    staleTime: 15_000,
  })
}

export function usePayrollRunPreflight(enabled = true): UseQueryResult<PayrollRunPreflightDto> {
  return useQuery<PayrollRunPreflightDto>({
    queryKey: ['payroll-run-preflight'],
    queryFn: () => api.get<PayrollRunPreflightDto>('/api/v1/payroll-runs/preflight').then(r => r.data),
    staleTime: 15_000,
    enabled,
  })
}

export function stepLabel(id: OnboardingStepId): string {
  switch (id) {
    case 'org-profile': return 'Organisation Profile'
    case 'tax-details': return 'Tax Details'
    case 'work-locations': return 'Work Locations'
    case 'org-structure': return 'Departments & Designations'
    case 'pay-schedule': return 'Pay Schedule'
    case 'statutory': return 'Statutory'
    case 'salary-structure': return 'Salary Structure'
    case 'deductor-employee': return 'Tax Deductor Employee'
    case 'first-employee': return 'Add First Employee'
  }
}

export function navMissingLabel(code: string): string {
  switch (code) {
    case 'work-locations': return 'Work Locations'
    case 'departments': return 'Departments'
    case 'designations': return 'Designations'
    case 'salary-structure': return 'Salary Structure'
    case 'pay-schedule': return 'Pay Schedule'
    case 'statutory': return 'Statutory configuration'
    case 'first-employee': return 'At least one payroll-ready employee'
    default: return code
  }
}
