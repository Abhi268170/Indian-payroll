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
import type { Designation } from './DesignationsPage'

const schema = z.object({
  name: z.string().min(1, 'Designation Name is required').max(150),
})

type FormData = z.infer<typeof schema>

interface Props {
  designationId: string | null
  onClose: () => void
  onSaved: () => void
}

export default function DesignationFormModal({ designationId, onClose, onSaved }: Props): ReactElement {
  const isEdit = designationId !== null
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: '' },
  })
  const { success: toastSuccess, error: toastError } = useToast()

  const { data: designation } = useQuery({
    queryKey: ['designation', designationId],
    queryFn: () => api.get<Designation>(`/api/v1/designations/${designationId}`).then(r => r.data),
    enabled: isEdit,
  })

  useEffect(() => {
    if (isEdit && designation) {
      reset({ name: designation.name })
    }
  }, [designation, isEdit, reset])

  const createMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.post('/api/v1/designations', { name: data.name }),
    onSuccess: () => {
      toastSuccess('Designation created')
      onSaved()
    },
    onError: () => {
      toastError('Failed to create designation')
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormData) =>
      api.put(`/api/v1/designations/${designationId}`, { name: data.name }),
    onSuccess: () => {
      toastSuccess('Designation updated')
      onSaved()
    },
    onError: () => {
      toastError('Failed to update designation')
    },
  })

  const isPending = isEdit ? updateMutation.isPending : createMutation.isPending

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg max-w-md w-full">
        <div className="flex items-center justify-between p-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">
            {isEdit ? 'Edit Designation' : 'New Designation'}
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
          onSubmit={handleSubmit(data => {
            if (isEdit) {
              updateMutation.mutate(data)
            } else {
              createMutation.mutate(data)
            }
          })}
        >
          <Input
            label="Designation Name"
            required
            error={errors.name?.message}
            {...register('name')}
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
