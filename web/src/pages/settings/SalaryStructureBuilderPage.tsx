import { useState, useMemo, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { Plus, Trash2, ChevronDown, ChevronRight } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'
import { formatINR } from '@/lib/format'
import type { SalaryComponentSummary } from './SalaryComponentsPage'
import { computePreview, type EmployerContribution, type PreviewComponent, type StatutoryOrgFlags } from '@/lib/salaryStructurePreview'
import type { StatutoryConfig } from './StatutoryComponentsPage'

type ComponentCategory = 'Earning' | 'Deduction' | 'Reimbursement' | 'Benefit' | 'Correction'
type FormulaType = 'Fixed' | 'PercentOfBasic' | 'PercentOfGross' | 'PercentOfCTC' | 'ResidualCTC'

interface DetailComponent {
  componentId: string
  componentName: string
  componentCode: string
  category: ComponentCategory
  isSystemComponent: boolean
  formulaType: FormulaType
  fixedAmount: number | null
  percentage: number | null
  displayOrder: number
  earningType: string | null
  considerForEpf: boolean
}

interface TemplateDetail {
  id: string
  name: string
  description: string | null
  isActive: boolean
  epfEnabled: boolean
  esiEnabled: boolean
  ptEnabled: boolean
  lwfEnabled: boolean
  components: DetailComponent[]
}

interface TemplateRow {
  componentId: string
  componentName: string
  componentCode: string
  category: ComponentCategory
  isSystemComponent: boolean
  formulaType: FormulaType
  fixedAmount: string
  percentage: string
  displayOrder: number
  // Carried through so the preview calculator can identify Basic (gratuity driver)
  // and PF-eligible components (employer EPF subtraction from residual).
  earningType: string | null
  considerForEpf: boolean
}

const FORMULA_LABELS: Record<FormulaType, string> = {
  Fixed: 'Fixed',
  PercentOfBasic: '% of Basic',
  PercentOfGross: '% of Gross',
  PercentOfCTC: '% of CTC',
  ResidualCTC: 'Residual (auto)',
}

const CATEGORY_GROUPS: { label: string; category: ComponentCategory }[] = [
  { label: 'Earnings', category: 'Earning' },
  { label: 'Deductions', category: 'Deduction' },
  { label: 'Reimbursements', category: 'Reimbursement' },
  { label: 'Benefits', category: 'Benefit' },
]

// Thin adapter over the shared computePreview() so the settings builder uses
// the same residual + employer-statutory math as the wizard + employee detail
// page. No per-employee flags here — templates are tenant-wide. Org flags come
// from the live StatutoryOrgConfig so the preview reflects what this specific
// tenant has actually enabled (e.g. employer-EPF-in-CTC off).
function computeAmounts(
  rows: TemplateRow[],
  previewCtc: number,
  orgFlags: StatutoryOrgFlags | undefined,
): {
  amounts: Map<string, number>
  employerContributions: EmployerContribution[]
} {
  if (!previewCtc || previewCtc <= 0) {
    return { amounts: new Map(), employerContributions: [] }
  }

  const templateComponents: PreviewComponent[] = rows.map(r => ({
    componentId: r.componentId,
    code: r.componentCode,
    name: r.componentName,
    earningType: r.earningType,
    considerForEpf: r.considerForEpf,
    formulaType: r.formulaType,
    percentage: r.percentage ? parseFloat(r.percentage) || null : null,
    fixedAmount: r.fixedAmount ? parseFloat(r.fixedAmount) || null : null,
    displayOrder: r.displayOrder,
  }))

  const out = computePreview({
    annualCtc: previewCtc,
    templateComponents,
    overrides: {},
    addedComponents: [],
    orgFlags,
  })

  const amounts = new Map<string, number>()
  for (const r of out.rows) amounts.set(r.componentId, r.annualAmount)
  return { amounts, employerContributions: out.employerContributions }
}

export default function SalaryStructureBuilderPage(): ReactElement {
  const { id } = useParams<{ id?: string }>()
  const isNew = !id
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()

  const [templateName, setTemplateName] = useState('')
  const [description, setDescription] = useState('')
  const [rows, setRows] = useState<TemplateRow[]>([])
  const [previewCtc, setPreviewCtc] = useState('600000')
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set(['Earnings']))
  const [initialized, setInitialized] = useState(false)
  // Template-level statutory defaults — used to pre-fill employee toggles at hire.
  // Save with the template; engine still reads per-employee flags at run time.
  const [templateEpfEnabled, setTemplateEpfEnabled] = useState(true)
  const [templateEsiEnabled, setTemplateEsiEnabled] = useState(true)
  const [templatePtEnabled, setTemplatePtEnabled] = useState(true)
  const [templateLwfEnabled, setTemplateLwfEnabled] = useState(true)

  // Load tenant statutory config so the residual preview reflects what this
  // tenant has actually enabled (employer-EPF-in-CTC, gratuity-in-CTC). Without
  // this the preview falls back to defaults and over- or under-states the residual.
  const { data: statutoryConfig } = useQuery<StatutoryConfig>({
    queryKey: ['statutory-config'],
    queryFn: () => api.get<StatutoryConfig>('/api/v1/statutory/config').then(r => r.data),
    retry: false,
  })

  // Load all components for the picker.
  // GET /api/v1/salary-components returns a PagedResult<T> ({items,total,page,pageSize})
  // after pagination shipped. Pull a large page so the builder sees every component
  // and unwrap .items for the picker.
  const { data: allComponents = [] } = useQuery<SalaryComponentSummary[]>({
    queryKey: ['salary-components', 'builder-all'],
    queryFn: async () => {
      const res = await api.get<{ items: SalaryComponentSummary[]; total: number }>(
        '/api/v1/salary-components',
        { params: { page: 1, pageSize: 200 } },
      )
      return res.data.items
    },
  })

  // Load template for edit mode
  useQuery<TemplateDetail>({
    queryKey: ['salary-structure-template', id],
    queryFn: async () => {
      const res = await api.get<TemplateDetail>(`/api/v1/salary-structure-templates/${id!}`)
      return res.data
    },
    enabled: !isNew,
    select: data => data,
    staleTime: Infinity,
  })

  // Initialize rows from template data when editing
  const { data: templateData } = useQuery<TemplateDetail>({
    queryKey: ['salary-structure-template', id],
    queryFn: async () => {
      const res = await api.get<TemplateDetail>(`/api/v1/salary-structure-templates/${id!}`)
      return res.data
    },
    enabled: !isNew,
  })

  if (!initialized && templateData) {
    setTemplateName(templateData.name)
    setDescription(templateData.description ?? '')
    setTemplateEpfEnabled(templateData.epfEnabled)
    setTemplateEsiEnabled(templateData.esiEnabled)
    setTemplatePtEnabled(templateData.ptEnabled)
    setTemplateLwfEnabled(templateData.lwfEnabled)
    setRows(templateData.components.map(c => ({
      componentId: c.componentId,
      componentName: c.componentName,
      componentCode: c.componentCode,
      category: c.category,
      isSystemComponent: c.isSystemComponent,
      formulaType: c.formulaType,
      fixedAmount: c.fixedAmount?.toString() ?? '',
      percentage: c.percentage?.toString() ?? '',
      displayOrder: c.displayOrder,
      earningType: c.earningType,
      considerForEpf: c.considerForEpf,
    })))
    setInitialized(true)
  }

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = {
        name: templateName,
        description: description || null,
        epfEnabled: templateEpfEnabled,
        esiEnabled: templateEsiEnabled,
        ptEnabled: templatePtEnabled,
        lwfEnabled: templateLwfEnabled,
        components: rows.map((r, i) => ({
          componentId: r.componentId,
          formulaType: r.formulaType,
          fixedAmount: r.formulaType === 'Fixed' ? parseFloat(r.fixedAmount) || null : null,
          percentage: r.formulaType !== 'Fixed' && r.formulaType !== 'ResidualCTC'
            ? parseFloat(r.percentage) || null
            : null,
          displayOrder: i,
        })),
      }
      return isNew
        ? api.post<{ id: string }>('/api/v1/salary-structure-templates', payload)
        : api.put(`/api/v1/salary-structure-templates/${id}`, payload)
    },
    onSuccess: async () => {
      toastSuccess(isNew ? 'Salary structure created' : 'Salary structure updated')
      await qc.invalidateQueries({ queryKey: ['salary-structure-templates'] })
      void navigate('/settings/salary-structures')
    },
    onError: (err: unknown) => {
      const msg = extractError(err)
      toastError(msg ?? 'Failed to save salary structure')
    },
  })

  const orgFlags: StatutoryOrgFlags | undefined = statutoryConfig ? {
    epfEnabled: statutoryConfig.epfEnabled,
    epfIncludeEmployerInCtc: statutoryConfig.epfIncludeEmployerInCtc,
    gratuityIncludedInCtc: statutoryConfig.gratuityIncludedInCtc,
  } : undefined

  const { amounts, employerContributions } = useMemo(
    () => computeAmounts(rows, parseFloat(previewCtc) || 0, orgFlags),
    [rows, previewCtc, orgFlags],
  )

  const alreadyAdded = new Set(rows.map(r => r.componentId))

  function addComponent(comp: SalaryComponentSummary): void {
    if (alreadyAdded.has(comp.id)) return
    const formulaType: FormulaType = comp.isSystemComponent ? 'ResidualCTC' : 'PercentOfCTC'
    setRows(prev => [
      ...prev,
      {
        componentId: comp.id,
        componentName: comp.name,
        componentCode: comp.code,
        category: comp.category,
        isSystemComponent: comp.isSystemComponent,
        formulaType,
        fixedAmount: '',
        percentage: '',
        displayOrder: prev.length,
        earningType: comp.earningType,
        considerForEpf: comp.considerForEpf,
      },
    ])
  }

  function removeRow(componentId: string): void {
    setRows(prev => prev.filter(r => r.componentId !== componentId))
  }

  function updateRow(componentId: string, patch: Partial<TemplateRow>): void {
    setRows(prev => prev.map(r => r.componentId === componentId ? { ...r, ...patch } : r))
  }

  function toggleGroup(label: string): void {
    setExpandedGroups(prev => {
      const next = new Set(prev)
      if (next.has(label)) next.delete(label)
      else next.add(label)
      return next
    })
  }

  const isLoading = !isNew && !initialized && !templateData

  if (isLoading) {
    return <div className="flex items-center justify-center py-20"><Spinner /></div>
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex-shrink-0 border-b border-[var(--color-border)] bg-white px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <input
            className="text-[18px] font-semibold text-[var(--color-text-primary)] border-0 outline-none bg-transparent placeholder-gray-300 min-w-[200px]"
            placeholder="Structure name..."
            value={templateName}
            onChange={e => { setTemplateName(e.target.value) }}
          />
        </div>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => { void navigate('/settings/salary-structures') }}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="primary"
            size="sm"
            loading={saveMutation.isPending}
            disabled={!templateName || rows.length === 0}
            onClick={() => { saveMutation.mutate() }}
          >
            {isNew ? 'Create' : 'Save'}
          </Button>
        </div>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Left: Component Picker */}
        <aside className="w-64 flex-shrink-0 border-r border-[var(--color-border)] bg-white overflow-y-auto">
          <div className="px-4 pt-4 pb-2">
            <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
              Components
            </p>
            <p className="text-[12px] text-[var(--color-text-muted)] mt-0.5">
              Click to add to structure
            </p>
          </div>

          {CATEGORY_GROUPS.map(group => {
            const groupComponents = allComponents.filter(c => c.category === group.category && c.isActive)
            const expanded = expandedGroups.has(group.label)
            return (
              <div key={group.label} className="border-t border-[var(--color-border)]">
                <button
                  type="button"
                  onClick={() => { toggleGroup(group.label) }}
                  className="w-full flex items-center justify-between px-4 py-2.5 text-[12px] font-semibold text-[var(--color-text-secondary)] hover:bg-gray-50 transition-colors"
                >
                  {group.label} ({groupComponents.length})
                  {expanded
                    ? <ChevronDown className="w-3.5 h-3.5" />
                    : <ChevronRight className="w-3.5 h-3.5" />}
                </button>
                {expanded && groupComponents.map(comp => {
                  const added = alreadyAdded.has(comp.id)
                  return (
                    <button
                      key={comp.id}
                      type="button"
                      disabled={added}
                      onClick={() => { addComponent(comp) }}
                      className={[
                        'w-full text-left px-4 py-2 flex items-center justify-between text-[13px] transition-colors',
                        added
                          ? 'text-[var(--color-text-muted)] cursor-default'
                          : 'text-[var(--color-text-primary)] hover:bg-blue-50 hover:text-[var(--color-primary)] cursor-pointer',
                      ].join(' ')}
                    >
                      <span className="truncate">{comp.name}</span>
                      {!added && <Plus className="w-3 h-3 flex-shrink-0 ml-1" />}
                    </button>
                  )
                })}
              </div>
            )
          })}
        </aside>

        {/* Right: Template Editor */}
        <div className="flex-1 overflow-y-auto px-6 py-6">
          <div className="flex items-center gap-4 mb-4">
            <div>
              <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Description</label>
              <input
                className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] w-80"
                placeholder="Optional description"
                value={description}
                onChange={e => { setDescription(e.target.value) }}
              />
            </div>
            <div>
              <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
                Preview Annual CTC (₹)
              </label>
              <input
                type="number"
                className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] w-40"
                value={previewCtc}
                onChange={e => { setPreviewCtc(e.target.value) }}
              />
            </div>
          </div>

          {/* Statutory defaults — pre-fill employee toggles at hire. Per-employee
              override still possible in the hire wizard. */}
          <div className="border border-[var(--color-border)] rounded-xl px-4 py-3 mb-4 bg-[var(--color-page-bg)]">
            <p className="text-[12px] font-medium text-[var(--color-text-primary)] mb-2">
              Statutory defaults
              <span className="ml-1.5 text-[11px] font-normal text-[var(--color-text-secondary)]">
                (pre-fill employee toggles at hire; operator can override per employee)
              </span>
            </p>
            <div className="flex flex-wrap gap-4">
              {([
                { label: 'EPF', value: templateEpfEnabled, set: setTemplateEpfEnabled },
                { label: 'ESI', value: templateEsiEnabled, set: setTemplateEsiEnabled },
                { label: 'Professional Tax', value: templatePtEnabled, set: setTemplatePtEnabled },
                { label: 'Labour Welfare Fund', value: templateLwfEnabled, set: setTemplateLwfEnabled },
              ]).map(t => (
                <label key={t.label} className="inline-flex items-center gap-2 text-[13px] text-[var(--color-text-primary)] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={t.value}
                    onChange={e => { t.set(e.target.checked) }}
                    className="w-4 h-4 rounded border-[var(--color-border)] text-[var(--color-primary)] focus:ring-[var(--color-primary)]"
                  />
                  {t.label}
                </label>
              ))}
            </div>
          </div>

          {rows.length === 0 ? (
            <div className="border-2 border-dashed border-[var(--color-border)] rounded-xl py-16 text-center">
              <p className="text-[14px] font-medium text-[var(--color-text-secondary)]">
                No components added
              </p>
              <p className="text-[13px] text-[var(--color-text-muted)] mt-1">
                Select components from the left panel to build your structure.
                Fixed Allowance is required.
              </p>
            </div>
          ) : (
            <div className="bg-white border border-[var(--color-border)] rounded-xl overflow-hidden">
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="border-b border-[var(--color-border)] bg-gray-50">
                    <th className="text-left px-4 py-3 font-medium text-[var(--color-text-secondary)]">Component</th>
                    <th className="text-left px-4 py-3 font-medium text-[var(--color-text-secondary)]">Formula</th>
                    <th className="text-left px-4 py-3 font-medium text-[var(--color-text-secondary)]">Value</th>
                    <th className="text-right px-4 py-3 font-medium text-[var(--color-text-secondary)]">Annual Amount</th>
                    <th className="px-4 py-3 w-8" />
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row, i) => {
                    const annual = amounts.get(row.componentId) ?? 0
                    return (
                      <tr
                        key={row.componentId}
                        className={i < rows.length - 1 ? 'border-b border-[var(--color-border)]' : ''}
                      >
                        <td className="px-4 py-3">
                          <span className="font-medium text-[var(--color-text-primary)]">{row.componentName}</span>
                          {row.isSystemComponent && (
                            <span className="ml-2 text-[10px] bg-blue-50 text-blue-700 px-1.5 py-0.5 rounded font-medium">
                              System
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {row.isSystemComponent ? (
                            <span className="text-[var(--color-text-muted)]">{FORMULA_LABELS[row.formulaType]}</span>
                          ) : (
                            <select
                              value={row.formulaType}
                              onChange={e => {
                                updateRow(row.componentId, {
                                  formulaType: e.target.value as FormulaType,
                                  fixedAmount: '',
                                  percentage: '',
                                })
                              }}
                              className="h-8 px-2 border border-[var(--color-border)] rounded-lg text-[12px] focus:outline-none focus:border-[var(--color-primary)] bg-white"
                            >
                              <option value="Fixed">Fixed Amount</option>
                              <option value="PercentOfBasic">% of Basic</option>
                              <option value="PercentOfGross">% of Gross</option>
                              <option value="PercentOfCTC">% of CTC</option>
                            </select>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {row.formulaType === 'ResidualCTC' ? (
                            <span className="text-[var(--color-text-muted)] text-[12px]">auto-calculated</span>
                          ) : row.formulaType === 'Fixed' ? (
                            <input
                              type="number"
                              className="h-8 w-28 px-2 border border-[var(--color-border)] rounded-lg text-[12px] focus:outline-none focus:border-[var(--color-primary)]"
                              placeholder="₹/month"
                              value={row.fixedAmount}
                              onChange={e => { updateRow(row.componentId, { fixedAmount: e.target.value }) }}
                            />
                          ) : (
                            <div className="flex items-center gap-1">
                              <input
                                type="number"
                                className="h-8 w-20 px-2 border border-[var(--color-border)] rounded-lg text-[12px] focus:outline-none focus:border-[var(--color-primary)]"
                                placeholder="0.00"
                                min={0}
                                max={100}
                                value={row.percentage}
                                onChange={e => { updateRow(row.componentId, { percentage: e.target.value }) }}
                              />
                              <span className="text-[12px] text-[var(--color-text-muted)]">%</span>
                            </div>
                          )}
                        </td>
                        <td className="px-4 py-3 text-right font-medium text-[var(--color-text-primary)]">
                          {annual > 0 ? formatINR(annual) : '—'}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {!row.isSystemComponent && (
                            <button
                              type="button"
                              onClick={() => { removeRow(row.componentId) }}
                              className="text-[var(--color-text-muted)] hover:text-red-500 transition-colors"
                              aria-label="Remove"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
                {employerContributions.length > 0 && (
                  <tbody className="border-t border-[var(--color-border)] bg-gray-50">
                    <tr>
                      <td colSpan={4} className="px-4 pt-2 text-[11px] uppercase tracking-wider text-[var(--color-text-muted)]">
                        Employer contributions (included in CTC)
                      </td>
                    </tr>
                    {employerContributions.map(ec => (
                      <tr key={ec.code} className="text-[var(--color-text-secondary)]">
                        <td className="px-4 py-1.5 text-[12px]" colSpan={3}>{ec.name}</td>
                        <td className="px-4 py-1.5 text-right text-[12px]">{formatINR(ec.annualAmount)}</td>
                        <td />
                      </tr>
                    ))}
                  </tbody>
                )}
                <tfoot>
                  <tr className="border-t-2 border-[var(--color-border)] bg-gray-50">
                    <td colSpan={3} className="px-4 py-3 font-semibold text-[var(--color-text-primary)]">
                      Total Annual CTC
                    </td>
                    <td className="px-4 py-3 text-right font-semibold text-[var(--color-text-primary)]">
                      {formatINR(parseFloat(previewCtc) || 0)}
                    </td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
