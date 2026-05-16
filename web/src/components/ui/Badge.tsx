import { type ReactElement } from 'react'
import { clsx } from 'clsx'

type BadgeVariant = 'success' | 'info' | 'warning' | 'danger' | 'neutral'

interface BadgeProps {
  variant?: BadgeVariant
  children: React.ReactNode
}

const variantClasses: Record<BadgeVariant, string> = {
  success: 'bg-[var(--color-badge-green-bg)] text-[var(--color-badge-green-text)]',
  info: 'bg-[var(--color-badge-blue-bg)] text-[var(--color-badge-blue-text)]',
  warning: 'bg-[var(--color-badge-orange-bg)] text-[var(--color-badge-orange-text)]',
  danger: 'bg-[var(--color-badge-red-bg)] text-[var(--color-badge-red-text)]',
  neutral: 'bg-[var(--color-badge-grey-bg)] text-[var(--color-badge-grey-text)]',
}

export function Badge({ variant = 'neutral', children }: BadgeProps): ReactElement {
  return (
    <span
      className={clsx(
        'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium',
        variantClasses[variant],
      )}
    >
      {children}
    </span>
  )
}
