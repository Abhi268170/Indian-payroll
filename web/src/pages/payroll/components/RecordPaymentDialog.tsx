import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { PayrollRunSummaryDto } from '@/types/api'

interface RecordPaymentDialogProps {
  run: PayrollRunSummaryDto
  onClose: () => void
}

function defaultPaymentDate(run: PayrollRunSummaryDto): string {
  if (run.payDay) return run.payDay.slice(0, 10)
  return new Date().toISOString().slice(0, 10)
}

export default function RecordPaymentDialog({ run, onClose }: RecordPaymentDialogProps): React.ReactElement {
  const queryClient = useQueryClient()
  const [paymentDate, setPaymentDate] = useState(defaultPaymentDate(run))
  const [reference, setReference] = useState('')
  const [notifyEmployees, setNotifyEmployees] = useState(true)

  const recordMutation = useMutation({
    mutationFn: () =>
      api.post(`/api/v1/payroll-runs/${run.id}/record-payment`, {
        paymentDate,
        paymentMode: 'BankTransfer',
        reference: reference || null,
        notifyEmployees,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payroll-run', run.id] })
      void queryClient.invalidateQueries({ queryKey: ['run-employees', run.id] })
      onClose()
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[460px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Record Payment</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{run.periodLabel}</p>
        </div>

        <div className="px-6 py-5 space-y-4">
          {/* Payment summary */}
          <div className="rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] px-4 py-3 grid grid-cols-3 gap-3">
            <div>
              <p className="text-[11px] text-[var(--color-text-secondary)]">Employees</p>
              <p className="text-[13px] font-semibold text-[var(--color-text-primary)] mt-0.5">{run.employeeCount}</p>
            </div>
            <div>
              <p className="text-[11px] text-[var(--color-text-secondary)]">Net Pay</p>
              <p className="text-[13px] font-semibold text-[var(--color-text-primary)] mt-0.5">{formatINR(run.totalNetPay)}</p>
            </div>
            <div>
              <p className="text-[11px] text-[var(--color-text-secondary)]">Mode</p>
              <p className="text-[13px] font-semibold text-[var(--color-text-primary)] mt-0.5">Bank Transfer</p>
            </div>
          </div>

          <div>
            <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1.5">Payment Date</label>
            <input
              type="date"
              value={paymentDate}
              onChange={e => { setPaymentDate(e.target.value) }}
              className="w-full h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
            />
          </div>

          <div>
            <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1.5">
              Reference Number <span className="font-normal">(optional)</span>
            </label>
            <input
              type="text"
              value={reference}
              onChange={e => { setReference(e.target.value) }}
              placeholder="e.g. UTR / NEFT reference"
              className="w-full h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
            />
          </div>

          <label className="flex items-center gap-2.5 cursor-pointer">
            <input
              type="checkbox"
              checked={notifyEmployees}
              onChange={e => { setNotifyEmployees(e.target.checked) }}
              className="w-4 h-4 rounded border-[var(--color-border)] accent-[var(--color-primary)]"
            />
            <span className="text-[13px] text-[var(--color-text-primary)]">Send payslip notification to employees</span>
          </label>

          {recordMutation.isError && (
            <p className="text-[12px] text-red-600">Failed to record payment. Please try again.</p>
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
            onClick={() => { recordMutation.mutate() }}
            disabled={!paymentDate || recordMutation.isPending}
            className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-60"
          >
            {recordMutation.isPending ? 'Recording…' : 'Record Payment'}
          </button>
        </div>
      </div>
    </div>
  )
}
