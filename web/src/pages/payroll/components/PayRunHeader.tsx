import { MoreVertical, Calendar, Users, DollarSign, FileSpreadsheet } from 'lucide-react'
import { formatINR } from '@/lib/format'
import type { PayrollRunSummaryDto } from '@/types/api'

interface PayRunHeaderProps {
  run: PayrollRunSummaryDto
  onApprove: () => void
  onDelete: () => void
  onRecordPayment: () => void
  onDeletePayment: () => void
  onRejectApproval: () => void
  onBankAdvice: () => void
  showMenu: boolean
  onToggleMenu: () => void
}

function statusBadge(status: string): React.ReactElement {
  const map: Record<string, string> = {
    Draft: 'bg-amber-50 text-amber-700 border border-amber-200',
    Approved: 'bg-blue-50 text-blue-700 border border-blue-200',
    Paid: 'bg-emerald-50 text-emerald-700 border border-emerald-200',
    Deleted: 'bg-gray-100 text-gray-500',
  }
  return (
    <span className={`inline-flex items-center h-6 px-2.5 rounded-md text-[12px] font-medium ${map[status] ?? 'bg-gray-100 text-gray-600'}`}>
      {status}
    </span>
  )
}

export default function PayRunHeader({ run, onApprove, onDelete, onRecordPayment, onDeletePayment, onRejectApproval, onBankAdvice, showMenu, onToggleMenu }: PayRunHeaderProps): React.ReactElement {
  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] px-6 py-5 mb-4">
      {/* Title row */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <h2 className="text-[17px] font-semibold text-[var(--color-text-primary)]">{run.periodLabel}</h2>
          {statusBadge(run.status)}
        </div>

        <div className="flex items-center gap-2 flex-shrink-0">
          {run.status === 'Draft' && (
            <button
              onClick={onApprove}
              className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] transition-colors"
            >
              Approve Payroll
            </button>
          )}
          {run.status === 'Approved' && (
            <>
              <button
                onClick={onBankAdvice}
                className="h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors flex items-center gap-1.5"
              >
                <FileSpreadsheet className="w-3.5 h-3.5" />
                Bank Advice
              </button>
              <button
                onClick={onRecordPayment}
                className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] transition-colors"
              >
                Record Payment
              </button>
              <button
                onClick={onRejectApproval}
                className="h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors"
              >
                Reject
              </button>
            </>
          )}
          {run.status === 'Paid' && (
            <button
              onClick={onBankAdvice}
              className="h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors flex items-center gap-1.5"
            >
              <FileSpreadsheet className="w-3.5 h-3.5" />
              Bank Advice
            </button>
          )}

          {/* Kebab menu */}
          <div className="relative">
            <button
              onClick={onToggleMenu}
              className="w-8 h-8 flex items-center justify-center rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-page-bg)] transition-colors"
            >
              <MoreVertical className="w-4 h-4 text-[var(--color-text-secondary)]" />
            </button>
            {showMenu && (
              <div className="absolute right-0 top-9 z-50 w-44 bg-white rounded-lg border border-[var(--color-border)] shadow-lg py-1">
                {run.status === 'Paid' && (
                  <button
                    onClick={onDeletePayment}
                    className="w-full text-left px-3 py-2 text-[13px] text-[var(--color-text-primary)] hover:bg-[var(--color-page-bg)]"
                  >
                    Delete Recorded Payment
                  </button>
                )}
                {run.status === 'Draft' && (
                  <button
                    onClick={onDelete}
                    className="w-full text-left px-3 py-2 text-[13px] text-red-600 hover:bg-red-50"
                  >
                    Delete Pay Run
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Info strip */}
      <div className="mt-4 grid grid-cols-4 gap-4">
        <div className="flex items-center gap-2.5">
          <Calendar className="w-4 h-4 text-[var(--color-text-secondary)]" />
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)]">Pay Day</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)]">
              {run.payDay ? new Date(run.payDay).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2.5">
          <Users className="w-4 h-4 text-[var(--color-text-secondary)]" />
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)]">Employees</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{run.employeeCount}</p>
          </div>
        </div>
        <div className="flex items-center gap-2.5">
          <DollarSign className="w-4 h-4 text-[var(--color-text-secondary)]" />
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)]">Net Pay</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{formatINR(run.totalNetPay)}</p>
          </div>
        </div>
        <div className="flex items-center gap-2.5">
          <DollarSign className="w-4 h-4 text-[var(--color-text-secondary)]" />
          <div>
            <p className="text-[11px] text-[var(--color-text-secondary)]">Payroll Cost</p>
            <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{formatINR(run.payrollCost)}</p>
          </div>
        </div>
      </div>
    </div>
  )
}
