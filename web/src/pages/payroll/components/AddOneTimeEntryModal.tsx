import { useState, type ReactElement } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'

interface OneTimeComponent {
  id: string
  name: string
  nameInPayslip: string
  code: string
  isTaxable: boolean | null
  considerForEpf: boolean | null
  considerForEsi: boolean | null
}

interface Props {
  runId: string
  employeeId: string
  category: 'Earning' | 'Deduction'
  onClose: () => void
}

export default function AddOneTimeEntryModal({
  runId,
  employeeId,
  category,
  onClose,
}: Props): ReactElement {
  const qc = useQueryClient()
  const [componentId, setComponentId] = useState('')
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: components = [], isLoading } = useQuery<OneTimeComponent[]>({
    queryKey: ['one-time-components', category],
    queryFn: async () => {
      const res = await api.get<OneTimeComponent[]>(
        `/api/v1/salary-components/one-time?category=${category}`,
      )
      return res.data
    },
  })

  const endpoint = category === 'Earning' ? 'earnings' : 'deductions'

  const mutation = useMutation({
    mutationFn: () =>
      api.post(
        `/api/v1/payroll-runs/${runId}/employees/${employeeId}/${endpoint}`,
        { componentId, amount: parseFloat(amount) },
      ),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['variable-inputs', runId, employeeId] })
      void qc.invalidateQueries({ queryKey: ['run-employees', runId] })
      void qc.invalidateQueries({ queryKey: ['payroll-run', runId] })
      onClose()
    },
    onError: (err: unknown) => {
      setError(extractError(err) ?? `Failed to add one-time ${category.toLowerCase()}`)
    },
  })

  const selected = components.find(c => c.id === componentId)
  const numericAmount = parseFloat(amount)
  const canSubmit = !!componentId && numericAmount > 0 && !mutation.isPending

  return (
    <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-md shadow-xl">
        <div className="flex items-center justify-between px-5 py-4 border-b border-[var(--color-border)]">
          <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
            Add One-time {category}
          </h3>
          <button
            type="button"
            onClick={onClose}
            className="w-7 h-7 flex items-center justify-center rounded hover:bg-gray-100 text-[var(--color-text-secondary)]"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-5 py-4 space-y-4">
          {error != null && (
            <p className="text-[12px] text-[var(--color-error)] bg-red-50 px-3 py-2 rounded-lg">
              {error}
            </p>
          )}

          <div>
            <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
              Component *
            </label>
            <select
              className="w-full h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white"
              value={componentId}
              onChange={e => { setComponentId(e.target.value) }}
              disabled={isLoading}
            >
              <option value="">
                {isLoading
                  ? 'Loading…'
                  : components.length === 0
                    ? `No one-time ${category.toLowerCase()}s configured`
                    : `Select a ${category.toLowerCase()}`}
              </option>
              {components.map(c => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {components.length === 0 && !isLoading && (
              <p className="mt-1.5 text-[11px] text-[var(--color-text-muted)]">
                Create a one-time {category.toLowerCase()} in Settings → Salary Components.
              </p>
            )}
          </div>

          <div>
            <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
              Amount (₹) *
            </label>
            <input
              type="number"
              min={0}
              step="0.01"
              className="w-full h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white"
              value={amount}
              onChange={e => { setAmount(e.target.value) }}
              placeholder="0.00"
            />
            {numericAmount > 0 && (
              <p className="mt-1 text-[11px] text-[var(--color-text-muted)]">
                {formatINR(numericAmount)}
              </p>
            )}
          </div>

          {selected && category === 'Earning' && (
            <div className="flex flex-wrap gap-1.5">
              {selected.isTaxable === true && (
                <span className="text-[10px] font-medium bg-amber-50 text-amber-700 px-1.5 py-0.5 rounded">
                  Taxable
                </span>
              )}
              {selected.considerForEpf === true && (
                <span className="text-[10px] font-medium bg-blue-50 text-blue-700 px-1.5 py-0.5 rounded">
                  Affects PF
                </span>
              )}
              {selected.considerForEsi === true && (
                <span className="text-[10px] font-medium bg-purple-50 text-purple-700 px-1.5 py-0.5 rounded">
                  Affects ESI
                </span>
              )}
            </div>
          )}
        </div>

        <div className="px-5 py-3 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="px-3 h-9 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={!canSubmit}
            onClick={() => { mutation.mutate() }}
            className="px-4 h-9 text-[13px] font-medium bg-[var(--color-primary)] text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:opacity-90"
          >
            {mutation.isPending ? 'Adding…' : 'Add'}
          </button>
        </div>
      </div>
    </div>
  )
}

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
