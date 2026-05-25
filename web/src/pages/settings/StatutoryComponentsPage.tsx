import { type ReactElement } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Navigate, useSearchParams } from 'react-router-dom'
import { clsx } from 'clsx'
import { api } from '@/lib/api'
import { Spinner } from '@/components/ui/Spinner'
import EpfTab from './statutory/EpfTab'
import EsiTab from './statutory/EsiTab'
import PtTab from './statutory/PtTab'
import LwfTab from './statutory/LwfTab'
import StatutoryBonusTab from './statutory/StatutoryBonusTab'

export interface StatutoryConfig {
  epfEnabled: boolean
  epfEstablishmentCode: string | null
  epfEmployeeContributionRate: string
  epfEmployerContributionRate: string
  epfIncludeEmployerInCtc: boolean
  epfOverrideAtEmployeeLevel: boolean
  epfProRateRestrictedPfWage: boolean
  epfConsiderSalaryOnLop: boolean
  esiEnabled: boolean
  esiEstablishmentCode: string | null
  esiNotifiedArea: boolean
  statutoryBonusEnabled: boolean
  bonusRate: number
  bonusMode: string
  bonusPayoutMonth: number | null
}

const TABS = [
  { id: 'epf', label: 'EPF' },
  { id: 'esi', label: 'ESI' },
  { id: 'pt', label: 'Professional Tax' },
  { id: 'lwf', label: 'Labour Welfare Fund' },
  { id: 'bonus', label: 'Statutory Bonus' },
]

export default function StatutoryComponentsPage(): ReactElement {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? 'epf'

  const { data: config, isLoading, error } = useQuery<StatutoryConfig>({
    queryKey: ['statutory-config'],
    queryFn: () => api.get<StatutoryConfig>('/api/v1/statutory/config').then(r => r.data),
    retry: false,
  })

  // Phase A made GET /api/v1/statutory/config return 404 when the row is missing
  // (instead of the old fallback DTO that lied about EPF being disabled). If we land
  // here without a config, route the user to the wizard so they can resolve it
  // instead of showing a hard error.
  const status = (error as { response?: { status?: number } } | null)?.response?.status
  if (status === 404) {
    // Phase 4 of the onboarding UX redesign removed /onboarding routes; the setup
    // checklist on /dashboard now owns the Statutory step.
    return <Navigate to="/dashboard" replace />
  }

  function setTab(id: string): void {
    setParams({ tab: id })
  }

  return (
    <div className="p-8 max-w-4xl">
      <div className="mb-6">
        <h2 className="text-[22px] font-bold text-[var(--color-text-primary)]">
          Statutory Components
        </h2>
        <p className="text-[13px] text-[var(--color-text-muted)] mt-1">
          Configure mandatory statutory deductions — EPF, ESI, Professional Tax, LWF, and Statutory Bonus.
        </p>
      </div>

      {/* Tab strip */}
      <div className="flex border-b border-[var(--color-border)] mb-6">
        {TABS.map(t => (
          <button
            key={t.id}
            onClick={() => { setTab(t.id); }}
            className={clsx(
              'px-4 py-2.5 text-[13px] font-medium border-b-2 -mb-px transition-colors',
              tab === t.id
                ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
                : 'border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)]',
            )}
          >
            {t.label}
          </button>
        ))}
      </div>

      {isLoading || !config ? (
        <Spinner />
      ) : (
        <div>
          {tab === 'epf' && <EpfTab config={config} />}
          {tab === 'esi' && <EsiTab config={config} />}
          {tab === 'pt' && <PtTab />}
          {tab === 'lwf' && <LwfTab />}
          {tab === 'bonus' && <StatutoryBonusTab config={config} />}
        </div>
      )}
    </div>
  )
}
