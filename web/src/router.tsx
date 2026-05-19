import { createBrowserRouter, Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import AppLayout from '@/components/layout/AppLayout'
import PlatformLayout from '@/components/layout/PlatformLayout'
import SettingsLayout from '@/components/layout/SettingsLayout'
import SettingsHomePage from '@/pages/settings/SettingsHomePage'
import WorkLocationsPage from '@/pages/settings/WorkLocationsPage'
import DepartmentsPage from '@/pages/settings/DepartmentsPage'
import DesignationsPage from '@/pages/settings/DesignationsPage'
import BusinessUnitsPage from '@/pages/settings/BusinessUnitsPage'
import OrgProfilePage from '@/pages/settings/OrgProfilePage'
import PaySchedulesPage from '@/pages/settings/PaySchedulesPage'
import SalaryComponentsPage from '@/pages/settings/SalaryComponentsPage'
import SalaryStructuresPage from '@/pages/settings/SalaryStructuresPage'
import SalaryStructureBuilderPage from '@/pages/settings/SalaryStructureBuilderPage'
import StatutoryComponentsPage from '@/pages/settings/StatutoryComponentsPage'
import TaxDetailsPage from '@/pages/settings/TaxDetailsPage'
import LoginPage from '@/pages/LoginPage'
import DashboardPage from '@/pages/DashboardPage'
import EmployeesPage from '@/pages/employees/EmployeesPage'
import AddEmployeeWizard from '@/pages/employees/AddEmployeeWizard'
import EmployeeDetailPage from '@/pages/employees/EmployeeDetailPage'
import PayRunsPage from '@/pages/payroll/PayRunsPage'
import PayRunDetailPage from '@/pages/payroll/PayRunDetailPage'
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
  if (!roles.includes('SuperAdmin')) return <Navigate to="/" replace />
  return children
}

function RootRedirect(): React.ReactElement {
  const user = useAuthStore(s => s.user)
  const token = useAuthStore(s => s.token)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (roles.includes('SuperAdmin')) return <Navigate to="/platform/orgs" replace />
  return <Navigate to="/dashboard" replace />
}

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/set-password', element: <SetPasswordPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },
  { path: '/', element: <RootRedirect /> },
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
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'employees', element: <EmployeesPage /> },
      { path: 'employees/new', element: <AddEmployeeWizard /> },
      { path: 'employees/:id/wizard/:step', element: <AddEmployeeWizard /> },
      { path: 'employees/:id', element: <EmployeeDetailPage /> },
      { path: 'pay-runs', element: <PayRunsPage /> },
      { path: 'pay-runs/:id', element: <PayRunDetailPage /> },
      {
        path: 'settings',
        element: <SettingsLayout />,
        children: [
          { index: true, element: <SettingsHomePage /> },
          { path: 'work-locations', element: <WorkLocationsPage /> },
          { path: 'departments', element: <DepartmentsPage /> },
          { path: 'designations', element: <DesignationsPage /> },
          { path: 'business-units', element: <BusinessUnitsPage /> },
          { path: 'org-profile', element: <OrgProfilePage /> },
          { path: 'pay-schedule', element: <PaySchedulesPage /> },
          { path: 'salary-components', element: <SalaryComponentsPage /> },
          { path: 'salary-structures', element: <SalaryStructuresPage /> },
          { path: 'salary-structures/new', element: <SalaryStructureBuilderPage /> },
          { path: 'salary-structures/:id/edit', element: <SalaryStructureBuilderPage /> },
          { path: 'statutory', element: <StatutoryComponentsPage /> },
          { path: 'tax-details', element: <TaxDetailsPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
