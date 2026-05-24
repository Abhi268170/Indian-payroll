import { type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'

export interface SalaryStructureTemplateSummary {
  id: string
  name: string
  description: string | null
  isActive: boolean
  componentCount: number
}

export default function SalaryStructuresPage(): ReactElement {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()

  const { data: templates = [], isLoading } = useQuery<SalaryStructureTemplateSummary[]>({
    queryKey: ['salary-structure-templates'],
    queryFn: async () => {
      const res = await api.get<SalaryStructureTemplateSummary[]>('/api/v1/salary-structure-templates')
      return res.data
    },
  })

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      api.put(`/api/v1/salary-structure-templates/${id}/active`, { isActive }),
    onSuccess: async (_, vars) => {
      toastSuccess(vars.isActive ? 'Template activated' : 'Template deactivated')
      await qc.invalidateQueries({ queryKey: ['salary-structure-templates'] })
    },
    onError: (err: unknown) => {
      const msg = extractError(err)
      toastError(msg ?? 'Failed to update template')
    },
  })

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">
            Salary Structures
          </h1>
          <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
            Define reusable CTC templates combining your salary components.
          </p>
        </div>
        <Button
          type="button"
          variant="primary"
          size="sm"
          onClick={() => { void navigate('/settings/salary-structures/new') }}
        >
          <Plus className="w-4 h-4 mr-1" />
          New Structure
        </Button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-20"><Spinner /></div>
      ) : templates.length === 0 ? (
        <EmptyState
          heading="No salary structures yet"
          subtext="Create a salary structure to define how CTC is distributed across components."
        />
      ) : (
        <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
          <table className="w-full text-[13px]">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-gray-50">
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Name</th>
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Components</th>
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Status</th>
                <th className="px-5 py-3" />
              </tr>
            </thead>
            <tbody>
              {templates.map((t, i) => (
                <tr
                  key={t.id}
                  className={i < templates.length - 1 ? 'border-b border-[var(--color-border)]' : ''}
                >
                  <td className="px-5 py-3">
                    <div className="font-medium text-[var(--color-text-primary)]">{t.name}</div>
                    {t.description && (
                      <div className="text-[12px] text-[var(--color-text-muted)]">{t.description}</div>
                    )}
                  </td>
                  <td className="px-5 py-3 text-[var(--color-text-secondary)]">
                    {t.componentCount} component{t.componentCount !== 1 ? 's' : ''}
                  </td>
                  <td className="px-5 py-3">
                    <span className={[
                      'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium',
                      t.isActive ? 'bg-green-50 text-green-700' : 'bg-gray-100 text-gray-500',
                    ].join(' ')}>
                      {t.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => { void navigate(`/settings/salary-structures/${t.id}/edit`) }}
                        className="text-[12px] text-[var(--color-primary)] hover:underline"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => { toggleActiveMutation.mutate({ id: t.id, isActive: !t.isActive }) }}
                        className="text-[12px] text-[var(--color-text-secondary)] hover:underline"
                      >
                        {t.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </div>
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

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string } } }).response
    return res?.data?.error ?? null
  }
  return null
}
