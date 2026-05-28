import { useState, useEffect } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Plus, RotateCcw, Trash2 } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { SalaryStructureTemplateSummaryDto, SalaryStructureTemplateDetailDto, ComponentOverrideRequest, EmployeeSalaryStructureDto, EmployeeDto } from '@/types/api'
import {
  computePreview,
  type PreviewComponent,
  type AddedComponent as PreviewAddedComponent,
  type EmployeeStatutoryFlags,
  type EmployerContribution,
  type FormulaType,
  type StatutoryOrgFlags,
} from '@/lib/salaryStructurePreview'
import type { StatutoryConfig } from '@/pages/settings/StatutoryComponentsPage'

interface Props {
  employeeId: string
  onSuccess: () => void
  onSkip: () => void
  isRevise?: boolean
}

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'

interface SalaryComponentSummary {
  id: string
  name: string
  code: string
  formulaType: string
  fixedAmount: number | null
  percentage: number | null
  earningType: string | null
  considerForEpf: boolean
}

interface Override {
  formulaType: string
  percentage: number | null
  fixedAmount: number | null
}

// overrideMap: componentId -> override (only for changed template comps or added comps)
type OverrideMap = Record<string, Override>

interface ComponentRow {
  componentId: string
  name: string
  code: string
  formulaType: string
  percentage: number | null
  fixedAmount: number | null
  monthlyAmount: number
  annualAmount: number
  isResidual: boolean
  isAdded: boolean  // true for override-only (added earning)
}

// Thin wrapper around the shared `computePreview` calculator so the wizard renders
// the same residual + employer-statutory math the backend uses.
function computeRows(
  template: SalaryStructureTemplateDetailDto,
  annualCTC: number,
  overrides: OverrideMap,
  addedComps: SalaryComponentSummary[],
  flags: EmployeeStatutoryFlags,
  orgFlags: StatutoryOrgFlags | undefined,
): { rows: ComponentRow[]; employerContributions: EmployerContribution[] } {
  if (!annualCTC || annualCTC <= 0) return { rows: [], employerContributions: [] }

  const templateComponents: PreviewComponent[] = template.components.map(c => ({
    componentId: c.componentId,
    code: c.componentCode,
    name: c.componentName,
    earningType: c.earningType,
    considerForEpf: c.considerForEpf,
    formulaType: c.formulaType as FormulaType,
    percentage: c.percentage,
    fixedAmount: c.fixedAmount,
    displayOrder: c.displayOrder,
  }))

  const previewAdded: PreviewAddedComponent[] = addedComps.map(sc => {
    const ov = overrides[sc.id]
    return {
      componentId: sc.id,
      code: sc.code,
      name: sc.name,
      earningType: sc.earningType,
      considerForEpf: sc.considerForEpf,
      formulaType: (ov?.formulaType ?? sc.formulaType) as FormulaType,
      percentage: ov?.percentage ?? sc.percentage,
      fixedAmount: ov?.fixedAmount ?? sc.fixedAmount,
    }
  })

  const previewOverrides: Record<string, { formulaType: FormulaType; percentage: number | null; fixedAmount: number | null }> = {}
  for (const [k, v] of Object.entries(overrides)) {
    previewOverrides[k] = {
      formulaType: v.formulaType as FormulaType,
      percentage: v.percentage,
      fixedAmount: v.fixedAmount,
    }
  }

  const out = computePreview({
    annualCtc: annualCTC,
    templateComponents,
    overrides: previewOverrides,
    addedComponents: previewAdded,
    employeeFlags: flags,
    orgFlags,
  })

  const rows: ComponentRow[] = out.rows.map(r => ({
    componentId: r.componentId,
    name: r.name,
    code: r.code,
    formulaType: r.formulaType,
    percentage: r.percentage,
    fixedAmount: r.fixedAmount,
    monthlyAmount: r.monthlyAmount,
    annualAmount: r.annualAmount,
    isResidual: r.isResidual,
    isAdded: r.isAdded,
  }))

  return { rows, employerContributions: out.employerContributions }
}

export default function WizardStep2Salary({ employeeId, onSuccess, onSkip, isRevise = false }: Props): React.ReactElement {
  const [annualCTC, setAnnualCTC] = useState('')
  const [templateId, setTemplateId] = useState('')
  const [epfEnabled, setEpfEnabled] = useState(true)
  const [esiEnabled, setEsiEnabled] = useState(true)
  const [ptEnabled, setPtEnabled] = useState(true)
  const [lwfEnabled, setLwfEnabled] = useState(true)
  const [overrides, setOverrides] = useState<OverrideMap>({})
  const [addedComps, setAddedComps] = useState<SalaryComponentSummary[]>([])
  const [showAddEarning, setShowAddEarning] = useState(false)
  const [addEarningId, setAddEarningId] = useState('')
  const [addEarningFormulaType, setAddEarningFormulaType] = useState('Fixed')
  const [addEarningAmount, setAddEarningAmount] = useState('')
  const [addEarningPercentage, setAddEarningPercentage] = useState('')
  const [showAddBenefit, setShowAddBenefit] = useState(false)
  const [addBenefitId, setAddBenefitId] = useState('')
  const [addBenefitAmount, setAddBenefitAmount] = useState('')
  const [addedBenefits, setAddedBenefits] = useState<SalaryComponentSummary[]>([])
  const [benefitOverrides, setBenefitOverrides] = useState<Record<string, number>>({})
  const [error, setError] = useState<string | null>(null)
  const [prefilled, setPrefilled] = useState(false)

  const { data: templates = [] } = useQuery<SalaryStructureTemplateSummaryDto[]>({
    queryKey: ['salary-structure-templates'],
    queryFn: () => api.get<SalaryStructureTemplateSummaryDto[]>('/api/v1/salary-structure-templates').then(r => r.data),
  })

  const { data: templateDetail } = useQuery<SalaryStructureTemplateDetailDto>({
    queryKey: ['salary-structure-template', templateId],
    queryFn: () => api.get<SalaryStructureTemplateDetailDto>(`/api/v1/salary-structure-templates/${templateId}`).then(r => r.data),
    enabled: !!templateId,
  })

  const { data: activeEarnings = [] } = useQuery<SalaryComponentSummary[]>({
    queryKey: ['active-earnings'],
    queryFn: () => api.get<SalaryComponentSummary[]>('/api/v1/salary-components/active-earnings').then(r => r.data),
  })

  const { data: activeBenefits = [] } = useQuery<SalaryComponentSummary[]>({
    queryKey: ['active-benefits'],
    queryFn: () => api.get<SalaryComponentSummary[]>('/api/v1/salary-components/active-benefits').then(r => r.data),
  })

  // Tenant statutory config — drives the orgFlags passed to the preview so
  // the residual reflects what this tenant has actually configured.
  const { data: statutoryConfig } = useQuery<StatutoryConfig>({
    queryKey: ['statutory-config'],
    queryFn: () => api.get<StatutoryConfig>('/api/v1/statutory/config').then(r => r.data),
    retry: false,
  })

  useEffect(() => {
    if (templates.length > 0 && !templateId) {
      const active = templates.find(t => t.isActive) ?? templates[0]
      if (active) setTemplateId(active.id)
    }
  }, [templates, templateId])

  // Reset overrides and added comps when template changes
  useEffect(() => {
    if (prefilled) return  // don't wipe overrides loaded from existing structure
    setOverrides({})
    setAddedComps([])
  }, [templateId, prefilled])

  const { data: existingSalary } = useQuery<EmployeeSalaryStructureDto>({
    queryKey: ['employee-salary', employeeId],
    queryFn: () => api.get<EmployeeSalaryStructureDto>(`/api/v1/employees/${employeeId}/salary-structure`).then(r => r.data),
    retry: false,
  })

  const { data: employeeDetail } = useQuery<EmployeeDto>({
    queryKey: ['employee', employeeId],
    queryFn: () => api.get<EmployeeDto>(`/api/v1/employees/${employeeId}`).then(r => r.data),
  })

  // Pre-fill form from existing salary structure + employee flags
  useEffect(() => {
    if (prefilled || !existingSalary || !employeeDetail) return
    setAnnualCTC(String(existingSalary.annualCTC))
    if (existingSalary.templateId) setTemplateId(existingSalary.templateId)
    setEpfEnabled(employeeDetail.epfEnabled)
    setEsiEnabled(employeeDetail.esiEnabled)
    setPtEnabled(employeeDetail.ptEnabled)
    setLwfEnabled(employeeDetail.lwfEnabled)
    setPrefilled(true)
  }, [existingSalary, employeeDetail, prefilled])

  const ctcNum = parseFloat(annualCTC.replace(/,/g, '')) || 0
  const employeeFlags: EmployeeStatutoryFlags = {
    epfEnabled, esiEnabled, ptEnabled, lwfEnabled, gratuityEnabled: true,
  }
  const orgFlags: StatutoryOrgFlags | undefined = statutoryConfig ? {
    epfEnabled: statutoryConfig.epfEnabled,
    epfIncludeEmployerInCtc: statutoryConfig.epfIncludeEmployerInCtc,
    gratuityIncludedInCtc: statutoryConfig.gratuityIncludedInCtc,
  } : undefined
  const previewOutput = templateDetail && ctcNum > 0
    ? computeRows(templateDetail, ctcNum, overrides, addedComps, employeeFlags, orgFlags)
    : { rows: [], employerContributions: [] }
  const rows = previewOutput.rows
  const employerContributions = previewOutput.employerContributions
  const monthlyGross = ctcNum > 0 ? ctcNum / 12 : 0

  const templateCompIds = new Set(templateDetail?.components.map(c => c.componentId) ?? [])

  // Earnings available to add (not already in template and not already added)
  const addedCompIds = new Set(addedComps.map(c => c.id))
  const availableToAdd = activeEarnings.filter(e => !templateCompIds.has(e.id) && !addedCompIds.has(e.id))

  function handlePctChange(componentId: string, rawVal: string, formulaType: string): void {
    const val = parseFloat(rawVal)
    if (isNaN(val)) {
      setOverrides(prev => {
        const next = { ...prev }
        delete next[componentId]
        return next
      })
      return
    }
    setOverrides(prev => ({ ...prev, [componentId]: { formulaType, percentage: val, fixedAmount: null } }))
  }

  function handleFixedChange(componentId: string, rawVal: string): void {
    const val = parseFloat(rawVal)
    if (isNaN(val)) {
      setOverrides(prev => {
        const next = { ...prev }
        delete next[componentId]
        return next
      })
      return
    }
    setOverrides(prev => ({ ...prev, [componentId]: { formulaType: 'Fixed', percentage: null, fixedAmount: val } }))
  }

  function handleResetOverride(componentId: string): void {
    setOverrides(prev => {
      const next = { ...prev }
      delete next[componentId]
      return next
    })
  }

  function handleAddEarning(): void {
    const sc = availableToAdd.find(e => e.id === addEarningId)
    if (!sc) return
    const isFixed = addEarningFormulaType === 'Fixed'
    const ov: Override = isFixed
      ? { formulaType: 'Fixed', percentage: null, fixedAmount: parseFloat(addEarningAmount) || 0 }
      : { formulaType: addEarningFormulaType, percentage: parseFloat(addEarningPercentage) || 0, fixedAmount: null }
    setAddedComps(prev => [...prev, sc])
    setOverrides(prev => ({ ...prev, [sc.id]: ov }))
    setAddEarningId('')
    setAddEarningFormulaType('Fixed')
    setAddEarningAmount('')
    setAddEarningPercentage('')
    setShowAddEarning(false)
  }

  function handleRemoveAdded(componentId: string): void {
    setAddedComps(prev => prev.filter(c => c.id !== componentId))
    setOverrides(prev => {
      const next = { ...prev }
      delete next[componentId]
      return next
    })
  }

  const addedBenefitIds = new Set(addedBenefits.map(b => b.id))
  const availableBenefits = activeBenefits.filter(b => !addedBenefitIds.has(b.id))

  function handleAddBenefit(): void {
    const sc = availableBenefits.find(b => b.id === addBenefitId)
    if (!sc) return
    const amount = parseFloat(addBenefitAmount) || 0
    setAddedBenefits(prev => [...prev, sc])
    setBenefitOverrides(prev => ({ ...prev, [sc.id]: amount }))
    setAddBenefitId('')
    setAddBenefitAmount('')
    setShowAddBenefit(false)
  }

  function handleRemoveBenefit(componentId: string): void {
    setAddedBenefits(prev => prev.filter(b => b.id !== componentId))
    setBenefitOverrides(prev => {
      const next = { ...prev }
      delete next[componentId]
      return next
    })
  }

  const save = useMutation({
    mutationFn: () => {
      const overridesPayload: ComponentOverrideRequest[] = []

      // Collect template overrides (only changed ones)
      if (templateDetail) {
        for (const comp of templateDetail.components) {
          const ov = overrides[comp.componentId]
          if (ov) {
            overridesPayload.push({
              salaryComponentId: comp.componentId,
              formulaType: ov.formulaType,
              percentage: ov.percentage,
              fixedAmount: ov.fixedAmount,
            })
          }
        }
      }

      // Collect added (override-only) earnings
      for (const sc of addedComps) {
        const ov = overrides[sc.id]
        overridesPayload.push({
          salaryComponentId: sc.id,
          formulaType: ov?.formulaType ?? 'Fixed',
          percentage: ov?.percentage ?? null,
          fixedAmount: ov?.fixedAmount ?? null,
        })
      }

      // Collect added benefits
      for (const sc of addedBenefits) {
        overridesPayload.push({
          salaryComponentId: sc.id,
          formulaType: 'Fixed',
          percentage: null,
          fixedAmount: benefitOverrides[sc.id] ?? 0,
        })
      }

      return api.put(`/api/v1/employees/${employeeId}/salary-structure`, {
        annualCTC: ctcNum,
        salaryStructureTemplateId: templateId || null,
        epfEnabled,
        esiEnabled,
        ptEnabled,
        lwfEnabled,
        overrides: overridesPayload,
      })
    },
    onSuccess: () => { setError(null); onSuccess() },
    onError: () => setError('Failed to save salary details. Please try again.'),
  })

  function handleSave(): void {
    if (!annualCTC || ctcNum <= 0) {
      setError('Annual CTC must be greater than zero.')
      return
    }
    setError(null)
    save.mutate()
  }

  function calcLabel(row: ComponentRow): string {
    if (row.isResidual) return 'Fixed Allowance (residual)'
    if (row.formulaType === 'PercentOfCTC') return `${row.percentage ?? ''}% of CTC`
    if (row.formulaType === 'PercentOfBasic') return `${row.percentage ?? ''}% of Basic`
    if (row.formulaType === 'PercentOfGross') return `${row.percentage ?? ''}% of Gross`
    return 'Fixed'
  }

  const isPctType = (ft: string): boolean =>
    ft === 'PercentOfCTC' || ft === 'PercentOfBasic' || ft === 'PercentOfGross'

  return (
    <div className="space-y-6">
      {/* Statutory */}
      <div>
        <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)] mb-3">Statutory Components</h3>
        <div className="grid grid-cols-2 gap-3">
          {[
            { label: 'Employee Provident Fund (EPF)', value: epfEnabled, set: setEpfEnabled },
            { label: 'Employee State Insurance (ESI)', value: esiEnabled, set: setEsiEnabled },
            { label: 'Professional Tax (PT)', value: ptEnabled, set: setPtEnabled },
            { label: 'Labour Welfare Fund (LWF)', value: lwfEnabled, set: setLwfEnabled },
          ].map(s => (
            <label key={s.label} className="flex items-center gap-2.5 cursor-pointer py-2 px-3 border border-[var(--color-border)] rounded-lg">
              <input
                type="checkbox"
                checked={s.value}
                onChange={e => s.set(e.target.checked)}
                className="w-4 h-4 accent-[var(--color-primary)]"
              />
              <span className="text-[12px] text-[var(--color-text-primary)]">{s.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* CTC + Template */}
      <div>
        <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)] mb-3">Salary Structure</h3>
        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <label className={labelCls}>Annual CTC (₹) <span className="text-red-500">*</span></label>
            <input
              type="number"
              min={0}
              step={1000}
              value={annualCTC}
              onChange={e => setAnnualCTC(e.target.value)}
              className={inputCls}
              placeholder="e.g. 840000"
            />
            {ctcNum > 0 && (
              <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">
                Monthly: {formatINR(monthlyGross)}
              </p>
            )}
          </div>
          <div>
            <label className={labelCls}>Salary Template</label>
            <select
              value={templateId}
              onChange={e => setTemplateId(e.target.value)}
              className={inputCls}
            >
              <option value="">No template</option>
              {templates.filter(t => t.isActive).map(t => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>
        </div>

        {/* Component breakdown table */}
        {rows.length > 0 && (
          <div className="border border-[var(--color-border)] rounded-lg overflow-hidden">
            <table className="w-full text-[12px]">
              <thead>
                <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                  <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Component</th>
                  <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Calculation</th>
                  <th className="px-4 py-2.5 font-medium text-[var(--color-text-secondary)] text-center w-28">Value</th>
                  <th className="text-right px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Monthly</th>
                  <th className="text-right px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Annual</th>
                  <th className="w-8"></th>
                </tr>
              </thead>
              <tbody>
                {rows.map(row => {
                  const isChanged = !!overrides[row.componentId]
                  return (
                    <tr key={row.componentId} className="border-b border-[var(--color-border)] last:border-0">
                      <td className="px-4 py-2 text-[var(--color-text-primary)]">
                        {row.name}
                        {isChanged && (
                          <span className="ml-1.5 text-[10px] px-1 py-0.5 rounded bg-amber-50 text-amber-600 border border-amber-200">
                            modified
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-2 text-[var(--color-text-secondary)]">{calcLabel(row)}</td>
                      <td className="px-4 py-2 text-center">
                        {row.isResidual ? (
                          <span className="text-[var(--color-text-secondary)]">—</span>
                        ) : isPctType(row.formulaType) ? (
                          <div className="flex items-center gap-1 justify-center">
                            <input
                              type="number"
                              min={0}
                              max={100}
                              step={0.5}
                              value={overrides[row.componentId]?.percentage ?? row.percentage ?? ''}
                              onChange={e => handlePctChange(row.componentId, e.target.value, row.formulaType)}
                              className="w-16 h-7 px-2 text-[12px] border border-[var(--color-border)] rounded text-center bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                            />
                            <span className="text-[var(--color-text-secondary)]">%</span>
                          </div>
                        ) : (
                          <input
                            type="number"
                            min={0}
                            step={100}
                            value={overrides[row.componentId]?.fixedAmount ?? row.fixedAmount ?? ''}
                            onChange={e => handleFixedChange(row.componentId, e.target.value)}
                            className="w-24 h-7 px-2 text-[12px] border border-[var(--color-border)] rounded text-right bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                          />
                        )}
                      </td>
                      <td className="px-4 py-2 text-right text-[var(--color-text-primary)]">{formatINR(row.monthlyAmount)}</td>
                      <td className="px-4 py-2 text-right text-[var(--color-text-primary)]">{formatINR(row.annualAmount)}</td>
                      <td className="px-2 py-2 text-center">
                        {!row.isResidual && isChanged && !row.isAdded && (
                          <button
                            type="button"
                            onClick={() => handleResetOverride(row.componentId)}
                            title="Reset to template default"
                            className="p-1 rounded hover:bg-[var(--color-bg-subtle)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                          >
                            <RotateCcw size={13} />
                          </button>
                        )}
                        {row.isAdded && (
                          <button
                            type="button"
                            onClick={() => handleRemoveAdded(row.componentId)}
                            title="Remove added earning"
                            className="p-1 rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-500"
                          >
                            <Trash2 size={13} />
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
              {employerContributions.length > 0 && (
                <tbody className="bg-[var(--color-page-bg)] border-t border-[var(--color-border)]">
                  <tr>
                    <td colSpan={6} className="px-4 pt-2 text-[11px] uppercase tracking-wider text-[var(--color-text-muted)]">
                      Employer contributions (included in CTC)
                    </td>
                  </tr>
                  {employerContributions.map(ec => (
                    <tr key={ec.code} className="text-[var(--color-text-secondary)]">
                      <td className="px-4 py-1.5 text-[12px]" colSpan={3}>{ec.name}</td>
                      <td className="px-4 py-1.5 text-right text-[12px]">{formatINR(ec.monthlyAmount)}</td>
                      <td className="px-4 py-1.5 text-right text-[12px]">{formatINR(ec.annualAmount)}</td>
                      <td></td>
                    </tr>
                  ))}
                </tbody>
              )}
              <tfoot>
                <tr className="bg-[var(--color-page-bg)] font-semibold">
                  <td className="px-4 py-2.5 text-[var(--color-text-primary)]" colSpan={3}>Cost to Company</td>
                  <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(monthlyGross)}</td>
                  <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(ctcNum)}</td>
                  <td></td>
                </tr>
              </tfoot>
            </table>

            {/* Add Earning inline row */}
            {availableToAdd.length > 0 && (
              <div className="border-t border-[var(--color-border)] px-4 py-2.5">
                {showAddEarning ? (
                  <div className="flex items-center gap-2 flex-wrap">
                    <select
                      value={addEarningId}
                      onChange={e => {
                        const id = e.target.value
                        setAddEarningId(id)
                        const sc = availableToAdd.find(x => x.id === id)
                        if (sc) {
                          setAddEarningFormulaType(sc.formulaType ?? 'Fixed')
                          setAddEarningAmount(sc.fixedAmount != null ? String(sc.fixedAmount) : '')
                          setAddEarningPercentage(sc.percentage != null ? String(sc.percentage) : '')
                        }
                      }}
                      className="h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)] flex-1 min-w-[160px]"
                    >
                      <option value="">Select earning component</option>
                      {availableToAdd.map(e => (
                        <option key={e.id} value={e.id}>{e.name}</option>
                      ))}
                    </select>
                    <select
                      value={addEarningFormulaType}
                      onChange={e => setAddEarningFormulaType(e.target.value)}
                      className="h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                    >
                      <option value="Fixed">Fixed ₹</option>
                      <option value="PercentOfCTC">% of CTC</option>
                      <option value="PercentOfBasic">% of Basic</option>
                      <option value="PercentOfGross">% of Gross</option>
                    </select>
                    {addEarningFormulaType === 'Fixed' ? (
                      <input
                        type="number"
                        min={0}
                        step={100}
                        placeholder="Amount ₹"
                        value={addEarningAmount}
                        onChange={e => setAddEarningAmount(e.target.value)}
                        className="w-28 h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                      />
                    ) : (
                      <input
                        type="number"
                        min={0}
                        max={100}
                        step={0.5}
                        placeholder="%"
                        value={addEarningPercentage}
                        onChange={e => setAddEarningPercentage(e.target.value)}
                        className="w-20 h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                      />
                    )}
                    <button
                      type="button"
                      onClick={handleAddEarning}
                      disabled={!addEarningId}
                      className="h-8 px-3 text-[12px] font-medium bg-[var(--color-primary)] text-white rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50"
                    >
                      Add
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setShowAddEarning(false)
                        setAddEarningId('')
                        setAddEarningFormulaType('Fixed')
                        setAddEarningAmount('')
                        setAddEarningPercentage('')
                      }}
                      className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <button
                    type="button"
                    onClick={() => setShowAddEarning(true)}
                    className="flex items-center gap-1.5 text-[12px] text-[var(--color-primary)] hover:text-[var(--color-primary-hover)]"
                  >
                    <Plus size={13} />
                    Add Earning
                  </button>
                )}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Other Benefits */}
      {activeBenefits.length > 0 && (
        <div>
          <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)] mb-3">Other Benefits</h3>
          <div className="border border-[var(--color-border)] rounded-lg overflow-hidden">
            {addedBenefits.length > 0 && (
              <table className="w-full text-[12px]">
                <thead>
                  <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                    <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Benefit</th>
                    <th className="text-right px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Monthly (₹)</th>
                    <th className="w-8"></th>
                  </tr>
                </thead>
                <tbody>
                  {addedBenefits.map(b => (
                    <tr key={b.id} className="border-b border-[var(--color-border)] last:border-0">
                      <td className="px-4 py-2 text-[var(--color-text-primary)]">{b.name}</td>
                      <td className="px-4 py-2 text-right">
                        <input
                          type="number"
                          min={0}
                          step={100}
                          value={benefitOverrides[b.id] ?? ''}
                          onChange={e => {
                            const val = parseFloat(e.target.value)
                            setBenefitOverrides(prev => ({ ...prev, [b.id]: isNaN(val) ? 0 : val }))
                          }}
                          className="w-28 h-7 px-2 text-[12px] border border-[var(--color-border)] rounded text-right bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                        />
                      </td>
                      <td className="px-2 py-2 text-center">
                        <button
                          type="button"
                          onClick={() => handleRemoveBenefit(b.id)}
                          className="p-1 rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-500"
                        >
                          <Trash2 size={13} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <div className={`px-4 py-2.5 ${addedBenefits.length > 0 ? 'border-t border-[var(--color-border)]' : ''}`}>
              {showAddBenefit ? (
                <div className="flex items-center gap-2">
                  <select
                    value={addBenefitId}
                    onChange={e => setAddBenefitId(e.target.value)}
                    className="h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)] flex-1"
                  >
                    <option value="">Select benefit</option>
                    {availableBenefits.map(b => (
                      <option key={b.id} value={b.id}>{b.name}</option>
                    ))}
                  </select>
                  <input
                    type="number"
                    min={0}
                    step={100}
                    placeholder="Amount ₹/month"
                    value={addBenefitAmount}
                    onChange={e => setAddBenefitAmount(e.target.value)}
                    className="w-32 h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-1 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
                  />
                  <button
                    type="button"
                    onClick={handleAddBenefit}
                    disabled={!addBenefitId}
                    className="h-8 px-3 text-[12px] font-medium bg-[var(--color-primary)] text-white rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50"
                  >
                    Add
                  </button>
                  <button
                    type="button"
                    onClick={() => { setShowAddBenefit(false); setAddBenefitId(''); setAddBenefitAmount('') }}
                    className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                  >
                    Cancel
                  </button>
                </div>
              ) : availableBenefits.length > 0 ? (
                <button
                  type="button"
                  onClick={() => setShowAddBenefit(true)}
                  className="flex items-center gap-1.5 text-[12px] text-[var(--color-primary)] hover:text-[var(--color-primary-hover)]"
                >
                  <Plus size={13} />
                  Add Benefit
                </button>
              ) : (
                <p className="text-[11px] text-[var(--color-text-secondary)]">All benefit plans added.</p>
              )}
            </div>
          </div>
        </div>
      )}

      {error && <p className="text-[12px] text-red-600">{error}</p>}

      <div className="flex items-center gap-3 pt-2 border-t border-[var(--color-border)]">
        <button
          type="button"
          disabled={save.isPending}
          onClick={handleSave}
          className="h-9 px-5 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {save.isPending ? 'Saving…' : isRevise ? 'Save' : 'Save and Continue'}
        </button>
        {!isRevise && (
          <button
            type="button"
            onClick={onSkip}
            className="h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
          >
            Skip
          </button>
        )}
      </div>
    </div>
  )
}
