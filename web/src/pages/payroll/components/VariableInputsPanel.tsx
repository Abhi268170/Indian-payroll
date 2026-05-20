import { useState, useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { X, Trash2 } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { EmployeeVariableInputsDto, ComponentBreakdownDto } from '@/types/api'

interface VariableInputsPanelProps {
  runId: string
  employeeId: string
  employeeName: string
  onClose: () => void
}

interface DeductionRow {
  label: string
  amount: number
}

interface BenefitRow {
  label: string
  amount: number
}

function SectionHeader({ title }: { title: string }): React.ReactElement {
  return (
    <h4 className="text-[12px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-3">
      {title}
    </h4>
  )
}


export default function VariableInputsPanel({ runId, employeeId, employeeName, onClose }: VariableInputsPanelProps): React.ReactElement {
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery<EmployeeVariableInputsDto>({
    queryKey: ['variable-inputs', runId, employeeId],
    queryFn: () => api.get<EmployeeVariableInputsDto>(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/inputs`).then(r => r.data),
  })

  const [lopDaysEdit, setLopDaysEdit] = useState<number | null>(null)
  const [tdsOverride, setTdsOverride] = useState<string>('')
  const [tdsReason, setTdsReason] = useState('')
  const [editingTds, setEditingTds] = useState(false)
  const prevDataRef = useRef<string | null>(null)

  const dataKey = data ? `${String(data.tdsOverrideAmount)}-${String(data.lopDays)}` : null
  if (dataKey !== prevDataRef.current) {
    prevDataRef.current = dataKey
    if (data) {
      setTdsOverride(data.tdsOverrideAmount !== null ? String(data.tdsOverrideAmount) : '')
      setLopDaysEdit(null)
    }
  }

  const lopDays = lopDaysEdit ?? (data?.lopDays ?? 0)

  const invalidateQueries = (): void => {
    void queryClient.invalidateQueries({ queryKey: ['variable-inputs', runId, employeeId] })
    void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
    void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
  }

  const lopMutation = useMutation({
    mutationFn: (days: number) => api.put(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/lop`, { lopDays: days }),
    onSuccess: invalidateQueries,
  })

  const tdsMutation = useMutation({
    mutationFn: ({ amount, reason }: { amount: number; reason: string }) =>
      api.put(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/tds-override`, { overrideAmount: amount, reason }),
    onSuccess: () => { setEditingTds(false); invalidateQueries() },
  })

  const removeEarningMutation = useMutation({
    mutationFn: (breakdownId: string) =>
      api.delete(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/earnings/${breakdownId}`),
    onSuccess: invalidateQueries,
  })

  function handleLopBlur(): void {
    if (data && lopDays !== data.lopDays) {
      lopMutation.mutate(lopDays)
    }
  }

  const salaryComponents = data?.components.filter(c => !c.isOneTimeEarning) ?? []
  const oneTimeEarnings = data?.components.filter(c => c.isOneTimeEarning) ?? []
  const effectiveTds = data ? (data.tdsOverrideAmount ?? data.tdsAmount) : 0

  function buildDeductions(d: EmployeeVariableInputsDto): DeductionRow[] {
    const rows: DeductionRow[] = []
    if (d.employeePf > 0) rows.push({ label: 'Provident Fund (Employee)', amount: d.employeePf })
    if (d.employeeEsi > 0) rows.push({ label: 'ESI (Employee)', amount: d.employeeEsi })
    if (d.ptAmount > 0) rows.push({ label: 'Professional Tax', amount: d.ptAmount })
    if (d.lwfEmployeeAmount > 0) rows.push({ label: 'Labour Welfare Fund (Employee)', amount: d.lwfEmployeeAmount })
    rows.push({ label: effectiveTds !== d.tdsAmount ? 'TDS (Overridden)' : 'TDS', amount: effectiveTds })
    return rows
  }

  function buildBenefits(d: EmployeeVariableInputsDto): BenefitRow[] {
    const rows: BenefitRow[] = []
    const totalPf = d.employerPf + d.epsAmount
    if (totalPf > 0) rows.push({ label: 'Provident Fund (Employer 12%)', amount: totalPf })
    if (d.employerEsi > 0) rows.push({ label: 'ESI (Employer)', amount: d.employerEsi })
    if (d.gratuityAmount > 0) rows.push({ label: 'Gratuity Accrual', amount: d.gratuityAmount })
    if (d.lwfEmployerAmount > 0) rows.push({ label: 'Labour Welfare Fund (Employer)', amount: d.lwfEmployerAmount })
    return rows
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      <div className="absolute inset-0 bg-black/20" onClick={() => { onClose() }} />

      <div className="relative w-[480px] h-full bg-white shadow-2xl flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-[var(--color-border)]">
          <div>
            <h3 className="text-[14px] font-semibold text-[var(--color-text-primary)]">{employeeName}</h3>
            <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">Pay Breakdown</p>
          </div>
          <button onClick={() => { onClose() }} className="w-7 h-7 flex items-center justify-center rounded-md hover:bg-[var(--color-page-bg)]">
            <X className="w-4 h-4 text-[var(--color-text-secondary)]" />
          </button>
        </div>

        {isLoading && (
          <div className="flex-1 flex items-center justify-center">
            <span className="inline-block w-5 h-5 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
          </div>
        )}

        {data && (
          <>
            {/* Summary strip */}
            <div className="grid grid-cols-3 divide-x divide-[var(--color-border)] border-b border-[var(--color-border)] bg-[var(--color-page-bg)]">
              <div className="px-4 py-3 text-center">
                <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Gross Pay</p>
                <p className="text-[14px] font-semibold text-[var(--color-text-primary)]">{formatINR(data.grossPay)}</p>
              </div>
              <div className="px-4 py-3 text-center">
                <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Deductions</p>
                <p className="text-[14px] font-semibold text-red-600">
                  {formatINR(data.grossPay - data.netPay)}
                </p>
              </div>
              <div className="px-4 py-3 text-center">
                <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Net Pay</p>
                <p className="text-[14px] font-semibold text-[var(--color-primary)]">{formatINR(data.netPay)}</p>
              </div>
            </div>

            <div className="flex-1 overflow-y-auto px-5 py-4 space-y-5">
              {/* Attendance */}
              <section>
                <SectionHeader title="Attendance" />
                <div className="grid grid-cols-3 gap-3">
                  <div>
                    <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">Base Days</label>
                    <div className="h-8 px-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] flex items-center text-[13px] text-[var(--color-text-secondary)]">
                      {data.baseDays}
                    </div>
                  </div>
                  <div>
                    <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">LOP Days</label>
                    <input
                      type="number"
                      min={0}
                      max={data.baseDays - 1}
                      value={lopDays}
                      onChange={e => { setLopDaysEdit(Number(e.target.value)) }}
                      onBlur={handleLopBlur}
                      className="w-full h-8 px-3 rounded-lg bg-white border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">Payable Days</label>
                    <div className="h-8 px-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] flex items-center text-[13px] font-medium text-[var(--color-text-primary)]">
                      {data.baseDays - lopDays}
                    </div>
                  </div>
                </div>
                {lopMutation.isError && (
                  <p className="mt-1.5 text-[12px] text-red-600">Failed to update LOP. Please try again.</p>
                )}
              </section>

              {/* Earnings */}
              <section>
                <SectionHeader title="Earnings" />
                <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                  <table className="w-full">
                    <tbody className="divide-y divide-[var(--color-border)]">
                      {salaryComponents.map((c: ComponentBreakdownDto) => (
                        <tr key={c.id}>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">{c.componentName}</td>
                          <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-primary)]">{formatINR(c.proratedAmount)}</td>
                          <td className="w-8" />
                        </tr>
                      ))}
                      {oneTimeEarnings.map((c: ComponentBreakdownDto) => (
                        <tr key={c.id} className="bg-blue-50/30">
                          <td className="px-3 py-2">
                            <span className="text-[13px] text-[var(--color-text-primary)]">{c.componentName}</span>
                            <span className="ml-1.5 text-[11px] text-blue-600 bg-blue-50 px-1.5 py-0.5 rounded">One-time</span>
                          </td>
                          <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-primary)]">{formatINR(c.fullAmount)}</td>
                          <td className="px-3 py-2 text-right">
                            <button
                              onClick={() => { removeEarningMutation.mutate(c.id) }}
                              className="w-6 h-6 flex items-center justify-center rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-600"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          </td>
                        </tr>
                      ))}
                      <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                        <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">Gross Pay</td>
                        <td className="px-3 py-2 text-[13px] text-right font-bold text-[var(--color-text-primary)]">{formatINR(data.grossPay)}</td>
                        <td className="w-8" />
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>

              {/* Deductions */}
              <section>
                <div className="flex items-center justify-between mb-3">
                  <SectionHeader title="Deductions" />
                  {!editingTds && (
                    <button
                      onClick={() => { setEditingTds(true) }}
                      className="text-[12px] text-[var(--color-primary)] hover:underline -mt-3"
                    >
                      Override TDS
                    </button>
                  )}
                </div>

                {(() => {
                  const deductions = buildDeductions(data)
                  const total = deductions.reduce((s, r) => s + r.amount, 0)
                  return (
                    <>
                      <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                        <table className="w-full">
                          <tbody className="divide-y divide-[var(--color-border)]">
                            {deductions.map((r, i) => (
                              <tr key={i}>
                                <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">{r.label}</td>
                                <td className="px-3 py-2 text-[13px] text-right font-medium text-red-600">{formatINR(r.amount)}</td>
                              </tr>
                            ))}
                            <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                              <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">Total Deductions</td>
                              <td className="px-3 py-2 text-[13px] text-right font-bold text-red-600">{formatINR(total)}</td>
                            </tr>
                          </tbody>
                        </table>
                      </div>

                      {editingTds && (
                        <div className="mt-3 p-3 rounded-lg border border-[var(--color-border)] space-y-2.5">
                          <p className="text-[12px] font-medium text-[var(--color-text-secondary)]">Override TDS Amount</p>
                          <div>
                            <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">Amount (₹)</label>
                            <input
                              type="number"
                              min={0}
                              value={tdsOverride}
                              onChange={e => { setTdsOverride(e.target.value) }}
                              className="w-full h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
                            />
                          </div>
                          <div>
                            <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">Reason <span className="text-red-500">*</span></label>
                            <textarea
                              value={tdsReason}
                              onChange={e => { setTdsReason(e.target.value) }}
                              rows={2}
                              className="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] text-[13px] resize-none focus:outline-none focus:border-[var(--color-primary)]"
                            />
                          </div>
                          <div className="flex gap-2">
                            <button
                              disabled={!tdsReason.trim() || !tdsOverride || tdsMutation.isPending}
                              onClick={() => { tdsMutation.mutate({ amount: parseFloat(tdsOverride), reason: tdsReason }) }}
                              className="h-7 px-3 rounded-lg bg-[var(--color-primary)] text-white text-[12px] font-medium disabled:opacity-60"
                            >
                              {tdsMutation.isPending ? 'Saving…' : 'Save'}
                            </button>
                            <button
                              onClick={() => { setEditingTds(false) }}
                              className="h-7 px-3 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)]"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      )}
                    </>
                  )
                })()}
              </section>

              {/* Benefits (employer contributions) */}
              {(() => {
                const benefits = buildBenefits(data)
                if (benefits.length === 0) return null
                const total = benefits.reduce((s, r) => s + r.amount, 0)
                return (
                  <section>
                    <SectionHeader title="Benefits (Employer Cost)" />
                    <p className="text-[11px] text-[var(--color-text-secondary)] mb-2">
                      These are included in CTC but not deducted from your gross pay.
                    </p>
                    <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                      <table className="w-full">
                        <tbody className="divide-y divide-[var(--color-border)]">
                          {benefits.map((r, i) => (
                            <tr key={i}>
                              <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">{r.label}</td>
                              <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-secondary)]">{formatINR(r.amount)}</td>
                            </tr>
                          ))}
                          <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                            <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">Total Employer Cost</td>
                            <td className="px-3 py-2 text-[13px] text-right font-bold text-[var(--color-text-secondary)]">{formatINR(total)}</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </section>
                )
              })()}
            </div>

            {/* Footer */}
            <div className="border-t border-[var(--color-border)] px-5 py-4 bg-[var(--color-page-bg)]">
              <div className="flex items-center justify-between">
                <div>
                  <span className="text-[13px] font-semibold text-[var(--color-text-primary)]">Net Pay</span>
                  <span className="ml-2 text-[11px] text-[var(--color-text-secondary)]">
                    CTC {formatINR(data.monthlyCTC)}/mo
                  </span>
                </div>
                <span className="text-[16px] font-bold text-[var(--color-primary)]">{formatINR(data.netPay)}</span>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
