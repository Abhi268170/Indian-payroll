import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Trash2, Edit2 } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import DepartmentFormModal from './DepartmentFormModal'

export interface Department {
  id: string
  name: string
  code: string | null
  description: string | null
}

export default function DepartmentsPage(): ReactElement {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: departments = [], isLoading } = useQuery<Department[]>({
    queryKey: ['departments'],
    queryFn: () => api.get<Department[]>('/api/v1/departments').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/departments/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['departments'] })
      toastSuccess('Department deleted')
    },
    onError: () => {
      toastError('Cannot delete — employees assigned to this department')
    },
  })

  function handleDelete(dept: Department): void {
    if (!confirm(`Delete "${dept.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(dept.id)
  }

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Departments</h1>
        <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
          New Department
        </Button>
      </div>

      {showModal && (
        <DepartmentFormModal
          departmentId={editingId}
          onClose={() => { setShowModal(false); setEditingId(null) }}
          onSaved={() => {
            void qc.invalidateQueries({ queryKey: ['departments'] })
            setShowModal(false)
            setEditingId(null)
          }}
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <Spinner />
        </div>
      ) : departments.length === 0 ? (
        <EmptyState
          heading="Enhance organisation structure with new departments"
          subtext="Create department based on the ones present in the organization and associate with employees"
          action={
            <Button variant="primary" size="sm" onClick={() => { setEditingId(null); setShowModal(true) }}>
              New Department
            </Button>
          }
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left py-3 px-4 text-[13px] font-medium text-[var(--color-text-primary)]">Name</th>
                <th className="text-left py-3 px-4 text-[13px] font-medium text-[var(--color-text-primary)]">Code</th>
                <th className="text-left py-3 px-4 text-[13px] font-medium text-[var(--color-text-primary)]">Description</th>
                <th className="text-center py-3 px-4 text-[13px] font-medium text-[var(--color-text-primary)]">Actions</th>
              </tr>
            </thead>
            <tbody>
              {departments.map(dept => (
                <tr key={dept.id} className="border-b border-[var(--color-border)] hover:bg-[var(--color-page-bg)]">
                  <td className="py-3 px-4 text-[13px] text-[var(--color-text-primary)]">{dept.name}</td>
                  <td className="py-3 px-4 text-[13px] text-[var(--color-text-secondary)]">{dept.code ?? '—'}</td>
                  <td className="py-3 px-4 text-[13px] text-[var(--color-text-secondary)]">{dept.description ?? '—'}</td>
                  <td className="py-3 px-4 flex items-center justify-center gap-2">
                    <button
                      onClick={() => { setEditingId(dept.id); setShowModal(true) }}
                      className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
                      title="Edit"
                    >
                      <Edit2 className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => { handleDelete(dept); }}
                      className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-error)] hover:bg-red-50 transition-colors"
                      title="Delete"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
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
