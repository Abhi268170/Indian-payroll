import { forwardRef, type ReactElement } from 'react'

interface ToggleProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string
}

export const Toggle = forwardRef<HTMLInputElement, ToggleProps>(
  function Toggle({ label, ...props }, ref): ReactElement {
    return (
      <label className="relative inline-flex items-center cursor-pointer gap-2">
        <input ref={ref} type="checkbox" className="sr-only peer" {...props} />
        <div
          className={[
            'w-9 h-5 bg-gray-200 rounded-full',
            'peer-checked:bg-[var(--color-primary)]',
            'relative',
            'after:content-[""] after:absolute after:top-0.5 after:left-0.5',
            'after:w-4 after:h-4 after:bg-white after:rounded-full after:transition-transform',
            'peer-checked:after:translate-x-4',
          ].join(' ')}
        />
        {label && (
          <span className="text-sm text-[var(--color-text-primary)]">{label}</span>
        )}
      </label>
    )
  },
)
