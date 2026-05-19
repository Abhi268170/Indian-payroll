import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'

interface RejectApprovalDialogProps {
  runId: string
  onClose: () => void
}

export default function RejectApprovalDialog({ runId, onClose }: RejectApprovalDialogProps): React.ReactElement {
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')

  const rejectMutation = useMutation({
    mutationFn: () => api.post(`/api/v1/payroll-runs/${runId}/reject-approval`, { reason }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
      onClose()
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[420px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Reject Approval</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">The payroll run will be moved back to Draft status.</p>
        </div>

        <div className="px-6 py-5">
          <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1.5">
            Reason <span className="font-normal">(optional)</span>
          </label>
          <textarea
            value={reason}
            onChange={e => { setReason(e.target.value) }}
            rows={3}
            placeholder="Enter reason for rejection..."
            className="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] text-[13px] resize-none focus:outline-none focus:border-[var(--color-primary)]"
          />
          {rejectMutation.isError && (
            <p className="mt-2 text-[12px] text-red-600">Failed to reject approval. Please try again.</p>
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
            onClick={() => { rejectMutation.mutate() }}
            disabled={rejectMutation.isPending}
            className="h-8 px-4 rounded-lg bg-red-600 text-white text-[13px] font-medium hover:bg-red-700 disabled:opacity-60"
          >
            {rejectMutation.isPending ? 'Rejecting…' : 'Reject'}
          </button>
        </div>
      </div>
    </div>
  )
}
