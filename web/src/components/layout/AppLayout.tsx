import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { clsx } from 'clsx'

const navItems = [
  { label: 'Employees', to: '/employees' },
  { label: 'Branches', to: '/org/branches' },
  { label: 'Departments', to: '/org/departments' },
  { label: 'Designations', to: '/org/designations' },
  { label: 'Cost Centres', to: '/org/cost-centres' },
]

export default function AppLayout(): React.ReactElement {
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  function handleLogout(): void {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-56 flex-shrink-0 bg-white border-r border-gray-200 flex flex-col">
        <div className="px-5 py-4 border-b border-gray-200">
          <span className="text-base font-semibold text-gray-900">Indian Payroll</span>
        </div>

        <nav className="flex-1 px-3 py-3 space-y-0.5">
          {navItems.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                clsx(
                  'block px-3 py-2 rounded-lg text-sm transition-colors',
                  isActive
                    ? 'bg-violet-50 text-violet-700 font-medium'
                    : 'text-gray-600 hover:bg-gray-100',
                )
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="px-4 py-3 border-t border-gray-200">
          <p className="text-xs text-gray-500 truncate mb-2">{user?.email}</p>
          <button
            onClick={handleLogout}
            className="w-full text-left text-xs text-gray-500 hover:text-gray-700 transition-colors"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-auto p-6">
        <Outlet />
      </main>
    </div>
  )
}
