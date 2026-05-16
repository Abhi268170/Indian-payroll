import { type ReactElement, type ReactNode } from 'react'

export interface TableColumn<T> {
  key: string
  header: string
  render: (row: T) => ReactNode
  className?: string
}

interface TableProps<T> {
  columns: TableColumn<T>[]
  data: T[] | undefined
  isLoading?: boolean
  skeletonRows?: number
  getRowKey: (row: T) => string
  onRowClick?: (row: T) => void
  emptyState?: ReactNode
}

function SkeletonCell(): ReactElement {
  return (
    <td className="px-4 py-3">
      <div className="h-4 bg-gray-100 rounded w-3/4" />
    </td>
  )
}

export function Table<T>({
  columns,
  data,
  isLoading = false,
  skeletonRows = 5,
  getRowKey,
  onRowClick,
  emptyState,
}: TableProps<T>): ReactElement {
  return (
    <div className="border border-[var(--color-border)] rounded-xl overflow-hidden">
      <table className="w-full text-[13px]">
        <thead>
          <tr className="bg-gray-50 border-b border-[var(--color-border)]">
            {columns.map(col => (
              <th
                key={col.key}
                className={`text-left px-4 py-3 font-medium text-[var(--color-text-secondary)] whitespace-nowrap ${col.className ?? ''}`}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-[var(--color-border)]">
          {isLoading
            ? Array.from({ length: skeletonRows }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  {columns.map(col => <SkeletonCell key={col.key} />)}
                </tr>
              ))
            : data && data.length > 0
              ? data.map(row => (
                  <tr
                    key={getRowKey(row)}
                    onClick={onRowClick ? () => { onRowClick(row) } : undefined}
                    className={`hover:bg-[var(--color-primary-light)] transition-colors ${onRowClick ? 'cursor-pointer' : ''}`}
                  >
                    {columns.map(col => (
                      <td
                        key={col.key}
                        className={`px-4 py-3 text-[var(--color-text-primary)] ${col.className ?? ''}`}
                      >
                        {col.render(row)}
                      </td>
                    ))}
                  </tr>
                ))
              : (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-0">
                    {emptyState ?? (
                      <div className="flex flex-col items-center gap-2 py-16">
                        <p className="text-sm font-medium text-[var(--color-text-primary)]">No items yet</p>
                        <p className="text-xs text-[var(--color-text-muted)]">Create your first item to get started.</p>
                      </div>
                    )}
                  </td>
                </tr>
              )}
        </tbody>
      </table>
    </div>
  )
}
