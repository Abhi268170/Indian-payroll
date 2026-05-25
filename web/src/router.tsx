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
import ImportEmployeesPage from '@/pages/employees/ImportEmployeesPage'
import PayRunsPage from '@/pages/payroll/PayRunsPage'
import PayRunDetailPage from '@/pages/payroll/PayRunDetailPage'
import ExitInitiationPage from '@/pages/employees/ExitInitiationPage'
import FnfSettlementPage from '@/pages/payroll/FnfSettlementPage'
import TenantsPage from '@/pages/platform/TenantsPage'
import ProvisionOrgPage from '@/pages/platform/ProvisionOrgPage'
import OrgDetailPage from '@/pages/platform/OrgDetailPage'
import SetPasswordPage from '@/pages/auth/SetPasswordPage'
import ForgotPasswordPage from '@/pages/auth/ForgotPasswordPage'
import OnboardingWizardPage from '@/pages/onboarding/OnboardingWizardPage'
import { useOnboardingStatus } from '@/hooks/useOnboardingStatus'

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

// SuperAdmin has no tenant_id claim, so TenantResolutionMiddleware never binds a tenant
// context. Every tenant-scoped query then throws "Tenant context not resolved". Bounce
// SuperAdmin out of any tenant route (settings, employees, pay runs, dashboard,
// onboarding) so they only reach the platform-admin surface.
function RequireTenantUser({ children }: { children: React.ReactElement }): React.ReactElement {
  const token = useAuthStore(s => s.token)
  const user = useAuthStore(s => s.user)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (roles.includes('SuperAdmin')) return <Navigate to="/platform/orgs" replace />
  return children
}

function RootRedirect(): React.ReactElement {
  const user = useAuthStore(s => s.user)
  const token = useAuthStore(s => s.token)
  const isAuth = token !== null && user !== null && user.exp * 1000 > Date.now()
  if (!isAuth) return <Navigate to="/login" replace />
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (roles.includes('SuperAdmin')) return <Navigate to="/platform/orgs" replace />
  return <OnboardingAwareRedirect />
}

// For non-SuperAdmin tenant users: query setup status and route to /onboarding while
// the tenant is incomplete, otherwise to /dashboard. While the status request is in
// flight we render nothing — fast, no flicker, no flash of dashboard.
//
// Fail-closed on error: if the status request fails (transient API outage, network
// blip), assume incomplete and route to /onboarding rather than leaking access to
// the dashboard. The wizard itself surfaces a retry path.
function OnboardingAwareRedirect(): React.ReactElement {
  const { data, isLoading, isError } = useOnboardingStatus()
  if (isLoading) return <></>
  if (isError || !data) return <Navigate to="/onboarding" replace />
  if (!data.setupComplete) return <Navigate to="/onboarding" replace />
  return <Navigate to="/dashboard" replace />
}

// Guard for People / Pay Runs deep links so direct navigation respects the
// navGate that the sidebar enforces. Settings remains reachable so the user
// can still edit existing values.
//
// Fail-closed on error: missing data means we cannot confirm the gate is open, so
// we send the user to /onboarding instead of allowing through. This matches the
// "hard redirect while incomplete" intent.
function RequireNavGate({
  gate,
  children,
}: { gate: 'people' | 'payRuns'; children: React.ReactElement }): React.ReactElement {
  const { data, isLoading, isError } = useOnboardingStatus()
  if (isLoading) return <></>
  if (isError || !data) return <Navigate to="/onboarding" replace />
  if (data.navGates[gate].enabled) return children
  return <Navigate to="/onboarding" replace />
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
  // Onboarding wizard — full-screen, no AppLayout sidebar. Settings deep-links open in a
  // new tab; the wizard stays open in the original tab. Guarded by RequireAuth only —
  // the wizard itself decides whether setup is complete and redirects to /dashboard.
  {
    path: '/onboarding',
    element: <RequireAuth><RequireTenantUser><OnboardingWizardPage /></RequireTenantUser></RequireAuth>,
  },
  {
    path: '/onboarding/:stepId',
    element: <RequireAuth><RequireTenantUser><OnboardingWizardPage /></RequireTenantUser></RequireAuth>,
  },
  {
    path: '/',
    element: <RequireAuth><RequireTenantUser><AppLayout /></RequireTenantUser></RequireAuth>,
    children: [
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'employees', element: <RequireNavGate gate="people"><EmployeesPage /></RequireNavGate> },
      { path: 'employees/import', element: <RequireNavGate gate="people"><ImportEmployeesPage /></RequireNavGate> },
      { path: 'employees/new', element: <RequireNavGate gate="people"><AddEmployeeWizard /></RequireNavGate> },
      { path: 'employees/:id/wizard/:step', element: <RequireNavGate gate="people"><AddEmployeeWizard /></RequireNavGate> },
      { path: 'employees/:id', element: <RequireNavGate gate="people"><EmployeeDetailPage /></RequireNavGate> },
      { path: 'employees/:id/exit/initiate', element: <RequireNavGate gate="people"><ExitInitiationPage /></RequireNavGate> },
      { path: 'pay-runs', element: <RequireNavGate gate="payRuns"><PayRunsPage /></RequireNavGate> },
      { path: 'pay-runs/:id', element: <RequireNavGate gate="payRuns"><PayRunDetailPage /></RequireNavGate> },
      { path: 'pay-runs/:id/fnf', element: <RequireNavGate gate="payRuns"><FnfSettlementPage /></RequireNavGate> },
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
