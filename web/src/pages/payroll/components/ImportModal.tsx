import { useRef, useState } from 'react'
import { Upload, Download, X, AlertCircle, CheckCircle } from 'lucide-react'

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
    title: 'Import One-Time Earnings',
    description: 'Upload a CSV with one-time earnings (tall format: one row per earning).',
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

export default function ImportModal({ runId, importType, onClose, onSuccess }: ImportModalProps): React.ReactElement {
  const config = TYPE_CONFIG[importType]
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [dragging, setDragging] = useState(false)

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

  async function handleUpload(): Promise<void> {
    if (!file) return
    setUploading(true)
    setError(null)

    const formData = new FormData()
    formData.append('file', file)

    try {
      const res = await fetch(`/api/v1/payroll-runs/${runId}/import/${config.endpoint}`, {
        method: 'POST',
        body: formData,
      })

      if (!res.ok) {
        const body = await res.json().catch(() => ({})) as Record<string, string>
        setError(body.error ?? `Upload failed (${String(res.status)})`)
        return
      }

      const data = await res.json() as ImportResult
      setResult(data)
      if (data.applied > 0) onSuccess()
    } catch {
      setError('Network error — please try again.')
    } finally {
      setUploading(false)
    }
  }

  function handleDrop(e: React.DragEvent<HTMLDivElement>): void {
    e.preventDefault()
    setDragging(false)
    const f = e.dataTransfer.files[0] ?? null
    handleFileChange(f)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[480px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden max-h-[90vh]">

        {/* Header */}
        <div className="px-6 py-5 border-b border-[var(--color-border)] flex items-start justify-between">
          <div>
            <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">{config.title}</h2>
            <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">{config.description}</p>
          </div>
          <button
            onClick={onClose}
            className="ml-4 mt-0.5 inline-flex items-center justify-center w-7 h-7 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
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
          {!result && (
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
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
          >
            {result ? 'Close' : 'Cancel'}
          </button>
          {!result && (
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
