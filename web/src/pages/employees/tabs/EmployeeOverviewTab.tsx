import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Pencil, X, Check } from 'lucide-react'
import { api } from '@/lib/api'
import type { EmployeeDto, DepartmentDto, DesignationDto } from '@/types/api'
import type { WorkLocation } from '@/pages/settings/WorkLocationsPage'
import type { BusinessUnit } from '@/pages/settings/BusinessUnitsPage'
import type { CostCentre } from '@/pages/settings/CostCentresPage'

interface Props {
  employee: EmployeeDto
  onSaved: () => void
}

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide'
const valueCls = 'text-[13px] text-[var(--color-text-primary)] mt-0.5'
const errCls = 'mt-1 text-[11px] text-red-500'
const fieldLabelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'

function fmtDate(iso: string | null): string {
  if (!iso) return '—'
  return iso.split('-').reverse().join('/')
}

function fmtBool(v: boolean): string {
  return v ? 'Yes' : 'No'
}

function ReadField({ label, value }: { label: string; value: string | null | undefined }): React.ReactElement {
  return (
    <div>
      <p className={labelCls}>{label}</p>
      <p className={valueCls}>{value || '—'}</p>
    </div>
  )
}

function SectionHeader({
  title,
  editing,
  onEdit,
  onCancel,
}: {
  title: string
  editing: boolean
  onEdit: () => void
  onCancel: () => void
}): React.ReactElement {
  return (
    <div className="flex items-center justify-between mb-4">
      <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)]">{title}</h3>
      {editing ? (
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex items-center gap-1 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
        >
          <X className="w-3.5 h-3.5" />
          Cancel
        </button>
      ) : (
        <button
          type="button"
          onClick={onEdit}
          className="inline-flex items-center gap-1 text-[12px] text-[var(--color-primary)] hover:opacity-80 transition-opacity"
        >
          <Pencil className="w-3.5 h-3.5" />
          Edit
        </button>
      )}
    </div>
  )
}

// ── Basic Information ──────────────────────────────────────────────────────────

const basicSchema = z.object({
  firstName: z.string().min(1, 'Required').max(100),
  middleName: z.string().max(100).optional(),
  lastName: z.string().min(1, 'Required').max(100),
  mobileNumber: z.string().regex(/^\d{10}$/, '10 digits required').optional().or(z.literal('')),
  gender: z.enum(['Male', 'Female', 'Other']),
  isDirector: z.boolean(),
  enablePortalAccess: z.boolean(),
  departmentId: z.string().min(1, 'Required'),
  designationId: z.string().min(1, 'Required'),
  workLocationId: z.string().min(1, 'Required'),
  businessUnitId: z.string().optional(),
  costCentreId: z.string().optional(),
})
type BasicValues = z.infer<typeof basicSchema>

function BasicSection({ employee, onSaved }: Props): React.ReactElement {
  const [editing, setEditing] = useState(false)

  const { data: departments = [] } = useQuery<DepartmentDto[]>({
    queryKey: ['departments'],
    queryFn: () => api.get<DepartmentDto[]>('/api/v1/departments').then(r => r.data),
  })
  const { data: designations = [] } = useQuery<DesignationDto[]>({
    queryKey: ['designations'],
    queryFn: () => api.get<DesignationDto[]>('/api/v1/designations').then(r => r.data),
  })
  const { data: workLocations = [] } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })
  const { data: businessUnits = [] } = useQuery<BusinessUnit[]>({
    queryKey: ['business-units'],
    queryFn: () => api.get<BusinessUnit[]>('/api/v1/business-units').then(r => r.data),
  })
  const { data: costCentres = [] } = useQuery<CostCentre[]>({
    queryKey: ['cost-centres'],
    queryFn: () => api.get<CostCentre[]>('/api/v1/cost-centres').then(r => r.data),
  })

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<BasicValues>({
    resolver: zodResolver(basicSchema),
    defaultValues: {
      firstName: employee.firstName,
      middleName: employee.middleName ?? '',
      lastName: employee.lastName,
      mobileNumber: employee.mobileNumber ?? '',
      gender: employee.gender as 'Male' | 'Female' | 'Other',
      isDirector: employee.isDirector,
      enablePortalAccess: employee.enablePortalAccess,
      departmentId: employee.departmentId,
      designationId: employee.designationId,
      workLocationId: employee.workLocationId,
      businessUnitId: employee.businessUnitId ?? '',
      costCentreId: employee.costCentreId ?? '',
    },
  })

  const save = useMutation({
    mutationFn: (v: BasicValues) => api.put(`/api/v1/employees/${employee.id}/basic-details`, {
      firstName: v.firstName,
      middleName: v.middleName || null,
      lastName: v.lastName,
      mobileNumber: v.mobileNumber || null,
      gender: v.gender,
      isDirector: v.isDirector,
      enablePortalAccess: v.enablePortalAccess,
      departmentId: v.departmentId,
      designationId: v.designationId,
      workLocationId: v.workLocationId,
      businessUnitId: v.businessUnitId || null,
      costCentreId: v.costCentreId || null,
    }),
    onSuccess: () => { setEditing(false); onSaved() },
  })

  function cancel(): void {
    reset()
    setEditing(false)
    save.reset()
  }

  const empTypeLabel: Record<string, string> = {
    FullTime: 'Full Time', PartTime: 'Part Time', Contract: 'Contract', Intern: 'Intern',
  }

  return (
    <section className="border border-[var(--color-border)] rounded-xl p-5">
      <SectionHeader title="Basic Information" editing={editing} onEdit={() => setEditing(true)} onCancel={cancel} />

      {!editing ? (
        <div className="grid grid-cols-2 gap-x-8 gap-y-4">
          <ReadField label="Full Name" value={employee.fullName} />
          <ReadField label="Work Email" value={employee.workEmail} />
          <ReadField label="Mobile Number" value={employee.mobileNumber} />
          <ReadField label="Gender" value={employee.gender} />
          <ReadField label="Date of Joining" value={fmtDate(employee.dateOfJoining)} />
          <ReadField label="Employment Type" value={empTypeLabel[employee.employmentType] ?? employee.employmentType} />
          <ReadField label="Department" value={employee.departmentName} />
          <ReadField label="Designation" value={employee.designationName} />
          <ReadField label="Work Location" value={employee.workLocationName} />
          <ReadField label="Business Unit" value={businessUnits.find(b => b.id === employee.businessUnitId)?.name ?? null} />
          <ReadField label="Cost Centre" value={costCentres.find(c => c.id === employee.costCentreId)?.name ?? null} />
          <ReadField label="Director" value={fmtBool(employee.isDirector)} />
          <ReadField label="Portal Access" value={fmtBool(employee.enablePortalAccess)} />
        </div>
      ) : (
        <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className={fieldLabelCls}>First Name <span className="text-red-500">*</span></label>
              <input {...register('firstName')} className={inputCls} />
              {errors.firstName && <p className={errCls}>{errors.firstName.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Middle Name</label>
              <input {...register('middleName')} className={inputCls} />
            </div>
            <div>
              <label className={fieldLabelCls}>Last Name <span className="text-red-500">*</span></label>
              <input {...register('lastName')} className={inputCls} />
              {errors.lastName && <p className={errCls}>{errors.lastName.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>Mobile Number</label>
              <input {...register('mobileNumber')} className={inputCls} placeholder="10-digit number" />
              {errors.mobileNumber && <p className={errCls}>{errors.mobileNumber.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Gender <span className="text-red-500">*</span></label>
              <select {...register('gender')} className={inputCls}>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className={fieldLabelCls}>Department <span className="text-red-500">*</span></label>
              <select {...register('departmentId')} className={inputCls}>
                {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
              {errors.departmentId && <p className={errCls}>{errors.departmentId.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Designation <span className="text-red-500">*</span></label>
              <select {...register('designationId')} className={inputCls}>
                {designations.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
              {errors.designationId && <p className={errCls}>{errors.designationId.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Work Location <span className="text-red-500">*</span></label>
              <select {...register('workLocationId')} className={inputCls}>
                {workLocations.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
              {errors.workLocationId && <p className={errCls}>{errors.workLocationId.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>Business Unit</label>
              <select {...register('businessUnitId')} className={inputCls}>
                <option value="">None</option>
                {businessUnits.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </div>
            <div>
              <label className={fieldLabelCls}>Cost Centre</label>
              <select {...register('costCentreId')} className={inputCls}>
                <option value="">None</option>
                {costCentres.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
          </div>

          <div className="flex items-center gap-6">
            <label className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" {...register('isDirector')} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
              <span className="text-[13px]">Mark as Director</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" {...register('enablePortalAccess')} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
              <span className="text-[13px]">Enable Portal Access</span>
            </label>
          </div>

          {save.isError && <p className={errCls}>Failed to save. Please try again.</p>}

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={!isDirty || save.isPending}
              className="inline-flex items-center gap-1.5 h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg disabled:opacity-50 hover:bg-[var(--color-primary-hover)] transition-colors"
            >
              <Check className="w-3.5 h-3.5" />
              {save.isPending ? 'Saving…' : 'Save'}
            </button>
            <button type="button" onClick={cancel} className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  )
}

// ── Personal Information ───────────────────────────────────────────────────────

const INDIAN_STATES = [
  'Andhra Pradesh','Arunachal Pradesh','Assam','Bihar','Chhattisgarh','Goa','Gujarat','Haryana',
  'Himachal Pradesh','Jharkhand','Karnataka','Kerala','Madhya Pradesh','Maharashtra','Manipur',
  'Meghalaya','Mizoram','Nagaland','Odisha','Punjab','Rajasthan','Sikkim','Tamil Nadu','Telangana',
  'Tripura','Uttar Pradesh','Uttarakhand','West Bengal',
  'Andaman and Nicobar Islands','Chandigarh','Dadra and Nagar Haveli and Daman and Diu',
  'Delhi','Jammu and Kashmir','Ladakh','Lakshadweep','Puducherry',
]

const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/

const personalSchema = z.object({
  dateOfBirth: z.string().min(1, 'Required'),
  fathersName: z.string().max(150).optional(),
  pan: z.string().regex(PAN_REGEX, 'Invalid PAN (e.g. ABCPM1234A)').optional().or(z.literal('')),
  personalEmail: z.string().email('Invalid email').optional().or(z.literal('')),
  differentlyAbledType: z.enum(['None', 'Visual', 'Hearing', 'Locomotive', 'Other']),
  isPWD: z.boolean(),
  addressLine1: z.string().max(200).optional(),
  addressLine2: z.string().max(200).optional(),
  city: z.string().max(100).optional(),
  residentialState: z.string().optional(),
  pinCode: z.string().regex(/^\d{6}$/, '6 digits required').optional().or(z.literal('')),
})
type PersonalValues = z.infer<typeof personalSchema>

function PersonalSection({ employee, onSaved }: Props): React.ReactElement {
  const [editing, setEditing] = useState(false)

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<PersonalValues>({
    resolver: zodResolver(personalSchema),
    defaultValues: {
      dateOfBirth: employee.dateOfBirth,
      fathersName: employee.fathersName ?? '',
      pan: '',
      personalEmail: employee.personalEmail ?? '',
      differentlyAbledType: (employee.differentlyAbledType as PersonalValues['differentlyAbledType']) ?? 'None',
      isPWD: employee.isPWD,
      addressLine1: employee.addressLine1 ?? '',
      addressLine2: employee.addressLine2 ?? '',
      city: employee.city ?? '',
      residentialState: employee.residentialState ?? '',
      pinCode: employee.pinCode ?? '',
    },
  })

  const save = useMutation({
    mutationFn: (v: PersonalValues) => api.put(`/api/v1/employees/${employee.id}/personal-details`, {
      dateOfBirth: v.dateOfBirth,
      fathersName: v.fathersName || null,
      pan: v.pan || null,
      personalEmail: v.personalEmail || null,
      differentlyAbledType: v.differentlyAbledType,
      isPWD: v.isPWD,
      addressLine1: v.addressLine1 || null,
      addressLine2: v.addressLine2 || null,
      city: v.city || null,
      residentialState: v.residentialState || null,
      pinCode: v.pinCode || null,
    }),
    onSuccess: () => { setEditing(false); onSaved() },
  })

  function cancel(): void { reset(); setEditing(false); save.reset() }

  const daLabel: Record<string, string> = {
    None: 'None', Visual: 'Visual Impairment', Hearing: 'Hearing Impairment',
    Locomotive: 'Locomotive Disability', Other: 'Other',
  }

  return (
    <section className="border border-[var(--color-border)] rounded-xl p-5">
      <SectionHeader title="Personal Information" editing={editing} onEdit={() => setEditing(true)} onCancel={cancel} />

      {!editing ? (
        <div className="grid grid-cols-2 gap-x-8 gap-y-4">
          <ReadField label="Date of Birth" value={fmtDate(employee.dateOfBirth)} />
          <ReadField label="Father's Name" value={employee.fathersName} />
          <ReadField label="PAN" value={employee.maskedPAN} />
          <ReadField label="Personal Email" value={employee.personalEmail} />
          <ReadField label="Differently Abled" value={daLabel[employee.differentlyAbledType] ?? employee.differentlyAbledType} />
          <ReadField label="Address" value={[employee.addressLine1, employee.addressLine2].filter(Boolean).join(', ')} />
          <ReadField label="City" value={employee.city} />
          <ReadField label="State" value={employee.residentialState} />
          <ReadField label="PIN Code" value={employee.pinCode} />
        </div>
      ) : (
        <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>Date of Birth <span className="text-red-500">*</span></label>
              <input type="date" {...register('dateOfBirth')} className={inputCls} />
              {errors.dateOfBirth && <p className={errCls}>{errors.dateOfBirth.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Father's Name</label>
              <input {...register('fathersName')} className={inputCls} />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>PAN</label>
              <input
                {...register('pan')}
                className={`${inputCls} font-mono uppercase`}
                placeholder="ABCDE1234F"
                maxLength={10}
              />
              {employee.maskedPAN && (
                <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">Current: {employee.maskedPAN} (leave blank to keep)</p>
              )}
              {errors.pan && <p className={errCls}>{errors.pan.message}</p>}
            </div>
            <div>
              <label className={fieldLabelCls}>Personal Email</label>
              <input type="email" {...register('personalEmail')} className={inputCls} />
              {errors.personalEmail && <p className={errCls}>{errors.personalEmail.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>Differently Abled Type</label>
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
            <label className={fieldLabelCls}>Address Line 1</label>
            <input {...register('addressLine1')} className={inputCls} />
          </div>
          <div>
            <label className={fieldLabelCls}>Address Line 2</label>
            <input {...register('addressLine2')} className={inputCls} />
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className={fieldLabelCls}>City</label>
              <input {...register('city')} className={inputCls} />
            </div>
            <div>
              <label className={fieldLabelCls}>State</label>
              <select {...register('residentialState')} className={inputCls}>
                <option value="">Select state</option>
                {INDIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
            <div>
              <label className={fieldLabelCls}>PIN Code</label>
              <input {...register('pinCode')} className={inputCls} maxLength={6} />
              {errors.pinCode && <p className={errCls}>{errors.pinCode.message}</p>}
            </div>
          </div>
          <label className="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" {...register('isPWD')} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
            <span className="text-[13px]">Person with Disability (PWD)</span>
          </label>

          {save.isError && <p className={errCls}>Failed to save. Please try again.</p>}

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={!isDirty || save.isPending}
              className="inline-flex items-center gap-1.5 h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg disabled:opacity-50 hover:bg-[var(--color-primary-hover)] transition-colors"
            >
              <Check className="w-3.5 h-3.5" />
              {save.isPending ? 'Saving…' : 'Save'}
            </button>
            <button type="button" onClick={cancel} className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  )
}

// ── Statutory Information ──────────────────────────────────────────────────────

const statutorySchema = z.object({
  epfEnabled: z.boolean(),
  esiEnabled: z.boolean(),
  ptEnabled: z.boolean(),
  lwfEnabled: z.boolean(),
  uan: z.string().max(20).optional(),
  esicipNumber: z.string().max(20).optional(),
})
type StatutoryValues = z.infer<typeof statutorySchema>

function StatutorySection({ employee, onSaved }: Props): React.ReactElement {
  const [editing, setEditing] = useState(false)

  const { register, handleSubmit, reset, formState: { isDirty } } = useForm<StatutoryValues>({
    resolver: zodResolver(statutorySchema),
    defaultValues: {
      epfEnabled: employee.epfEnabled,
      esiEnabled: employee.esiEnabled,
      ptEnabled: employee.ptEnabled,
      lwfEnabled: employee.lwfEnabled,
      uan: employee.uan ?? '',
      esicipNumber: employee.esicipNumber ?? '',
    },
  })

  const save = useMutation({
    mutationFn: (v: StatutoryValues) => api.put(`/api/v1/employees/${employee.id}/statutory-details`, {
      epfEnabled: v.epfEnabled,
      esiEnabled: v.esiEnabled,
      ptEnabled: v.ptEnabled,
      lwfEnabled: v.lwfEnabled,
      uan: v.uan || null,
      esicipNumber: v.esicipNumber || null,
    }),
    onSuccess: () => { setEditing(false); onSaved() },
  })

  function cancel(): void { reset(); setEditing(false); save.reset() }

  function Toggle({ label, enabled }: { label: string; enabled: boolean }): React.ReactElement {
    return (
      <div className="flex items-center justify-between py-2.5 border-b border-[var(--color-border)] last:border-0">
        <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
        <span className={`text-[12px] font-medium px-2 py-0.5 rounded-full ${enabled ? 'bg-emerald-50 text-emerald-700' : 'bg-gray-100 text-gray-500'}`}>
          {enabled ? 'Enabled' : 'Disabled'}
        </span>
      </div>
    )
  }

  return (
    <section className="border border-[var(--color-border)] rounded-xl p-5">
      <SectionHeader title="Statutory Information" editing={editing} onEdit={() => setEditing(true)} onCancel={cancel} />

      {!editing ? (
        <div>
          <div className="mb-4">
            <Toggle label="Employee Provident Fund (EPF)" enabled={employee.epfEnabled} />
            <Toggle label="Employee State Insurance (ESI)" enabled={employee.esiEnabled} />
            <Toggle label="Professional Tax (PT)" enabled={employee.ptEnabled} />
            <Toggle label="Labour Welfare Fund (LWF)" enabled={employee.lwfEnabled} />
          </div>
          <div className="grid grid-cols-2 gap-x-8 gap-y-4 pt-2">
            <ReadField label="UAN" value={employee.uan} />
            <ReadField label="ESIC IP Number" value={employee.esicipNumber} />
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-4">
          <div className="space-y-1">
            {[
              { name: 'epfEnabled' as const, label: 'Employee Provident Fund (EPF)' },
              { name: 'esiEnabled' as const, label: 'Employee State Insurance (ESI)' },
              { name: 'ptEnabled' as const, label: 'Professional Tax (PT)' },
              { name: 'lwfEnabled' as const, label: 'Labour Welfare Fund (LWF)' },
            ].map(f => (
              <label key={f.name} className="flex items-center gap-3 py-2 cursor-pointer">
                <input type="checkbox" {...register(f.name)} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
                <span className="text-[13px] text-[var(--color-text-primary)]">{f.label}</span>
              </label>
            ))}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={fieldLabelCls}>UAN</label>
              <input {...register('uan')} className={inputCls} placeholder="Universal Account Number" />
            </div>
            <div>
              <label className={fieldLabelCls}>ESIC IP Number</label>
              <input {...register('esicipNumber')} className={inputCls} />
            </div>
          </div>

          {save.isError && <p className={errCls}>Failed to save. Please try again.</p>}

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={!isDirty || save.isPending}
              className="inline-flex items-center gap-1.5 h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg disabled:opacity-50 hover:bg-[var(--color-primary-hover)] transition-colors"
            >
              <Check className="w-3.5 h-3.5" />
              {save.isPending ? 'Saving…' : 'Save'}
            </button>
            <button type="button" onClick={cancel} className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  )
}

// ── Payment Information ────────────────────────────────────────────────────────

const paymentSchema = z.object({
  paymentMode: z.enum(['Cash', 'Cheque', 'BankTransfer', 'DirectDeposit']),
  accountHolderName: z.string().max(150).optional(),
  bankName: z.string().max(150).optional(),
  accountType: z.enum(['Savings', 'Current', 'Salary']).optional(),
  accountNumber: z.string().max(20).optional(),
  confirmAccountNumber: z.string().max(20).optional(),
  ifscCode: z.string().length(11, 'IFSC must be 11 characters').optional().or(z.literal('')),
}).superRefine((v, ctx) => {
  const needsBank = v.paymentMode === 'BankTransfer' || v.paymentMode === 'DirectDeposit'
  if (needsBank && v.accountNumber && v.confirmAccountNumber && v.accountNumber !== v.confirmAccountNumber) {
    ctx.addIssue({ code: 'custom', path: ['confirmAccountNumber'], message: 'Account numbers do not match' })
  }
})
type PaymentValues = z.infer<typeof paymentSchema>

function PaymentSection({ employee, onSaved }: Props): React.ReactElement {
  const [editing, setEditing] = useState(false)

  const { register, handleSubmit, reset, watch, formState: { errors, isDirty } } = useForm<PaymentValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      paymentMode: (employee.paymentMode as PaymentValues['paymentMode']) ?? 'Cash',
      accountHolderName: employee.accountHolderName ?? '',
      bankName: employee.bankName ?? '',
      accountType: (employee.accountType as PaymentValues['accountType']) ?? undefined,
      accountNumber: '',
      confirmAccountNumber: '',
      ifscCode: employee.ifscCode ?? '',
    },
  })

  const mode = watch('paymentMode')
  const hasBankFields = mode === 'BankTransfer' || mode === 'DirectDeposit'

  const save = useMutation({
    mutationFn: (v: PaymentValues) => api.put(`/api/v1/employees/${employee.id}/payment-info`, {
      paymentMode: v.paymentMode,
      accountHolderName: hasBankFields ? (v.accountHolderName || null) : null,
      bankName: hasBankFields ? (v.bankName || null) : null,
      accountType: hasBankFields ? (v.accountType || null) : null,
      accountNumber: hasBankFields ? (v.accountNumber || null) : null,
      ifsc: hasBankFields ? (v.ifscCode || null) : null,
    }),
    onSuccess: () => { setEditing(false); onSaved() },
  })

  function cancel(): void { reset(); setEditing(false); save.reset() }

  const modeLabel: Record<string, string> = {
    Cash: 'Cash', Cheque: 'Cheque', BankTransfer: 'Bank Transfer', DirectDeposit: 'Direct Deposit (NEFT/RTGS)',
  }

  return (
    <section className="border border-[var(--color-border)] rounded-xl p-5">
      <SectionHeader title="Payment Information" editing={editing} onEdit={() => setEditing(true)} onCancel={cancel} />

      {!editing ? (
        <div className="grid grid-cols-2 gap-x-8 gap-y-4">
          <ReadField label="Payment Mode" value={modeLabel[employee.paymentMode] ?? employee.paymentMode} />
          {(employee.paymentMode === 'BankTransfer' || employee.paymentMode === 'DirectDeposit') && (
            <>
              <ReadField label="Account Holder Name" value={employee.accountHolderName} />
              <ReadField label="Bank Name" value={employee.bankName} />
              <ReadField label="Account Type" value={employee.accountType} />
              <ReadField label="Account Number" value={employee.maskedAccountNumber} />
            </>
          )}
        </div>
      ) : (
        <form onSubmit={handleSubmit(v => save.mutate(v))} className="space-y-4">
          <div>
            <label className={fieldLabelCls}>Payment Mode <span className="text-red-500">*</span></label>
            <select {...register('paymentMode')} className={`${inputCls} w-64`}>
              <option value="Cash">Cash</option>
              <option value="Cheque">Cheque</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="DirectDeposit">Direct Deposit (NEFT/RTGS)</option>
            </select>
          </div>

          {hasBankFields && (
            <>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={fieldLabelCls}>Account Holder Name</label>
                  <input {...register('accountHolderName')} className={inputCls} />
                </div>
                <div>
                  <label className={fieldLabelCls}>Bank Name</label>
                  <input {...register('bankName')} className={inputCls} />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={fieldLabelCls}>Account Type</label>
                  <select {...register('accountType')} className={inputCls}>
                    <option value="">Select</option>
                    <option value="Savings">Savings</option>
                    <option value="Current">Current</option>
                    <option value="Salary">Salary</option>
                  </select>
                </div>
                <div>
                  <label className={fieldLabelCls}>IFSC Code</label>
                  <input {...register('ifscCode')} className={`${inputCls} font-mono uppercase`} maxLength={11} placeholder="HDFC0001234" />
                  {errors.ifscCode && <p className={errCls}>{errors.ifscCode.message}</p>}
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={fieldLabelCls}>Account Number</label>
                  <input
                    type="password"
                    {...register('accountNumber')}
                    className={inputCls}
                    autoComplete="new-password"
                    placeholder="Enter new account number"
                  />
                  {employee.maskedAccountNumber && (
                    <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">Current: {employee.maskedAccountNumber} (leave blank to keep)</p>
                  )}
                  {errors.accountNumber && <p className={errCls}>{errors.accountNumber.message}</p>}
                </div>
                <div>
                  <label className={fieldLabelCls}>Re-enter Account Number</label>
                  <input
                    type="password"
                    {...register('confirmAccountNumber')}
                    className={inputCls}
                    autoComplete="new-password"
                  />
                  {errors.confirmAccountNumber && <p className={errCls}>{errors.confirmAccountNumber.message}</p>}
                </div>
              </div>
            </>
          )}

          {save.isError && <p className={errCls}>Failed to save. Please try again.</p>}

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={!isDirty || save.isPending}
              className="inline-flex items-center gap-1.5 h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg disabled:opacity-50 hover:bg-[var(--color-primary-hover)] transition-colors"
            >
              <Check className="w-3.5 h-3.5" />
              {save.isPending ? 'Saving…' : 'Save'}
            </button>
            <button type="button" onClick={cancel} className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  )
}

// ── Root ───────────────────────────────────────────────────────────────────────

export default function EmployeeOverviewTab({ employee, onSaved }: Props): React.ReactElement {
  return (
    <div className="p-5 space-y-4">
      <BasicSection employee={employee} onSaved={onSaved} />
      <PersonalSection employee={employee} onSaved={onSaved} />
      <StatutorySection employee={employee} onSaved={onSaved} />
      <PaymentSection employee={employee} onSaved={onSaved} />
    </div>
  )
}
