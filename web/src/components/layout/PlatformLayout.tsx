import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { clsx } from 'clsx'
import BrandMark from '@/components/BrandMark'

export default function PlatformLayout(): React.ReactElement {
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex h-screen bg-gray-50">
      <aside className="w-56 flex-shrink-0 bg-gray-900 flex flex-col">
        <div className="px-5 py-4 border-b border-gray-700">
          <div className="flex items-center gap-2.5 mb-1">
            <BrandMark size="sm" />
            <p className="text-sm font-semibold text-white">Indian Payroll</p>
          </div>
          <span className="text-[11px] font-semibold text-gray-400 uppercase tracking-widest">Platform Admin</span>
        </div>

        <nav className="flex-1 px-3 py-3 space-y-0.5">
          <NavLink
            to="/platform/orgs"
            className={({ isActive }) =>
              clsx(
                'block px-3 py-2 rounded-lg text-sm transition-colors',
                isActive
                  ? 'bg-gray-700 text-white font-medium'
                  : 'text-gray-400 hover:bg-gray-800 hover:text-white',
              )
            }
          >
            Organisations
          </NavLink>
        </nav>

        <div className="px-4 py-3 border-t border-gray-700">
          <p className="text-xs text-gray-500 truncate mb-2">{user?.email}</p>
          <button
            onClick={handleLogout}
            className="w-full text-left text-xs text-gray-500 hover:text-gray-300 transition-colors"
          >
            Sign out
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-auto p-6">
        <Outlet />
      </main>
    </div>
  )
}
