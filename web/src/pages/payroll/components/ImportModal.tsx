import { useEffect, useRef, useState } from 'react'
import { Upload, Download, X, AlertCircle, CheckCircle, Loader2 } from 'lucide-react'
import { api } from '@/lib/api'

export type ImportType = 'lop' | 'earnings' | 'reimbursements'

interface ImportModalProps {
  runId: string
  importType: ImportType
  onClose: () => void
  onSuccess: () => void
}

const TYPE_CONFIG: Record<ImportType, {
  title: string
  description: string
  templateHeaders: string
  endpoint: string
}> = {
  lop: {
    title: 'Import LOP Details',
    description: 'Upload a CSV with Loss of Pay days per employee.',
    templateHeaders: 'Employee Code,LOP Days',
    endpoint: 'lop',
  },
  earnings: {
    title: 'Import One-Time Earnings / Deductions',
    description: 'Upload a CSV with one-time earnings or deductions. Direction is determined by the component type.',
    templateHeaders: 'Employee Code,Component Code,Amount',
    endpoint: 'earnings',
  },
  reimbursements: {
    title: 'Import Expense Reimbursements',
    description: 'Upload a CSV with expense reimbursement entries.',
    templateHeaders: 'Employee Code,Report Number,Amount',
    endpoint: 'reimbursements',
  },
}

interface ImportError {
  row: number
  employeeCode: string
  reason: string
}

interface ImportResult {
  applied: number
  errors: ImportError[]
}

interface JobStatus {
  jobId: string
  status: 'queued' | 'running' | 'completed' | 'failed'
  processed: number
  total: number
  resultJson: string | null
  error: string | null
}

function uploadErrorMessage(err: unknown): string {
  const axiosErr = err as { response?: { data?: { error?: string; errors?: string[] }; status?: number } }
  const data = axiosErr.response?.data
  if (data?.error) return data.error
  if (data?.errors && data.errors.length > 0) return data.errors[0] ?? 'Please check the file and try again.'
  if (axiosErr.response?.status === 404) return 'This payroll run could not be found. Refresh the page and try again.'
  if (axiosErr.response?.status === 413) return 'The file is too large. Please split it into smaller files and try again.'
  if (axiosErr.response?.status === 422) return 'The file could not be imported. Please check the skipped rows and try again.'
  return 'Upload failed. Please check your connection and try again.'
}

export default function ImportModal({ runId, importType, onClose, onSuccess }: ImportModalProps): React.ReactElement {
  const config = TYPE_CONFIG[importType]
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [polling, setPolling] = useState(false)
  const [jobStatus, setJobStatus] = useState<JobStatus | null>(null)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [dragging, setDragging] = useState(false)
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    return () => {
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [])

  function downloadTemplate(): void {
    const content = `${config.templateHeaders}\n`
    const blob = new Blob([content], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${importType}-template.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  function handleFileChange(f: File | null): void {
    if (!f) return
    if (!f.name.endsWith('.csv')) {
      setError('Only .csv files are accepted.')
      return
    }
    setFile(f)
    setError(null)
    setResult(null)
  }

  function startPolling(jobId: string): void {
    setPolling(true)
    pollRef.current = setInterval(() => {
      void (async () => {
        try {
          const { data } = await api.get<JobStatus>(`/api/v1/jobs/${jobId}/status`)
          setJobStatus(data)

          if (data.status === 'completed') {
            stopPolling()
            const parsed: ImportResult = data.resultJson
              ? (JSON.parse(data.resultJson) as ImportResult)
              : { applied: 0, errors: [] }
            setResult(parsed)
            if (parsed.applied > 0) onSuccess()
          } else if (data.status === 'failed') {
            stopPolling()
            setError(data.error ?? 'Import failed. Please try again.')
          }
        } catch {
          stopPolling()
          setError('Lost connection while tracking import progress.')
        }
      })()
    }, 1500)
  }

  function stopPolling(): void {
    if (pollRef.current) {
      clearInterval(pollRef.current)
      pollRef.current = null
    }
    setPolling(false)
  }

  async function handleUpload(): Promise<void> {
    if (!file) return
    setUploading(true)
    setError(null)

    const formData = new FormData()
    formData.append('file', file)

    try {
      const { data } = await api.post<{ jobId: string }>(
        `/api/v1/payroll-runs/${runId}/import/${config.endpoint}`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      )
      setUploading(false)
      startPolling(data.jobId)
    } catch (err: unknown) {
      setUploading(false)
      setError(uploadErrorMessage(err))
    }
  }

  function handleDrop(e: React.DragEvent<HTMLDivElement>): void {
    e.preventDefault()
    setDragging(false)
    const f = e.dataTransfer.files[0] ?? null
    handleFileChange(f)
  }

  const progressPct = jobStatus && jobStatus.total > 0
    ? Math.round((jobStatus.processed / jobStatus.total) * 100)
    : null

  const isInFlight = uploading || polling

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={isInFlight ? undefined : onClose} />
      <div className="relative w-[480px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden max-h-[90vh]">

        {/* Header */}
        <div className="px-6 py-5 border-b border-[var(--color-border)] flex items-start justify-between">
          <div>
            <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">{config.title}</h2>
            <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">{config.description}</p>
          </div>
          <button
            onClick={isInFlight ? undefined : onClose}
            disabled={isInFlight}
            className="ml-4 mt-0.5 inline-flex items-center justify-center w-7 h-7 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors disabled:opacity-40"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4 overflow-y-auto">

          {/* Template download */}
          <div className="flex items-center justify-between p-3 rounded-lg bg-[var(--color-page-bg)] border border-[var(--color-border)]">
            <div>
              <p className="text-[13px] font-medium text-[var(--color-text-primary)]">Download template</p>
              <p className="text-[12px] text-[var(--color-text-secondary)]">{config.templateHeaders}</p>
            </div>
            <button
              onClick={downloadTemplate}
              className="inline-flex items-center gap-1.5 h-7 px-3 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] hover:bg-white transition-colors"
            >
              <Download className="w-3.5 h-3.5" />
              CSV
            </button>
          </div>

          {/* Drop zone */}
          {!result && !polling && (
            <div
              onDragOver={e => { e.preventDefault(); setDragging(true) }}
              onDragLeave={() => { setDragging(false) }}
              onDrop={handleDrop}
              onClick={() => inputRef.current?.click()}
              className={`
                flex flex-col items-center justify-center gap-2 p-8 rounded-lg border-2 border-dashed cursor-pointer transition-colors
                ${dragging ? 'border-[var(--color-primary)] bg-blue-50' : 'border-[var(--color-border)] hover:border-[var(--color-primary)] hover:bg-[var(--color-page-bg)]'}
              `}
            >
              <Upload className="w-8 h-8 text-[var(--color-text-muted)]" />
              {file ? (
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{file.name}</p>
              ) : (
                <>
                  <p className="text-[13px] text-[var(--color-text-secondary)]">Drop CSV here or <span className="text-[var(--color-primary)] font-medium">browse</span></p>
                  <p className="text-[12px] text-[var(--color-text-muted)]">Only .csv files accepted</p>
                </>
              )}
              <input
                ref={inputRef}
                type="file"
                accept=".csv"
                className="hidden"
                onChange={e => { handleFileChange(e.target.files?.[0] ?? null) }}
              />
            </div>
          )}

          {/* Progress */}
          {polling && (
            <div className="space-y-3">
              <div className="flex items-center gap-2 p-3 rounded-lg bg-blue-50 border border-blue-200">
                <Loader2 className="w-4 h-4 text-blue-600 shrink-0 animate-spin" />
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">
                  {jobStatus?.status === 'queued' ? 'Queued…' : `Processing… ${progressPct !== null ? `${progressPct}%` : ''}`}
                </p>
              </div>
              {progressPct !== null && (
                <div className="w-full bg-gray-100 rounded-full h-1.5">
                  <div
                    className="bg-[var(--color-primary)] h-1.5 rounded-full transition-all duration-300"
                    style={{ width: `${progressPct}%` }}
                  />
                </div>
              )}
            </div>
          )}

          {/* Error banner */}
          {error && (
            <div className="flex items-start gap-2 p-3 rounded-lg bg-red-50 border border-red-200">
              <AlertCircle className="w-4 h-4 text-red-500 mt-0.5 shrink-0" />
              <p className="text-[13px] text-red-700">{error}</p>
            </div>
          )}

          {/* Results */}
          {result && (
            <div className="space-y-3">
              <div className={`flex items-center gap-2 p-3 rounded-lg border ${result.applied > 0 ? 'bg-green-50 border-green-200' : 'bg-yellow-50 border-yellow-200'}`}>
                <CheckCircle className={`w-4 h-4 shrink-0 ${result.applied > 0 ? 'text-green-600' : 'text-yellow-600'}`} />
                <p className="text-[13px] font-medium text-[var(--color-text-primary)]">
                  {result.applied} row{result.applied !== 1 ? 's' : ''} applied
                  {result.errors.length > 0 ? `, ${String(result.errors.length)} skipped` : ''}
                </p>
              </div>

              {result.errors.length > 0 && (
                <div className="border border-[var(--color-border)] rounded-lg overflow-hidden">
                  <div className="px-3 py-2 bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                    <p className="text-[12px] font-medium text-[var(--color-text-secondary)]">Skipped rows</p>
                  </div>
                  <div className="divide-y divide-[var(--color-border)] max-h-48 overflow-y-auto">
                    {result.errors.map((err, i) => (
                      <div key={i} className="px-3 py-2 flex items-start gap-2">
                        <span className="text-[11px] text-[var(--color-text-muted)] w-12 shrink-0">Row {err.row}</span>
                        <span className="text-[12px] font-medium text-[var(--color-text-primary)] w-24 shrink-0">{err.employeeCode || '—'}</span>
                        <span className="text-[12px] text-[var(--color-text-secondary)]">{err.reason}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            onClick={onClose}
            disabled={isInFlight}
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] disabled:opacity-40"
          >
            {result ? 'Close' : 'Cancel'}
          </button>
          {!result && !polling && (
            <button
              onClick={() => { void handleUpload() }}
              disabled={!file || uploading}
              className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-60 flex items-center gap-1.5"
            >
              <Upload className="w-3.5 h-3.5" />
              {uploading ? 'Uploading…' : 'Upload'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
