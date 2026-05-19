import { useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { PayrollRunSummaryDto } from '@/types/api'

interface ApprovePayrollDialogProps {
  run: PayrollRunSummaryDto
  onClose: () => void
}

export default function ApprovePayrollDialog({ run, onClose }: ApprovePayrollDialogProps): React.ReactElement {
  const queryClient = useQueryClient()

  const approveMutation = useMutation({
    mutationFn: () => api.post(`/api/v1/payroll-runs/${run.id}/approve`, {}),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payroll-run', run.id] })
      void queryClient.invalidateQueries({ queryKey: ['run-employees', run.id] })
      void queryClient.invalidateQueries({ queryKey: ['pending-tasks', run.id] })
      onClose()
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[480px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Approve Payroll</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{run.periodLabel}</p>
        </div>

        <div className="px-6 py-5 space-y-4">
          {/* Summary strip */}
          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] px-4 py-3">
              <p className="text-[11px] text-[var(--color-text-secondary)]">Employees</p>
              <p className="text-[15px] font-semibold text-[var(--color-text-primary)] mt-0.5">{run.employeeCount}</p>
            </div>
            <div className="rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] px-4 py-3">
              <p className="text-[11px] text-[var(--color-text-secondary)]">Total Net Pay</p>
              <p className="text-[15px] font-semibold text-[var(--color-text-primary)] mt-0.5">{formatINR(run.totalNetPay)}</p>
            </div>
          </div>

          {/* Warning bullets */}
          <div className="rounded-lg bg-amber-50 border border-amber-200 px-4 py-3 space-y-2">
            <div className="flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
              <p className="text-[13px] text-amber-800 font-medium">Please review before approving</p>
            </div>
            <ul className="ml-6 space-y-1.5 list-disc">
              <li className="text-[12px] text-amber-700">Expense reimbursements added after this point will not be included in this payroll.</li>
              <li className="text-[12px] text-amber-700">Employee Income Tax (IT) declarations will be locked and cannot be changed for this payroll period.</li>
              <li className="text-[12px] text-amber-700">Payroll will be marked as Approved and cannot be reverted without rejecting the approval.</li>
            </ul>
          </div>

          {approveMutation.isError && (
            <p className="text-[13px] text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-2.5">
              Approval failed. Ensure all blocking issues are resolved before approving.
            </p>
          )}
        </div>

        <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            onClick={onClose}
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
          >
            Cancel
          </button>
          <button
            onClick={() => { approveMutation.mutate() }}
            disabled={approveMutation.isPending}
            className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-60"
          >
            {approveMutation.isPending ? 'Approving…' : 'Submit and Approve'}
          </button>
        </div>
      </div>
    </div>
  )
}
