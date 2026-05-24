import { type ReactElement } from 'react'
import { clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { Loader2 } from 'lucide-react'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'
type ButtonSize = 'sm' | 'md' | 'lg'

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  loading?: boolean
  iconOnly?: boolean
}

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    'bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white',
  secondary:
    'border border-[var(--color-border-strong)] bg-white hover:bg-gray-50 text-[var(--color-text-primary)]',
  ghost:
    'text-[var(--color-primary)] hover:bg-[var(--color-primary-light)]',
  danger:
    'bg-[var(--color-error)] hover:bg-red-700 text-white',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-xs',
  md: 'h-9 px-4 text-sm',
  lg: 'h-10 px-5 text-sm',
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  iconOnly = false,
  disabled,
  children,
  className,
  ...props
}: ButtonProps): ReactElement {
  return (
    <button
      disabled={disabled ?? loading}
      className={twMerge(
        clsx(
          'inline-flex items-center justify-center gap-1.5 font-medium rounded-lg transition-colors',
          'disabled:opacity-50 disabled:cursor-not-allowed',
          variantClasses[variant],
          iconOnly ? 'w-9 h-9 p-0' : sizeClasses[size],
        ),
        className,
      )}
      {...props}
    >
      {loading ? (
        <>
          <Loader2 className="w-4 h-4 animate-spin" />
          <span>Saving…</span>
        </>
      ) : (
        children
      )}
    </button>
  )
}
