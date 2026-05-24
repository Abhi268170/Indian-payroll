import { useState, type ReactElement } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Info } from 'lucide-react'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import type { SalaryComponentSummary } from '../SalaryComponentsPage'

export default function AddCorrectionModal({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}): ReactElement {
  const [correctionName, setCorrectionName] = useState('')
  const [selectedEarningId, setSelectedEarningId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: earnings = [], isLoading } = useQuery<SalaryComponentSummary[]>({
    queryKey: ['active-earnings'],
    queryFn: async () => {
      const res = await api.get<SalaryComponentSummary[]>('/api/v1/salary-components/active-earnings')
      return res.data
    },
  })

  const selectedEarning = earnings.find(e => e.id === selectedEarningId)

  const mutation = useMutation({
    mutationFn: () =>
      api.post('/api/v1/salary-components/corrections', {
        correctionName,
        forCorrectionOfComponentId: selectedEarningId,
      }),
    onSuccess: onAdded,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to create correction')
    },
  })

  return (
    <Modal
      title="Add Correction"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          <Button
            type="button"
            variant="primary"
            size="sm"
            loading={mutation.isPending}
            disabled={!selectedEarningId || !correctionName}
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
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
            Create correction for *
          </label>
          {isLoading ? (
            <div className="flex items-center gap-2 py-2"><Spinner /></div>
          ) : (
            <select
              className={inputCls}
              value={selectedEarningId}
              onChange={e => {
                setSelectedEarningId(e.target.value)
                const earning = earnings.find(ea => ea.id === e.target.value)
                if (earning && !correctionName) {
                  setCorrectionName(`${earning.name} Correction`)
                }
              }}
            >
              <option value="">Select an earning component</option>
              {earnings.map(e => (
                <option key={e.id} value={e.id}>{e.name}</option>
              ))}
            </select>
          )}
        </div>

        {selectedEarning && (
          <div className="flex items-start gap-2 bg-blue-50 border border-blue-200 rounded-lg px-3 py-2.5">
            <Info className="w-4 h-4 text-blue-500 mt-0.5 flex-shrink-0" />
            <p className="text-[12px] text-blue-800">
              This correction component will have the same configuration as{' '}
              <strong>{selectedEarning.name}</strong>. Use it to make one-time
              corrections to this earning in a pay run.
            </p>
          </div>
        )}

        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
            Correction Name *
          </label>
          <input
            className={inputCls}
            value={correctionName}
            onChange={e => { setCorrectionName(e.target.value) }}
            placeholder="e.g. Basic Salary Correction"
          />
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
