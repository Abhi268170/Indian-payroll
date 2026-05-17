import { useState, type ReactElement } from 'react'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'

const REIMBURSEMENT_TYPES = [
  { value: 'FuelAndMaintenance', label: 'Fuel & Vehicle Maintenance' },
  { value: 'BooksPeriodicals', label: 'Books & Periodicals' },
  { value: 'FoodCoupons', label: 'Food Coupons' },
  { value: 'GiftVouchers', label: 'Gift Vouchers' },
  { value: 'MobileAndInternet', label: 'Mobile & Internet' },
  { value: 'LeaveTravelAssistance', label: 'Leave Travel Assistance' },
  { value: 'DriverSalary', label: 'Driver Salary' },
  { value: 'HelperAllowance', label: 'Helper Allowance' },
  { value: 'UniformAllowance', label: 'Uniform Allowance' },
  { value: 'ChildrenEducationAllowance', label: 'Children Education Allowance' },
  { value: 'Other', label: 'Other' },
]

export default function AddReimbursementModal({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}): ReactElement {
  const [name, setName] = useState('')
  const [nameInPayslip, setNameInPayslip] = useState('')
  const [reimbursementType, setReimbursementType] = useState('FuelAndMaintenance')
  const [amount, setAmount] = useState('')
  const [unclaimedHandling, setUnclaimedHandling] = useState<'DoNotPay' | 'PayAsTaxable'>('DoNotPay')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      api.post('/api/v1/salary-components/reimbursements', {
        name,
        nameInPayslip: nameInPayslip || name,
        reimbursementType,
        amount: parseFloat(amount),
        unclaimedHandling,
      }),
    onSuccess: onAdded,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to create reimbursement')
    },
  })

  return (
    <Modal
      title="Add Reimbursement"
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
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Reimbursement Type *</label>
          <select className={inputCls} value={reimbursementType} onChange={e => { setReimbursementType(e.target.value) }}>
            {REIMBURSEMENT_TYPES.map(t => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Name *</label>
          <input
            className={inputCls}
            value={name}
            onChange={e => {
              setName(e.target.value)
              if (!nameInPayslip) setNameInPayslip(e.target.value)
            }}
          />
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Name in Payslip</label>
          <input className={inputCls} value={nameInPayslip} onChange={e => { setNameInPayslip(e.target.value) }} />
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Monthly Amount (₹) *</label>
          <input type="number" className={inputCls} value={amount} onChange={e => { setAmount(e.target.value) }} placeholder="0.00" />
        </div>
        <div>
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-2">If unclaimed</label>
          <div className="space-y-2">
            {([
              { value: 'DoNotPay', label: 'Do not pay' },
              { value: 'PayAsTaxable', label: 'Pay as taxable' },
            ] as const).map(opt => (
              <label key={opt.value} className="flex items-center gap-2.5 cursor-pointer">
                <input
                  type="radio"
                  checked={unclaimedHandling === opt.value}
                  onChange={() => { setUnclaimedHandling(opt.value) }}
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
