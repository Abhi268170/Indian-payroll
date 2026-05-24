import { useState, type ReactElement } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

interface EmployeeOverview {
  id: string
  employeeCode: string
  fullName: string
  designation: string
  department: string
  dateOfJoining: string
}

interface ExitResponse {
  id: string
  employeeId: string
  fnfPayrollRunId: string
  fnfPayrollRunType: string
  fnfPayDate: string | null
}

const REASONS = [
  { value: 'TerminatedByEmployer', label: 'Terminated By Employer' },
  { value: 'TerminationByDeath', label: 'Termination By Death' },
  { value: 'TerminationByDisability', label: 'Termination by Disability' },
  { value: 'ResignedByEmployee', label: 'Resigned By Employee' },
]

export default function ExitInitiationPage(): ReactElement {
  const { id: employeeId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [lastWorkingDay, setLastWorkingDay] = useState('')
  const [reason, setReason] = useState('ResignedByEmployee')
  const [mode, setMode] = useState<'RegularSchedule' | 'CustomDate'>('RegularSchedule')
  const [settlementDate, setSettlementDate] = useState('')
  const [personalEmail, setPersonalEmail] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: employee } = useQuery<EmployeeOverview>({
    queryKey: ['employee', employeeId],
    queryFn: () => api.get<EmployeeOverview>(`/api/v1/employees/${employeeId}`).then(r => r.data),
    enabled: Boolean(employeeId),
  })

  const mutation = useMutation({
    mutationFn: () => api.post<ExitResponse>(`/api/v1/employees/${employeeId}/exit`, {
      lastWorkingDay,
      reason,
      settlementMode: mode,
      settlementDate: mode === 'CustomDate' ? settlementDate : null,
      personalEmail: personalEmail || null,
      notes: notes || null,
    }).then(r => r.data),
    onSuccess: (data) => {
      navigate(`/pay-runs/${data.fnfPayrollRunId}/fnf`)
    },
    onError: (err: unknown) => {
      setError(extractError(err) ?? 'Failed to initiate exit')
    },
  })

  const canSubmit = lastWorkingDay && reason && (mode === 'RegularSchedule' || settlementDate)

  return (
    <div className="px-8 py-8 max-w-5xl">
      <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)] mb-6">
        {employee?.fullName ?? 'Employee'}&apos;s Exit details
      </h1>

      <div className="grid grid-cols-3 gap-8">
        <div className="col-span-2 space-y-5">
          {error && (
            <div className="text-[13px] text-red-700 bg-red-50 px-4 py-3 rounded-lg">{error}</div>
          )}

          <Field label="Last Working Day *">
            <input
              type="date"
              className={inputCls}
              value={lastWorkingDay}
              onChange={e => { setLastWorkingDay(e.target.value) }}
            />
          </Field>

          <Field label="Reason for Exit *">
            <select className={inputCls} value={reason} onChange={e => { setReason(e.target.value) }}>
              {REASONS.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </Field>

          <div>
            <label className="block text-[12px] text-[var(--color-text-secondary)] mb-2">
              When do you want to settle the final pay?
            </label>
            <div className="space-y-2">
              {(['RegularSchedule', 'CustomDate'] as const).map(v => (
                <label key={v} className="flex items-center gap-2.5 cursor-pointer text-[13px]">
                  <input
                    type="radio"
                    checked={mode === v}
                    onChange={() => { setMode(v) }}
                    className="accent-[var(--color-primary)]"
                  />
                  {v === 'RegularSchedule' ? 'Pay as per the regular pay schedule' : 'Pay on a given date'}
                </label>
              ))}
            </div>
          </div>

          {mode === 'CustomDate' && (
            <Field label="Final Settlement Date *">
              <input
                type="date"
                className={inputCls}
                value={settlementDate}
                onChange={e => { setSettlementDate(e.target.value) }}
              />
            </Field>
          )}

          <Field label="Personal Email Address">
            <input
              type="email"
              className={inputCls}
              value={personalEmail}
              onChange={e => { setPersonalEmail(e.target.value) }}
              placeholder="Email for final payslip + Form-16"
            />
          </Field>

          <Field label="Notes">
            <textarea
              className={inputCls + ' h-24'}
              value={notes}
              onChange={e => { setNotes(e.target.value) }}
            />
          </Field>

          <div className="flex gap-3 pt-4 border-t border-[var(--color-border)]">
            <button
              type="button"
              disabled={!canSubmit || mutation.isPending}
              onClick={() => { mutation.mutate() }}
              className="px-5 h-9 text-[13px] font-medium bg-[var(--color-primary)] text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:opacity-90"
            >
              {mutation.isPending ? 'Initiating…' : 'Proceed'}
            </button>
            <button
              type="button"
              onClick={() => { navigate(`/employees/${employeeId}`) }}
              className="px-4 h-9 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
            >
              Cancel
            </button>
          </div>
        </div>

        {employee && (
          <aside className="bg-[var(--color-page-bg)] rounded-xl p-5 self-start">
            <div className="w-16 h-16 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-2xl font-semibold mb-3">
              {employee.fullName.charAt(0)}
            </div>
            <div className="text-[15px] font-semibold text-[var(--color-text-primary)]">{employee.fullName}</div>
            <div className="text-[12px] text-[var(--color-text-muted)] mb-4">ID: {employee.employeeCode}</div>
            <Row label="Designation" value={employee.designation} />
            <Row label="Department" value={employee.department} />
            <Row label="Date of Joining" value={formatDate(employee.dateOfJoining)} />
          </aside>
        )}
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }): ReactElement {
  return (
    <div>
      <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">{label}</label>
      {children}
    </div>
  )
}

function Row({ label, value }: { label: string; value: string }): ReactElement {
  return (
    <div className="text-[12px] py-1">
      <span className="text-[var(--color-text-secondary)]">{label}:</span>{' '}
      <span className="text-[var(--color-text-primary)]">{value}</span>
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
