import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { DepartmentDto, DesignationDto, CreateEmployeeRequest } from '@/types/api'
import type { WorkLocation } from '@/pages/settings/WorkLocationsPage'
import type { BusinessUnit } from '@/pages/settings/BusinessUnitsPage'
import InlineCreateModal from './InlineCreateModal'

const schema = z.object({
  firstName: z.string().min(1, 'Required').max(100),
  middleName: z.string().max(100).optional(),
  lastName: z.string().min(1, 'Required').max(100),
  employeeCode: z.string().max(20).optional(),
  workEmail: z.string().email('Invalid email'),
  mobileNumber: z.string().regex(/^\d{10}$/, '10 digits required').optional().or(z.literal('')),
  gender: z.enum(['Male', 'Female', 'Other'], { required_error: 'Required' }),
  dateOfJoining: z.string().min(1, 'Required'),
  dateOfBirth: z.string().min(1, 'Required'),
  employmentType: z.enum(['FullTime', 'PartTime', 'Contract', 'Intern'], { required_error: 'Required' }),
  isDirector: z.boolean(),
  enablePortalAccess: z.boolean(),
  departmentId: z.string().min(1, 'Required'),
  designationId: z.string().min(1, 'Required'),
  workLocationId: z.string().min(1, 'Required'),
  businessUnitId: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'
const errCls = 'mt-1 text-[11px] text-red-500'

interface Props {
  onSuccess: (id: string) => void
  onCancel: () => void
}

export default function WizardStep1Basic({ onSuccess, onCancel }: Props): React.ReactElement {
  const [showNewDept, setShowNewDept] = useState(false)
  const [showNewDesig, setShowNewDesig] = useState(false)
  const [showNewBU, setShowNewBU] = useState(false)

  const { data: departments = [], refetch: refetchDepts } = useQuery<DepartmentDto[]>({
    queryKey: ['departments'],
    queryFn: () => api.get<DepartmentDto[]>('/api/v1/departments').then(r => r.data),
  })
  const { data: designations = [], refetch: refetchDesigs } = useQuery<DesignationDto[]>({
    queryKey: ['designations'],
    queryFn: () => api.get<DesignationDto[]>('/api/v1/designations').then(r => r.data),
  })
  const { data: workLocations = [] } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })
  const { data: businessUnits = [], refetch: refetchBUs } = useQuery<BusinessUnit[]>({
    queryKey: ['business-units'],
    queryFn: () => api.get<BusinessUnit[]>('/api/v1/business-units').then(r => r.data),
  })

  const { register, handleSubmit, setValue, watch, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      isDirector: false,
      enablePortalAccess: false,
      workLocationId: '',
    },
  })

  // Auto-select first work location once the query resolves
  useEffect(() => {
    const first = workLocations[0]
    if (first) setValue('workLocationId', first.id, { shouldValidate: false })
  }, [workLocations, setValue])

  const workEmail = watch('workEmail')

  const create = useMutation({
    mutationFn: (body: CreateEmployeeRequest) =>
      api.post<{ id: string }>('/api/v1/employees', body).then(r => r.data),
    onSuccess: data => onSuccess(data.id),
  })

  function onSubmit(v: FormValues): void {
    create.mutate({
      firstName: v.firstName,
      middleName: v.middleName || undefined,
      lastName: v.lastName,
      employeeCode: v.employeeCode || undefined,
      workEmail: v.workEmail,
      mobileNumber: v.mobileNumber || undefined,
      gender: v.gender,
      dateOfJoining: v.dateOfJoining,
      dateOfBirth: v.dateOfBirth,
      employmentType: v.employmentType,
      isDirector: v.isDirector,
      enablePortalAccess: v.enablePortalAccess,
      departmentId: v.departmentId,
      designationId: v.designationId,
      workLocationId: v.workLocationId,
      businessUnitId: v.businessUnitId || undefined,
    })
  }

  return (
    <>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        {/* Name */}
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className={labelCls}>First Name <span className="text-red-500">*</span></label>
            <input {...register('firstName')} className={inputCls} />
            {errors.firstName && <p className={errCls}>{errors.firstName.message}</p>}
          </div>
          <div>
            <label className={labelCls}>Middle Name</label>
            <input {...register('middleName')} className={inputCls} />
          </div>
          <div>
            <label className={labelCls}>Last Name <span className="text-red-500">*</span></label>
            <input {...register('lastName')} className={inputCls} />
            {errors.lastName && <p className={errCls}>{errors.lastName.message}</p>}
          </div>
        </div>

        {/* Email + Code */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelCls}>Work Email <span className="text-red-500">*</span></label>
            <input type="email" {...register('workEmail')} className={inputCls} placeholder="name@company.com" />
            {workEmail && (
              <p className="mt-1 text-[11px] text-amber-600">
                Email cannot be changed after creation — used for portal login and payslips.
              </p>
            )}
            {errors.workEmail && <p className={errCls}>{errors.workEmail.message}</p>}
          </div>
          <div>
            <label className={labelCls}>
              Employee Code
              <span className="text-[11px] text-[var(--color-text-secondary)] font-normal ml-1">(auto-generated if blank)</span>
            </label>
            <input {...register('employeeCode')} className={inputCls} placeholder="e.g. EMP001" />
          </div>
        </div>

        {/* Mobile + Gender */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelCls}>Mobile Number</label>
            <input {...register('mobileNumber')} className={inputCls} placeholder="10-digit number" />
            {errors.mobileNumber && <p className={errCls}>{errors.mobileNumber.message}</p>}
          </div>
          <div>
            <label className={labelCls}>Gender <span className="text-red-500">*</span></label>
            <select {...register('gender')} className={inputCls}>
              <option value="">Select</option>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
            {errors.gender && <p className={errCls}>{errors.gender.message}</p>}
          </div>
        </div>

        {/* Dates */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelCls}>Date of Joining <span className="text-red-500">*</span></label>
            <input type="date" {...register('dateOfJoining')} className={inputCls} />
            {errors.dateOfJoining && <p className={errCls}>{errors.dateOfJoining.message}</p>}
          </div>
          <div>
            <label className={labelCls}>Date of Birth <span className="text-red-500">*</span></label>
            <input type="date" {...register('dateOfBirth')} className={inputCls} />
            {errors.dateOfBirth && <p className={errCls}>{errors.dateOfBirth.message}</p>}
          </div>
        </div>

        {/* Employment type */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelCls}>Employment Type <span className="text-red-500">*</span></label>
            <select {...register('employmentType')} className={inputCls}>
              <option value="">Select</option>
              <option value="FullTime">Full Time</option>
              <option value="PartTime">Part Time</option>
              <option value="Contract">Contract</option>
              <option value="Intern">Intern</option>
            </select>
            {errors.employmentType && <p className={errCls}>{errors.employmentType.message}</p>}
          </div>
        </div>

        {/* Org */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-[12px] font-medium text-[var(--color-text-secondary)]">
                Department <span className="text-red-500">*</span>
              </label>
              <button
                type="button"
                onClick={() => setShowNewDept(true)}
                className="text-[11px] text-[var(--color-primary)] hover:underline"
              >
                + New
              </button>
            </div>
            <select {...register('departmentId')} className={inputCls}>
              <option value="">Select</option>
              {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
            {errors.departmentId && <p className={errCls}>{errors.departmentId.message}</p>}
          </div>
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-[12px] font-medium text-[var(--color-text-secondary)]">
                Designation <span className="text-red-500">*</span>
              </label>
              <button
                type="button"
                onClick={() => setShowNewDesig(true)}
                className="text-[11px] text-[var(--color-primary)] hover:underline"
              >
                + New
              </button>
            </div>
            <select {...register('designationId')} className={inputCls}>
              <option value="">Select</option>
              {designations.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
            {errors.designationId && <p className={errCls}>{errors.designationId.message}</p>}
          </div>
          <div>
            <label className={labelCls}>Work Location <span className="text-red-500">*</span></label>
            <select {...register('workLocationId')} className={inputCls}>
              <option value="">Select</option>
              {workLocations.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
            {errors.workLocationId && <p className={errCls}>{errors.workLocationId.message}</p>}
          </div>
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-[12px] font-medium text-[var(--color-text-secondary)]">
                Business Unit
              </label>
              <button
                type="button"
                onClick={() => setShowNewBU(true)}
                className="text-[11px] text-[var(--color-primary)] hover:underline"
              >
                + New
              </button>
            </div>
            <select {...register('businessUnitId')} className={inputCls}>
              <option value="">None</option>
              {businessUnits.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
          </div>
        </div>

        {/* Toggles */}
        <div className="flex items-center gap-6 pt-1">
          <label className="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" {...register('isDirector')} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
            <span className="text-[13px]">
              Director / person with substantial interest
              <span className="block text-[11px] text-[var(--color-text-secondary)]">Affects TDS perquisite computation</span>
            </span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" {...register('enablePortalAccess')} className="w-4 h-4 rounded accent-[var(--color-primary)]" />
            <span className="text-[13px]">
              Enable Portal Access
              <span className="block text-[11px] text-[var(--color-text-secondary)]">View payslips, submit IT declarations</span>
            </span>
          </label>
        </div>

        {create.isError && (
          <p className="text-[12px] text-red-600">Failed to create employee. Please try again.</p>
        )}

        <div className="flex items-center gap-3 pt-2 border-t border-[var(--color-border)]">
          <button
            type="submit"
            disabled={isSubmitting || create.isPending}
            className="h-9 px-5 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
          >
            {create.isPending ? 'Saving…' : 'Save and Continue'}
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
          >
            Cancel
          </button>
          <p className="ml-auto text-[11px] text-[var(--color-text-secondary)]">* Required fields</p>
        </div>
      </form>

      {showNewDept && (
        <InlineCreateModal
          title="New Department"
          fields={[
            { name: 'name', label: 'Department Name', required: true },
            { name: 'code', label: 'Department Code', required: false },
          ]}
          onSave={async (values) => {
            const res = await api.post<{ id: string }>('/api/v1/departments', {
              name: values.name,
              code: values.code || null,
            })
            await refetchDepts()
            setValue('departmentId', res.data.id)
            setShowNewDept(false)
          }}
          onClose={() => setShowNewDept(false)}
        />
      )}

      {showNewDesig && (
        <InlineCreateModal
          title="New Designation"
          fields={[{ name: 'name', label: 'Designation Name', required: true }]}
          onSave={async (values) => {
            const res = await api.post<{ id: string }>('/api/v1/designations', {
              name: values.name,
            })
            await refetchDesigs()
            setValue('designationId', res.data.id)
            setShowNewDesig(false)
          }}
          onClose={() => setShowNewDesig(false)}
        />
      )}

      {showNewBU && (
        <InlineCreateModal
          title="New Business Unit"
          fields={[
            { name: 'name', label: 'Business Unit Name', required: true },
            { name: 'description', label: 'Description', required: false },
          ]}
          onSave={async (values) => {
            const res = await api.post<{ id: string }>('/api/v1/business-units', {
              name: values.name,
              description: values.description || null,
            })
            await refetchBUs()
            setValue('businessUnitId', res.data.id)
            setShowNewBU(false)
          }}
          onClose={() => setShowNewBU(false)}
        />
      )}
    </>
  )
}
