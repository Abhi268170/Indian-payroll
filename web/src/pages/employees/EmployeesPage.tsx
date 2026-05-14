import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'
import type { EmployeeDto, DepartmentDto, DesignationDto, BranchDto, CostCentreDto } from '@/types/api'

const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/

const schema = z.object({
  firstName: z.string().min(1, 'Required').max(100),
  lastName: z.string().min(1, 'Required').max(100),
  employeeCode: z.string().min(1, 'Required').max(20),
  pan: z.string().regex(PAN_REGEX, 'Invalid PAN (e.g. ABCDE1234F)'),
  dateOfBirth: z.string().min(1, 'Required'),
  gender: z.enum(['Male', 'Female', 'Other']),
  dateOfJoining: z.string().min(1, 'Required'),
  workState: z.string().min(1, 'Select a state'),
  employmentType: z.enum(['FullTime', 'PartTime', 'Contract', 'Intern']),
  departmentId: z.string().min(1, 'Select a department'),
  designationId: z.string().min(1, 'Select a designation'),
  branchId: z.string().optional(),
  costCentreId: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

const INDIAN_STATES = [
  'AN','AP','AR','AS','BR','CG','CH','DD','DL','DN',
  'GA','GJ','HP','HR','JH','JK','KA','KL','LA','LD',
  'MH','ML','MN','MP','MZ','NL','OR','PB','PY','RJ',
  'SK','TG','TN','TR','UP','UT','WB',
]

export default function EmployeesPage(): React.ReactElement {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { data: employees, isLoading } = useQuery<EmployeeDto[]>({
    queryKey: ['employees'],
    queryFn: () => api.get<EmployeeDto[]>('/api/employees').then(r => r.data),
  })
  const { data: departments } = useQuery<DepartmentDto[]>({
    queryKey: ['departments'],
    queryFn: () => api.get<DepartmentDto[]>('/api/org/departments').then(r => r.data),
  })
  const { data: designations } = useQuery<DesignationDto[]>({
    queryKey: ['designations'],
    queryFn: () => api.get<DesignationDto[]>('/api/org/designations').then(r => r.data),
  })
  const { data: branches } = useQuery<BranchDto[]>({
    queryKey: ['branches'],
    queryFn: () => api.get<BranchDto[]>('/api/org/branches').then(r => r.data),
  })
  const { data: costCentres } = useQuery<CostCentreDto[]>({
    queryKey: ['cost-centres'],
    queryFn: () => api.get<CostCentreDto[]>('/api/org/cost-centres').then(r => r.data),
  })

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const create = useMutation({
    mutationFn: (v: FormValues) => api.post('/api/employees', {
      firstName: v.firstName,
      lastName: v.lastName,
      employeeCode: v.employeeCode,
      pan: v.pan,
      dateOfBirth: v.dateOfBirth,
      gender: v.gender,
      dateOfJoining: v.dateOfJoining,
      workState: v.workState,
      employmentType: v.employmentType,
      departmentId: v.departmentId,
      designationId: v.designationId,
      branchId: v.branchId ?? null,
      costCentreId: v.costCentreId ?? null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['employees'] })
      reset()
      setShowForm(false)
      setApiError(null)
    },
    onError: () => setApiError('Failed to create employee.'),
  })

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500'
  const errorCls = 'mt-1 text-xs text-red-500'

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-semibold text-gray-900">Employees</h1>
        <button
          onClick={() => setShowForm(f => !f)}
          className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 transition-colors"
        >
          {showForm ? 'Cancel' : '+ New Employee'}
        </button>
      </div>

      {showForm && (
        <form
          onSubmit={handleSubmit(v => create.mutate(v))}
          className="bg-white border border-gray-200 rounded-xl p-5 mb-6 space-y-4 max-w-2xl"
        >
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input {...register('firstName')} className={inputCls} />
              {errors.firstName && <p className={errorCls}>{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input {...register('lastName')} className={inputCls} />
              {errors.lastName && <p className={errorCls}>{errors.lastName.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Employee Code</label>
              <input {...register('employeeCode')} className={inputCls} placeholder="e.g. EMP001" />
              {errors.employeeCode && <p className={errorCls}>{errors.employeeCode.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">PAN</label>
              <input {...register('pan')} className={inputCls} placeholder="ABCDE1234F" />
              {errors.pan && <p className={errorCls}>{errors.pan.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Date of Birth</label>
              <input type="date" {...register('dateOfBirth')} className={inputCls} />
              {errors.dateOfBirth && <p className={errorCls}>{errors.dateOfBirth.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Gender</label>
              <select {...register('gender')} className={inputCls}>
                <option value="">Select</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
              {errors.gender && <p className={errorCls}>{errors.gender.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Date of Joining</label>
              <input type="date" {...register('dateOfJoining')} className={inputCls} />
              {errors.dateOfJoining && <p className={errorCls}>{errors.dateOfJoining.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Employment Type</label>
              <select {...register('employmentType')} className={inputCls}>
                <option value="">Select</option>
                <option value="FullTime">Full Time</option>
                <option value="PartTime">Part Time</option>
                <option value="Contract">Contract</option>
                <option value="Intern">Intern</option>
              </select>
              {errors.employmentType && <p className={errorCls}>{errors.employmentType.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Work State</label>
              <select {...register('workState')} className={inputCls}>
                <option value="">Select state</option>
                {INDIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
              {errors.workState && <p className={errorCls}>{errors.workState.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Department</label>
              <select {...register('departmentId')} className={inputCls}>
                <option value="">Select department</option>
                {departments?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
              {errors.departmentId && <p className={errorCls}>{errors.departmentId.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Designation</label>
              <select {...register('designationId')} className={inputCls}>
                <option value="">Select designation</option>
                {designations?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
              {errors.designationId && <p className={errorCls}>{errors.designationId.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Branch (optional)</label>
              <select {...register('branchId')} className={inputCls}>
                <option value="">None</option>
                {branches?.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Cost Centre (optional)</label>
              <select {...register('costCentreId')} className={inputCls}>
                <option value="">None</option>
                {costCentres?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
          </div>
          {apiError && <p className="text-xs text-red-600">{apiError}</p>}
          <button
            type="submit"
            disabled={isSubmitting || create.isPending}
            className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 disabled:opacity-50 transition-colors"
          >
            Create Employee
          </button>
        </form>
      )}

      {isLoading ? (
        <p className="text-sm text-gray-500">Loading…</p>
      ) : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Code</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Name</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Gender</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Joined</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Type</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {employees?.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-6 text-center text-gray-400">No employees yet.</td></tr>
              )}
              {employees?.map(e => (
                <tr key={e.id}>
                  <td className="px-4 py-3 text-gray-500 font-mono">{e.employeeCode}</td>
                  <td className="px-4 py-3 text-gray-900">{e.fullName}</td>
                  <td className="px-4 py-3 text-gray-500">{e.gender}</td>
                  <td className="px-4 py-3 text-gray-500">{e.dateOfJoining}</td>
                  <td className="px-4 py-3 text-gray-500">{e.employmentType}</td>
                  <td className="px-4 py-3">
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                      e.status === 'Active' ? 'bg-green-50 text-green-700' : 'bg-gray-100 text-gray-600'
                    }`}>{e.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
