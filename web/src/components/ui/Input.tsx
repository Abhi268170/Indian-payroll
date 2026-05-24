import { forwardRef, type ReactElement } from 'react'
import { clsx } from 'clsx'

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  helpText?: string
  required?: boolean
  currencyPrefix?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  function Input(
    { label, error, helpText, required, currencyPrefix, className, id, ...props },
    ref,
  ): ReactElement {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <div className="space-y-1">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-sm font-medium text-[var(--color-text-primary)]"
          >
            {label}
            {required && (
              <span className="text-[var(--color-error)] ml-0.5">*</span>
            )}
          </label>
        )}
        <div className={clsx(currencyPrefix && 'relative')}>
          {currencyPrefix && (
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-[var(--color-text-muted)]">
              ₹
            </span>
          )}
          <input
            ref={ref}
            id={inputId}
            aria-invalid={error ? 'true' : undefined}
            className={clsx(
              'w-full h-9 border border-[var(--color-border)] rounded-lg text-sm',
              'bg-white text-[var(--color-text-primary)]',
              'placeholder:text-[var(--color-text-muted)]',
              'focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)]',
              'disabled:bg-gray-50 disabled:text-[var(--color-text-disabled)]',
              'aria-[invalid=true]:border-[var(--color-error)] aria-[invalid=true]:ring-1 aria-[invalid=true]:ring-[var(--color-error)]',
              currencyPrefix ? 'pl-7 pr-3' : 'px-3',
              className,
            )}
            {...props}
          />
        </div>
        {error && (
          <p className="text-xs text-[var(--color-error)]">{error}</p>
        )}
        {helpText && !error && (
          <p className="text-xs text-[var(--color-text-muted)]">{helpText}</p>
        )}
      </div>
    )
  },
)
