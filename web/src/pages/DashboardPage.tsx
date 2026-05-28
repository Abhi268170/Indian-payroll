import { type ReactElement } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Users, Calendar, CheckCircle2, AlertCircle, ChevronRight, AlertTriangle } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { CurrentPayPeriodDto, EmployeeListItemDto, PayrollHistoryItemDto } from '@/types/api'
import { useOnboardingStatus, usePayrollRunPreflight } from '@/hooks/useOnboardingStatus'
import SetupChecklistCard from './dashboard/components/SetupChecklistCard'
import WelcomeBanner from './dashboard/components/WelcomeBanner'

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

function Kpi({
  icon, label, value, tone, dim = false, subtitle,
}: {
  icon: ReactElement
  label: string
  value: string
  tone: 'neutral' | 'good' | 'warn'
  dim?: boolean
  subtitle?: string
}): ReactElement {
  // dim = true mutes the tile while setup is incomplete (the number is irrelevant
  // until the user has actually configured anything). Keeps the dashboard from
  // looking empty/broken on day 0.
  const toneCls = dim
    ? 'bg-[var(--color-page-bg)] text-[var(--color-text-secondary)]'
    : tone === 'good'
      ? 'bg-emerald-50 text-emerald-700'
      : tone === 'warn'
        ? 'bg-amber-50 text-amber-700'
        : 'bg-[var(--color-primary-light)] text-[var(--color-primary)]'
  return (
    <div className={`bg-white rounded-xl border border-[var(--color-border)] px-5 py-4 ${dim ? 'opacity-70' : ''}`}>
      <div className="flex items-center gap-3">
        <div className={`w-9 h-9 rounded-lg flex items-center justify-center ${toneCls}`}>{icon}</div>
        <div className="min-w-0">
          <p className="text-[11px] uppercase tracking-wide text-[var(--color-text-secondary)] font-medium">{label}</p>
          <p className={`text-[18px] font-semibold mt-0.5 ${dim ? 'text-[var(--color-text-secondary)]' : 'text-[var(--color-text-primary)]'}`}>{value}</p>
          {subtitle && (
            <p className="text-[11px] text-[var(--color-text-secondary)] mt-0.5 truncate">{subtitle}</p>
          )}
        </div>
      </div>
    </div>
  )
}

export default function DashboardPage(): ReactElement {
  const { data: preflight } = usePayrollRunPreflight()
  const { data: onboardingStatus } = useOnboardingStatus()
  const setupComplete = onboardingStatus?.setupComplete ?? false

  const { data: period } = useQuery<CurrentPayPeriodDto | null>({
    queryKey: ['current-period'],
    queryFn: async () => {
      const res = await api.get<CurrentPayPeriodDto>('/api/v1/payroll-runs/current-period')
      if (res.status === 204) return null
      return res.data
    },
  })

  const { data: activeEmployees } = useQuery<PagedResult<EmployeeListItemDto>>({
    queryKey: ['employees', 'active-1'],
    queryFn: () => api.get<PagedResult<EmployeeListItemDto>>('/api/v1/employees', {
      params: { page: 1, pageSize: 1, status: 'Active' },
    }).then(r => r.data),
  })

  const { data: history } = useQuery<PagedResult<PayrollHistoryItemDto>>({
    queryKey: ['payroll-history', 'top1'],
    queryFn: () => api.get<PagedResult<PayrollHistoryItemDto>>('/api/v1/payroll-runs/history', {
      params: { page: 1, pageSize: 1 },
    }).then(r => r.data),
  })

  const activeCount = activeEmployees?.total ?? 0
  const lastRun = history?.items?.[0]

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Dashboard</h1>
        <p className="mt-0.5 text-[13px] text-[var(--color-text-secondary)]">Current payroll state at a glance.</p>
      </div>

      {/* Welcome banner — dismissible, shows once per user while setup is incomplete */}
      <WelcomeBanner />

      {/* Setup checklist — disappears once setupComplete is true */}
      <SetupChecklistCard />

      {/* Non-blocking warnings from preflight (e.g. Tax Details / Deductor incomplete).
          Hard blockers gate the Pay Runs nav item and the Process Payroll button — they
          don't appear here. */}
      {preflight && preflight.warnings.length > 0 && (
        <div className="space-y-2">
          {preflight.warnings.map(w => (
            <div key={w.code} className="flex items-start gap-2.5 rounded-lg bg-amber-50 border border-amber-200 px-4 py-3">
              <AlertTriangle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
              <p className="text-[13px] text-amber-800 flex-1">{w.message}</p>
              <Link to={w.fixUrl} className="text-[12px] font-medium text-amber-900 hover:underline">Configure</Link>
            </div>
          ))}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
        <Kpi
          icon={<Users className="w-4 h-4" />}
          label="Active Employees"
          value={setupComplete || activeCount > 0 ? activeCount.toString() : 'Setup needed'}
          subtitle={!setupComplete && activeCount === 0 ? 'Add your first employee' : undefined}
          tone="neutral"
          dim={!setupComplete && activeCount === 0}
        />
        <Kpi
          icon={<Calendar className="w-4 h-4" />}
          label="Current Period"
          value={period?.periodLabel ?? (setupComplete ? '—' : 'Setup needed')}
          subtitle={!setupComplete && !period ? 'Configure Pay Schedule' : undefined}
          tone="neutral"
          dim={!setupComplete && !period}
        />
        <Kpi
          icon={period?.hasOutstandingRun
            ? <AlertCircle className="w-4 h-4" />
            : <CheckCircle2 className="w-4 h-4" />}
          label="Pay Run Status"
          value={
            period?.hasOutstandingRun
              ? (period.outstandingRunStatus ?? 'In Progress')
              : period
                ? 'Not Started'
                : setupComplete
                  ? '—'
                  : 'Setup needed'
          }
          tone={period?.hasOutstandingRun ? 'warn' : 'good'}
          // Flip on this tile's own signal (period), not on lastRun.
          // Stays dim only when we have no period data AND setup isn't done.
          dim={!period && !setupComplete}
        />
        <Kpi
          icon={<CheckCircle2 className="w-4 h-4" />}
          label="Last Paid Run"
          value={lastRun ? lastRun.periodLabel : (setupComplete ? '—' : 'No runs yet')}
          tone="neutral"
          dim={!setupComplete && !lastRun}
        />
      </div>

      {period && !period.hasOutstandingRun && (
        <Link
          to="/pay-runs"
          className="block bg-white rounded-xl border border-[var(--color-border)] px-5 py-4 hover:border-[var(--color-border-strong)] transition-colors"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[14px] font-semibold text-[var(--color-text-primary)]">
                Process pay run for {period.periodLabel}
              </p>
              <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
                {activeCount} active employee{activeCount === 1 ? '' : 's'} ready to be paid.
              </p>
            </div>
            <ChevronRight className="w-4 h-4 text-[var(--color-text-secondary)]" />
          </div>
        </Link>
      )}

      {lastRun && (
        <Link
          to={`/pay-runs/${lastRun.id}`}
          className="block bg-white rounded-xl border border-[var(--color-border)] px-5 py-4 hover:border-[var(--color-border-strong)] transition-colors"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[14px] font-semibold text-[var(--color-text-primary)]">
                Last paid run — {lastRun.periodLabel}
              </p>
              <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
                {lastRun.employeeCount} employee{lastRun.employeeCount === 1 ? '' : 's'} · Total net pay {formatINR(lastRun.totalNetPay)}
              </p>
            </div>
            <ChevronRight className="w-4 h-4 text-[var(--color-text-secondary)]" />
          </div>
        </Link>
      )}

    </div>
  )
}
