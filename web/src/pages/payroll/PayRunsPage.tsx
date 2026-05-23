import { useEffect, useRef, useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useNavigate, Link } from 'react-router-dom'
import { AlertCircle, ChevronRight, Loader2 } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { CurrentPayPeriodDto, PayrollHistoryItemDto } from '@/types/api'
import PeriodCard from './components/PeriodCard'

type Tab = 'run' | 'history'

interface JobStatus {
  jobId: string
  status: 'queued' | 'running' | 'completed' | 'failed'
  processed: number
  total: number
  resultJson: string | null
  error: string | null
}

function statusBadge(status: string): React.ReactElement {
  return (
    <span className="inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium bg-emerald-50 text-emerald-700">
      {status}
    </span>
  )
}

export default function PayRunsPage(): React.ReactElement {
  const navigate = useNavigate()
  const [tab, setTab] = useState<Tab>('run')
  const [initiating, setInitiating] = useState(false)
  const [initiateError, setInitiateError] = useState<string | null>(null)
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    return () => {
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [])

  const { data: period, isLoading, error } = useQuery<CurrentPayPeriodDto | null>({
    queryKey: ['current-period'],
    queryFn: async () => {
      const res = await api.get<CurrentPayPeriodDto>('/api/v1/payroll-runs/current-period')
      if (res.status === 204) return null
      return res.data
    },
  })

  const { data: history = [] } = useQuery<PayrollHistoryItemDto[]>({
    queryKey: ['payroll-history'],
    queryFn: () => api.get<PayrollHistoryItemDto[]>('/api/v1/payroll-runs/history').then(r => r.data),
    enabled: tab === 'history',
  })

  function startPolling(jobId: string): void {
    pollRef.current = setInterval(() => {
      void (async () => {
        try {
          const { data } = await api.get<JobStatus>(`/api/v1/jobs/${jobId}/status`)
          if (data.status === 'completed') {
            clearInterval(pollRef.current!)
            pollRef.current = null
            setInitiating(false)
            if (data.resultJson) {
              const dto = JSON.parse(data.resultJson) as { id: string }
              void navigate(`/pay-runs/${dto.id}`)
            }
          } else if (data.status === 'failed') {
            clearInterval(pollRef.current!)
            pollRef.current = null
            setInitiating(false)
            setInitiateError(data.error ?? 'Payroll initiation failed. Please try again.')
          }
        } catch {
          clearInterval(pollRef.current!)
          pollRef.current = null
          setInitiating(false)
          setInitiateError('Lost connection during payroll initiation. Please refresh and check status.')
        }
      })()
    }, 1500)
  }

  const initiateMutation = useMutation({
    mutationFn: () => api.post<{ jobId: string }>('/api/v1/payroll-runs/initiate', {}),
    onSuccess: (res) => {
      setInitiating(true)
      setInitiateError(null)
      startPolling(res.data.jobId)
    },
    onError: () => {
      setInitiateError('Failed to initiate payroll run. Please try again.')
    },
  })

  const tabCls = (t: Tab): string =>
    `h-9 px-4 text-[13px] font-medium border-b-2 transition-colors ${
      tab === t
        ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
        : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
    }`

  return (
    <div>
      <div className="mb-5">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Pay Runs</h1>
        <p className="mt-0.5 text-[13px] text-[var(--color-text-secondary)]">Manage payroll processing and view payment history.</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-[var(--color-border)] mb-5 flex gap-0">
        <button className={tabCls('run')} onClick={() => { setTab('run') }}>Run Payroll</button>
        <button className={tabCls('history')} onClick={() => { setTab('history') }}>Payroll History</button>
      </div>

      {tab === 'run' && (
        <>
          {isLoading && (
            <div className="flex items-center gap-2 text-[13px] text-[var(--color-text-secondary)]">
              <span className="inline-block w-4 h-4 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
              Loading…
            </div>
          )}

          {error && (
            <div className="flex items-center gap-2.5 rounded-lg bg-red-50 border border-red-200 px-4 py-3">
              <AlertCircle className="w-4 h-4 text-red-500 flex-shrink-0" />
              <p className="text-[13px] text-red-700">Failed to load pay period data.</p>
            </div>
          )}

          {!isLoading && !error && period === null && (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <p className="text-[18px] font-semibold text-[var(--color-text-primary)]">You deserve a break today!</p>
              <p className="mt-2 text-[13px] text-[var(--color-text-secondary)]">You have no outstanding pay runs.</p>
            </div>
          )}

          {!isLoading && period !== null && period?.hasOutstandingRun && period.outstandingRunId && (
            <div className="bg-white rounded-xl border border-[var(--color-border)] px-6 py-5 flex items-center justify-between">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">
                  {period.periodLabel} — {period.outstandingRunStatus}
                </p>
                <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
                  A pay run is in progress for this period.
                </p>
              </div>
              <Link
                to={`/pay-runs/${period.outstandingRunId}`}
                className="inline-flex items-center gap-1 h-8 px-3 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] transition-colors"
              >
                Continue <ChevronRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          )}

          {initiating && (
            <div className="flex items-center gap-3 rounded-lg bg-blue-50 border border-blue-200 px-4 py-4">
              <Loader2 className="w-5 h-5 text-blue-600 animate-spin shrink-0" />
              <div>
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">Calculating payroll…</p>
                <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">This may take a minute. Please wait.</p>
              </div>
            </div>
          )}

          {!isLoading && period != null && !period.hasOutstandingRun && !initiating && (
            <PeriodCard
              period={period}
              onProcess={() => { initiateMutation.mutate() }}
              processing={initiateMutation.isPending}
            />
          )}

          {initiateError && (
            <div className="mt-3 flex items-center gap-2.5 rounded-lg bg-red-50 border border-red-200 px-4 py-3">
              <AlertCircle className="w-4 h-4 text-red-500 flex-shrink-0" />
              <p className="text-[13px] text-red-700">{initiateError}</p>
            </div>
          )}
        </>
      )}

      {tab === 'history' && (
        <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
          {history.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16">
              <p className="text-[14px] text-[var(--color-text-secondary)]">No completed pay runs yet.</p>
            </div>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b border-[var(--color-border)] bg-[var(--color-page-bg)]">
                  <th className="px-4 py-3 text-left text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Period</th>
                  <th className="px-4 py-3 text-left text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Payment Date</th>
                  <th className="px-4 py-3 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Employees</th>
                  <th className="px-4 py-3 text-right text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Total Net Pay</th>
                  <th className="px-4 py-3 text-left text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide">Status</th>
                  <th className="w-10" />
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {history.map(run => (
                  <tr
                    key={run.id}
                    onClick={() => void navigate(`/pay-runs/${run.id}`)}
                    className="hover:bg-[var(--color-page-bg)] cursor-pointer transition-colors"
                  >
                    <td className="px-4 py-3 text-[13px] font-medium text-[var(--color-text-primary)]">{run.periodLabel}</td>
                    <td className="px-4 py-3 text-[13px] text-[var(--color-text-secondary)]">
                      {run.paidAt ? new Date(run.paidAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'}
                    </td>
                    <td className="px-4 py-3 text-[13px] text-[var(--color-text-secondary)] text-right">{run.employeeCount}</td>
                    <td className="px-4 py-3 text-[13px] font-medium text-[var(--color-text-primary)] text-right">{formatINR(run.totalNetPay)}</td>
                    <td className="px-4 py-3">{statusBadge('Paid')}</td>
                    <td className="px-4 py-3 text-right">
                      <ChevronRight className="w-4 h-4 text-[var(--color-text-secondary)]" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  )
}
