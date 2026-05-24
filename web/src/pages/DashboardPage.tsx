import { type ReactElement } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Users, Calendar, CheckCircle2, AlertCircle, ChevronRight } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { CurrentPayPeriodDto, EmployeeListItemDto, PayrollHistoryItemDto } from '@/types/api'

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

function Kpi({
  icon, label, value, tone,
}: { icon: ReactElement; label: string; value: string; tone: 'neutral' | 'good' | 'warn' }): ReactElement {
  const toneCls = tone === 'good'
    ? 'bg-emerald-50 text-emerald-700'
    : tone === 'warn'
      ? 'bg-amber-50 text-amber-700'
      : 'bg-[var(--color-primary-light)] text-[var(--color-primary)]'
  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] px-5 py-4">
      <div className="flex items-center gap-3">
        <div className={`w-9 h-9 rounded-lg flex items-center justify-center ${toneCls}`}>{icon}</div>
        <div>
          <p className="text-[11px] uppercase tracking-wide text-[var(--color-text-secondary)] font-medium">{label}</p>
          <p className="text-[18px] font-semibold text-[var(--color-text-primary)] mt-0.5">{value}</p>
        </div>
      </div>
    </div>
  )
}

export default function DashboardPage(): ReactElement {
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

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
        <Kpi
          icon={<Users className="w-4 h-4" />}
          label="Active Employees"
          value={activeCount.toString()}
          tone="neutral"
        />
        <Kpi
          icon={<Calendar className="w-4 h-4" />}
          label="Current Period"
          value={period?.periodLabel ?? '—'}
          tone="neutral"
        />
        <Kpi
          icon={period?.hasOutstandingRun
            ? <AlertCircle className="w-4 h-4" />
            : <CheckCircle2 className="w-4 h-4" />}
          label="Pay Run Status"
          value={period?.hasOutstandingRun ? (period.outstandingRunStatus ?? 'In Progress') : 'Not Started'}
          tone={period?.hasOutstandingRun ? 'warn' : 'good'}
        />
        <Kpi
          icon={<CheckCircle2 className="w-4 h-4" />}
          label="Last Paid Run"
          value={lastRun ? lastRun.periodLabel : '—'}
          tone="neutral"
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
