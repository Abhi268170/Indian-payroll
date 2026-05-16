import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { MapPin, Plus, Pencil, Trash2, PowerOff, Power } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { Badge } from '@/components/ui/Badge'
import { Drawer } from '@/components/ui/Drawer'
import { EmptyState } from '@/components/ui/EmptyState'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'

// Mirrors IndianState enum — stored as string on the backend
const INDIAN_STATES = [
  { value: 'AndhraPradesh', label: 'Andhra Pradesh' },
  { value: 'ArunachalPradesh', label: 'Arunachal Pradesh' },
  { value: 'Assam', label: 'Assam' },
  { value: 'Bihar', label: 'Bihar' },
  { value: 'Chhattisgarh', label: 'Chhattisgarh' },
  { value: 'Goa', label: 'Goa' },
  { value: 'Gujarat', label: 'Gujarat' },
  { value: 'Haryana', label: 'Haryana' },
  { value: 'HimachalPradesh', label: 'Himachal Pradesh' },
  { value: 'Jharkhand', label: 'Jharkhand' },
  { value: 'Karnataka', label: 'Karnataka' },
  { value: 'Kerala', label: 'Kerala' },
  { value: 'MadhyaPradesh', label: 'Madhya Pradesh' },
  { value: 'Maharashtra', label: 'Maharashtra' },
  { value: 'Manipur', label: 'Manipur' },
  { value: 'Meghalaya', label: 'Meghalaya' },
  { value: 'Mizoram', label: 'Mizoram' },
  { value: 'Nagaland', label: 'Nagaland' },
  { value: 'Odisha', label: 'Odisha' },
  { value: 'Punjab', label: 'Punjab' },
  { value: 'Rajasthan', label: 'Rajasthan' },
  { value: 'Sikkim', label: 'Sikkim' },
  { value: 'TamilNadu', label: 'Tamil Nadu' },
  { value: 'Telangana', label: 'Telangana' },
  { value: 'Tripura', label: 'Tripura' },
  { value: 'UttarPradesh', label: 'Uttar Pradesh' },
  { value: 'Uttarakhand', label: 'Uttarakhand' },
  { value: 'WestBengal', label: 'West Bengal' },
  { value: 'AndamanAndNicobar', label: 'Andaman & Nicobar Islands' },
  { value: 'Chandigarh', label: 'Chandigarh' },
  { value: 'DadraAndNagarHaveliAndDamanAndDiu', label: 'Dadra & Nagar Haveli and Daman & Diu' },
  { value: 'Delhi', label: 'Delhi' },
  { value: 'JammuAndKashmir', label: 'Jammu & Kashmir' },
  { value: 'Ladakh', label: 'Ladakh' },
  { value: 'Lakshadweep', label: 'Lakshadweep' },
  { value: 'Puducherry', label: 'Puducherry' },
] as const

function stateLabel(value: string): string {
  return INDIAN_STATES.find(s => s.value === value)?.label ?? value
}

interface WorkLocation {
  id: string
  name: string
  addressLine1: string | null
  addressLine2: string | null
  state: string
  city: string | null
  pinCode: string | null
  isActive: boolean
  employeeCount: number
}

const createSchema = z.object({
  name: z.string().min(1, 'Name is required').max(150),
  state: z.string().min(1, 'State is required'),
  addressLine1: z.string().max(250).optional().or(z.literal('')),
  addressLine2: z.string().max(250).optional().or(z.literal('')),
  city: z.string().max(250).optional().or(z.literal('')),
  pinCode: z.string().regex(/^\d{6}$/, 'Must be 6 digits').optional().or(z.literal('')),
})

const updateSchema = createSchema.omit({ state: true })

type CreateFormData = z.infer<typeof createSchema>
type UpdateFormData = z.infer<typeof updateSchema>

function WorkLocationForm({
  location,
  onClose,
}: {
  location: WorkLocation | null
  onClose: () => void
}): ReactElement {
  const isEdit = location !== null
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()

  const createForm = useForm<CreateFormData>({
    resolver: zodResolver(createSchema),
    defaultValues: { name: '', state: '', addressLine1: '', addressLine2: '', city: '', pinCode: '' },
  })

  const updateForm = useForm<UpdateFormData>({
    resolver: zodResolver(updateSchema),
    defaultValues: {
      name: location?.name ?? '',
      addressLine1: location?.addressLine1 ?? '',
      addressLine2: location?.addressLine2 ?? '',
      city: location?.city ?? '',
      pinCode: location?.pinCode ?? '',
    },
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateFormData) =>
      api.post('/api/v1/work-locations', {
        name: data.name,
        state: data.state,
        addressLine1: data.addressLine1 || null,
        addressLine2: data.addressLine2 || null,
        city: data.city || null,
        pinCode: data.pinCode || null,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
      toastSuccess('Work location created')
      onClose()
    },
    onError: () => {
      toastError('Failed to create work location')
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateFormData) =>
      api.put(`/api/v1/work-locations/${location!.id}`, {
        name: data.name,
        addressLine1: data.addressLine1 || null,
        addressLine2: data.addressLine2 || null,
        city: data.city || null,
        pinCode: data.pinCode || null,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
      toastSuccess('Work location updated')
      onClose()
    },
    onError: () => {
      toastError('Failed to update work location')
    },
  })

  if (isEdit) {
    const { register, handleSubmit, formState: { errors } } = updateForm
    return (
      <Drawer
        title="Edit Work Location"
        subtitle={location.name}
        onClose={onClose}
        footer={
          <>
            <Button
              variant="primary"
              loading={updateMutation.isPending}
              onClick={() => { void handleSubmit(data => updateMutation.mutate(data))() }}
            >
              Save Changes
            </Button>
            <Button variant="secondary" onClick={onClose}>Cancel</Button>
          </>
        }
      >
        <form className="space-y-4" onSubmit={e => e.preventDefault()}>
          <Input label="Name" required error={errors.name?.message} {...register('name')} />
          <div className="space-y-1">
            <label className="block text-sm font-medium text-[var(--color-text-primary)]">State</label>
            <div className="h-9 px-3 flex items-center border border-[var(--color-border)] rounded-lg bg-gray-50 text-sm text-[var(--color-text-secondary)]">
              {stateLabel(location.state)}
            </div>
            <p className="text-xs text-[var(--color-text-muted)]">State cannot be changed after creation</p>
          </div>
          <Input label="Address Line 1" error={errors.addressLine1?.message} {...register('addressLine1')} />
          <Input label="Address Line 2" error={errors.addressLine2?.message} {...register('addressLine2')} />
          <Input label="City" error={errors.city?.message} {...register('city')} />
          <Input label="Pin Code" maxLength={6} error={errors.pinCode?.message} {...register('pinCode')} />
        </form>
      </Drawer>
    )
  }

  const { register, handleSubmit, formState: { errors } } = createForm
  return (
    <Drawer
      title="Add Work Location"
      onClose={onClose}
      footer={
        <>
          <Button
            variant="primary"
            loading={createMutation.isPending}
            onClick={() => { void handleSubmit(data => createMutation.mutate(data))() }}
          >
            Add Location
          </Button>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
        </>
      }
    >
      <form className="space-y-4" onSubmit={e => e.preventDefault()}>
        <Input label="Name" required error={errors.name?.message} {...register('name')} />
        <Select
          label="State"
          required
          error={errors.state?.message}
          placeholder="Select state"
          options={INDIAN_STATES as unknown as { value: string; label: string }[]}
          {...register('state')}
        />
        <Input label="Address Line 1" error={errors.addressLine1?.message} {...register('addressLine1')} />
        <Input label="Address Line 2" error={errors.addressLine2?.message} {...register('addressLine2')} />
        <Input label="City" error={errors.city?.message} {...register('city')} />
        <Input label="Pin Code" maxLength={6} error={errors.pinCode?.message} {...register('pinCode')} />
      </form>
    </Drawer>
  )
}

export default function WorkLocationsPage(): ReactElement {
  const [drawer, setDrawer] = useState<'create' | WorkLocation | null>(null)
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: locations = [], isLoading } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/work-locations/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
      toastSuccess('Work location deleted')
    },
    onError: () => {
      toastError('Cannot delete — location has employees assigned')
    },
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      api.post(`/api/v1/work-locations/${id}/${activate ? 'activate' : 'deactivate'}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
    },
    onError: () => {
      toastError('Failed to update status')
    },
  })

  function confirmDelete(loc: WorkLocation): void {
    if (loc.employeeCount > 0) {
      toastError(`Cannot delete — ${loc.employeeCount} employee(s) assigned`)
      return
    }
    if (!confirm(`Delete "${loc.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(loc.id)
  }

  return (
    <div className="max-w-5xl mx-auto px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-[18px] font-semibold text-[var(--color-text-primary)]">Work Locations</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-0.5">
            Offices and branches where employees work
          </p>
        </div>
        <Button variant="primary" size="sm" onClick={() => setDrawer('create')}>
          <Plus className="w-4 h-4" />
          Add Location
        </Button>
      </div>

      <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-16">
            <Spinner />
          </div>
        ) : locations.length === 0 ? (
          <EmptyState
            icon={<MapPin className="w-5 h-5" />}
            heading="No work locations yet"
            subtext="Add offices and branches where your employees work"
            action={
              <Button variant="primary" size="sm" onClick={() => setDrawer('create')}>
                <Plus className="w-4 h-4" />
                Add Location
              </Button>
            }
          />
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-page-bg)]">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Name</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">State</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">City</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Employees</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {locations.map(loc => (
                <tr key={loc.id} className="hover:bg-[var(--color-page-bg)] transition-colors">
                  <td className="px-4 py-3">
                    <div className="font-medium text-[var(--color-text-primary)]">{loc.name}</div>
                    {loc.addressLine1 && (
                      <div className="text-xs text-[var(--color-text-muted)] mt-0.5">{loc.addressLine1}</div>
                    )}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {stateLabel(loc.state)}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {loc.city ?? <span className="text-[var(--color-text-muted)]">—</span>}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {loc.employeeCount}
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={loc.isActive ? 'success' : 'neutral'}>
                      {loc.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1 justify-end">
                      <button
                        title="Edit"
                        onClick={() => setDrawer(loc)}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
                      >
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button
                        title={loc.isActive ? 'Deactivate' : 'Activate'}
                        onClick={() => toggleMutation.mutate({ id: loc.id, activate: !loc.isActive })}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
                      >
                        {loc.isActive ? <PowerOff className="w-3.5 h-3.5" /> : <Power className="w-3.5 h-3.5" />}
                      </button>
                      <button
                        title="Delete"
                        onClick={() => confirmDelete(loc)}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-error)] hover:bg-red-50 transition-colors"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {drawer !== null && (
        <WorkLocationForm
          location={drawer === 'create' ? null : drawer}
          onClose={() => setDrawer(null)}
        />
      )}
    </div>
  )
}
