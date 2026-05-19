import { useState } from 'react'
import { Download } from 'lucide-react'

interface BankAdviceModalProps {
  runId: string
  periodLabel: string
  onClose: () => void
}

type Format = 'Standard'

const FORMATS: { value: Format; label: string }[] = [
  { value: 'Standard', label: 'Standard Format' },
]

export default function BankAdviceModal({ runId, periodLabel, onClose }: BankAdviceModalProps): React.ReactElement {
  const [downloading, setDownloading] = useState(false)

  function handleDownload(): void {
    setDownloading(true)
    const a = document.createElement('a')
    a.href = `/api/v1/payroll-runs/${runId}/bank-advice/download`
    a.download = `bank-advice-${periodLabel.replace(/\s/g, '-')}.xlsx`
    a.click()
    setTimeout(() => { setDownloading(false); onClose() }, 500)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[400px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Download Bank Advice</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{periodLabel}</p>
        </div>

        <div className="px-6 py-5 space-y-3">
          <p className="text-[12px] font-medium text-[var(--color-text-secondary)]">Format</p>
          {FORMATS.map(f => (
            <label key={f.value} className="flex items-center gap-2.5 cursor-pointer">
              <input
                type="radio"
                name="format"
                value={f.value}
                checked
                onChange={() => { /* single format — no-op */ }}
                className="accent-[var(--color-primary)]"
              />
              <span className="text-[13px] text-[var(--color-text-primary)]">{f.label}</span>
            </label>
          ))}
          <p className="text-[12px] text-[var(--color-text-secondary)] pt-1">
            Additional bank formats (HDFC, ICICI, Kotak) — coming soon.
          </p>
        </div>

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
