import { useState, type ReactElement } from 'react'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'

const EARNING_TYPES = [
  { value: 'Basic', label: 'Basic' },
  { value: 'HouseRentAllowance', label: 'House Rent Allowance' },
  { value: 'ConveyanceAllowance', label: 'Conveyance Allowance' },
  { value: 'MedicalAllowance', label: 'Medical Allowance' },
  { value: 'SpecialAllowance', label: 'Special Allowance' },
  { value: 'LeaveTravelAllowance', label: 'Leave Travel Allowance' },
  { value: 'ChildrenEducationAllowance', label: 'Children Education Allowance' },
  { value: 'ChildrenHostelAllowance', label: 'Children Hostel Allowance' },
  { value: 'OverTime', label: 'Overtime (Variable)' },
  { value: 'OvertimeFlat', label: 'Overtime (Flat)' },
  { value: 'Bonus', label: 'Bonus' },
  { value: 'PerformanceBonus', label: 'Performance Bonus' },
  { value: 'FixedBonus', label: 'Fixed Bonus' },
  { value: 'CommissionOnSales', label: 'Commission on Sales' },
  { value: 'AdvanceSalary', label: 'Advance Salary' },
  { value: 'Gratuity', label: 'Gratuity' },
  { value: 'ExGratia', label: 'Ex-Gratia' },
  { value: 'NightShiftAllowance', label: 'Night Shift Allowance' },
  { value: 'CityCompensatoryAllowance', label: 'City Compensatory Allowance' },
  { value: 'DearnesAllowance', label: 'Dearness Allowance' },
  { value: 'UniformAllowance', label: 'Uniform Allowance' },
  { value: 'ToolAllowance', label: 'Tool Allowance' },
  { value: 'WashingAllowance', label: 'Washing Allowance' },
  { value: 'MobileAllowance', label: 'Mobile Allowance' },
  { value: 'InternetAllowance', label: 'Internet Allowance' },
  { value: 'FoodAllowance', label: 'Food Allowance' },
  { value: 'BooksPeriodicals', label: 'Books & Periodicals' },
  { value: 'HighAltitudeAllowance', label: 'High Altitude Allowance' },
  { value: 'BorderRemoteAllowance', label: 'Border/Remote Area Allowance' },
  { value: 'ArrearsEarning', label: 'Arrears Earning' },
  { value: 'NotInList', label: 'Not In List' },
  { value: 'Other', label: 'Other' },
]

export default function AddEarningModal({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}): ReactElement {
  const [name, setName] = useState('')
  const [nameInPayslip, setNameInPayslip] = useState('')
  const [earningType, setEarningType] = useState('Basic')
  const [payType, setPayType] = useState<'Monthly' | 'FlatAmount'>('Monthly')
  const [formulaType, setFormulaType] = useState<'Fixed' | 'PercentOfBasic' | 'PercentOfCTC' | 'PercentOfGross'>('PercentOfCTC')
  const [fixedAmount, setFixedAmount] = useState('')
  const [percentage, setPercentage] = useState('')
  const [isTaxable, setIsTaxable] = useState(true)
  const [considerForEpf, setConsiderForEpf] = useState(false)
  const [epfInclusionRule, setEpfInclusionRule] = useState<'Always' | 'OnlyWhenPfWageBelowLimit'>('Always')
  const [considerForEsi, setConsiderForEsi] = useState(false)
  const [calculateOnProRata, setCalculateOnProRata] = useState(true)
  const [showInPayslip, setShowInPayslip] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      api.post('/api/v1/salary-components/earnings', {
        name, nameInPayslip: nameInPayslip || name,
        earningType, payType, formulaType,
        fixedAmount: formulaType === 'Fixed' ? parseFloat(fixedAmount) : null,
        percentage: formulaType !== 'Fixed' ? parseFloat(percentage) : null,
        isTaxable, considerForEpf, epfInclusionRule,
        considerForEsi, calculateOnProRata, showInPayslip,
      }),
    onSuccess: onAdded,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to create earning')
    },
  })

  return (
    <Modal
      title="Add Earning"
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
      <div className="space-y-4 max-h-[60vh] overflow-y-auto pr-1">
        {error && (
          <p className="text-[12px] text-[var(--color-error)] bg-red-50 px-3 py-2 rounded-lg">{error}</p>
        )}

        <Field label="Earning Name *">
          <input
            className={inputCls}
            value={name}
            onChange={e => {
              setName(e.target.value)
              if (!nameInPayslip) setNameInPayslip(e.target.value)
            }}
            placeholder="e.g. Basic Salary"
          />
        </Field>

        <Field label="Name in Payslip">
          <input className={inputCls} value={nameInPayslip} onChange={e => { setNameInPayslip(e.target.value) }} />
        </Field>

        <Field label="Earning Type *">
          <select className={inputCls} value={earningType} onChange={e => { setEarningType(e.target.value) }}>
            {EARNING_TYPES.map(t => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </Field>

        <Field label="Pay Type">
          <div className="flex gap-4">
            {(['Monthly', 'FlatAmount'] as const).map(v => (
              <label key={v} className="flex items-center gap-2 cursor-pointer text-[13px]">
                <input type="radio" checked={payType === v} onChange={() => { setPayType(v) }} className="accent-[var(--color-primary)]" />
                {v === 'Monthly' ? 'Monthly' : 'Flat Amount'}
              </label>
            ))}
          </div>
        </Field>

        <Field label="Calculation Type">
          <select className={inputCls} value={formulaType} onChange={e => { setFormulaType(e.target.value as typeof formulaType) }}>
            <option value="Fixed">Fixed Amount</option>
            <option value="PercentOfBasic">% of Basic</option>
            <option value="PercentOfGross">% of Gross</option>
            <option value="PercentOfCTC">% of CTC</option>
          </select>
        </Field>

        {formulaType === 'Fixed' ? (
          <Field label="Amount (₹/month)">
            <input type="number" className={inputCls} value={fixedAmount} onChange={e => { setFixedAmount(e.target.value) }} placeholder="0.00" />
          </Field>
        ) : (
          <Field label="Percentage (%)">
            <input type="number" className={inputCls} value={percentage} onChange={e => { setPercentage(e.target.value) }} min={0} max={100} placeholder="0.00" />
          </Field>
        )}

        <div className="space-y-2">
          <Toggle label="Taxable" checked={isTaxable} onChange={setIsTaxable} />
          <Toggle label="Consider for EPF" checked={considerForEpf} onChange={setConsiderForEpf} />
          {considerForEpf && (
            <div className="ml-6 space-y-1.5">
              {(['Always', 'OnlyWhenPfWageBelowLimit'] as const).map(v => (
                <label key={v} className="flex items-center gap-2 cursor-pointer text-[12px] text-[var(--color-text-secondary)]">
                  <input type="radio" checked={epfInclusionRule === v} onChange={() => { setEpfInclusionRule(v) }} className="accent-[var(--color-primary)]" />
                  {v === 'Always' ? 'Always' : 'Only when PF wage < ₹15,000'}
                </label>
              ))}
            </div>
          )}
          <Toggle label="Consider for ESI" checked={considerForEsi} onChange={setConsiderForEsi} />
          <Toggle label="Pro-rata on attendance" checked={calculateOnProRata} onChange={setCalculateOnProRata} />
          <Toggle label="Show in payslip" checked={showInPayslip} onChange={setShowInPayslip} />
        </div>
      </div>
    </Modal>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }): ReactElement {
  return (
    <div>
      <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">{label}</label>
      {children}
    </div>
  )
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }): ReactElement {
  return (
    <label className="flex items-center gap-2.5 cursor-pointer">
      <input type="checkbox" checked={checked} onChange={e => { onChange(e.target.checked) }} className="accent-[var(--color-primary)]" />
      <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
    </label>
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
