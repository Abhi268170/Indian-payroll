import { type ReactElement } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { LogOut, Settings } from 'lucide-react'
import { clsx } from 'clsx'

function navItemCls(isActive: boolean): string {
  return clsx(
    'flex items-center gap-2 h-9 px-3 rounded-lg text-[13px] transition-colors w-full',
    isActive
      ? 'bg-[var(--color-sidebar-active)] text-white'
      : 'text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)]',
  )
}

export default function AppLayout(): ReactElement {
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  function handleLogout(): void {
    logout()
    void navigate('/login', { replace: true })
  }

  const initials = user?.email ? user.email.slice(0, 2).toUpperCase() : '??'
  const orgName = user?.tenant_slug ?? 'Organisation'

  return (
    <div className="flex h-screen overflow-hidden bg-[var(--color-page-bg)]">
      {/* Sidebar */}
      <aside className="w-56 flex-shrink-0 flex flex-col h-screen bg-[var(--color-sidebar-bg)]">
        {/* Logo */}
        <div className="h-14 flex items-center gap-2.5 px-4 border-b border-white/10 flex-shrink-0">
          <div className="w-7 h-7 rounded-lg bg-[var(--color-sidebar-active)] flex items-center justify-center flex-shrink-0">
            <span className="text-[11px] font-bold text-white">IP</span>
          </div>
          <span className="text-[13px] font-semibold text-white tracking-tight">Indian Payroll</span>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-3 space-y-0.5 overflow-y-auto">
          {/* Main nav items added here as features are built */}
          <NavLink to="/settings" className={({ isActive }) => navItemCls(isActive)}>
            <Settings className="w-4 h-4 flex-shrink-0" />
            Settings
          </NavLink>
        </nav>

        {/* User footer */}
        <div className="px-3 pb-3 border-t border-white/10 flex-shrink-0 pt-2 space-y-0.5">
          <div className="flex items-center gap-2.5 px-3 pt-1 pb-0.5">
            <div className="w-6 h-6 rounded-full bg-[var(--color-sidebar-active)] flex items-center justify-center flex-shrink-0">
              <span className="text-[10px] font-semibold text-white">{initials}</span>
            </div>
            <p className="text-[12px] text-[var(--color-sidebar-text)] truncate">{user?.email}</p>
          </div>
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 w-full h-8 px-3 rounded-lg text-[13px] text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)] transition-colors"
          >
            <LogOut className="w-3.5 h-3.5 flex-shrink-0" />
            Sign out
          </button>
        </div>
      </aside>

      {/* Right column */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Topbar */}
        <header className="h-14 flex-shrink-0 bg-[var(--color-topbar-bg)] border-b border-[var(--color-border)] flex items-center justify-between px-5 z-40">
          <div />
          <div className="flex items-center gap-2">
            <div className="inline-flex items-center h-7 px-2.5 rounded-md border border-[var(--color-border)] bg-[var(--color-page-bg)] text-[12px] text-[var(--color-text-secondary)] font-medium">
              {orgName}
            </div>
            <div className="w-8 h-8 rounded-full bg-[var(--color-primary)] flex items-center justify-center text-[11px] font-semibold text-white select-none">
              {initials}
            </div>
          </div>
        </header>

        {/* Main content */}
        <main className="flex-1 overflow-y-auto px-6 py-5">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
