import { useState } from 'react'
import { Download, X } from 'lucide-react'

interface ExportModalProps {
  runId: string
  periodLabel: string
  onClose: () => void
}

type Format = 'csv' | 'xls'

const FORMATS: { value: Format; label: string }[] = [
  { value: 'csv', label: 'CSV (Comma Separated Values)' },
  { value: 'xls', label: 'XLS (Microsoft Excel)' },
]

export default function ExportModal({ runId, periodLabel, onClose }: ExportModalProps): React.ReactElement {
  const [format, setFormat] = useState<Format>('csv')
  const [downloading, setDownloading] = useState(false)

  function handleDownload(): void {
    setDownloading(true)
    const a = document.createElement('a')
    a.href = `/api/v1/payroll-runs/${runId}/export?format=${format}`
    a.download = `Payroll_${periodLabel.replace(/\s/g, '-')}.${format}`
    a.click()
    setTimeout(() => { setDownloading(false); onClose() }, 500)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[420px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">

        {/* Header */}
        <div className="px-6 py-5 border-b border-[var(--color-border)] flex items-start justify-between">
          <div>
            <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Export Payroll Data</h2>
            <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">{periodLabel} — Employee Pay Run Details</p>
          </div>
          <button
            onClick={onClose}
            className="ml-4 mt-0.5 inline-flex items-center justify-center w-7 h-7 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-3">
          <p className="text-[12px] font-medium text-[var(--color-text-secondary)]">Export format</p>
          {FORMATS.map(f => (
            <label key={f.value} className="flex items-center gap-2.5 cursor-pointer">
              <input
                type="radio"
                name="format"
                value={f.value}
                checked={format === f.value}
                onChange={() => { setFormat(f.value) }}
                className="accent-[var(--color-primary)]"
              />
              <span className="text-[13px] text-[var(--color-text-primary)]">{f.label}</span>
            </label>
          ))}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            onClick={onClose}
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
          >
            Cancel
          </button>
          <button
            onClick={handleDownload}
            disabled={downloading}
            className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-60 flex items-center gap-1.5"
          >
            <Download className="w-3.5 h-3.5" />
            {downloading ? 'Downloading…' : 'Download'}
          </button>
        </div>
      </div>
    </div>
  )
}
