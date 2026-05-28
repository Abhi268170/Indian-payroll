import { useEffect, useRef, useState, type ReactElement } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Building2, Upload, Trash2 } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { DateInput } from '@/components/ui/DateInput'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'

const INDIAN_STATES = [
  'AndhraPradesh', 'ArunachalPradesh', 'Assam', 'Bihar', 'Chhattisgarh',
  'Goa', 'Gujarat', 'Haryana', 'HimachalPradesh', 'Jharkhand', 'Karnataka',
  'Kerala', 'MadhyaPradesh', 'Maharashtra', 'Manipur', 'Meghalaya', 'Mizoram',
  'Nagaland', 'Odisha', 'Punjab', 'Rajasthan', 'Sikkim', 'TamilNadu',
  'Telangana', 'Tripura', 'UttarPradesh', 'Uttarakhand', 'WestBengal',
  'AndamanAndNicobar', 'Chandigarh', 'DadraAndNagarHaveliAndDamanAndDiu',
  'Delhi', 'JammuAndKashmir', 'Ladakh', 'Lakshadweep', 'Puducherry',
]

const STATE_LABELS: Record<string, string> = {
  AndhraPradesh: 'Andhra Pradesh', ArunachalPradesh: 'Arunachal Pradesh',
  Assam: 'Assam', Bihar: 'Bihar', Chhattisgarh: 'Chhattisgarh', Goa: 'Goa',
  Gujarat: 'Gujarat', Haryana: 'Haryana', HimachalPradesh: 'Himachal Pradesh',
  Jharkhand: 'Jharkhand', Karnataka: 'Karnataka', Kerala: 'Kerala',
  MadhyaPradesh: 'Madhya Pradesh', Maharashtra: 'Maharashtra', Manipur: 'Manipur',
  Meghalaya: 'Meghalaya', Mizoram: 'Mizoram', Nagaland: 'Nagaland', Odisha: 'Odisha',
  Punjab: 'Punjab', Rajasthan: 'Rajasthan', Sikkim: 'Sikkim', TamilNadu: 'Tamil Nadu',
  Telangana: 'Telangana', Tripura: 'Tripura', UttarPradesh: 'Uttar Pradesh',
  Uttarakhand: 'Uttarakhand', WestBengal: 'West Bengal',
  AndamanAndNicobar: 'Andaman & Nicobar Islands', Chandigarh: 'Chandigarh',
  DadraAndNagarHaveliAndDamanAndDiu: 'Dadra & Nagar Haveli and Daman & Diu',
  Delhi: 'Delhi', JammuAndKashmir: 'Jammu & Kashmir', Ladakh: 'Ladakh',
  Lakshadweep: 'Lakshadweep', Puducherry: 'Puducherry',
}

const INDUSTRIES = [
  'Information Technology', 'Manufacturing', 'Healthcare', 'Finance & Banking',
  'Retail & E-Commerce', 'Education', 'Construction & Real Estate',
  'Logistics & Transportation', 'Hospitality & Tourism', 'Media & Entertainment',
  'Consulting & Professional Services', 'Agriculture', 'Telecommunications', 'Other',
]

const schema = z.object({
  companyName: z.string().min(1, 'Company name is required').max(200),
  legalName: z.string().max(200).optional().or(z.literal('')),
  pan: z.string().min(1, 'Company PAN is required for tax filings').regex(/^[A-Z]{5}[0-9]{4}[A-Z]$/, 'Format: AAAAA9999A'),
  gstin: z.string().optional().or(z.literal('')),
  website: z.string().max(500).optional().or(z.literal('')),
  industry: z.string().optional().or(z.literal('')),
  incorporationDate: z.string().optional().or(z.literal('')),
  addressLine1: z.string().max(250).optional().or(z.literal('')),
  addressLine2: z.string().max(250).optional().or(z.literal('')),
  city: z.string().max(100).optional().or(z.literal('')),
  state: z.string().optional().or(z.literal('')),
  pinCode: z.string().regex(/^\d{6}$/, 'Must be 6 digits').optional().or(z.literal('')),
})

type FormData = z.infer<typeof schema>

interface OrgProfileDto {
  companyName: string
  legalName: string | null
  pan: string | null
  gstin: string | null
  website: string | null
  industry: string | null
  incorporationDate: string | null
  addressLine1: string | null
  addressLine2: string | null
  city: string | null
  state: string | null
  pinCode: string | null
  filingAddressWorkLocationId: string | null
  hasLogo: boolean
}

export default function OrgProfilePage(): ReactElement {
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [logoVersion, setLogoVersion] = useState(0)
  const [logoObjectUrl, setLogoObjectUrl] = useState<string | null>(null)

  const { data: profile, isLoading } = useQuery<OrgProfileDto>({
    queryKey: ['org-profile'],
    queryFn: () => api.get<OrgProfileDto>('/api/v1/org-profile').then(r => r.data),
  })

  const { register, handleSubmit, reset, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      companyName: '', legalName: '', pan: '', gstin: '', website: '',
      industry: '', incorporationDate: '', addressLine1: '', addressLine2: '',
      city: '', state: '', pinCode: '',
    },
  })

  useEffect(() => {
    if (profile?.hasLogo !== true) {
      return () => { /* nothing to revoke */ }
    }
    let cancelled = false
    let objectUrl: string | null = null
    void api.get<Blob>('/api/v1/org-profile/logo', { responseType: 'blob' }).then(r => {
      if (!cancelled) {
        objectUrl = URL.createObjectURL(r.data)
        setLogoObjectUrl(prev => { if (prev) URL.revokeObjectURL(prev); return objectUrl })
      }
    })
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [profile?.hasLogo, logoVersion])

  useEffect(() => {
    if (profile) {
      reset({
        companyName: profile.companyName,
        legalName: profile.legalName ?? '',
        pan: profile.pan ?? '',
        gstin: profile.gstin ?? '',
        website: profile.website ?? '',
        industry: profile.industry ?? '',
        incorporationDate: profile.incorporationDate ?? '',
        addressLine1: profile.addressLine1 ?? '',
        addressLine2: profile.addressLine2 ?? '',
        city: profile.city ?? '',
        state: profile.state ?? '',
        pinCode: profile.pinCode ?? '',
      })
    }
  }, [profile, reset])

  const logoSrc = logoObjectUrl

  const updateMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.put('/api/v1/org-profile', {
        companyName: data.companyName,
        legalName: data.legalName !== '' ? data.legalName : null,
        pan: data.pan !== '' ? data.pan : null,
        gstin: data.gstin !== '' ? data.gstin : null,
        website: data.website !== '' ? data.website : null,
        industry: data.industry !== '' ? data.industry : null,
        incorporationDate: data.incorporationDate !== '' ? data.incorporationDate : null,
        addressLine1: data.addressLine1 !== '' ? data.addressLine1 : null,
        addressLine2: data.addressLine2 !== '' ? data.addressLine2 : null,
        city: data.city !== '' ? data.city : null,
        state: data.state !== '' ? data.state : null,
        pinCode: data.pinCode !== '' ? data.pinCode : null,
        filingAddressWorkLocationId: null,
      }),
    onSuccess: () => {
      toastSuccess('Organisation profile saved')
      void qc.invalidateQueries({ queryKey: ['org-profile'] })
    },
    onError: () => { toastError('Failed to save profile') },
  })

  const uploadLogoMutation = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api.post('/api/v1/org-profile/logo', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
    },
    onSuccess: () => {
      toastSuccess('Logo uploaded')
      void qc.invalidateQueries({ queryKey: ['org-profile'] })
      setLogoVersion(v => v + 1)
    },
    onError: () => { toastError('Logo upload failed. Must be PNG/JPEG under 2 MB.') },
  })

  const deleteLogoMutation = useMutation({
    mutationFn: () => api.delete('/api/v1/org-profile/logo'),
    onSuccess: () => {
      toastSuccess('Logo removed')
      setLogoObjectUrl(prev => { if (prev) URL.revokeObjectURL(prev); return null })
      void qc.invalidateQueries({ queryKey: ['org-profile'] })
    },
    onError: () => { toastError('Failed to remove logo') },
  })

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>): void {
    const file = e.target.files?.[0]
    if (!file) return
    uploadLogoMutation.mutate(file)
    e.target.value = ''
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner />
      </div>
    )
  }

  return (
    <div className="px-8 py-8 max-w-3xl">
      <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)] mb-8">
        Organisation Profile
      </h1>

      {/* Logo */}
      <Section title="Company Logo">
        <div className="flex items-center gap-6">
          <div className="w-20 h-20 rounded-xl border border-[var(--color-border)] bg-[var(--color-page-bg)] flex items-center justify-center overflow-hidden flex-shrink-0">
            {logoSrc != null ? (
              <img src={logoSrc} alt="Company logo" className="w-full h-full object-contain" />
            ) : (
              <Building2 className="w-8 h-8 text-[var(--color-text-muted)]" />
            )}
          </div>
          <div className="flex flex-col gap-2">
            <p className="text-[12px] text-[var(--color-text-muted)]">PNG or JPEG, max 2 MB</p>
            <div className="flex items-center gap-2">
              <input
                ref={fileInputRef}
                type="file"
                accept="image/png,image/jpeg"
                className="hidden"
                onChange={handleFileChange}
              />
              <Button
                variant="secondary"
                size="sm"
                onClick={() => { fileInputRef.current?.click() }}
                loading={uploadLogoMutation.isPending}
              >
                <Upload className="w-3.5 h-3.5 mr-1.5" />
                {logoSrc != null ? 'Replace' : 'Upload Logo'}
              </Button>
              {logoSrc != null && (
                <button
                  onClick={() => { deleteLogoMutation.mutate() }}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-error)] hover:bg-red-50 transition-colors"
                  title="Remove logo"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              )}
            </div>
          </div>
        </div>
      </Section>

      <form
        className="space-y-8 mt-8"
        onSubmit={e => {
          void handleSubmit(data => { updateMutation.mutate(data) })(e)
        }}
      >
        {/* Basic Details */}
        <Section title="Basic Details">
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Company Name"
              required
              error={errors.companyName?.message}
              {...register('companyName')}
            />
            <Input
              label="Legal Name"
              helpText="If different from company name"
              error={errors.legalName?.message}
              {...register('legalName')}
            />
            <Input
              label="Company PAN *"
              placeholder="AAAAA9999A"
              helpText="Required for Form 24Q + Form 16 filings."
              error={errors.pan?.message}
              {...register('pan')}
            />
            <Input
              label="GSTIN"
              placeholder="22AAAAA0000A1Z5"
              error={errors.gstin?.message}
              {...register('gstin')}
            />
            <Input
              label="Website"
              placeholder="https://example.com"
              error={errors.website?.message}
              {...register('website')}
            />
            <Select
              label="Industry"
              options={INDUSTRIES.map(i => ({ value: i, label: i }))}
              placeholder="Select industry"
              {...register('industry')}
            />
            <div>
              <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1.5">Date of Incorporation</label>
              <Controller
                control={control}
                name="incorporationDate"
                render={({ field }) => (
                  <DateInput value={field.value ?? ''} onChange={field.onChange} ariaLabel="Date of Incorporation" />
                )}
              />
              {errors.incorporationDate?.message && (
                <p className="mt-1 text-[12px] text-red-600">{errors.incorporationDate.message}</p>
              )}
            </div>
          </div>
        </Section>

        {/* Registered Address */}
        <Section title="Registered Address">
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <Input
                label="Address Line 1"
                error={errors.addressLine1?.message}
                {...register('addressLine1')}
              />
            </div>
            <div className="col-span-2">
              <Input
                label="Address Line 2"
                error={errors.addressLine2?.message}
                {...register('addressLine2')}
              />
            </div>
            <Input
              label="City"
              error={errors.city?.message}
              {...register('city')}
            />
            <Input
              label="Pin Code"
              placeholder="6 digits"
              error={errors.pinCode?.message}
              {...register('pinCode')}
            />
            <div className="col-span-2">
              <Select
                label="State"
                options={INDIAN_STATES.map(s => ({ value: s, label: STATE_LABELS[s] ?? s }))}
                placeholder="Select state"
                {...register('state')}
              />
            </div>
          </div>
        </Section>

        <div className="flex items-center gap-3">
          <Button type="submit" variant="primary" size="sm" loading={updateMutation.isPending}>
            Save Profile
          </Button>
        </div>
      </form>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }): ReactElement {
  return (
    <div className="bg-white border border-[var(--color-border)] rounded-xl p-6">
      <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-5">{title}</h2>
      {children}
    </div>
  )
}
