import { useState, type ReactElement } from 'react'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'

export default function AddDeductionModal({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}): ReactElement {
  const [name, setName] = useState('')
  const [nameInPayslip, setNameInPayslip] = useState('')
  const [frequency, setFrequency] = useState<'EveryMonth' | 'OnceAYear'>('EveryMonth')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      api.post('/api/v1/salary-components/deductions', {
        name,
        nameInPayslip: nameInPayslip || name,
        deductionFrequency: frequency,
      }),
    onSuccess: onAdded,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to create deduction')
    },
  })

  return (
    <Modal
      title="Add Deduction"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          <Button
            type="button"
            variant="primary"
            size="sm"
            loading={mutation.isPending}
            onClick={() => { mutation.mutate() }}
          >
            Create
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {error && (
          <p className="text-[12px] text-[var(--color-error)] bg-red-50 px-3 py-2 rounded-lg">{error}</p>
        )}
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Deduction Name *</label>
          <input
            className={inputCls}
            value={name}
            onChange={e => {
              setName(e.target.value)
              if (!nameInPayslip) setNameInPayslip(e.target.value)
            }}
            placeholder="e.g. Loan Recovery"
          />
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Name in Payslip</label>
          <input className={inputCls} value={nameInPayslip} onChange={e => { setNameInPayslip(e.target.value) }} />
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-2">Deduction Frequency</label>
          <div className="space-y-2">
            {([
              { value: 'EveryMonth', label: 'Every month' },
              { value: 'OnceAYear', label: 'Once a year' },
            ] as const).map(opt => (
              <label key={opt.value} className="flex items-center gap-2.5 cursor-pointer">
                <input
                  type="radio"
                  checked={frequency === opt.value}
                  onChange={() => { setFrequency(opt.value) }}
                  className="accent-[var(--color-primary)]"
                />
                <span className="text-[13px] text-[var(--color-text-primary)]">{opt.label}</span>
              </label>
            ))}
          </div>
        </div>
      </div>
    </Modal>
  )
}

const inputCls = 'w-full h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white'

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
