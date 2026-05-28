import { useState } from 'react'
import { Download, X } from 'lucide-react'
import { api } from '@/lib/api'

interface ExportModalProps {
  runId: string
  periodLabel: string
  onClose: () => void
}

type ExportType = 'payroll-details' | 'tds-breakup'
type Format = 'csv' | 'xlsx'

const EXPORT_TYPES: { value: ExportType; label: string; description: string }[] = [
  {
    value: 'payroll-details',
    label: 'Payroll Details',
    description: 'Salary breakup per employee — earnings, benefits, deductions, gross, net, CTC.',
  },
  {
    value: 'tds-breakup',
    label: 'TDS Breakup',
    description: 'Per-employee TDS working — slab-wise tax, 87A rebate, surcharge, cess, monthly TDS.',
  },
]

const FORMATS: { value: Format; label: string }[] = [
  { value: 'xlsx', label: 'XLSX (Microsoft Excel)' },
  { value: 'csv', label: 'CSV (Comma Separated Values)' },
]

export default function ExportModal({ runId, periodLabel, onClose }: ExportModalProps): React.ReactElement {
  const [exportType, setExportType] = useState<ExportType>('payroll-details')
  const [format, setFormat] = useState<Format>('xlsx')
  const [downloading, setDownloading] = useState(false)

  async function handleDownload(): Promise<void> {
    setDownloading(true)
    try {
      const res = await api.get<Blob>(
        `/api/v1/payroll-runs/${runId}/export/${exportType}?format=${format}`,
        { responseType: 'blob' },
      )
      const url = URL.createObjectURL(res.data)
      const a = document.createElement('a')
      a.href = url
      const prefix = exportType === 'payroll-details' ? 'PayrollDetails' : 'TDSBreakup'
      a.download = `${prefix}_${periodLabel.replace(/\s/g, '-')}.${format}`
      a.click()
      URL.revokeObjectURL(url)
      onClose()
    } finally {
      setDownloading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[460px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">

        <div className="px-6 py-5 border-b border-[var(--color-border)] flex items-start justify-between">
          <div>
            <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Export Payroll Data</h2>
            <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">{periodLabel}</p>
          </div>
          <button
            onClick={onClose}
            className="ml-4 mt-0.5 inline-flex items-center justify-center w-7 h-7 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-6 py-5 space-y-5">
          <div className="space-y-2.5">
            <p className="text-[12px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">Export type</p>
            {EXPORT_TYPES.map(t => (
              <label
                key={t.value}
                className="flex items-start gap-2.5 cursor-pointer p-2.5 rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-page-bg)] transition-colors"
              >
                <input
                  type="radio"
                  name="export-type"
                  value={t.value}
                  checked={exportType === t.value}
                  onChange={() => { setExportType(t.value) }}
                  className="mt-0.5 accent-[var(--color-primary)]"
                />
                <span>
                  <span className="block text-[13px] font-medium text-[var(--color-text-primary)]">{t.label}</span>
                  <span className="block text-[11px] text-[var(--color-text-secondary)] mt-0.5">{t.description}</span>
                </span>
              </label>
            ))}
          </div>

          <div className="space-y-2">
            <p className="text-[12px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">Format</p>
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
        </div>

        <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            onClick={onClose}
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
          >
            Cancel
          </button>
          <button
            onClick={() => { void handleDownload() }}
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
