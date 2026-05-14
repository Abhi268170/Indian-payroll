import { createBrowserRouter, Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import AppLayout from '@/components/layout/AppLayout'
import LoginPage from '@/pages/LoginPage'
import BranchesPage from '@/pages/org/BranchesPage'
import DepartmentsPage from '@/pages/org/DepartmentsPage'
import DesignationsPage from '@/pages/org/DesignationsPage'
import CostCentresPage from '@/pages/org/CostCentresPage'
import EmployeesPage from '@/pages/employees/EmployeesPage'

function RequireAuth({ children }: { children: React.ReactElement }): React.ReactElement {
  const isAuthenticated = useAuthStore(s => s.isAuthenticated())
  if (!isAuthenticated) return <Navigate to="/login" replace />
  return children
}

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <RequireAuth><AppLayout /></RequireAuth>,
    children: [
      { index: true, element: <Navigate to="/employees" replace /> },
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
