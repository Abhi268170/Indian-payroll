import { useState, type ReactElement } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'

interface ComponentDetail {
  id: string
  name: string
  nameInPayslip: string
  code: string
  category: string
  isActive: boolean
  isSystemComponent: boolean
  isAssociatedWithEmployee: boolean
  // Earning
  earningType: string | null
  payType: string | null
  formulaType: string | null
  fixedAmount: number | null
  percentage: number | null
  isTaxable: boolean | null
  considerForEpf: boolean | null
  epfInclusionRule: string | null
  considerForEsi: boolean | null
  calculateOnProRata: boolean | null
  showInPayslip: boolean | null
  // Deduction
  deductionFrequency: string | null
  // Reimbursement
  reimbursementType: string | null
  reimbursementAmount: number | null
  unclaimedHandling: string | null
  // Benefit
  benefitType: string | null
  benefitPercentage: number | null
}

export default function EditComponentModal({
  id,
  onClose,
  onSaved,
}: {
  id: string
  onClose: () => void
  onSaved: () => void
}): ReactElement {
  const { data, isLoading } = useQuery<ComponentDetail>({
    queryKey: ['salary-component', id],
    queryFn: async () => {
      const res = await api.get<ComponentDetail>(`/api/v1/salary-components/${id}`)
      return res.data
    },
  })

  return (
    <Modal
      title={data ? `Edit ${data.category}` : 'Edit Component'}
      onClose={onClose}
      footer={null}
    >
      {isLoading || !data ? (
        <div className="flex justify-center py-12"><Spinner /></div>
      ) : (
        <EditForm detail={data} onClose={onClose} onSaved={onSaved} />
      )}
    </Modal>
  )
}

function EditForm({
  detail,
  onClose,
  onSaved,
}: {
  detail: ComponentDetail
  onClose: () => void
  onSaved: () => void
}): ReactElement {
  const locked = detail.isAssociatedWithEmployee

  // Always-editable
  const [name, setName] = useState(detail.name)
  const [nameInPayslip, setNameInPayslip] = useState(detail.nameInPayslip)

  // Earning
  const [formulaType, setFormulaType] = useState(detail.formulaType ?? 'Fixed')
  const [fixedAmount, setFixedAmount] = useState(String(detail.fixedAmount ?? ''))
  const [percentage, setPercentage] = useState(String(detail.percentage ?? ''))
  const [isTaxable, setIsTaxable] = useState(detail.isTaxable ?? true)
  const [considerForEpf, setConsiderForEpf] = useState(detail.considerForEpf ?? false)
  const [epfInclusionRule, setEpfInclusionRule] = useState<'Always' | 'OnlyWhenPfWageBelowLimit'>(
    (detail.epfInclusionRule as 'Always' | 'OnlyWhenPfWageBelowLimit') ?? 'Always',
  )
  const [considerForEsi, setConsiderForEsi] = useState(detail.considerForEsi ?? false)
  const [calculateOnProRata, setCalculateOnProRata] = useState(detail.calculateOnProRata ?? true)
  const [showInPayslip, setShowInPayslip] = useState(detail.showInPayslip ?? true)

  // Deduction
  const [deductionFrequency, setDeductionFrequency] = useState<'EveryMonth' | 'OnceAYear' | 'Quarterly' | 'HalfYearly'>(
    (detail.deductionFrequency as 'EveryMonth' | 'OnceAYear' | 'Quarterly' | 'HalfYearly') ?? 'EveryMonth',
  )

  // Reimbursement
  const [reimbursementAmount, setReimbursementAmount] = useState(String(detail.reimbursementAmount ?? ''))
  const [unclaimedHandling, setUnclaimedHandling] = useState<'DoNotPay' | 'PayAsTaxable'>(
    (detail.unclaimedHandling as 'DoNotPay' | 'PayAsTaxable') ?? 'DoNotPay',
  )

  // Benefit
  const [benefitPercentage, setBenefitPercentage] = useState(String(detail.benefitPercentage ?? ''))

  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => {
      const body: Record<string, unknown> = { name, nameInPayslip }

      if (detail.category === 'Earning') {
        if (!locked) {
          body.formulaType = formulaType
          body.fixedAmount = formulaType === 'Fixed' ? parseFloat(fixedAmount) || 0 : null
          body.percentage = formulaType !== 'Fixed' ? parseFloat(percentage) || 0 : null
          body.isTaxable = isTaxable
          body.considerForEpf = considerForEpf
          body.epfInclusionRule = epfInclusionRule
          body.considerForEsi = considerForEsi
          body.calculateOnProRata = calculateOnProRata
          body.showInPayslip = showInPayslip
        } else if (formulaType === 'Fixed') {
          body.fixedAmount = parseFloat(fixedAmount) || 0
        } else {
          body.formulaType = formulaType
          body.percentage = parseFloat(percentage) || 0
        }
      }

      if (detail.category === 'Deduction') {
        body.deductionFrequency = deductionFrequency
      }

      if (detail.category === 'Reimbursement') {
        body.reimbursementAmount = parseFloat(reimbursementAmount) || 0
        body.unclaimedHandling = unclaimedHandling
      }

      if (detail.category === 'Benefit' && benefitPercentage) {
        body.benefitPercentage = parseFloat(benefitPercentage)
      }

      return api.put(`/api/v1/salary-components/${detail.id}`, body)
    },
    onSuccess: onSaved,
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to save')
    },
  })

  return (
    <div className="space-y-4 max-h-[65vh] overflow-y-auto pr-1">
      {error && (
        <p className="text-[12px] text-[var(--color-error)] bg-red-50 px-3 py-2 rounded-lg">{error}</p>
      )}

      {locked && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 text-[12px] text-amber-800">
          {detail.category === 'Earning'
            ? 'This component is associated with employees. Only the name and amount/percentage can be edited. Amount changes apply to new employees only.'
            : detail.category === 'Deduction'
              ? 'This component is associated with employees. Only the Name in Payslip can be edited.'
              : 'Some fields are locked because this component is associated with employees.'}
        </div>
      )}

      {/* ── Common name fields ───────────────────────────── */}
      <Field label="Name *">
        <input
          className={inputCls}
          value={name}
          onChange={e => { setName(e.target.value) }}
          disabled={detail.category === 'Deduction' && locked}
        />
      </Field>

      <Field label="Name in Payslip">
        <input
          className={inputCls}
          value={nameInPayslip}
          onChange={e => { setNameInPayslip(e.target.value) }}
        />
      </Field>

      {/* ── Earning fields ───────────────────────────────── */}
      {detail.category === 'Earning' && (
        <>
          {detail.earningType && (
            <Field label="Earning Type">
              <input className={`${inputCls} bg-gray-50 text-[var(--color-text-secondary)]`} value={humanise(detail.earningType)} disabled />
            </Field>
          )}

          <Field label="Calculation Type">
            <select
              className={`${inputCls} ${locked ? 'bg-gray-50 text-[var(--color-text-secondary)]' : ''}`}
              value={formulaType}
              onChange={e => { setFormulaType(e.target.value) }}
              disabled={locked}
            >
              <option value="Fixed">Fixed Amount</option>
              <option value="PercentOfBasic">% of Basic</option>
              <option value="PercentOfGross">% of Gross</option>
              <option value="PercentOfCTC">% of CTC</option>
            </select>
          </Field>

          {formulaType === 'Fixed' ? (
            <Field label="Amount (₹/month)">
              <input
                type="number"
                className={inputCls}
                value={fixedAmount}
                onChange={e => { setFixedAmount(e.target.value) }}
                placeholder="0.00"
              />
            </Field>
          ) : (
            <Field label="Percentage (%)">
              <input
                type="number"
                className={inputCls}
                value={percentage}
                onChange={e => { setPercentage(e.target.value) }}
                min={0}
                max={100}
                placeholder="0.00"
              />
            </Field>
          )}

          <div className="space-y-2">
            <Toggle label="Taxable" checked={isTaxable} onChange={setIsTaxable} disabled={locked} />
            <Toggle label="Consider for EPF" checked={considerForEpf} onChange={setConsiderForEpf} disabled={locked} />
            {considerForEpf && !locked && (
              <div className="ml-6 space-y-1.5">
                {(['Always', 'OnlyWhenPfWageBelowLimit'] as const).map(v => (
                  <label key={v} className="flex items-center gap-2 cursor-pointer text-[12px] text-[var(--color-text-secondary)]">
                    <input
                      type="radio"
                      checked={epfInclusionRule === v}
                      onChange={() => { setEpfInclusionRule(v) }}
                      className="accent-[var(--color-primary)]"
                    />
                    {v === 'Always' ? 'Always' : 'Only when PF wage < ₹15,000'}
                  </label>
                ))}
              </div>
            )}
            <Toggle label="Consider for ESI" checked={considerForEsi} onChange={setConsiderForEsi} disabled={locked} />
            <Toggle label="Pro-rata on attendance" checked={calculateOnProRata} onChange={setCalculateOnProRata} disabled={locked} />
            <Toggle label="Show in payslip" checked={showInPayslip} onChange={setShowInPayslip} disabled={locked} />
          </div>
        </>
      )}

      {/* ── Deduction fields ─────────────────────────────── */}
      {detail.category === 'Deduction' && (
        <Field label="Deduction Frequency">
          <div className="space-y-2">
            {([
              { value: 'EveryMonth', label: 'Every month' },
              { value: 'OnceAYear', label: 'Once a year' },
              { value: 'Quarterly', label: 'Quarterly' },
              { value: 'HalfYearly', label: 'Half-Yearly' },
            ] as const).map(opt => (
              <label key={opt.value} className={`flex items-center gap-2.5 ${locked ? 'opacity-50' : 'cursor-pointer'}`}>
                <input
                  type="radio"
                  checked={deductionFrequency === opt.value}
                  onChange={() => { if (!locked) setDeductionFrequency(opt.value) }}
                  disabled={locked}
                  className="accent-[var(--color-primary)]"
                />
                <span className="text-[13px] text-[var(--color-text-primary)]">{opt.label}</span>
              </label>
            ))}
          </div>
        </Field>
      )}

      {/* ── Reimbursement fields ─────────────────────────── */}
      {detail.category === 'Reimbursement' && (
        <>
          {detail.reimbursementType && (
            <Field label="Reimbursement Type">
              <input className={`${inputCls} bg-gray-50 text-[var(--color-text-secondary)]`} value={humanise(detail.reimbursementType)} disabled />
            </Field>
          )}
          <Field label="Monthly Amount (₹)">
            <input
              type="number"
              className={inputCls}
              value={reimbursementAmount}
              onChange={e => { setReimbursementAmount(e.target.value) }}
              placeholder="0.00"
            />
          </Field>
          <Field label="If unclaimed">
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
          </Field>
        </>
      )}

      {/* ── Benefit fields ───────────────────────────────── */}
      {detail.category === 'Benefit' && detail.benefitType === 'VPF' && (
        <Field label="Contribution (% of PF Wage)">
          <input
            type="number"
            className={inputCls}
            value={benefitPercentage}
            onChange={e => { setBenefitPercentage(e.target.value) }}
            min={1}
            max={100}
            placeholder="e.g. 12"
          />
        </Field>
      )}

      <div className="flex justify-end gap-2 pt-2 border-t border-[var(--color-border)]">
        <Button type="button" variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
        <Button
          type="button"
          variant="primary"
          size="sm"
          loading={mutation.isPending}
          onClick={() => { mutation.mutate() }}
        >
          Save
        </Button>
      </div>
    </div>
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

function Toggle({
  label,
  checked,
  onChange,
  disabled = false,
}: {
  label: string
  checked: boolean
  onChange: (v: boolean) => void
  disabled?: boolean
}): ReactElement {
  return (
    <label className={`flex items-center gap-2.5 ${disabled ? 'opacity-50' : 'cursor-pointer'}`}>
      <input
        type="checkbox"
        checked={checked}
        onChange={e => { if (!disabled) onChange(e.target.checked) }}
        disabled={disabled}
        className="accent-[var(--color-primary)]"
      />
      <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
    </label>
  )
}

// camelCase enum value → "Camel Case" label
function humanise(val: string): string {
  return val.replace(/([A-Z])/g, ' $1').trim()
}

const inputCls =
  'w-full h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white disabled:bg-gray-50 disabled:text-[var(--color-text-secondary)]'

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
