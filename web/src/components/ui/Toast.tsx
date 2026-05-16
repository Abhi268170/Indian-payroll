import {
  useCallback,
  useState,
  type ReactElement,
  type ReactNode,
} from 'react'
import { X, CheckCircle, AlertCircle, AlertTriangle, Info } from 'lucide-react'
import { ToastContext, type ToastContextValue } from './toastContext'

type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface Toast {
  id: string
  variant: ToastVariant
  title: string
  description?: string
}

const DISMISS_DELAY: Record<ToastVariant, number | null> = {
  success: 3000,
  info: 3000,
  warning: 5000,
  error: null,
}

function variantIcon(variant: ToastVariant): ReactElement {
  switch (variant) {
    case 'success': return <CheckCircle className="w-4 h-4 text-[var(--color-success)]" />
    case 'error': return <AlertCircle className="w-4 h-4 text-[var(--color-error)]" />
    case 'warning': return <AlertTriangle className="w-4 h-4 text-[var(--color-warning)]" />
    case 'info': return <Info className="w-4 h-4 text-[var(--color-info)]" />
  }
}

export function ToastProvider({ children }: { children: ReactNode }): ReactElement {
  const [toasts, setToasts] = useState<Toast[]>([])

  const dismiss = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id))
  }, [])

  const add = useCallback((variant: ToastVariant, title: string, description?: string) => {
    const id = String(Date.now()) + String(Math.random())
    setToasts(prev => [...prev, { id, variant, title, description }])
    const delay = DISMISS_DELAY[variant]
    if (delay !== null) {
      setTimeout(() => { dismiss(id) }, delay)
    }
  }, [dismiss])

  const ctx: ToastContextValue = {
    success: (title, description) => { add('success', title, description) },
    error: (title, description) => { add('error', title, description) },
    warning: (title, description) => { add('warning', title, description) },
    info: (title, description) => { add('info', title, description) },
  }

  return (
    <ToastContext.Provider value={ctx}>
      {children}
      <div className="fixed top-4 right-4 z-[60] flex flex-col gap-2 pointer-events-none">
        {toasts.map(toast => (
          <div
            key={toast.id}
            className="flex items-start gap-3 w-80 bg-white rounded-xl shadow-lg border border-[var(--color-border)] px-4 py-3 pointer-events-auto"
          >
            <div className="mt-0.5 flex-shrink-0">{variantIcon(toast.variant)}</div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-[var(--color-text-primary)]">{toast.title}</p>
              {toast.description && (
                <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{toast.description}</p>
              )}
            </div>
            <button
              onClick={() => { dismiss(toast.id) }}
              className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] flex-shrink-0"
              aria-label="Dismiss"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
