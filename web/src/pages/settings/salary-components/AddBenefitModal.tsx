import { useState, type ReactElement } from 'react'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'

type BenefitType = 'VPF' | 'NPS' | 'OtherNonTaxable'

export default function AddBenefitModal({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}): ReactElement {
  const [benefitType, setBenefitType] = useState<BenefitType>('VPF')
  const [name, setName] = useState('')
  const [nameInPayslip, setNameInPayslip] = useState('')
  const [benefitPercentage, setBenefitPercentage] = useState('')
  const [isApplicableToAllEmployees, setIsApplicableToAllEmployees] = useState(true)
  const [isNpsGovernmentSector, setIsNpsGovernmentSector] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const defaultName: Record<BenefitType, string> = {
    VPF: 'Voluntary Provident Fund',
    NPS: 'National Pension Scheme',
    OtherNonTaxable: '',
  }

  function handleTypeChange(t: BenefitType): void {
    setBenefitType(t)
    setName(defaultName[t])
    setNameInPayslip(defaultName[t])
  }

  const mutation = useMutation({
    mutationFn: () =>
      api.post('/api/v1/salary-components/benefits', {
        name: name || defaultName[benefitType],
        nameInPayslip: nameInPayslip || name || defaultName[benefitType],
        benefitType,
        benefitPercentage: benefitType === 'VPF' ? parseFloat(benefitPercentage) : null,
        isApplicableToAllEmployees,
        isNpsGovernmentSector: benefitType === 'NPS' ? isNpsGovernmentSector : null,
      }),
    onSuccess: onAdded,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to create benefit')
    },
  })

  return (
    <Modal
      title="Add Benefit"
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
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-2">Benefit Type *</label>
          <div className="space-y-2">
            {([
              { value: 'VPF', label: 'Voluntary Provident Fund (VPF)', desc: 'Additional PF contribution by employee' },
              { value: 'NPS', label: 'National Pension Scheme (NPS)', desc: 'Employer contribution under Sec 80CCD(2)' },
              { value: 'OtherNonTaxable', label: 'Other Non-Taxable Benefit', desc: 'Custom employer benefit' },
            ] as const).map(opt => (
              <label key={opt.value} className="flex items-start gap-2.5 cursor-pointer">
                <input
                  type="radio"
                  checked={benefitType === opt.value}
                  onChange={() => { handleTypeChange(opt.value) }}
                  className="accent-[var(--color-primary)] mt-0.5"
                />
                <div>
                  <span className="text-[13px] text-[var(--color-text-primary)]">{opt.label}</span>
                  <p className="text-[11px] text-[var(--color-text-muted)]">{opt.desc}</p>
                </div>
              </label>
            ))}
          </div>
        </div>

        {benefitType === 'OtherNonTaxable' && (
          <>
            <div>
              <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Name *</label>
              <input className={inputCls} value={name} onChange={e => {
                setName(e.target.value)
                if (!nameInPayslip) setNameInPayslip(e.target.value)
              }} />
            </div>
            <div>
              <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Name in Payslip</label>
              <input className={inputCls} value={nameInPayslip} onChange={e => { setNameInPayslip(e.target.value) }} />
            </div>
          </>
        )}

        {benefitType === 'VPF' && (
          <div>
            <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
              Contribution (% of PF Wage) *
            </label>
            <input
              type="number"
              className={inputCls}
              value={benefitPercentage}
              onChange={e => { setBenefitPercentage(e.target.value) }}
              min={1}
              max={100}
              placeholder="e.g. 12"
            />
          </div>
        )}

        {benefitType === 'NPS' && (
          <div>
            <label className="block text-[12px] text-[var(--color-text-secondary)] mb-2">Sector</label>
            <div className="space-y-2">
              {([
                { value: false, label: 'Private sector (10% of Basic + DA)' },
                { value: true, label: 'Government sector (14% of Basic + DA)' },
              ] as const).map(opt => (
                <label key={String(opt.value)} className="flex items-center gap-2.5 cursor-pointer">
                  <input
                    type="radio"
                    checked={isNpsGovernmentSector === opt.value}
                    onChange={() => { setIsNpsGovernmentSector(opt.value) }}
                    className="accent-[var(--color-primary)]"
                  />
                  <span className="text-[13px] text-[var(--color-text-primary)]">{opt.label}</span>
                </label>
              ))}
            </div>
          </div>
        )}

        <label className="flex items-center gap-2.5 cursor-pointer">
          <input
            type="checkbox"
            checked={isApplicableToAllEmployees}
            onChange={e => { setIsApplicableToAllEmployees(e.target.checked) }}
            className="accent-[var(--color-primary)]"
          />
          <span className="text-[13px] text-[var(--color-text-primary)]">Applicable to all employees</span>
        </label>
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
