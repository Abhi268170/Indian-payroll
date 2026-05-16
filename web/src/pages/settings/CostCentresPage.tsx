import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Trash2, Edit2 } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import CostCentreFormModal from './CostCentreFormModal'

export interface CostCentre {
  id: string
  name: string
  code: string | null
}

export default function CostCentresPage(): ReactElement {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: costCentres = [], isLoading } = useQuery<CostCentre[]>({
    queryKey: ['cost-centres'],
    queryFn: () => api.get<CostCentre[]>('/api/v1/cost-centres').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/cost-centres/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['cost-centres'] })
      toastSuccess('Cost Centre deleted')
    },
    onError: () => {
      toastError('Cannot delete — payroll assigned to this cost centre')
    },
  })

  function handleDelete(cc: CostCentre): void {
    if (!confirm(`Delete "${cc.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(cc.id)
  }

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Cost Centres</h1>
        <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
          New Cost Centre
        </Button>
      </div>

      {showModal && (
        <CostCentreFormModal
          costCentreId={editingId}
          onClose={() => { setShowModal(false); setEditingId(null) }}
          onSaved={() => {
            void qc.invalidateQueries({ queryKey: ['cost-centres'] })
            setShowModal(false)
            setEditingId(null)
          }}
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <Spinner />
        </div>
      ) : costCentres.length === 0 ? (
        <EmptyState
          heading="Allocate payroll to cost centres"
          subtext="Create cost centres to track and report on payroll allocation"
          action={
            <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
              New Cost Centre
            </Button>
          }
        />
      ) : (
        <div className="space-y-2">
          {costCentres.map(cc => (
            <div key={cc.id} className="flex items-center justify-between p-4 border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-page-bg)]">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{cc.name}</p>
                {cc.code && <p className="text-[12px] text-[var(--color-text-muted)]">{cc.code}</p>}
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => { setEditingId(cc.id); setShowModal(true) }}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
                  title="Edit"
                >
                  <Edit2 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => handleDelete(cc)}
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
