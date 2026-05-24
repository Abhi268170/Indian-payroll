import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Trash2, Edit2 } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import BusinessUnitFormModal from './BusinessUnitFormModal'

export interface BusinessUnit {
  id: string
  name: string
  description: string | null
}

export default function BusinessUnitsPage(): ReactElement {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: businessUnits = [], isLoading } = useQuery<BusinessUnit[]>({
    queryKey: ['business-units'],
    queryFn: () => api.get<BusinessUnit[]>('/api/v1/business-units').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/business-units/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['business-units'] })
      toastSuccess('Business Unit deleted')
    },
    onError: () => {
      toastError('Cannot delete — employees assigned to this business unit')
    },
  })

  function handleDelete(bu: BusinessUnit): void {
    if (!confirm(`Delete "${bu.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(bu.id)
  }

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Business Units</h1>
        <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
          New Business Unit
        </Button>
      </div>

      {showModal && (
        <BusinessUnitFormModal
          businessUnitId={editingId}
          onClose={() => { setShowModal(false); setEditingId(null) }}
          onSaved={() => {
            void qc.invalidateQueries({ queryKey: ['business-units'] })
            setShowModal(false)
            setEditingId(null)
          }}
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <Spinner />
        </div>
      ) : businessUnits.length === 0 ? (
        <EmptyState
          heading="Organize with business units"
          subtext="Create business units to structure your organization and track employees"
          action={
            <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
              New Business Unit
            </Button>
          }
        />
      ) : (
        <div className="space-y-2">
          {businessUnits.map(bu => (
            <div key={bu.id} className="flex items-center justify-between p-4 border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-page-bg)]">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{bu.name}</p>
                {bu.description && <p className="text-[12px] text-[var(--color-text-muted)] line-clamp-1">{bu.description}</p>}
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => { setEditingId(bu.id); setShowModal(true) }}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
                  title="Edit"
                >
                  <Edit2 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => { handleDelete(bu) }}
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
