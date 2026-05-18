import { type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { MapPin } from 'lucide-react'
import { api } from '@/lib/api'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'
import type { WorkLocation } from '../WorkLocationsPage'

interface LwfConfig {
  stateCode: string
  effectiveDate: string
  employeeAmount: number
  employerAmount: number
  isPercentageBased: boolean
  employeeRate: number | null
  employerRate: number | null
  frequency: string
  deductionMonth: number | null
  wageThreshold: number | null
  isActive: boolean
}

const STATE_NAMES: Record<string, string> = {
  MH: 'Maharashtra', KA: 'Karnataka', AP: 'Andhra Pradesh',
  TS: 'Telangana', WB: 'West Bengal', GJ: 'Gujarat',
  MP: 'Madhya Pradesh', CH: 'Chandigarh', HR: 'Haryana',
  KL: 'Kerala',
}

const stateCodeMap: Record<string, string> = {
  Maharashtra: 'MH', Karnataka: 'KA', AndhraPradesh: 'AP',
  Telangana: 'TS', WestBengal: 'WB', Gujarat: 'GJ',
  MadhyaPradesh: 'MP', Chandigarh: 'CH', Haryana: 'HR',
  Kerala: 'KL',
}

function formatAmount(cfg: LwfConfig): string {
  if (cfg.isPercentageBased) {
    const er = ((cfg.employeeRate ?? 0) * 100).toFixed(1)
    const orr = ((cfg.employerRate ?? 0) * 100).toFixed(1)
    return `${er}% employee / ${orr}% employer`
  }
  return `₹${cfg.employeeAmount.toLocaleString('en-IN')} employee / ₹${cfg.employerAmount.toLocaleString('en-IN')} employer`
}

export default function LwfTab(): ReactElement {
  const qc = useQueryClient()
  const toast = useToast()

  const toggle = useMutation({
    mutationFn: ({ stateCode, enable }: { stateCode: string; enable: boolean }) =>
      api.put(`/api/v1/statutory/lwf-configs/${stateCode}/toggle`, { enable }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['lwf-configs'] })
      toast.success('LWF status updated')
    },
    onError: () => { toast.error('Failed to update LWF status') },
  })

  const { data: locations } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })

  const uniqueStateCodes = [...new Set((locations ?? []).map(l => stateCodeMap[l.state]).filter((s): s is string => Boolean(s)))]

  const { data: lwfConfigs, isLoading } = useQuery<LwfConfig[]>({
    queryKey: ['lwf-configs', uniqueStateCodes.join(',')],
    queryFn: () => {
      const params = uniqueStateCodes.map(s => `states=${s}`).join('&')
      return api.get<LwfConfig[]>(`/api/v1/statutory/lwf-configs?${params}`).then(r => r.data)
    },
    enabled: uniqueStateCodes.length > 0,
  })

  const uniqueStates = [...new Set((locations ?? []).map(l => l.state))].filter(
    s => stateCodeMap[s],
  )

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Labour Welfare Fund
        </h3>
        <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
          LWF is applicable based on employee's work location state. Only states with applicable
          law are shown.
        </p>
      </div>

      {isLoading && <Spinner />}

      {!isLoading && uniqueStates.length === 0 && (
        <div className="text-center py-12 text-[var(--color-text-muted)]">
          <MapPin className="w-8 h-8 mx-auto mb-3 opacity-40" />
          <p className="text-[13px]">No work locations with LWF-applicable states configured.</p>
        </div>
      )}

      {!isLoading && uniqueStates.length > 0 && (
        <div className="space-y-3">
          {uniqueStates.map(stateEnum => {
            const code = stateCodeMap[stateEnum]
            const cfg = (lwfConfigs ?? []).find(c => c.stateCode === code)
            const locs = (locations ?? []).filter(l => l.state === stateEnum)
            const stateName = STATE_NAMES[code ?? ''] ?? stateEnum

            if (!cfg) return null

            return (
              <div
                key={stateEnum}
                className={`border rounded-lg p-4 ${cfg.isActive ? 'border-[var(--color-border)]' : 'border-[var(--color-border)] opacity-60'}`}
              >
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <div className="text-[14px] font-semibold text-[var(--color-text-primary)]">
                        {stateName}
                      </div>
                      <span className={`text-[11px] font-medium px-1.5 py-0.5 rounded ${cfg.isActive ? 'bg-[var(--color-success-light)] text-[var(--color-success)]' : 'bg-[var(--color-border)] text-[var(--color-text-muted)]'}`}>
                        {cfg.isActive ? 'Enabled' : 'Disabled'}
                      </span>
                    </div>
                    <div className="text-[12px] text-[var(--color-text-muted)] mt-0.5">
                      {locs.map(l => l.name).join(', ')}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-[13px] font-medium text-[var(--color-text-primary)]">
                      {formatAmount(cfg)}
                    </div>
                    <div className="text-[12px] text-[var(--color-text-muted)] mt-0.5">
                      {cfg.frequency}
                      {cfg.deductionMonth != null && ` (Month ${String(cfg.deductionMonth)})`}
                      {cfg.wageThreshold != null && ` · Exempt if wage > ₹${cfg.wageThreshold.toLocaleString('en-IN')}`}
                    </div>
                    <button
                      className={`mt-1 text-[12px] font-medium hover:underline ${cfg.isActive ? 'text-[var(--color-error)]' : 'text-[var(--color-primary)]'}`}
                      onClick={() => { toggle.mutate({ stateCode: cfg.stateCode, enable: !cfg.isActive }) }}
                    >
                      {cfg.isActive ? 'Disable' : 'Enable'}
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
