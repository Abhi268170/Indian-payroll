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

const INDIAN_STATES = [
  'Andhra Pradesh','Arunachal Pradesh','Assam','Bihar','Chhattisgarh','Goa','Gujarat','Haryana',
  'Himachal Pradesh','Jharkhand','Karnataka','Kerala','Madhya Pradesh','Maharashtra','Manipur',
  'Meghalaya','Mizoram','Nagaland','Odisha','Punjab','Rajasthan','Sikkim','Tamil Nadu','Telangana',
  'Tripura','Uttar Pradesh','Uttarakhand','West Bengal',
  'Andaman and Nicobar Islands','Chandigarh','Dadra and Nagar Haveli and Daman and Diu',
  'Delhi','Jammu and Kashmir','Ladakh','Lakshadweep','Puducherry',
]

const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/
const AADHAAR_REGEX = /^\d{12}$/

const schema = z.object({
  fathersName: z.string().min(1, 'Required').max(150),
  pan: z.string().regex(PAN_REGEX, 'Invalid PAN format (e.g. ABCPM1234A)').optional().or(z.literal('')),
  aadhaar: z.string().regex(AADHAAR_REGEX, 'Must be 12 digits').optional().or(z.literal('')),
  personalEmail: z.string().email('Invalid email').optional().or(z.literal('')),
  differentlyAbledType: z.enum(['None', 'Visual', 'Hearing', 'Locomotive', 'Other']),
  isPWD: z.boolean(),
  addressLine1: z.string().max(200).optional(),
  addressLine2: z.string().max(200).optional(),
  city: z.string().max(100).optional(),
  residentialState: z.string().optional(),
  pinCode: z.string().regex(/^\d{6}$/, '6 digits required').optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'
const errCls = 'mt-1 text-[11px] text-red-500'

export default function WizardStep3Personal({ employeeId, onSuccess, onSkip }: Props): React.ReactElement {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { differentlyAbledType: 'None', isPWD: false },
  })

  const save = useMutation({
    mutationFn: (v: FormValues) => api.put(`/api/v1/employees/${employeeId}/personal-details`, {
      fathersName: v.fathersName,
      pan: v.pan || null,
      aadhaar: v.aadhaar || null,
      personalEmail: v.personalEmail || null,
      differentlyAbledType: v.differentlyAbledType,
      isPWD: v.isPWD,
      addressLine1: v.addressLine1 || null,
      addressLine2: v.addressLine2 || null,
      city: v.city || null,
      residentialState: v.residentialState || null,
      pinCode: v.pinCode || null,
    }),
    onSuccess,
  })

  return (
    <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-5">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className={labelCls}>Father's Name <span className="text-red-500">*</span></label>
          <input {...register('fathersName')} className={inputCls} />
          {errors.fathersName && <p className={errCls}>{errors.fathersName.message}</p>}
        </div>
        <div>
          <label className={labelCls}>PAN</label>
          <input
            {...register('pan')}
            className={`${inputCls} font-mono uppercase`}
            placeholder="ABCDE1234F"
            maxLength={10}
          />
          <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">Required for TDS computation and Form 16</p>
          {errors.pan && <p className={errCls}>{errors.pan.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className={labelCls}>Aadhaar Number</label>
          <input
            {...register('aadhaar')}
            className={`${inputCls} font-mono`}
            placeholder="12-digit Aadhaar number"
            maxLength={12}
            inputMode="numeric"
          />
          <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">Stored encrypted. Masked as XXXX-XXXX-1234 in all views.</p>
          {errors.aadhaar && <p className={errCls}>{errors.aadhaar.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className={labelCls}>Personal Email</label>
          <input type="email" {...register('personalEmail')} className={inputCls} />
          {errors.personalEmail && <p className={errCls}>{errors.personalEmail.message}</p>}
        </div>
        <div>
          <label className={labelCls}>Differently Abled Type</label>
          <select {...register('differentlyAbledType')} className={inputCls}>
            <option value="None">None</option>
            <option value="Visual">Visual Impairment</option>
            <option value="Hearing">Hearing Impairment</option>
            <option value="Locomotive">Locomotive Disability</option>
            <option value="Other">Other</option>
          </select>
        </div>
      </div>

      <div>
        <label className={labelCls}>Address Line 1</label>
        <input {...register('addressLine1')} className={inputCls} />
      </div>
      <div>
        <label className={labelCls}>Address Line 2</label>
        <input {...register('addressLine2')} className={inputCls} />
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div>
          <label className={labelCls}>City</label>
          <input {...register('city')} className={inputCls} />
        </div>
        <div>
          <label className={labelCls}>State</label>
          <select {...register('residentialState')} className={inputCls}>
            <option value="">Select state</option>
            {INDIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <div>
          <label className={labelCls}>PIN Code</label>
          <input {...register('pinCode')} className={inputCls} maxLength={6} />
          {errors.pinCode && <p className={errCls}>{errors.pinCode.message}</p>}
        </div>
      </div>

      <label className="flex items-center gap-2 cursor-pointer">
        <input type="checkbox" {...register('isPWD')} className="w-4 h-4 accent-[var(--color-primary)]" />
        <span className="text-[13px]">Person with Disability (PWD)</span>
      </label>

      {save.isError && <p className="text-[12px] text-red-600">Failed to save. Please try again.</p>}

      <div className="flex items-center gap-3 pt-2 border-t border-[var(--color-border)]">
        <button
          type="submit"
          disabled={isSubmitting || save.isPending}
          className="h-9 px-5 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {save.isPending ? 'Saving…' : 'Save and Continue'}
        </button>
        <button type="button" onClick={onSkip} className="h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
          Skip
        </button>
      </div>
    </form>
  )
}
