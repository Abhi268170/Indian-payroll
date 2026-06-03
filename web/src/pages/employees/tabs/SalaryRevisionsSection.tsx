import { useState, type ReactElement } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import { Badge } from '@/components/ui/Badge'
import type {
  SalaryRevisionSummaryDto,
  SalaryRevisionArrearPreviewDto,
  SalaryStructureTemplateSummaryDto,
  CreateSalaryRevisionRequest,
} from '@/types/api'

interface Props {
  employeeId: string
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function monthLabel(month: number, year: number): string {
  return `${MONTHS[month - 1] ?? '?'} ${String(year)}`
}

function extractError(err: unknown): string {
  const data = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response?.data
  return data?.error ?? data?.errors?.[0] ?? 'Something went wrong.'
}

const btnPrimary =
  'h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] transition-colors disabled:opacity-50'
const btnOutline =
  'h-8 px-3 text-[12px] text-[var(--color-primary)] border border-[var(--color-primary)]/40 rounded-lg hover:bg-[var(--color-primary)]/5 transition-colors disabled:opacity-50'
const inputCls =
  'h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg focus:outline-none focus:border-[var(--color-primary)] bg-white'
const labelCls = 'text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide'

export default function SalaryRevisionsSection({ employeeId }: Props): ReactElement {
  const qc = useQueryClient()
  const nowYear = new Date().getFullYear()

  const [showForm, setShowForm] = useState(false)
  const [previewRid, setPreviewRid] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const [newCtc, setNewCtc] = useState('')
  const [templateId, setTemplateId] = useState('')
  const [effMonth, setEffMonth] = useState(1)
  const [effYear, setEffYear] = useState(nowYear)
  const [payoutMonth, setPayoutMonth] = useState(1)
  const [payoutYear, setPayoutYear] = useState(nowYear)
  const [notes, setNotes] = useState('')

  const { data: revisions = [] } = useQuery<SalaryRevisionSummaryDto[]>({
    queryKey: ['salary-revisions', employeeId],
    queryFn: () =>
      api.get<SalaryRevisionSummaryDto[]>(`/api/v1/employees/${employeeId}/salary-revisions`).then(r => r.data),
  })

  const { data: templates = [] } = useQuery<SalaryStructureTemplateSummaryDto[]>({
    queryKey: ['salary-structure-templates'],
    queryFn: () =>
      api.get<SalaryStructureTemplateSummaryDto[]>('/api/v1/salary-structure-templates').then(r => r.data),
  })

  const { data: preview, isFetching: previewLoading } = useQuery<SalaryRevisionArrearPreviewDto>({
    queryKey: ['salary-revision-arrears', previewRid],
    queryFn: () =>
      api
        .get<SalaryRevisionArrearPreviewDto>(
          `/api/v1/employees/${employeeId}/salary-revisions/${String(previewRid)}/arrear-preview`,
        )
        .then(r => r.data),
    enabled: previewRid !== null,
    retry: false,
  })

  const createMutation = useMutation({
    mutationFn: (): Promise<{ id: string }> => {
      const body: CreateSalaryRevisionRequest = {
        newAnnualCTC: parseFloat(newCtc) || 0,
        effectiveFromMonth: effMonth,
        effectiveFromYear: effYear,
        payoutMonth,
        payoutYear,
        salaryStructureTemplateId: templateId || null,
        overrides: [],
        notes: notes.trim() === '' ? null : notes.trim(),
      }
      return api
        .post<{ id: string }>(`/api/v1/employees/${employeeId}/salary-revisions`, body)
        .then(r => r.data)
    },
    onSuccess: data => {
      setFormError(null)
      setShowForm(false)
      setNewCtc('')
      setNotes('')
      void qc.invalidateQueries({ queryKey: ['salary-revisions', employeeId] })
      setPreviewRid(data.id)
    },
    onError: err => { setFormError(extractError(err)) },
  })

  const applyMutation = useMutation({
    mutationFn: (rid: string): Promise<void> =>
      api.post(`/api/v1/employees/${employeeId}/salary-revisions/${rid}/apply`).then(() => undefined),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['salary-revisions', employeeId] })
      void qc.invalidateQueries({ queryKey: ['employee-salary', employeeId] })
    },
  })

  const effAfterPayout = effYear * 12 + effMonth > payoutYear * 12 + payoutMonth
  const canSubmit = parseFloat(newCtc) > 0 && templateId !== '' && !effAfterPayout

  return (
    <div className="border border-[var(--color-border)] rounded-xl overflow-hidden">
      <div className="px-5 py-3 border-b border-[var(--color-border)] bg-[var(--color-page-bg)] flex items-center justify-between">
        <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)]">Salary Revisions</h3>
        {!showForm && (
          <button onClick={() => { setShowForm(true) }} className={btnOutline}>
            New revision
          </button>
        )}
      </div>

      {showForm && (
        <div className="p-5 border-b border-[var(--color-border)] space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1">
              <label className={labelCls}>New Annual CTC</label>
              <input
                type="number"
                value={newCtc}
                onChange={e => { setNewCtc(e.target.value) }}
                className={inputCls}
                placeholder="e.g. 1320000"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className={labelCls}>Salary Structure Template</label>
              <select value={templateId} onChange={e => { setTemplateId(e.target.value) }} className={inputCls}>
                <option value="">Select template…</option>
                {templates.map(t => (
                  <option key={t.id} value={t.id}>{t.name}</option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1">
              <label className={labelCls}>Effective From</label>
              <div className="flex gap-2">
                <select value={effMonth} onChange={e => { setEffMonth(Number(e.target.value)) }} className={`${inputCls} flex-1`}>
                  {MONTHS.map((m, i) => <option key={m} value={i + 1}>{m}</option>)}
                </select>
                <select value={effYear} onChange={e => { setEffYear(Number(e.target.value)) }} className={`${inputCls} w-24`}>
                  {[nowYear - 1, nowYear, nowYear + 1].map(y => <option key={y} value={y}>{y}</option>)}
                </select>
              </div>
            </div>
            <div className="flex flex-col gap-1">
              <label className={labelCls}>Payout In</label>
              <div className="flex gap-2">
                <select value={payoutMonth} onChange={e => { setPayoutMonth(Number(e.target.value)) }} className={`${inputCls} flex-1`}>
                  {MONTHS.map((m, i) => <option key={m} value={i + 1}>{m}</option>)}
                </select>
                <select value={payoutYear} onChange={e => { setPayoutYear(Number(e.target.value)) }} className={`${inputCls} w-24`}>
                  {[nowYear - 1, nowYear, nowYear + 1].map(y => <option key={y} value={y}>{y}</option>)}
                </select>
              </div>
            </div>
          </div>
          <div className="flex flex-col gap-1">
            <label className={labelCls}>Notes (optional)</label>
            <input type="text" value={notes} onChange={e => { setNotes(e.target.value) }} className={inputCls} placeholder="e.g. Annual hike" />
          </div>

          {effAfterPayout && (
            <p className="text-[12px] text-[var(--color-error)]">Effective-from month must be on or before the payout month.</p>
          )}
          {formError !== null && <p className="text-[12px] text-[var(--color-error)]">{formError}</p>}

          <div className="flex gap-2">
            <button onClick={() => { createMutation.mutate() }} disabled={!canSubmit || createMutation.isPending} className={btnPrimary}>
              {createMutation.isPending ? 'Creating…' : 'Create & preview arrears'}
            </button>
            <button onClick={() => { setShowForm(false); setFormError(null) }} className={btnOutline}>Cancel</button>
          </div>
        </div>
      )}

      {revisions.length === 0 && !showForm ? (
        <p className="px-5 py-6 text-[13px] text-[var(--color-text-secondary)] text-center">No salary revisions yet.</p>
      ) : (
        <table className="w-full text-[12px]">
          <thead>
            <tr className="border-b border-[var(--color-border)]">
              <th className="text-left px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Status</th>
              <th className="text-left px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Effective</th>
              <th className="text-left px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Payout</th>
              <th className="text-right px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Prev → New CTC</th>
              <th className="text-right px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Actions</th>
            </tr>
          </thead>
          <tbody>
            {revisions.map(r => (
              <tr key={r.id} className="border-b border-[var(--color-border)] last:border-0">
                <td className="px-5 py-3">
                  <Badge variant={r.status === 'Applied' ? 'success' : 'warning'}>{r.status}</Badge>
                  {r.arrearsPaid && <span className="ml-2 text-[11px] text-[var(--color-text-muted)]">arrears paid</span>}
                </td>
                <td className="px-5 py-3 text-[var(--color-text-primary)]">{monthLabel(r.effectiveFromMonth, r.effectiveFromYear)}</td>
                <td className="px-5 py-3 text-[var(--color-text-primary)]">{monthLabel(r.payoutMonth, r.payoutYear)}</td>
                <td className="px-5 py-3 text-right text-[var(--color-text-primary)]">
                  {formatINR(r.previousAnnualCTC)} → {formatINR(r.newAnnualCTC)}
                </td>
                <td className="px-5 py-3 text-right">
                  <button onClick={() => { setPreviewRid(r.id) }} className="text-[var(--color-primary)] hover:underline mr-3">Preview</button>
                  {r.status === 'Pending' && (
                    <button
                      onClick={() => { applyMutation.mutate(r.id) }}
                      disabled={applyMutation.isPending}
                      className="text-[var(--color-primary)] hover:underline disabled:opacity-50"
                    >
                      Apply
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {previewRid !== null && (
        <div className="p-5 border-t border-[var(--color-border)] bg-[var(--color-page-bg)] space-y-3">
          <div className="flex items-center justify-between">
            <h4 className="text-[12px] font-semibold text-[var(--color-text-primary)] uppercase tracking-wide">Arrears preview</h4>
            <button onClick={() => { setPreviewRid(null) }} className="text-[11px] text-[var(--color-text-muted)] hover:underline">Close</button>
          </div>

          {previewLoading && <p className="text-[12px] text-[var(--color-text-secondary)]">Computing…</p>}

          {!previewLoading && preview && (
            preview.totalArrear === 0 ? (
              <p className="text-[12px] text-[var(--color-text-secondary)]">
                No arrears — revision is future-dated or no finalised months in range.
              </p>
            ) : (
              <>
                <table className="w-full text-[12px] bg-white border border-[var(--color-border)] rounded-lg overflow-hidden">
                  <tbody>
                    {preview.lines.map(l => (
                      <tr key={l.code} className="border-b border-[var(--color-border)] last:border-0">
                        <td className="px-4 py-2 text-[var(--color-text-primary)]">Arrears — {l.name}</td>
                        <td className="px-4 py-2 text-right text-[var(--color-text-primary)]">{formatINR(l.amount)}</td>
                      </tr>
                    ))}
                    <tr className="font-semibold border-t border-[var(--color-border)]">
                      <td className="px-4 py-2 text-[var(--color-text-primary)]">Total arrears</td>
                      <td className="px-4 py-2 text-right text-[var(--color-text-primary)]">{formatINR(preview.totalArrear)}</td>
                    </tr>
                  </tbody>
                </table>
                <p className="text-[11px] text-[var(--color-text-muted)]">
                  Taxable portion {formatINR(preview.totalTaxableArrear)} — taxed in the payout month (PF/ESI unaffected).
                </p>
              </>
            )
          )}

          {!previewLoading && preview && preview.excludedMonths.length > 0 && (
            <div className="flex items-start gap-2 text-[11px] text-[var(--color-warning)]">
              <AlertTriangle size={14} className="mt-0.5 shrink-0" />
              <span>
                {preview.excludedMonths.length} month(s) excluded:{' '}
                {preview.excludedMonths.map(m => monthLabel(m.month, m.year)).join(', ')} (no finalised run).
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
