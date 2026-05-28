import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import {
  computePreview,
  type PreviewInputs,
  type PreviewOutput,
  type PreviewApiRequest,
  type PreviewApiResponse,
} from '@/lib/salaryStructurePreview'

// Backend-authoritative preview hook. Calls `POST /api/v1/salary-structure-templates/preview`
// — the same SalaryStructurePreviewCalculator that the server uses for the
// employee salary detail query, so the operator never sees a residual that
// disagrees with what payroll will actually produce at run time.
//
// While the request is in flight (or before annual CTC is set), the hook falls
// back to the local TS computePreview so the user still gets instant feedback
// as they type. Once the server response lands, that wins. An equivalence test
// in the backend guarantees the two implementations agree on the same inputs.
export function useSalaryStructurePreview(inputs: PreviewInputs): {
  data: PreviewOutput
  isPending: boolean
  isError: boolean
} {
  const enabled = inputs.annualCtc > 0 && inputs.templateComponents.length > 0
  const apiRequest: PreviewApiRequest = {
    annualCtc: inputs.annualCtc,
    templateComponents: inputs.templateComponents.map(c => ({
      componentId: c.componentId,
      formulaType: c.formulaType,
      fixedAmount: c.fixedAmount,
      percentage: c.percentage,
      displayOrder: c.displayOrder,
    })),
    overrides: Object.entries(inputs.overrides).map(([id, ov]) => ({
      salaryComponentId: id,
      formulaType: ov.formulaType,
      fixedAmount: ov.fixedAmount,
      percentage: ov.percentage,
    })),
    addedComponents: inputs.addedComponents.map(a => ({
      componentId: a.componentId,
      formulaType: a.formulaType,
      fixedAmount: a.fixedAmount,
      percentage: a.percentage,
    })),
    benefits: (inputs.benefits ?? []).map(b => ({
      componentId: b.componentId,
      annualAmount: b.annualAmount,
    })),
    employeeFlags: inputs.employeeFlags ?? {
      epfEnabled: true, esiEnabled: true, ptEnabled: true, lwfEnabled: true, gratuityEnabled: true,
    },
    workStateCode: inputs.workStateCode,
    year: inputs.year,
    month: inputs.month,
  }

  const query = useQuery<PreviewApiResponse>({
    queryKey: ['salary-structure-preview', apiRequest],
    queryFn: async () => {
      const res = await api.post<PreviewApiResponse>('/api/v1/salary-structure-templates/preview', apiRequest)
      return res.data
    },
    enabled,
    staleTime: 5_000,  // small window of caching for repeated keystrokes that produce identical payloads
  })

  // Fallback to local compute while pending. The local TS calculator covers
  // earnings + employer-side; employee deductions + benefits + net pay come
  // from the backend (state-dependent + engine-delegated). The fallback
  // surfaces empty arrays for those so the UI doesn't render stale numbers.
  const fallback = computePreview(inputs)
  const data: PreviewOutput = query.data
    ? {
        rows: query.data.rows,
        employerContributions: query.data.employerContributions,
        employeeDeductions: query.data.employeeDeductions,
        netPayMonthly: query.data.netPayMonthly,
        benefits: query.data.benefits,
      }
    : fallback

  return { data, isPending: query.isPending, isError: query.isError }
}
