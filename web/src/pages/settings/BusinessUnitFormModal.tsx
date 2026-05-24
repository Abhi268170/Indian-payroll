import { useEffect, type ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useToast } from '@/components/ui/useToast'
import type { BusinessUnit } from './BusinessUnitsPage'

const schema = z.object({
  name: z.string().min(1, 'Business Unit Name is required').max(150),
  description: z.string().max(500).optional().or(z.literal('')),
})

type FormData = z.infer<typeof schema>

interface Props {
  businessUnitId: string | null
  onClose: () => void
  onSaved: () => void
}

export default function BusinessUnitFormModal({ businessUnitId, onClose, onSaved }: Props): ReactElement {
  const isEdit = businessUnitId !== null
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', description: '' },
  })
  const { success: toastSuccess, error: toastError } = useToast()

  const { data: businessUnit } = useQuery({
    queryKey: ['business-unit', businessUnitId],
    queryFn: () => api.get<BusinessUnit>(`/api/v1/business-units/${businessUnitId ?? ''}`).then(r => r.data),
    enabled: isEdit,
  })

  useEffect(() => {
    if (isEdit && businessUnit) {
      reset({
        name: businessUnit.name,
        description: businessUnit.description ?? '',
      })
    }
  }, [businessUnit, isEdit, reset])

  const createMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.post('/api/v1/business-units', {
        name: data.name,
        description: data.description ?? null,
      }),
    onSuccess: () => {
      toastSuccess('Business Unit created')
      onSaved()
    },
    onError: () => {
      toastError('Failed to create business unit')
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.put(`/api/v1/business-units/${businessUnitId ?? ''}`, {
        name: data.name,
        description: data.description ?? null,
      }),
    onSuccess: () => {
      toastSuccess('Business Unit updated')
      onSaved()
    },
    onError: () => {
      toastError('Failed to update business unit')
    },
  })

  const isPending = isEdit ? updateMutation.isPending : createMutation.isPending

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg max-w-md w-full">
        <div className="flex items-center justify-between p-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">
            {isEdit ? 'Edit Business Unit' : 'New Business Unit'}
          </h2>
          <button
            onClick={onClose}
            className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
        <form
          className="space-y-4 p-5"
          onSubmit={e => {
            void handleSubmit(data => {
              if (isEdit) {
                updateMutation.mutate(data)
              } else {
                createMutation.mutate(data)
              }
            })(e)
          }}
        >
          <Input
            label="Business Unit Name"
            required
            error={errors.name?.message}
            {...register('name')}
          />
          <div>
            <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-2">Description</label>
            <textarea
              className="w-full px-3 py-2 border border-[var(--color-border)] rounded-lg text-[13px] focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]"
              placeholder="Max 500 characters"
              maxLength={500}
              {...register('description')}
            />
            {errors.description && <p className="text-xs text-[var(--color-error)] mt-1">{errors.description.message}</p>}
          </div>
          <div className="flex items-center gap-3 pt-2">
            <Button type="submit" variant="primary" size="sm" loading={isPending}>Save</Button>
            <Button type="button" variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
