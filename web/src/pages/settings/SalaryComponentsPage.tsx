import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, ChevronDown } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import AddEarningModal from './salary-components/AddEarningModal'
import AddDeductionModal from './salary-components/AddDeductionModal'
import AddReimbursementModal from './salary-components/AddReimbursementModal'
import AddBenefitModal from './salary-components/AddBenefitModal'
import AddCorrectionModal from './salary-components/AddCorrectionModal'

export type ComponentCategory = 'Earning' | 'Deduction' | 'Reimbursement' | 'Benefit' | 'Correction'

export interface SalaryComponentSummary {
  id: string
  name: string
  nameInPayslip: string
  code: string
  category: ComponentCategory
  isActive: boolean
  isSystemComponent: boolean
  isAssociatedWithEmployee: boolean
}

type ModalType = ComponentCategory | null

const TABS: { label: string; category: ComponentCategory | null }[] = [
  { label: 'All', category: null },
  { label: 'Earnings', category: 'Earning' },
  { label: 'Deductions', category: 'Deduction' },
  { label: 'Reimbursements', category: 'Reimbursement' },
  { label: 'Benefits', category: 'Benefit' },
  { label: 'Corrections', category: 'Correction' },
]

const CATEGORY_LABELS: Record<ComponentCategory, string> = {
  Earning: 'Earning',
  Deduction: 'Deduction',
  Reimbursement: 'Reimbursement',
  Benefit: 'Benefit',
  Correction: 'Correction',
}

const ADD_OPTIONS: { label: string; type: ComponentCategory }[] = [
  { label: 'Earning', type: 'Earning' },
  { label: 'Deduction', type: 'Deduction' },
  { label: 'Reimbursement', type: 'Reimbursement' },
  { label: 'Benefit', type: 'Benefit' },
  { label: 'Correction', type: 'Correction' },
]

export default function SalaryComponentsPage(): ReactElement {
  const [activeTab, setActiveTab] = useState<ComponentCategory | null>(null)
  const [showAddMenu, setShowAddMenu] = useState(false)
  const [openModal, setOpenModal] = useState<ModalType>(null)
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()

  const queryKey = ['salary-components', activeTab]
  const { data: components = [], isLoading } = useQuery<SalaryComponentSummary[]>({
    queryKey,
    queryFn: async () => {
      const params = activeTab ? `?category=${activeTab}` : ''
      const res = await api.get<SalaryComponentSummary[]>(`/api/v1/salary-components${params}`)
      return res.data
    },
  })

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      api.put(`/api/v1/salary-components/${id}/active`, { isActive }),
    onSuccess: async (_, vars) => {
      toastSuccess(vars.isActive ? 'Component activated' : 'Component deactivated')
      await qc.invalidateQueries({ queryKey: ['salary-components'] })
    },
    onError: (err: unknown) => {
      const msg = extractError(err)
      toastError(msg ?? 'Failed to update component')
    },
  })

  function handleAdded(): void {
    setOpenModal(null)
    void qc.invalidateQueries({ queryKey: ['salary-components'] })
  }

  const filtered = activeTab ? components.filter(c => c.category === activeTab) : components

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">
          Salary Components
        </h1>
        <div className="relative">
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={() => { setShowAddMenu(v => !v) }}
          >
            <Plus className="w-4 h-4 mr-1" />
            Add Component
            <ChevronDown className="w-3.5 h-3.5 ml-1" />
          </Button>
          {showAddMenu && (
            <div className="absolute right-0 top-full mt-1 w-44 bg-white rounded-xl border border-[var(--color-border)] shadow-lg z-20 py-1">
              {ADD_OPTIONS.map(opt => (
                <button
                  key={opt.type}
                  type="button"
                  onClick={() => {
                    setShowAddMenu(false)
                    setOpenModal(opt.type)
                  }}
                  className="w-full text-left px-4 py-2 text-[13px] text-[var(--color-text-primary)] hover:bg-gray-50 transition-colors"
                >
                  {opt.label}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Tab bar */}
      <div className="flex gap-1 border-b border-[var(--color-border)] mb-6">
        {TABS.map(tab => (
          <button
            key={tab.label}
            type="button"
            onClick={() => { setActiveTab(tab.category) }}
            className={[
              'px-4 py-2 text-[13px] font-medium border-b-2 -mb-px transition-colors',
              activeTab === tab.category
                ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
                : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]',
            ].join(' ')}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-20"><Spinner /></div>
      ) : filtered.length === 0 ? (
        <EmptyState
          heading="No components yet"
          subtext="Add salary components to define how employees are paid."
        />
      ) : (
        <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
          <table className="w-full text-[13px]">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-gray-50">
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Name</th>
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Code</th>
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Type</th>
                <th className="text-left px-5 py-3 font-medium text-[var(--color-text-secondary)]">Status</th>
                <th className="px-5 py-3" />
              </tr>
            </thead>
            <tbody>
              {filtered.map((comp, i) => (
                <tr
                  key={comp.id}
                  className={i < filtered.length - 1 ? 'border-b border-[var(--color-border)]' : ''}
                >
                  <td className="px-5 py-3">
                    <div className="font-medium text-[var(--color-text-primary)]">{comp.name}</div>
                    {comp.nameInPayslip !== comp.name && (
                      <div className="text-[11px] text-[var(--color-text-muted)]">
                        Payslip: {comp.nameInPayslip}
                      </div>
                    )}
                    {comp.isSystemComponent && (
                      <span className="inline-block mt-0.5 text-[10px] font-medium bg-blue-50 text-blue-700 px-1.5 py-0.5 rounded">
                        System
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-[var(--color-text-secondary)] font-mono text-[12px]">
                    {comp.code}
                  </td>
                  <td className="px-5 py-3 text-[var(--color-text-secondary)]">
                    {CATEGORY_LABELS[comp.category]}
                  </td>
                  <td className="px-5 py-3">
                    <span className={[
                      'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium',
                      comp.isActive
                        ? 'bg-green-50 text-green-700'
                        : 'bg-gray-100 text-gray-500',
                    ].join(' ')}>
                      {comp.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right">
                    {!comp.isSystemComponent && (
                      <button
                        type="button"
                        onClick={() => {
                          toggleActiveMutation.mutate({ id: comp.id, isActive: !comp.isActive })
                        }}
                        className="text-[12px] text-[var(--color-primary)] hover:underline"
                      >
                        {comp.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {openModal === 'Earning' && (
        <AddEarningModal onClose={() => { setOpenModal(null) }} onAdded={handleAdded} />
      )}
      {openModal === 'Deduction' && (
        <AddDeductionModal onClose={() => { setOpenModal(null) }} onAdded={handleAdded} />
      )}
      {openModal === 'Reimbursement' && (
        <AddReimbursementModal onClose={() => { setOpenModal(null) }} onAdded={handleAdded} />
      )}
      {openModal === 'Benefit' && (
        <AddBenefitModal onClose={() => { setOpenModal(null) }} onAdded={handleAdded} />
      )}
      {openModal === 'Correction' && (
        <AddCorrectionModal onClose={() => { setOpenModal(null) }} onAdded={handleAdded} />
      )}

      {showAddMenu && (
        <div className="fixed inset-0 z-10" onClick={() => { setShowAddMenu(false) }} />
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
