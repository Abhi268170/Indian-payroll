import { type ReactElement, type ReactNode } from 'react'

interface EmptyStateProps {
  icon?: ReactNode
  heading: string
  subtext?: string
  action?: ReactNode
}

export function EmptyState({ icon, heading, subtext, action }: EmptyStateProps): ReactElement {
  return (
    <div className="flex flex-col items-center gap-2 py-16">
      {icon && (
        <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center text-[var(--color-text-muted)]">
          {icon}
        </div>
      )}
      <p className="text-sm font-medium text-[var(--color-text-primary)]">{heading}</p>
      {subtext && (
        <p className="text-xs text-[var(--color-text-muted)]">{subtext}</p>
      )}
      {action && <div className="mt-1">{action}</div>}
    </div>
  )
}
