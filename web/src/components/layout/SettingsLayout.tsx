import { type ReactElement } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { X } from 'lucide-react'

export default function SettingsLayout(): ReactElement {
  const navigate = useNavigate()

  function handleClose(): void {
    void navigate(-1)
  }

  return (
    <div className="fixed inset-0 z-50 bg-[var(--color-page-bg)] flex flex-col">
      <header className="h-14 flex-shrink-0 bg-white border-b border-[var(--color-border)] flex items-center justify-between px-6">
        <div className="flex items-center gap-2.5">
          <div className="w-7 h-7 rounded-lg bg-[var(--color-primary)] flex items-center justify-center flex-shrink-0">
            <span className="text-[11px] font-bold text-white">IP</span>
          </div>
          <span className="text-[15px] font-semibold text-[var(--color-text-primary)]">Settings</span>
        </div>
        <button
          onClick={handleClose}
          className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg text-[13px] text-[var(--color-text-secondary)] hover:bg-gray-100 transition-colors"
        >
          <X className="w-4 h-4" />
          Close Settings
        </button>
      </header>

      <div className="flex-1 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  )
}
