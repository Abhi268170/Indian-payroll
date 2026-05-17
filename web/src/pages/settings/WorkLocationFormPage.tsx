import { type ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { useToast } from '@/components/ui/useToast'
import { INDIAN_STATES, stateLabel, type WorkLocation } from './WorkLocationsPage'

const createSchema = z.object({
  name: z.string().min(1, 'Work Location Name is required').max(150),
  addressLine1: z.string().max(250).optional().or(z.literal('')),
  addressLine2: z.string().max(250).optional().or(z.literal('')),
  state: z.string().min(1, 'State is required'),
  city: z.string().max(250).optional().or(z.literal('')),
  pinCode: z.string().regex(/^\d{6}$/, 'Must be 6 digits').optional().or(z.literal('')),
})

const updateSchema = createSchema.omit({ state: true }).extend({
  ptRegistrationNumber: z.string().max(50).optional().or(z.literal('')),
})

type CreateFormData = z.infer<typeof createSchema>
type UpdateFormData = z.infer<typeof updateSchema>

interface Props {
  location: WorkLocation | null
  onSaved: () => void
  onCancel: () => void
}

export default function WorkLocationFormPage({ location, onSaved, onCancel }: Props): ReactElement {
  const isEdit = location !== null
  const { success: toastSuccess, error: toastError } = useToast()

  const createForm = useForm<CreateFormData>({
    resolver: zodResolver(createSchema),
    defaultValues: { name: '', addressLine1: '', addressLine2: '', state: '', city: '', pinCode: '' },
  })

  const updateForm = useForm<UpdateFormData>({
    resolver: zodResolver(updateSchema),
    defaultValues: {
      name: location?.name ?? '',
      addressLine1: location?.addressLine1 ?? '',
      addressLine2: location?.addressLine2 ?? '',
      city: location?.city ?? '',
      pinCode: location?.pinCode ?? '',
      ptRegistrationNumber: location?.ptRegistrationNumber ?? '',
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
      toastSuccess('Work location created')
      onSaved()
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
        ptRegistrationNumber: data.ptRegistrationNumber || null,
      }),
    onSuccess: () => {
      toastSuccess('Work location updated')
      onSaved()
    },
    onError: () => {
      toastError('Failed to update work location')
    },
  })

  if (isEdit) {
    const { register, handleSubmit, formState: { errors } } = updateForm
    const isPending = updateMutation.isPending

    return (
      <div className="px-8 py-8 max-w-2xl">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)] mb-8">
          Edit Work Location
        </h1>
        <form
          className="space-y-5"
          onSubmit={handleSubmit(data => updateMutation.mutate(data))}
        >
          <Input
            label="Work Location Name"
            required
            error={errors.name?.message}
            {...register('name')}
          />

          <fieldset>
            <legend className="text-sm font-medium text-[var(--color-text-primary)] mb-3">
              Address
            </legend>
            <div className="space-y-3">
              <Input
                placeholder="Address Line 1"
                error={errors.addressLine1?.message}
                {...register('addressLine1')}
              />
              <Input
                placeholder="Address Line 2"
                error={errors.addressLine2?.message}
                {...register('addressLine2')}
              />
              <div style={{ display: 'flex', gap: '12px' }}>
                <div style={{ flex: 1 }} className="space-y-1">
                  <div className="h-9 px-3 flex items-center border border-[var(--color-border)] rounded-lg bg-gray-50 text-sm text-[var(--color-text-secondary)]">
                    {stateLabel(location.state)}
                  </div>
                  <p className="text-xs text-[var(--color-text-muted)]">State (fixed)</p>
                </div>
                <div style={{ flex: 1 }}>
                  <Input placeholder="City" error={errors.city?.message} {...register('city')} />
                </div>
                <div style={{ flex: 1 }}>
                  <Input placeholder="PIN Code" maxLength={6} error={errors.pinCode?.message} {...register('pinCode')} />
                </div>
              </div>
            </div>
          </fieldset>

          <div>
            <label className="block text-sm font-medium text-[var(--color-text-primary)] mb-1">
              PT Registration Number
            </label>
            <Input
              placeholder="e.g. 27999999999P"
              maxLength={50}
              error={errors.ptRegistrationNumber?.message}
              {...register('ptRegistrationNumber')}
            />
            <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
              Professional Tax registration number for this work location's state (if applicable).
            </p>
          </div>

          <div className="flex items-center gap-3 pt-2">
            <Button type="submit" variant="primary" loading={isPending}>Save</Button>
            <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
            <span className="text-[12px] text-[var(--color-error)] ml-2">* Indicates mandatory fields</span>
          </div>
        </form>
      </div>
    )
  }

  const { register, handleSubmit, formState: { errors } } = createForm
  const isPending = createMutation.isPending

  return (
    <div className="px-8 py-8 max-w-2xl">
      <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)] mb-8">
        New Work Location
      </h1>
      <form
        className="space-y-5"
        onSubmit={handleSubmit(data => createMutation.mutate(data))}
      >
        <Input
          label="Work Location Name"
          required
          error={errors.name?.message}
          {...register('name')}
        />

        <fieldset>
          <legend className="text-sm font-medium text-[var(--color-text-primary)] mb-3">
            Address <span className="text-[var(--color-error)]">*</span>
          </legend>
          <div className="space-y-3">
            <Input
              placeholder="Address Line 1"
              error={errors.addressLine1?.message}
              {...register('addressLine1')}
            />
            <Input
              placeholder="Address Line 2"
              error={errors.addressLine2?.message}
              {...register('addressLine2')}
            />
            <div style={{ display: 'flex', gap: '12px' }}>
              <div style={{ flex: 1 }}>
                <Select
                  placeholder="Select a state"
                  options={INDIAN_STATES as unknown as { value: string; label: string }[]}
                  error={errors.state?.message}
                  {...register('state')}
                />
              </div>
              <div style={{ flex: 1 }}>
                <Input placeholder="City" error={errors.city?.message} {...register('city')} />
              </div>
              <div style={{ flex: 1 }}>
                <Input placeholder="PIN Code" maxLength={6} error={errors.pinCode?.message} {...register('pinCode')} />
              </div>
            </div>
          </div>
        </fieldset>

        <div className="flex items-center gap-3 pt-2">
          <Button type="submit" variant="primary" loading={isPending}>Save</Button>
          <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
          <span className="text-[12px] text-[var(--color-error)] ml-2">* Indicates mandatory fields</span>
        </div>
      </form>
    </div>
  )
}
