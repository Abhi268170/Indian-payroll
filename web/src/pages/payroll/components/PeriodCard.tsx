import { Calendar, Users, AlertTriangle } from 'lucide-react'
import type { CurrentPayPeriodDto } from '@/types/api'

interface PeriodCardProps {
  period: CurrentPayPeriodDto
  onProcess: () => void
  processing: boolean
}

export default function PeriodCard({ period, onProcess, processing }: PeriodCardProps): React.ReactElement {
  const payDayPassed = period.payDay !== null && new Date(period.payDay) < new Date()

  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
      <div className="px-6 py-5 border-b border-[var(--color-border)]">
        <h2 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Process Pay Run for {period.periodLabel}
        </h2>
        <p className="mt-0.5 text-[13px] text-[var(--color-text-secondary)]">
          Review employee data and initiate the payroll cycle.
        </p>
      </div>

      {payDayPassed && (
        <div className="mx-6 mt-4 flex items-start gap-2.5 rounded-lg bg-amber-50 border border-amber-200 px-4 py-3">
          <AlertTriangle className="w-4 h-4 text-amber-600 mt-0.5 flex-shrink-0" />
          <p className="text-[13px] text-amber-700">
            The pay day for {period.periodLabel} has passed. Consider initiating the pay run immediately.
          </p>
        </div>
      )}

      <div className="px-6 py-5 grid grid-cols-3 gap-4">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg bg-[var(--color-primary-light)] flex items-center justify-center">
            <Calendar className="w-4 h-4 text-[var(--color-primary)]" />
          </div>
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">Pay Period</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)] mt-0.5">{period.periodLabel}</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg bg-[var(--color-primary-light)] flex items-center justify-center">
            <Calendar className="w-4 h-4 text-[var(--color-primary)]" />
          </div>
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">Pay Day</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)] mt-0.5">
              {period.payDay ? new Date(period.payDay).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : 'Not set'}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg bg-[var(--color-primary-light)] flex items-center justify-center">
            <Users className="w-4 h-4 text-[var(--color-primary)]" />
          </div>
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">Employees</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)] mt-0.5">{period.activeEmployeeCount} active</p>
          </div>
        </div>
      </div>

      <div className="px-6 pb-5">
        <button
          onClick={onProcess}
          disabled={processing}
          className="inline-flex items-center h-9 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
        >
          {processing ? 'Initiating…' : 'Process Payroll'}
        </button>
      </div>
    </div>
  )
}
