import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle } from 'lucide-react'
import { api } from '@/lib/api'

interface SkipEmployeeDialogProps {
  runId: string
  employeeId: string
  employeeName: string
  periodLabel: string
  onClose: () => void
}

export default function SkipEmployeeDialog({ runId, employeeId, employeeName, periodLabel, onClose }: SkipEmployeeDialogProps): React.ReactElement {
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')

  const skipMutation = useMutation({
    mutationFn: () => api.post(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/skip`, { reason }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
      void queryClient.invalidateQueries({ queryKey: ['pending-tasks', runId] })
      onClose()
    },
  })

  const canSubmit = reason.trim().length > 0

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[420px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Skip Employee</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{employeeName} · {periodLabel}</p>
        </div>

        <div className="px-6 py-5 space-y-4">
          <div className="flex items-start gap-2.5 rounded-lg bg-amber-50 border border-amber-200 px-4 py-3">
            <AlertTriangle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
            <p className="text-[12px] text-amber-700">
              This employee will be excluded from the current payroll run. Their salary will not be processed for {periodLabel}. You can undo this before the payroll is approved.
            </p>
          </div>

          <div>
            <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1.5">
              Reason <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={e => { setReason(e.target.value) }}
              rows={3}
              placeholder="Enter reason for skipping this employee..."
              className="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] text-[13px] resize-none focus:outline-none focus:border-[var(--color-primary)]"
            />
          </div>

          {skipMutation.isError && (
            <p className="text-[12px] text-red-600">Failed to skip employee. Please try again.</p>
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
            onClick={() => { skipMutation.mutate() }}
            disabled={!canSubmit || skipMutation.isPending}
            className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium disabled:opacity-60"
          >
            {skipMutation.isPending ? 'Skipping…' : 'Skip Employee'}
          </button>
        </div>
      </div>
    </div>
  )
}
