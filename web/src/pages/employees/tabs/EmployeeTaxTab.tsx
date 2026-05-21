import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'

interface Props {
  employeeId: string
}

interface FyOpeningDto {
  employeeId: string
  fiscalYear: number
  monthsCount: number
  grossSalary: number
  tdsDeducted: number
  pfDeducted: number
}

const CURRENT_FY = new Date().getMonth() >= 3
  ? new Date().getFullYear()
  : new Date().getFullYear() - 1

const FY_OPTIONS = [CURRENT_FY, CURRENT_FY - 1, CURRENT_FY - 2]

function fyLabel(y: number): string {
  return `FY ${y}-${String(y + 1).slice(2)}`
}

export default function EmployeeTaxTab({ employeeId }: Props): React.ReactElement {
  const qc = useQueryClient()
  const [selectedFy, setSelectedFy] = useState(CURRENT_FY)
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({ monthsCount: 1, grossSalary: '', tdsDeducted: '', pfDeducted: '' })
  const [err, setErr] = useState<string | null>(null)

  const { data, isLoading } = useQuery<FyOpeningDto | null>({
    queryKey: ['employee-fy-opening', employeeId, selectedFy],
    queryFn: async () => {
      try {
        const r = await api.get<FyOpeningDto>(`/api/v1/employees/${employeeId}/fy-opening/${selectedFy}`)
        return r.data
      } catch {
        return null
      }
    },
  })

  function startEdit(): void {
    setForm({
      monthsCount: data?.monthsCount ?? 1,
      grossSalary: data ? String(data.grossSalary) : '',
      tdsDeducted: data ? String(data.tdsDeducted) : '',
      pfDeducted: data ? String(data.pfDeducted) : '',
    })
    setErr(null)
    setEditing(true)
  }

  const save = useMutation({
    mutationFn: () =>
      api.put(`/api/v1/employees/${employeeId}/fy-opening/${selectedFy}`, {
        monthsCount: Number(form.monthsCount),
        grossSalary: parseFloat(form.grossSalary) || 0,
        tdsDeducted: parseFloat(form.tdsDeducted) || 0,
        pfDeducted: parseFloat(form.pfDeducted) || 0,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['employee-fy-opening', employeeId, selectedFy] })
      setEditing(false)
    },
    onError: () => setErr('Save failed. Check values and try again.'),
  })

  return (
    <div className="p-5 space-y-5">
      {/* FY Opening Balance */}
      <div className="border border-[var(--color-border)] rounded-xl p-5">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-[14px] font-semibold text-[var(--color-text-primary)]">Prior Payroll Opening Balance</h3>
            <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
              Salary & TDS paid before this system was adopted (same employer, same FY).
            </p>
          </div>
          <div className="flex items-center gap-2">
            <select
              value={selectedFy}
              onChange={e => { setSelectedFy(Number(e.target.value)); setEditing(false) }}
              className="h-8 px-2 text-[12px] border border-[var(--color-border)] rounded-lg bg-white text-[var(--color-text-primary)]"
            >
              {FY_OPTIONS.map(y => (
                <option key={y} value={y}>{fyLabel(y)}</option>
              ))}
            </select>
            {!editing && (
              <button
                onClick={startEdit}
                className="h-8 px-3 text-[12px] font-medium border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-bg-secondary)] transition-colors"
              >
                {data ? 'Edit' : 'Add'}
              </button>
            )}
          </div>
        </div>

        {isLoading ? (
          <p className="text-[13px] text-[var(--color-text-secondary)]">Loading…</p>
        ) : !editing ? (
          data ? (
            <div className="grid grid-cols-3 gap-4">
              <div>
                <p className="text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">Months</p>
                <p className="text-[20px] font-semibold text-[var(--color-text-primary)] mt-0.5">{data.monthsCount}</p>
              </div>
              <div>
                <p className="text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">Gross Salary</p>
                <p className="text-[20px] font-semibold text-[var(--color-text-primary)] mt-0.5">{formatINR(data.grossSalary)}</p>
              </div>
              <div>
                <p className="text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">TDS Deducted</p>
                <p className="text-[20px] font-semibold text-[var(--color-text-primary)] mt-0.5">{formatINR(data.tdsDeducted)}</p>
              </div>
              <div>
                <p className="text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">PF Deducted</p>
                <p className="text-[20px] font-semibold text-[var(--color-text-primary)] mt-0.5">{formatINR(data.pfDeducted)}</p>
              </div>
            </div>
          ) : (
            <p className="text-[13px] text-[var(--color-text-secondary)]">
              No opening balance recorded for {fyLabel(selectedFy)}.
            </p>
          )
        ) : (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1">
                  Months Count
                </label>
                <input
                  type="number"
                  min={1}
                  max={12}
                  value={form.monthsCount}
                  onChange={e => setForm(f => ({ ...f, monthsCount: Number(e.target.value) }))}
                  className="w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg"
                />
              </div>
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1">
                  Gross Salary (₹)
                </label>
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={form.grossSalary}
                  onChange={e => setForm(f => ({ ...f, grossSalary: e.target.value }))}
                  className="w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg"
                  placeholder="0.00"
                />
              </div>
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1">
                  TDS Deducted (₹)
                </label>
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={form.tdsDeducted}
                  onChange={e => setForm(f => ({ ...f, tdsDeducted: e.target.value }))}
                  className="w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg"
                  placeholder="0.00"
                />
              </div>
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1">
                  PF Deducted (₹)
                </label>
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={form.pfDeducted}
                  onChange={e => setForm(f => ({ ...f, pfDeducted: e.target.value }))}
                  className="w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg"
                  placeholder="0.00"
                />
              </div>
            </div>
            {err && <p className="text-[12px] text-red-600">{err}</p>}
            <div className="flex gap-2">
              <button
                onClick={() => void save.mutateAsync()}
                disabled={save.isPending}
                className="h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
              >
                {save.isPending ? 'Saving…' : 'Save'}
              </button>
              <button
                onClick={() => setEditing(false)}
                className="h-8 px-4 text-[12px] font-medium border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-bg-secondary)] transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
