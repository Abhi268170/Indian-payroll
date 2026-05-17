import { useState, useEffect } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { SalaryStructureTemplateSummaryDto, SalaryStructureTemplateDetailDto } from '@/types/api'

interface Props {
  employeeId: string
  onSuccess: () => void
  onSkip: () => void
}

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'

interface ComponentRow {
  componentId: string
  name: string
  code: string
  formulaType: string
  percentage: number | null
  monthlyAmount: number
  annualAmount: number
  isResidual: boolean
}

function computeComponents(
  template: SalaryStructureTemplateDetailDto,
  annualCTC: number
): ComponentRow[] {
  if (!annualCTC || annualCTC <= 0) return []

  const monthlyGross = annualCTC / 12
  const rows: ComponentRow[] = []
  let basicMonthly = 0
  let nonResidualSum = 0

  const sorted = [...template.components].sort((a, b) => a.displayOrder - b.displayOrder)

  for (const c of sorted) {
    if (c.formulaType === 'ResidualCTC') continue

    let monthly = 0
    if (c.formulaType === 'PercentOfCTC' && c.percentage != null) {
      monthly = Math.round((annualCTC * (c.percentage / 100) / 12) * 100) / 100
    } else if (c.formulaType === 'PercentOfBasic' && c.percentage != null) {
      monthly = Math.round(basicMonthly * (c.percentage / 100) * 100) / 100
    } else if (c.formulaType === 'PercentOfGross' && c.percentage != null) {
      monthly = Math.round(monthlyGross * (c.percentage / 100) * 100) / 100
    } else if (c.formulaType === 'Fixed' && c.fixedAmount != null) {
      monthly = c.fixedAmount
    }

    if (c.componentCode === 'BASIC') basicMonthly = monthly
    nonResidualSum += monthly

    rows.push({
      componentId: c.componentId,
      name: c.componentName,
      code: c.componentCode,
      formulaType: c.formulaType,
      percentage: c.percentage ?? null,
      monthlyAmount: monthly,
      annualAmount: Math.round(monthly * 12 * 100) / 100,
      isResidual: false,
    })
  }

  const residual = sorted.find(c => c.formulaType === 'ResidualCTC')
  if (residual) {
    const residualMonthly = Math.round((monthlyGross - nonResidualSum) * 100) / 100
    rows.push({
      componentId: residual.componentId,
      name: residual.componentName,
      code: residual.componentCode,
      formulaType: 'ResidualCTC',
      percentage: null,
      monthlyAmount: residualMonthly,
      annualAmount: Math.round(residualMonthly * 12 * 100) / 100,
      isResidual: true,
    })
  }

  return rows
}

export default function WizardStep2Salary({ employeeId, onSuccess, onSkip }: Props): React.ReactElement {
  const [annualCTC, setAnnualCTC] = useState('')
  const [templateId, setTemplateId] = useState('')
  const [epfEnabled, setEpfEnabled] = useState(true)
  const [esiEnabled, setEsiEnabled] = useState(true)
  const [ptEnabled, setPtEnabled] = useState(true)
  const [lwfEnabled, setLwfEnabled] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const { data: templates = [] } = useQuery<SalaryStructureTemplateSummaryDto[]>({
    queryKey: ['salary-structure-templates'],
    queryFn: () => api.get<SalaryStructureTemplateSummaryDto[]>('/api/v1/salary-structure-templates').then(r => r.data),
  })

  const { data: templateDetail } = useQuery<SalaryStructureTemplateDetailDto>({
    queryKey: ['salary-structure-template', templateId],
    queryFn: () => api.get<SalaryStructureTemplateDetailDto>(`/api/v1/salary-structure-templates/${templateId}`).then(r => r.data),
    enabled: !!templateId,
  })

  // Auto-select first active template
  useEffect(() => {
    if (templates.length > 0 && !templateId) {
      const active = templates.find(t => t.isActive) ?? templates[0]
      if (active) setTemplateId(active.id)
    }
  }, [templates, templateId])

  const ctcNum = parseFloat(annualCTC.replace(/,/g, '')) || 0
  const components = templateDetail && ctcNum > 0
    ? computeComponents(templateDetail, ctcNum)
    : []

  const save = useMutation({
    mutationFn: () => api.put(`/api/v1/employees/${employeeId}/salary-structure`, {
      annualCTC: ctcNum,
      salaryStructureTemplateId: templateId || null,
      epfEnabled,
      esiEnabled,
      ptEnabled,
      lwfEnabled,
    }),
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

  const monthlyGross = ctcNum > 0 ? ctcNum / 12 : 0

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

        {/* Component breakdown */}
        {components.length > 0 && (
          <div className="border border-[var(--color-border)] rounded-lg overflow-hidden">
            <table className="w-full text-[12px]">
              <thead>
                <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                  <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Component</th>
                  <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Calculation</th>
                  <th className="text-right px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Monthly</th>
                  <th className="text-right px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Annual</th>
                </tr>
              </thead>
              <tbody>
                {components.map(c => (
                  <tr key={c.componentId} className="border-b border-[var(--color-border)] last:border-0">
                    <td className="px-4 py-2.5 text-[var(--color-text-primary)]">{c.name}</td>
                    <td className="px-4 py-2.5 text-[var(--color-text-secondary)]">
                      {c.isResidual
                        ? 'Fixed Allowance (residual)'
                        : c.formulaType === 'PercentOfCTC'
                          ? `${c.percentage}% of CTC`
                          : c.formulaType === 'PercentOfBasic'
                            ? `${c.percentage}% of Basic`
                            : c.formulaType === 'PercentOfGross'
                              ? `${c.percentage}% of Gross`
                              : 'Fixed'}
                    </td>
                    <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(c.monthlyAmount)}</td>
                    <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(c.annualAmount)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="bg-[var(--color-page-bg)] font-semibold">
                  <td className="px-4 py-2.5 text-[var(--color-text-primary)]" colSpan={2}>Cost to Company</td>
                  <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(monthlyGross)}</td>
                  <td className="px-4 py-2.5 text-right text-[var(--color-text-primary)]">{formatINR(ctcNum)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </div>

      {error && <p className="text-[12px] text-red-600">{error}</p>}

      <div className="flex items-center gap-3 pt-2 border-t border-[var(--color-border)]">
        <button
          type="button"
          disabled={save.isPending}
          onClick={handleSave}
          className="h-9 px-5 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {save.isPending ? 'Saving…' : 'Save and Continue'}
        </button>
        <button
          type="button"
          onClick={onSkip}
          className="h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
        >
          Skip
        </button>
      </div>
    </div>
  )
}
