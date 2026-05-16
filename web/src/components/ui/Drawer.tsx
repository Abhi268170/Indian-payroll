import { useEffect, type ReactElement, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { clsx } from 'clsx'

interface DrawerTab {
  label: string
  key: string
}

interface DrawerProps {
  title: string
  subtitle?: string
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  tabs?: DrawerTab[]
  activeTab?: string
  onTabChange?: (key: string) => void
}

export function Drawer({
  title,
  subtitle,
  onClose,
  children,
  footer,
  tabs,
  activeTab,
  onTabChange,
}: DrawerProps): ReactElement {
  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent): void {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => { document.removeEventListener('keydown', handleKeyDown) }
  }, [onClose])

  return (
    <>
      <div className="fixed inset-0 z-40" onClick={onClose} />
      <aside className="fixed right-0 top-0 h-full w-[480px] bg-white border-l border-[var(--color-border)] shadow-xl z-50 flex flex-col overflow-hidden">
        <div className="flex items-start justify-between px-5 py-4 border-b border-[var(--color-border)]">
          <div>
            <h2 className="text-[15px] font-semibold text-[var(--color-text-primary)]">{title}</h2>
            {subtitle && (
              <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{subtitle}</p>
            )}
          </div>
          <button
            onClick={onClose}
            className="inline-flex items-center justify-center w-7 h-7 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-gray-100 transition-colors"
            aria-label="Close"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {tabs && tabs.length > 0 && (
          <div className="flex border-b border-[var(--color-border)] px-5">
            {tabs.map(tab => (
              <button
                key={tab.key}
                onClick={() => onTabChange?.(tab.key)}
                className={clsx(
                  'py-3 px-1 mr-5 text-sm font-medium border-b-2 -mb-px transition-colors',
                  activeTab === tab.key
                    ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
                    : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]',
                )}
              >
                {tab.label}
              </button>
            ))}
          </div>
        )}

        <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>

        {footer && (
          <div className="px-5 py-4 border-t border-[var(--color-border)] flex gap-2">
            {footer}
          </div>
        )}
      </aside>
    </>
  )
}
