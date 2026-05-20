import { useState } from 'react'
import { Download, Eye } from 'lucide-react'
import { formatINR } from '@/lib/format'
import type { PayrunEmployeeDto } from '@/types/api'

interface EmployeeSummaryTableProps {
  employees: PayrunEmployeeDto[]
  runStatus: string
  runId: string
  onOpenVariableInputs: (employeeId: string, employeeName: string) => void
  onSkipEmployee: (employeeId: string, employeeName: string) => void
  onDownloadPayslip: (employeeId: string, employeeName: string) => void
  onReEvaluate: () => void
  isReEvaluating: boolean
}

type FilterMode = 'All' | 'Active' | 'Skipped'

export default function EmployeeSummaryTable({
  employees,
  runStatus,
  onOpenVariableInputs,
  onSkipEmployee,
  onDownloadPayslip,
  onReEvaluate,
  isReEvaluating,
}: EmployeeSummaryTableProps): React.ReactElement {
  const [filter, setFilter] = useState<FilterMode>('All')

  const visible = employees.filter(e => {
    if (filter === 'Active') return e.status === 'Active'
    if (filter === 'Skipped') return e.status === 'Skipped'
    return true
  })

  const isDraft = runStatus === 'Draft'
  const isPaidOrApproved = runStatus === 'Approved' || runStatus === 'Paid'
  const hasOnboardingBlocked = employees.some(
    e => e.status === 'Skipped' && e.skipReason?.startsWith('Onboarding incomplete')
  )

  return (
    <div>
      {/* Filter tabs */}
      <div className="flex items-center gap-1 mb-3">
        {(['All', 'Active', 'Skipped'] as FilterMode[]).map(f => (
          <button
            key={f}
            onClick={() => { setFilter(f) }}
            className={`h-7 px-3 rounded-lg text-[12px] font-medium transition-colors ${
              filter === f
                ? 'bg-[var(--color-primary)] text-white'
                : 'bg-[var(--color-page-bg)] text-[var(--color-text-secondary)] border border-[var(--color-border)] hover:border-[var(--color-primary)]'
            }`}
          >
            {f}
            <span className="ml-1.5 opacity-70">
              {String(f === 'All' ? employees.length : employees.filter(e => e.status === f).length)}
            </span>
          </button>
        ))}
        {isDraft && hasOnboardingBlocked && (
          <button
            onClick={onReEvaluate}
            disabled={isReEvaluating}
            className="ml-auto h-7 px-3 rounded-lg text-[12px] font-medium border border-amber-400 text-amber-700 bg-amber-50 hover:bg-amber-100 disabled:opacity-50 transition-colors"
          >
            {isReEvaluating ? 'Re-evaluating…' : 'Re-evaluate Skipped'}
          </button>
        )}
      </div>

      <div className="rounded-xl border border-[var(--color-border)] overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
              <th className="px-4 py-2.5 text-left text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Employee</th>
              {isDraft && (
                <>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Gross Pay</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Deductions</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Taxes</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Net Pay</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">LOP</th>
                </>
              )}
              {isPaidOrApproved && (
                <>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Net Pay</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">TDS</th>
                  <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">PF</th>
                </>
              )}
              <th className="px-4 py-2.5 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--color-border)]">
            {visible.map(emp => (
              <tr
                key={emp.employeeId}
                className={`hover:bg-[var(--color-page-bg)] transition-colors ${emp.status === 'Skipped' ? 'opacity-60' : ''}`}
              >
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2.5">
                    <div className="w-7 h-7 rounded-full bg-[var(--color-primary)]/10 flex items-center justify-center flex-shrink-0">
                      <span className="text-[10px] font-semibold text-[var(--color-primary)]">
                        {emp.employeeName.slice(0, 2).toUpperCase()}
                      </span>
                    </div>
                    <div>
                      <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{emp.employeeName}</p>
                      <p className="text-[11px] text-[var(--color-text-secondary)]">{emp.employeeCode} · {emp.designation}</p>
                    </div>
                  </div>
                  {emp.status === 'Skipped' && emp.skipReason && (
                    <p className="mt-1 ml-9 text-[11px] text-amber-600">{emp.skipReason}</p>
                  )}
                </td>

                {isDraft && (
                  <>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-primary)]">{formatINR(emp.grossPay)}</td>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-secondary)]">{formatINR(emp.employeePf)}</td>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-secondary)]">{formatINR(emp.tdsOverrideAmount ?? emp.tdsAmount)}</td>
                    <td className="px-4 py-3 text-right text-[13px] font-semibold text-[var(--color-text-primary)]">{formatINR(emp.netPay)}</td>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-secondary)]">{emp.lopDays > 0 ? `${String(emp.lopDays)}d` : '—'}</td>
                  </>
                )}

                {isPaidOrApproved && (
                  <>
                    <td className="px-4 py-3 text-right text-[13px] font-semibold text-[var(--color-text-primary)]">{formatINR(emp.netPay)}</td>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-secondary)]">{formatINR(emp.tdsOverrideAmount ?? emp.tdsAmount)}</td>
                    <td className="px-4 py-3 text-right text-[13px] text-[var(--color-text-secondary)]">{formatINR(emp.employeePf)}</td>
                  </>
                )}

                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1.5">
                    {isDraft && emp.status === 'Active' && (
                      <>
                        <button
                          onClick={() => { onOpenVariableInputs(emp.employeeId, emp.employeeName) }}
                          className="h-7 px-2.5 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors"
                        >
                          Inputs
                        </button>
                        <button
                          onClick={() => { onSkipEmployee(emp.employeeId, emp.employeeName) }}
                          className="h-7 px-2.5 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors"
                        >
                          Skip
                        </button>
                      </>
                    )}
                    {isPaidOrApproved && (
                      <button
                        onClick={() => { onDownloadPayslip(emp.employeeId, emp.employeeName) }}
                        className="h-7 w-7 flex items-center justify-center rounded-lg border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors"
                        title="Download payslip"
                      >
                        <Download className="w-3.5 h-3.5" />
                      </button>
                    )}
                    {isDraft && emp.status === 'Skipped' && (
                      <button
                        onClick={() => { onOpenVariableInputs(emp.employeeId, emp.employeeName) }}
                        className="h-7 w-7 flex items-center justify-center rounded-lg border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] transition-colors"
                        title="View details"
                      >
                        <Eye className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {visible.length === 0 && (
              <tr>
                <td colSpan={10} className="px-4 py-8 text-center text-[13px] text-[var(--color-text-secondary)]">
                  No employees match this filter.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
