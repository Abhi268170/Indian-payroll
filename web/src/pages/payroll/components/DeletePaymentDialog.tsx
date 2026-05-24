import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'

interface DeletePaymentDialogProps {
  runId: string
  periodLabel: string
  onClose: () => void
}

export default function DeletePaymentDialog({ runId, periodLabel, onClose }: DeletePaymentDialogProps): React.ReactElement {
  const queryClient = useQueryClient()

  const deleteMutation = useMutation({
    mutationFn: () => api.delete(`/api/v1/payroll-runs/${runId}/payment`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
      void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
      onClose()
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[400px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Delete Recorded Payment</h2>
        </div>

        <div className="px-6 py-5">
          <p className="text-[13px] text-[var(--color-text-primary)]">
            Are you sure you want to delete the recorded payment for <strong>{periodLabel}</strong>?
          </p>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-2">
            The payroll run will be moved back to Approved status. Employee payslips will remain accessible.
          </p>
          {deleteMutation.isError && (
            <p className="mt-3 text-[12px] text-red-600">Failed to delete payment. Please try again.</p>
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
            onClick={() => { deleteMutation.mutate() }}
            disabled={deleteMutation.isPending}
            className="h-8 px-4 rounded-lg bg-red-600 text-white text-[13px] font-medium hover:bg-red-700 disabled:opacity-60"
          >
            {deleteMutation.isPending ? 'Deleting…' : 'Delete Payment'}
          </button>
        </div>
      </div>
    </div>
  )
}
