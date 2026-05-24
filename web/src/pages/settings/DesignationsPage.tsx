import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Trash2, Edit2 } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import DesignationFormModal from './DesignationFormModal'

export interface Designation {
  id: string
  name: string
}

export default function DesignationsPage(): ReactElement {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: designations = [], isLoading } = useQuery<Designation[]>({
    queryKey: ['designations'],
    queryFn: () => api.get<Designation[]>('/api/v1/designations').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/designations/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['designations'] })
      toastSuccess('Designation deleted')
    },
    onError: () => {
      toastError('Cannot delete — employees assigned to this designation')
    },
  })

  function handleDelete(desig: Designation): void {
    if (!confirm(`Delete "${desig.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(desig.id)
  }

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Designations</h1>
        <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
          New Designation
        </Button>
      </div>

      {showModal && (
        <DesignationFormModal
          designationId={editingId}
          onClose={() => { setShowModal(false); setEditingId(null) }}
          onSaved={() => {
            void qc.invalidateQueries({ queryKey: ['designations'] })
            setShowModal(false)
            setEditingId(null)
          }}
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <Spinner />
        </div>
      ) : designations.length === 0 ? (
        <EmptyState
          heading="Track employee job titles with designations"
          subtext="Create designation based on the ones present in the organization and associate with employees"
          action={
            <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
              New Designation
            </Button>
          }
        />
      ) : (
        <div className="space-y-2">
          {designations.map(desig => (
            <div key={desig.id} className="flex items-center justify-between p-4 border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-page-bg)]">
              <span className="text-[13px] text-[var(--color-text-primary)]">{desig.name}</span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => { setEditingId(desig.id); setShowModal(true) }}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
                  title="Edit"
                >
                  <Edit2 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => { handleDelete(desig); }}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-error)] hover:bg-red-50 transition-colors"
                  title="Delete"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
