import { useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Trash2, Plus } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { EmployeeVariableInputsDto, ComponentBreakdownDto } from '@/types/api'
import AddOneTimeEntryModal from './AddOneTimeEntryModal'

interface EmployeePayBreakdownProps {
  runId: string
  employeeId: string
  employeeName: string
  readOnly: boolean
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

export default function EmployeePayBreakdown({
  runId,
  employeeId,
  employeeName,
  readOnly,
}: EmployeePayBreakdownProps): React.ReactElement {
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery<EmployeeVariableInputsDto>({
    queryKey: ['variable-inputs', runId, employeeId],
    queryFn: () =>
      api
        .get<EmployeeVariableInputsDto>(
          `/api/v1/payroll-runs/${runId}/employees/${employeeId}/inputs`,
        )
        .then(r => r.data),
  })

  const [lopDaysEdit, setLopDaysEdit] = useState<number | null>(null)
  const [tdsOverride, setTdsOverride] = useState<string>('')
  const [tdsReason, setTdsReason] = useState('')
  const [editingTds, setEditingTds] = useState(false)
  const [addModalCategory, setAddModalCategory] = useState<'Earning' | 'Deduction' | null>(null)
  // isDirtyLopRef: true while the user has typed into the LOP input but not yet saved.
  // Using a ref (not state) so it can be read inside useEffect without becoming a dep.
  const isDirtyLopRef = useRef(false)

  const lopDays = lopDaysEdit ?? (data?.lopDays ?? 0)

  const invalidateQueries = (): void => {
    void queryClient.invalidateQueries({ queryKey: ['variable-inputs', runId, employeeId] })
    void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
    void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
  }

  const lopMutation = useMutation({
    mutationFn: (days: number) =>
      api.put(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/lop`, { lopDays: days }),
    onSuccess: () => {
      isDirtyLopRef.current = false
      setLopDaysEdit(null) // reset edit; display reverts to server value on next data refresh
      invalidateQueries()
    },
  })

  const tdsMutation = useMutation({
    mutationFn: ({ amount, reason }: { amount: number; reason: string }) =>
      api.put(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/tds-override`, {
        overrideAmount: amount,
        reason,
      }),
    onSuccess: () => {
      setEditingTds(false)
      invalidateQueries()
    },
  })

  const removeEarningMutation = useMutation({
    mutationFn: (breakdownId: string) =>
      api.delete(
        `/api/v1/payroll-runs/${runId}/employees/${employeeId}/earnings/${breakdownId}`,
      ),
    onSuccess: invalidateQueries,
  })

  function handleLopBlur(): void {
    if (data && lopDays !== data.lopDays) {
      lopMutation.mutate(lopDays)
    }
  }

  const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'
  const salaryComponents = data?.components.filter(c => !c.isOneTimeEarning) ?? []
  const oneTimeEarnings =
    data?.components.filter(
      c => c.isOneTimeEarning && c.salaryComponentId !== EMPTY_GUID && !c.isDeduction,
    ) ?? []
  const oneTimeDeductions =
    data?.components.filter(
      c => c.isOneTimeEarning && c.salaryComponentId !== EMPTY_GUID && c.isDeduction,
    ) ?? []
  const reimbursements =
    data?.components.filter(
      c => c.isOneTimeEarning && c.salaryComponentId === EMPTY_GUID,
    ) ?? []

  const effectiveTds = data ? (data.tdsOverrideAmount ?? data.tdsAmount) : 0
  const explicitDeductions = data
    ? data.employeePf +
      data.employeeEsi +
      data.ptAmount +
      data.lwfEmployeeAmount +
      effectiveTds +
      oneTimeDeductions.reduce((s, c) => s + c.fullAmount, 0)
    : 0

  function buildDeductions(d: EmployeeVariableInputsDto): DeductionRow[] {
    const rows: DeductionRow[] = []
    if (d.employeePf > 0) rows.push({ label: 'Provident Fund (Employee)', amount: d.employeePf })
    if (d.employeeEsi > 0) rows.push({ label: 'ESI (Employee)', amount: d.employeeEsi })
    if (d.ptAmount > 0) rows.push({ label: 'Professional Tax', amount: d.ptAmount })
    if (d.lwfEmployeeAmount > 0)
      rows.push({ label: 'Labour Welfare Fund (Employee)', amount: d.lwfEmployeeAmount })
    rows.push({
      label: effectiveTds !== d.tdsAmount ? 'TDS (Overridden)' : 'TDS',
      amount: effectiveTds,
    })
    return rows
  }

  function buildBenefits(d: EmployeeVariableInputsDto): BenefitRow[] {
    const rows: BenefitRow[] = []
    const totalPf = d.employerPf + d.epsAmount
    if (totalPf > 0) rows.push({ label: 'Provident Fund (Employer 12%)', amount: totalPf })
    if (d.employerEsi > 0) rows.push({ label: 'ESI (Employer)', amount: d.employerEsi })
    if (d.gratuityAmount > 0) rows.push({ label: 'Gratuity Accrual', amount: d.gratuityAmount })
    if (d.lwfEmployerAmount > 0)
      rows.push({ label: 'Labour Welfare Fund (Employer)', amount: d.lwfEmployerAmount })
    return rows
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <span className="inline-block w-5 h-5 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (!data) return <></>

  const deductionRows = buildDeductions(data)
  const totalStatutoryDeductions = deductionRows.reduce((s, r) => s + r.amount, 0)
  const totalOneTimeDeductions = oneTimeDeductions.reduce((s, c) => s + c.fullAmount, 0)
  const benefits = buildBenefits(data)
  const totalBenefits = benefits.reduce((s, r) => s + r.amount, 0)

  return (
    <div className="py-4 px-5">
      <p className="text-[12px] font-semibold text-[var(--color-text-secondary)] mb-3">
        {employeeName} — Pay Breakdown
      </p>

      {/* Summary strip */}
      <div className="grid grid-cols-3 divide-x divide-[var(--color-border)] border border-[var(--color-border)] rounded-lg mb-5 bg-[var(--color-page-bg)]">
        <div className="px-4 py-3 text-center">
          <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Gross Pay</p>
          <p className="text-[14px] font-semibold text-[var(--color-text-primary)]">
            {formatINR(data.grossPay)}
          </p>
        </div>
        <div className="px-4 py-3 text-center">
          <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Deductions</p>
          <p className="text-[14px] font-semibold text-red-600">{formatINR(explicitDeductions)}</p>
        </div>
        <div className="px-4 py-3 text-center">
          <p className="text-[11px] text-[var(--color-text-secondary)] mb-0.5">Net Pay</p>
          <p className="text-[14px] font-semibold text-[var(--color-primary)]">
            {formatINR(data.netPay)}
          </p>
        </div>
      </div>

      <div className="space-y-5">
        {/* Attendance */}
        <section>
          <SectionHeader title="Attendance" />
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">
                Base Days
              </label>
              <div className="h-8 px-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] flex items-center text-[13px] text-[var(--color-text-secondary)]">
                {data.baseDays}
              </div>
            </div>
            <div>
              <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">
                LOP Days
              </label>
              {readOnly ? (
                <div className="h-8 px-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] flex items-center text-[13px] text-[var(--color-text-secondary)]">
                  {lopDays}
                </div>
              ) : (
                <input
                  type="number"
                  min={0}
                  max={data.baseDays - 1}
                  value={lopDays}
                  onChange={e => {
                    isDirtyLopRef.current = true
                    setLopDaysEdit(Number(e.target.value))
                  }}
                  onBlur={handleLopBlur}
                  className="w-full h-8 px-3 rounded-lg bg-white border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
                />
              )}
            </div>
            <div>
              <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">
                Payable Days
              </label>
              <div className="h-8 px-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)] flex items-center text-[13px] font-medium text-[var(--color-text-primary)]">
                {data.baseDays - lopDays}
              </div>
            </div>
          </div>
          {lopMutation.isError && (
            <p className="mt-1.5 text-[12px] text-red-600">
              Failed to update LOP. Please try again.
            </p>
          )}
        </section>

        {/* Earnings */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <SectionHeader title="Earnings" />
            {!readOnly && (
              <button
                onClick={() => { setAddModalCategory('Earning') }}
                className="text-[12px] text-[var(--color-primary)] hover:underline -mt-3 flex items-center gap-1"
              >
                <Plus className="w-3 h-3" />
                Add Earning
              </button>
            )}
          </div>
          <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
            <table className="w-full">
              <tbody className="divide-y divide-[var(--color-border)]">
                {salaryComponents.map((c: ComponentBreakdownDto) => (
                  <tr key={c.id}>
                    <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">
                      {c.componentName}
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-primary)]">
                      {formatINR(c.proratedAmount)}
                    </td>
                    <td className="w-8" />
                  </tr>
                ))}
                {oneTimeEarnings.map((c: ComponentBreakdownDto) => (
                  <tr key={c.id} className="bg-blue-50/30">
                    <td className="px-3 py-2">
                      <span className="text-[13px] text-[var(--color-text-primary)]">
                        {c.componentName}
                      </span>
                      <span className="ml-1.5 text-[11px] text-blue-600 bg-blue-50 px-1.5 py-0.5 rounded">
                        One-time
                      </span>
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-primary)]">
                      {formatINR(c.fullAmount)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      {!readOnly && (
                        <button
                          onClick={() => { removeEarningMutation.mutate(c.id) }}
                          className="w-6 h-6 flex items-center justify-center rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-600"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                  <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">
                    Gross Pay
                  </td>
                  <td className="px-3 py-2 text-[13px] text-right font-bold text-[var(--color-text-primary)]">
                    {formatINR(data.grossPay)}
                  </td>
                  <td className="w-8" />
                </tr>
                {reimbursements.map((c: ComponentBreakdownDto) => (
                  <tr key={c.id} className="bg-green-50/30">
                    <td className="px-3 py-2">
                      <span className="text-[13px] text-[var(--color-text-primary)]">
                        {c.componentName}
                      </span>
                      <span className="ml-1.5 text-[11px] text-green-700 bg-green-50 px-1.5 py-0.5 rounded">
                        Reimbursement
                      </span>
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-primary)]">
                      {formatINR(c.fullAmount)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      {!readOnly && (
                        <button
                          onClick={() => { removeEarningMutation.mutate(c.id) }}
                          className="w-6 h-6 flex items-center justify-center rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-600"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* Deductions */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <SectionHeader title="Deductions" />
            {!readOnly && (
              <div className="flex items-center gap-3 -mt-3">
                <button
                  onClick={() => { setAddModalCategory('Deduction') }}
                  className="text-[12px] text-[var(--color-primary)] hover:underline flex items-center gap-1"
                >
                  <Plus className="w-3 h-3" />
                  Add Deduction
                </button>
                {!editingTds && (
                  <button
                    onClick={() => {
                      setTdsOverride(
                        data.tdsOverrideAmount !== null ? String(data.tdsOverrideAmount) : '',
                      )
                      setEditingTds(true)
                    }}
                    className="text-[12px] text-[var(--color-primary)] hover:underline"
                  >
                    Override TDS
                  </button>
                )}
              </div>
            )}
          </div>

          <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
            <table className="w-full">
              <tbody className="divide-y divide-[var(--color-border)]">
                {deductionRows.map((r, i) => (
                  <tr key={i}>
                    <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">
                      {r.label}
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-medium text-red-600">
                      {formatINR(r.amount)}
                    </td>
                    <td className="w-8" />
                  </tr>
                ))}
                {oneTimeDeductions.map((c: ComponentBreakdownDto) => (
                  <tr key={c.id} className="bg-orange-50/30">
                    <td className="px-3 py-2">
                      <span className="text-[13px] text-[var(--color-text-primary)]">
                        {c.componentName}
                      </span>
                      <span className="ml-1.5 text-[11px] text-orange-700 bg-orange-50 px-1.5 py-0.5 rounded">
                        One-time
                      </span>
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-medium text-red-600">
                      {formatINR(c.fullAmount)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      {!readOnly && (
                        <button
                          onClick={() => { removeEarningMutation.mutate(c.id) }}
                          className="w-6 h-6 flex items-center justify-center rounded hover:bg-red-50 text-[var(--color-text-secondary)] hover:text-red-600"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                  <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">
                    Total Deductions
                  </td>
                  <td className="px-3 py-2 text-[13px] text-right font-bold text-red-600">
                    {formatINR(totalStatutoryDeductions + totalOneTimeDeductions)}
                  </td>
                  <td className="w-8" />
                </tr>
              </tbody>
            </table>
          </div>

          {!readOnly && editingTds && (
            <div className="mt-3 p-3 rounded-lg border border-[var(--color-border)] space-y-2.5">
              <p className="text-[12px] font-medium text-[var(--color-text-secondary)]">
                Override TDS Amount
              </p>
              <div>
                <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">
                  Amount (₹)
                </label>
                <input
                  type="number"
                  min={0}
                  value={tdsOverride}
                  onChange={e => { setTdsOverride(e.target.value) }}
                  className="w-full h-8 px-3 rounded-lg border border-[var(--color-border)] text-[13px] focus:outline-none focus:border-[var(--color-primary)]"
                />
              </div>
              <div>
                <label className="block text-[11px] text-[var(--color-text-secondary)] mb-1">
                  Reason <span className="text-red-500">*</span>
                </label>
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
                  onClick={() => {
                    tdsMutation.mutate({ amount: parseFloat(tdsOverride), reason: tdsReason })
                  }}
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
        </section>

        {/* Benefits (employer contributions) */}
        {benefits.length > 0 && (
          <section>
            <SectionHeader title="Benefits (Employer Cost)" />
            <p className="text-[11px] text-[var(--color-text-secondary)] mb-2">
              Included in CTC but not deducted from gross pay.
            </p>
            <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
              <table className="w-full">
                <tbody className="divide-y divide-[var(--color-border)]">
                  {benefits.map((r, i) => (
                    <tr key={i}>
                      <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">
                        {r.label}
                      </td>
                      <td className="px-3 py-2 text-[13px] text-right font-medium text-[var(--color-text-secondary)]">
                        {formatINR(r.amount)}
                      </td>
                    </tr>
                  ))}
                  <tr className="bg-[var(--color-page-bg)] border-t-2 border-[var(--color-border)]">
                    <td className="px-3 py-2 text-[12px] font-semibold text-[var(--color-text-secondary)]">
                      Total Employer Cost
                    </td>
                    <td className="px-3 py-2 text-[13px] text-right font-bold text-[var(--color-text-secondary)]">
                      {formatINR(totalBenefits)}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        )}

        <p className="text-[11px] text-[var(--color-text-secondary)] border-t border-[var(--color-border)] pt-3">
          Monthly CTC: {formatINR(data.monthlyCTC)}
        </p>
      </div>

      {addModalCategory != null && (
        <AddOneTimeEntryModal
          runId={runId}
          employeeId={employeeId}
          category={addModalCategory}
          onClose={() => { setAddModalCategory(null) }}
        />
      )}
    </div>
  )
}
