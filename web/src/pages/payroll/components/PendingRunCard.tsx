import { Link } from 'react-router-dom'
import { Info } from 'lucide-react'
import { formatINR } from '@/lib/format'
import type { PendingRunCardDto } from '@/types/api'

interface Props {
  run: PendingRunCardDto
}

const TYPE_LABEL: Record<string, string> = {
  Regular: 'Regular Payroll',
  FinalSettlement: 'Final Settlement Payroll',
  BulkFinalSettlement: 'Bulk Final Settlement Payroll',
}

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function statusBadge(status: string): React.ReactElement {
  const cls = status === 'Draft'
    ? 'bg-gray-100 text-gray-600'
    : 'bg-amber-50 text-amber-700'
  return (
    <span className={`inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium ${cls}`}>
      {status}
    </span>
  )
}

function targetHref(run: PendingRunCardDto): string {
  if (run.type === 'FinalSettlement' && run.status === 'Draft') {
    return `/pay-runs/${run.id}/fnf`
  }
  return `/pay-runs/${run.id}`
}

export default function PendingRunCard({ run }: Props): React.ReactElement {
  const typeLabel = TYPE_LABEL[run.type] ?? run.type
  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
      <div className="px-6 py-5 border-b border-[var(--color-border)] flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">{typeLabel}</h3>
          {statusBadge(run.status)}
        </div>
        <Link
          to={targetHref(run)}
          className="inline-flex items-center h-8 px-3 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] transition-colors"
        >
          View Details
        </Link>
      </div>

      <div className="px-6 py-5 grid grid-cols-3 gap-4">
        <div>
          <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">Employees' Net Pay</p>
          <p className="text-[14px] font-semibold text-[var(--color-text-primary)] mt-1">{formatINR(run.totalNetPay)}</p>
        </div>
        <div>
          <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">Payment Date</p>
          <p className="text-[14px] font-medium text-[var(--color-text-primary)] mt-1">{formatDate(run.payDay)}</p>
        </div>
        <div>
          <p className="text-[11px] text-[var(--color-text-secondary)] uppercase tracking-wide font-medium">
            {run.type === 'FinalSettlement' ? 'Employee' : 'No. of Employees'}
          </p>
          <p className="text-[14px] font-medium text-[var(--color-text-primary)] mt-1">
            {run.type === 'FinalSettlement' ? (run.primaryEmployeeLabel ?? '—') : run.employeeCount}
          </p>
        </div>
      </div>

      {run.status === 'Draft' && run.payDay && (
        <div className="px-6 pb-4">
          <div className="flex items-start gap-2 text-[12px] text-[var(--color-text-secondary)]">
            <Info className="w-3.5 h-3.5 mt-0.5 flex-shrink-0" />
            <span>Please approve this payroll on or before {formatDate(run.payDay)}.</span>
          </div>
        </div>
      )}
    </div>
  )
}
