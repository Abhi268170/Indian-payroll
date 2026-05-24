import { useState, type ReactElement } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'

interface PayrollRunDto {
  id: string
  type: string
  status: string
  year: number
  month: number
  periodLabel: string
  payDay: string | null
  totalNetPay: number
  employeeCount: number
}

interface EmployeeRowDto {
  employeeId: string
  employeeCode: string
  employeeName: string
  grossPay: number
  netPay: number
}

interface AdhocDeductionForm {
  name: string
  amount: string
}

export default function FnfSettlementPage(): ReactElement {
  const { id: runId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [lopDays, setLopDays] = useState(0)
  const [bonus, setBonus] = useState('')
  const [commission, setCommission] = useState('')
  const [leaveEncash, setLeaveEncash] = useState('')
  const [gratuity, setGratuity] = useState('')
  const [hasNoticePay, setHasNoticePay] = useState(false)
  const [noticeDir, setNoticeDir] = useState<'Payable' | 'Receivable'>('Receivable')
  const [noticeAmount, setNoticeAmount] = useState('')
  const [payslipNotes, setPayslipNotes] = useState('')
  const [deductions, setDeductions] = useState<AdhocDeductionForm[]>([])
  const [error, setError] = useState<string | null>(null)

  const { data: run } = useQuery<PayrollRunDto>({
    queryKey: ['payroll-run', runId],
    queryFn: () => api.get<PayrollRunDto>(`/api/v1/payroll-runs/${runId}`).then(r => r.data),
    enabled: Boolean(runId),
  })

  const { data: rows = [] } = useQuery<EmployeeRowDto[]>({
    queryKey: ['fnf-rows', runId],
    queryFn: () => api.get<EmployeeRowDto[]>(`/api/v1/payroll-runs/${runId}/employees`).then(r => r.data),
    enabled: Boolean(runId),
  })

  // Single-employee FnF: pre-pick first row. Bulk: user picks.
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null)
  const employeeId = selectedEmployeeId ?? rows[0]?.employeeId ?? null

  const mutation = useMutation({
    mutationFn: () => api.put(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/fnf-settlement`, {
      lopDays,
      bonus: parseFloat(bonus) || 0,
      commission: parseFloat(commission) || 0,
      leaveEncashment: parseFloat(leaveEncash) || 0,
      gratuity: parseFloat(gratuity) || 0,
      hasNoticePay,
      noticePayDirection: hasNoticePay ? noticeDir : null,
      noticePayAmount: parseFloat(noticeAmount) || 0,
      payslipNotes: payslipNotes || null,
      deductions: deductions
        .filter(d => d.name && parseFloat(d.amount) > 0)
        .map(d => ({ name: d.name, amount: parseFloat(d.amount) })),
    }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['fnf-rows', runId] })
      void qc.invalidateQueries({ queryKey: ['payroll-run', runId] })
    },
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to save FnF settlement')
    },
  })

  if (!runId) return <div>Missing run id</div>

  const isBulk = run?.type === 'BulkFinalSettlement'
  const monthLabel = run?.periodLabel ?? ''

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">
          {isBulk ? 'Bulk Final Settlement Payroll' : 'Final Settlement Payroll'}
        </h1>
        <button onClick={() => { navigate('/pay-runs') }} className="text-[13px] text-[var(--color-text-secondary)]">
          Close
        </button>
      </div>

      <div className="flex gap-8 mb-6 pb-4 border-b border-[var(--color-border)]">
        <Stat label="Pay Period" value={monthLabel} />
        <Stat label="Pay Date" value={run?.payDay ? formatDate(run.payDay) : '—'} />
        <Stat label="Employees" value={String(run?.employeeCount ?? 0)} />
        <Stat label="Total Net Pay" value={run ? formatINR(run.totalNetPay) : '—'} />
      </div>

      {isBulk && rows.length > 1 && (
        <div className="mb-6">
          <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Employee</label>
          <select
            className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] bg-white"
            value={employeeId ?? ''}
            onChange={e => { setSelectedEmployeeId(e.target.value) }}
          >
            {rows.map(r => <option key={r.employeeId} value={r.employeeId}>{r.employeeName} ({r.employeeCode})</option>)}
          </select>
        </div>
      )}

      {error && (
        <div className="text-[13px] text-red-700 bg-red-50 px-4 py-3 rounded-lg mb-4">{error}</div>
      )}

      <div className="grid grid-cols-2 gap-6">
        <Card title="Attendance">
          <Field label="LOP Days">
            <input
              type="number"
              min={0}
              className={inputCls}
              value={lopDays}
              onChange={e => { setLopDays(parseInt(e.target.value || '0', 10)) }}
            />
          </Field>
        </Card>

        <Card title="Additional Earnings">
          <MoneyField label="Bonus" value={bonus} onChange={setBonus} />
          <MoneyField label="Commission" value={commission} onChange={setCommission} />
          <MoneyField label="Leave Encashment" value={leaveEncash} onChange={setLeaveEncash} />
          <MoneyField label="Gratuity" value={gratuity} onChange={setGratuity}
            hint="Tax-exempt up to ₹20,00,000 lifetime under Sec 10(10)" />
        </Card>

        <Card title="Deductions">
          {deductions.map((d, i) => (
            <div key={i} className="flex gap-2 mb-2">
              <input
                placeholder="Deduction Name"
                className={inputCls + ' flex-1'}
                value={d.name}
                onChange={e => { updateDeduction(i, { ...d, name: e.target.value }) }}
              />
              <input
                type="number" min={0} placeholder="0"
                className={inputCls + ' w-32'}
                value={d.amount}
                onChange={e => { updateDeduction(i, { ...d, amount: e.target.value }) }}
              />
              <button
                onClick={() => { setDeductions(deductions.filter((_, j) => j !== i)) }}
                className="px-2 text-red-600 hover:bg-red-50 rounded"
              >×</button>
            </div>
          ))}
          <button
            onClick={() => { setDeductions([...deductions, { name: '', amount: '' }]) }}
            className="text-[12px] text-[var(--color-primary)] hover:underline"
          >+ Add Deduction</button>
        </Card>

        <Card title="Notice Pay">
          <label className="flex items-center gap-2 cursor-pointer text-[13px] mb-3">
            <input
              type="checkbox"
              checked={hasNoticePay}
              onChange={e => { setHasNoticePay(e.target.checked) }}
              className="accent-[var(--color-primary)]"
            />
            Does this Employee hold Notice Pay?
          </label>
          {hasNoticePay && (
            <>
              <div className="space-y-1.5 mb-3">
                {(['Payable', 'Receivable'] as const).map(v => (
                  <label key={v} className="flex items-center gap-2.5 cursor-pointer text-[13px]">
                    <input
                      type="radio"
                      checked={noticeDir === v}
                      onChange={() => { setNoticeDir(v) }}
                      className="accent-[var(--color-primary)]"
                    />
                    {v === 'Payable' ? 'Payable (company pays employee)' : 'Receivable (recovered from employee)'}
                  </label>
                ))}
              </div>
              <MoneyField label={`${noticeDir} Amount`} value={noticeAmount} onChange={setNoticeAmount} />
            </>
          )}
        </Card>

        <div className="col-span-2">
          <Card title="Notes">
            <textarea
              className={inputCls + ' h-24'}
              value={payslipNotes}
              placeholder="This will be shown in full and final settlement payslip"
              onChange={e => { setPayslipNotes(e.target.value) }}
            />
          </Card>
        </div>
      </div>

      <div className="flex justify-end gap-3 mt-6 pt-6 border-t border-[var(--color-border)]">
        <button
          onClick={() => { navigate('/pay-runs') }}
          className="px-4 h-9 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
        >Cancel</button>
        <button
          disabled={!employeeId || mutation.isPending}
          onClick={() => { mutation.mutate() }}
          className="px-5 h-9 text-[13px] font-medium bg-[var(--color-primary)] text-white rounded-lg disabled:opacity-50 hover:opacity-90"
        >
          {mutation.isPending ? 'Saving…' : 'Save and Continue'}
        </button>
      </div>
    </div>
  )

  function updateDeduction(i: number, next: AdhocDeductionForm): void {
    setDeductions(deductions.map((d, j) => j === i ? next : d))
  }
}

function Card({ title, children }: { title: string; children: React.ReactNode }): ReactElement {
  return (
    <div className="bg-white border border-[var(--color-border)] rounded-xl p-5">
      <h3 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">{title}</h3>
      {children}
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }): ReactElement {
  return (
    <div className="mb-3">
      <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">{label}</label>
      {children}
    </div>
  )
}

function MoneyField({ label, value, onChange, hint }: { label: string; value: string; onChange: (v: string) => void; hint?: string }): ReactElement {
  return (
    <div className="mb-3">
      <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">{label}</label>
      <input
        type="number" min={0} step="0.01" placeholder="0"
        className={inputCls}
        value={value}
        onChange={e => { onChange(e.target.value) }}
      />
      {hint != null && <p className="text-[11px] text-[var(--color-text-muted)] mt-1">{hint}</p>}
    </div>
  )
}

function Stat({ label, value }: { label: string; value: string }): ReactElement {
  return (
    <div>
      <div className="text-[11px] uppercase tracking-wide text-[var(--color-text-secondary)]">{label}</div>
      <div className="text-[14px] font-medium text-[var(--color-text-primary)]">{value}</div>
    </div>
  )
}

const inputCls = 'w-full h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white'

function formatDate(iso: string): string {
  const d = new Date(iso)
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
}

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
