import { useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'

interface Props {
  employeeId: string
  onSuccess: () => void
  onSkip: () => void
}

const schema = z.object({
  paymentMode: z.enum(['Cash', 'Cheque', 'BankTransfer', 'DirectDeposit']),
  accountHolderName: z.string().max(150).optional(),
  bankName: z.string().max(150).optional(),
  accountType: z.enum(['Savings', 'Current', 'Salary']).optional(),
  accountNumber: z.string().max(20).optional(),
  confirmAccountNumber: z.string().max(20).optional(),
  ifscCode: z.string().max(11).optional(),
}).superRefine((v, ctx) => {
  const needsBank = v.paymentMode === 'BankTransfer' || v.paymentMode === 'DirectDeposit'
  if (needsBank) {
    // Mirrors UpdatePaymentInfoCommand.cs:29 — all bank fields are required when paying via bank.
    if (!v.accountNumber) {
      ctx.addIssue({ code: 'custom', path: ['accountNumber'], message: 'Account number required' })
    }
    if (v.accountNumber && v.confirmAccountNumber && v.accountNumber !== v.confirmAccountNumber) {
      ctx.addIssue({ code: 'custom', path: ['confirmAccountNumber'], message: 'Account numbers do not match' })
    }
    if (!v.accountHolderName) {
      ctx.addIssue({ code: 'custom', path: ['accountHolderName'], message: 'Account holder name required' })
    }
    if (!v.bankName) {
      ctx.addIssue({ code: 'custom', path: ['bankName'], message: 'Bank name required' })
    }
    if (!v.accountType) {
      ctx.addIssue({ code: 'custom', path: ['accountType'], message: 'Account type required' })
    }
    if (!v.ifscCode) {
      ctx.addIssue({ code: 'custom', path: ['ifscCode'], message: 'IFSC code required' })
    } else if (v.ifscCode.length !== 11) {
      ctx.addIssue({ code: 'custom', path: ['ifscCode'], message: 'IFSC code must be 11 characters' })
    }
  }
})
type FormValues = z.infer<typeof schema>

const MODES: { value: string; label: string; sub: string; disabled?: boolean }[] = [
  { value: 'BankTransfer', label: 'Bank Transfer', sub: 'Manual NEFT/IMPS — download bank advice after payroll run' },
  { value: 'DirectDeposit', label: 'Direct Deposit', sub: 'Automated transfer', disabled: true },
  { value: 'Cheque', label: 'Cheque', sub: 'Physical cheque payment', disabled: true },
  { value: 'Cash', label: 'Cash', sub: 'Cash payment', disabled: true },
]

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'
const errCls = 'mt-1 text-[11px] text-red-500'

export default function WizardStep4Payment({ employeeId, onSuccess, onSkip }: Props): React.ReactElement {
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { paymentMode: 'BankTransfer' },
  })

  const mode = watch('paymentMode')
  const hasBankFields = mode === 'BankTransfer' || mode === 'DirectDeposit'

  const save = useMutation({
    mutationFn: (v: FormValues) => api.put(`/api/v1/employees/${employeeId}/payment-info`, {
      paymentMode: v.paymentMode,
      accountHolderName: hasBankFields ? (v.accountHolderName || null) : null,
      bankName: hasBankFields ? (v.bankName || null) : null,
      accountType: hasBankFields ? (v.accountType || null) : null,
      accountNumber: hasBankFields ? (v.accountNumber || null) : null,
      ifsc: hasBankFields ? (v.ifscCode || null) : null,
    }),
    onSuccess,
  })

  return (
    <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-6">
      {/* Payment mode cards */}
      <div>
        <p className="text-[12px] font-medium text-[var(--color-text-secondary)] mb-3">Payment Mode</p>
        <div className="grid grid-cols-2 gap-3">
          {MODES.map(m => {
            const active = mode === m.value
            return (
              <label
                key={m.value}
                className={`flex items-start gap-3 p-3.5 border-2 rounded-xl transition-colors ${
                  m.disabled
                    ? 'border-[var(--color-border)] opacity-50 cursor-not-allowed'
                    : active
                      ? 'border-[var(--color-primary)] bg-[var(--color-primary)]/5 cursor-pointer'
                      : 'border-[var(--color-border)] hover:border-[var(--color-primary)]/40 cursor-pointer'
                }`}
              >
                <input
                  type="radio"
                  value={m.value}
                  {...register('paymentMode')}
                  disabled={m.disabled}
                  className="mt-0.5 accent-[var(--color-primary)]"
                />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{m.label}</p>
                    {m.disabled && (
                      <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-[var(--color-bg-subtle)] text-[var(--color-text-secondary)]">
                        Coming soon
                      </span>
                    )}
                  </div>
                  <p className="text-[11px] text-[var(--color-text-secondary)]">{m.sub}</p>
                </div>
              </label>
            )
          })}
        </div>
      </div>

      {/* Bank fields */}
      {hasBankFields && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelCls}>Account Holder Name</label>
              <input {...register('accountHolderName')} className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>Bank Name</label>
              <input {...register('bankName')} className={inputCls} />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelCls}>Account Number <span className="text-red-500">*</span></label>
              <input
                type="password"
                {...register('accountNumber')}
                className={inputCls}
                autoComplete="new-password"
              />
              {errors.accountNumber && <p className={errCls}>{errors.accountNumber.message}</p>}
            </div>
            <div>
              <label className={labelCls}>Re-enter Account Number</label>
              <input
                type="password"
                {...register('confirmAccountNumber')}
                className={inputCls}
                autoComplete="new-password"
              />
              {errors.confirmAccountNumber && <p className={errCls}>{errors.confirmAccountNumber.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelCls}>Account Type</label>
              <select {...register('accountType')} className={inputCls}>
                <option value="">Select</option>
                <option value="Savings">Savings</option>
                <option value="Current">Current</option>
                <option value="Salary">Salary</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>IFSC Code</label>
              <input
                {...register('ifscCode')}
                className={`${inputCls} font-mono uppercase`}
                placeholder="HDFC0001234"
                maxLength={11}
              />
            </div>
          </div>
        </div>
      )}

      {save.isError && <p className="text-[12px] text-red-600">Failed to save. Please try again.</p>}

      <div className="flex items-center gap-3 pt-2 border-t border-[var(--color-border)]">
        <button
          type="submit"
          disabled={isSubmitting || save.isPending}
          className="h-9 px-5 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {save.isPending ? 'Saving…' : 'Save and Finish'}
        </button>
        <button type="button" onClick={onSkip} className="h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
          Skip
        </button>
      </div>
    </form>
  )
}
