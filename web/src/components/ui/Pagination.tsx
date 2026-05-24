import { useState, useEffect, type ReactElement } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'

interface Props {
  page: number
  pageSize: number
  total: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
  pageSizeOptions?: number[]
}

const DEFAULT_OPTIONS = [25, 50, 100]

export function Pagination({
  page,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_OPTIONS,
}: Props): ReactElement | null {
  if (total === 0) return null
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, total)

  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] bg-white">
      <div>
        Showing <span className="text-[var(--color-text-primary)] font-medium">{start}</span>–
        <span className="text-[var(--color-text-primary)] font-medium">{end}</span> of{' '}
        <span className="text-[var(--color-text-primary)] font-medium">{total}</span>
      </div>
      <div className="flex items-center gap-3">
        <label className="flex items-center gap-1.5">
          <span>Rows per page:</span>
          <select
            value={pageSize}
            onChange={e => { onPageSizeChange(parseInt(e.target.value, 10)) }}
            className="h-7 px-2 border border-[var(--color-border)] rounded text-[12px] bg-white"
          >
            {pageSizeOptions.map(opt => <option key={opt} value={opt}>{opt}</option>)}
          </select>
        </label>
        <div className="flex items-center gap-1">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => { onPageChange(page - 1) }}
            className="w-7 h-7 flex items-center justify-center rounded border border-[var(--color-border)] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50"
            aria-label="Previous page"
          >
            <ChevronLeft className="w-3.5 h-3.5" />
          </button>
          <span className="px-2 min-w-[60px] text-center text-[var(--color-text-primary)]">
            {page} / {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => { onPageChange(page + 1) }}
            className="w-7 h-7 flex items-center justify-center rounded border border-[var(--color-border)] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50"
            aria-label="Next page"
          >
            <ChevronRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  )
}

// Hook for persisted pageSize in localStorage per listing key.
export function usePersistedPageSize(key: string, defaultSize = 25): [number, (n: number) => void] {
  const storageKey = `pagination-size:${key}`
  const [size, setSize] = useState<number>(() => {
    if (typeof window === 'undefined') return defaultSize
    const stored = window.localStorage.getItem(storageKey)
    return stored != null ? parseInt(stored, 10) || defaultSize : defaultSize
  })
  useEffect(() => {
    window.localStorage.setItem(storageKey, String(size))
  }, [size, storageKey])
  return [size, setSize]
}
