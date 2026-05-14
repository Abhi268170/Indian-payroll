import { createBrowserRouter, Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import AppLayout from '@/components/layout/AppLayout'
import PlatformLayout from '@/components/layout/PlatformLayout'
import LoginPage from '@/pages/LoginPage'
import BranchesPage from '@/pages/org/BranchesPage'
import DepartmentsPage from '@/pages/org/DepartmentsPage'
import DesignationsPage from '@/pages/org/DesignationsPage'
import CostCentresPage from '@/pages/org/CostCentresPage'
import EmployeesPage from '@/pages/employees/EmployeesPage'
import TenantsPage from '@/pages/platform/TenantsPage'
import ProvisionOrgPage from '@/pages/platform/ProvisionOrgPage'
import OrgDetailPage from '@/pages/platform/OrgDetailPage'
import SetPasswordPage from '@/pages/auth/SetPasswordPage'
import ForgotPasswordPage from '@/pages/auth/ForgotPasswordPage'

function RequireAuth({ children }: { children: React.ReactElement }): React.ReactElement {
  const token = useAuthStore(s => s.token)
  const user = useAuthStore(s => s.user)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  return children
}

function RequireSuperAdmin({ children }: { children: React.ReactElement }): React.ReactElement {
  const token = useAuthStore(s => s.token)
  const user = useAuthStore(s => s.user)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (!roles.includes('SuperAdmin')) return <Navigate to="/employees" replace />
  return children
}

// After login, route SuperAdmin to platform, everyone else to tenant app
function RootRedirect(): React.ReactElement {
  const user = useAuthStore(s => s.user)
  const token = useAuthStore(s => s.token)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (roles.includes('SuperAdmin')) return <Navigate to="/platform/orgs" replace />
  return <Navigate to="/employees" replace />
}

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/set-password',
    element: <SetPasswordPage />,
  },
  {
    path: '/forgot-password',
    element: <ForgotPasswordPage />,
  },
  {
    path: '/',
    element: <RootRedirect />,
  },
  {
    path: '/platform',
    element: <RequireSuperAdmin><PlatformLayout /></RequireSuperAdmin>,
    children: [
      { index: true, element: <Navigate to="/platform/orgs" replace /> },
      { path: 'orgs', element: <TenantsPage /> },
      { path: 'orgs/new', element: <ProvisionOrgPage /> },
      { path: 'orgs/:id', element: <OrgDetailPage /> },
    ],
  },
  {
    path: '/',
    element: <RequireAuth><AppLayout /></RequireAuth>,
    children: [
      { path: 'employees', element: <EmployeesPage /> },
      { path: 'org/branches', element: <BranchesPage /> },
      { path: 'org/departments', element: <DepartmentsPage /> },
      { path: 'org/designations', element: <DesignationsPage /> },
      { path: 'org/cost-centres', element: <CostCentresPage /> },
    ],
  },
  {
    path: '*',
    element: <Navigate to="/" replace />,
  },
])
