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
import type { CostCentre } from './CostCentresPage'

const schema = z.object({
  name: z.string().min(1, 'Cost Centre Name is required').max(150),
  code: z.string().max(20).optional().or(z.literal('')),
})

type FormData = z.infer<typeof schema>

interface Props {
  costCentreId: string | null
  onClose: () => void
  onSaved: () => void
}

export default function CostCentreFormModal({ costCentreId, onClose, onSaved }: Props): ReactElement {
  const isEdit = costCentreId !== null
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', code: '' },
  })
  const { success: toastSuccess, error: toastError } = useToast()

  const { data: costCentre } = useQuery({
    queryKey: ['cost-centre', costCentreId],
    queryFn: () => api.get<CostCentre>(`/api/v1/cost-centres/${costCentreId ?? ''}`).then(r => r.data),
    enabled: isEdit,
  })

  useEffect(() => {
    if (isEdit && costCentre) {
      reset({
        name: costCentre.name,
        code: costCentre.code ?? '',
      })
    }
  }, [costCentre, isEdit, reset])

  const createMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.post('/api/v1/cost-centres', {
        name: data.name,
        code: data.code ?? null,
      }),
    onSuccess: () => {
      toastSuccess('Cost Centre created')
      onSaved()
    },
    onError: () => {
      toastError('Failed to create cost centre')
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.put(`/api/v1/cost-centres/${costCentreId ?? ''}`, {
        name: data.name,
        code: data.code ?? null,
      }),
    onSuccess: () => {
      toastSuccess('Cost Centre updated')
      onSaved()
    },
    onError: () => {
      toastError('Failed to update cost centre')
    },
  })

  const isPending = isEdit ? updateMutation.isPending : createMutation.isPending

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg max-w-md w-full">
        <div className="flex items-center justify-between p-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">
            {isEdit ? 'Edit Cost Centre' : 'New Cost Centre'}
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
            label="Cost Centre Name"
            required
            error={errors.name?.message}
            {...register('name')}
          />
          <Input
            label="Code"
            placeholder="e.g., CC001"
            error={errors.code?.message}
            {...register('code')}
          />
          <div className="flex items-center gap-3 pt-2">
            <Button type="submit" variant="primary" size="sm" loading={isPending}>Save</Button>
            <Button type="button" variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
