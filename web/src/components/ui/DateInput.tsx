import { useEffect, useId, useState, type ReactElement } from 'react'
import { Calendar } from 'lucide-react'

interface DateInputProps {
  // ISO 8601 (yyyy-MM-dd) — same shape the rest of the app uses
  value: string
  onChange: (iso: string) => void
  required?: boolean
  disabled?: boolean
  min?: string
  max?: string
  className?: string
  ariaLabel?: string
}

const DD_MM_YYYY_RE = /^(\d{2})\/(\d{2})\/(\d{4})$/

function isoToDisplay(iso: string): string {
  if (!iso) return ''
  const [y, m, d] = iso.split('-')
  if (!y || !m || !d) return ''
  return `${d}/${m}/${y}`
}

function displayToIso(display: string): string | null {
  const match = DD_MM_YYYY_RE.exec(display.trim())
  if (!match) return null
  const [, dd, mm, yyyy] = match
  const day = Number(dd)
  const month = Number(mm)
  const year = Number(yyyy)
  if (month < 1 || month > 12) return null
  if (day < 1 || day > 31) return null
  if (year < 1900 || year > 2100) return null
  // Confirm the resulting date is real (e.g. reject 31/02).
  const d = new Date(Date.UTC(year, month - 1, day))
  if (d.getUTCFullYear() !== year || d.getUTCMonth() !== month - 1 || d.getUTCDate() !== day) return null
  return `${yyyy}-${mm}-${dd}`
}

// Always-dd/MM/yyyy date input. Renders a text input that mirrors a hidden
// native date picker so users can either type the Indian-format string or use
// the system picker without leaking en-US mm/dd/yyyy to the surface.
export function DateInput({
  value,
  onChange,
  required,
  disabled,
  min,
  max,
  className,
  ariaLabel,
}: DateInputProps): ReactElement {
  const id = useId()
  const [display, setDisplay] = useState(() => isoToDisplay(value))

  useEffect(() => {
    // Keep display in sync when parent updates ISO value externally.
    setDisplay(isoToDisplay(value))
  }, [value])

  function commit(next: string): void {
    if (next === '') {
      onChange('')
      return
    }
    const iso = displayToIso(next)
    if (iso !== null) onChange(iso)
  }

  const baseCls = className ?? 'w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-[13px] focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'

  return (
    <div className="relative">
      <input
        id={id}
        type="text"
        inputMode="numeric"
        placeholder="dd/mm/yyyy"
        value={display}
        required={required}
        disabled={disabled}
        aria-label={ariaLabel}
        className={`${baseCls} pr-9`}
        onChange={e => {
          setDisplay(e.target.value)
          commit(e.target.value)
        }}
        onBlur={e => commit(e.target.value)}
      />
      <input
        type="date"
        value={value || ''}
        min={min}
        max={max}
        disabled={disabled}
        tabIndex={-1}
        aria-hidden="true"
        className="absolute right-2 top-1/2 -translate-y-1/2 w-5 h-5 opacity-0 cursor-pointer"
        onChange={e => onChange(e.target.value)}
      />
      <Calendar className="absolute right-2 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-secondary)] pointer-events-none" />
    </div>
  )
}
