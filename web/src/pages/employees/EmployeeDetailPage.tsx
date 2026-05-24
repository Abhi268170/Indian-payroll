import { useState } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, AlertCircle, MoreHorizontal } from 'lucide-react'
import { api } from '@/lib/api'
import type { EmployeeDto } from '@/types/api'
import EmployeeOverviewTab from './tabs/EmployeeOverviewTab'
import EmployeeSalaryTab from './tabs/EmployeeSalaryTab'
import EmployeePayslipsTab from './tabs/EmployeePayslipsTab'
import EmployeeTaxTab from './tabs/EmployeeTaxTab'

type Tab = 'overview' | 'salary' | 'tax' | 'investments' | 'payslips'

const TABS: { key: Tab; label: string }[] = [
  { key: 'overview', label: 'Overview' },
  { key: 'salary', label: 'Salary Details' },
  { key: 'tax', label: 'Tax' },
  { key: 'investments', label: 'Investments' },
  { key: 'payslips', label: 'Payslips & Forms' },
]

function statusBadge(status: string): React.ReactElement {
  const map: Record<string, string> = {
    Active: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    Inactive: 'bg-gray-100 text-gray-600 border-gray-200',
    Exited: 'bg-red-50 text-red-600 border-red-200',
  }
  return (
    <span className={`inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium border ${map[status] ?? 'bg-gray-100 text-gray-600 border-gray-200'}`}>
      {status}
    </span>
  )
}

function initials(name: string): string {
  return name.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase()
}

function ComingSoon(): React.ReactElement {
  return (
    <div className="py-16 text-center text-[13px] text-[var(--color-text-secondary)]">
      Coming soon
    </div>
  )
}

export default function EmployeeDetailPage(): React.ReactElement {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [tab, setTab] = useState<Tab>(() => {
    const t = searchParams.get('tab')
    return (t === 'salary' || t === 'tax' || t === 'investments' || t === 'payslips') ? t : 'overview'
  })
  const [kebabOpen, setKebabOpen] = useState(false)

  const { data: employee, isLoading, error, refetch } = useQuery<EmployeeDto>({
    queryKey: ['employee', id],
    queryFn: () => api.get<EmployeeDto>(`/api/v1/employees/${id}`).then(r => r.data),
    enabled: !!id,
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20 text-[13px] text-[var(--color-text-secondary)]">
        Loading…
      </div>
    )
  }

  if (error || !employee) {
    return (
      <div className="py-20 text-center">
        <p className="text-[13px] text-red-600">Employee not found.</p>
        <button onClick={() => navigate('/employees')} className="mt-3 text-[13px] text-[var(--color-primary)]">
          Back to Employees
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-0">
      {/* Back */}
      <button
        onClick={() => navigate('/employees')}
        className="inline-flex items-center gap-1.5 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors mb-3"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        Employees
      </button>

      {/* Profile header card */}
      <div className="bg-white border border-[var(--color-border)] rounded-xl px-5 py-4">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-full bg-[var(--color-primary)]/10 flex items-center justify-center flex-shrink-0 mt-0.5">
            <span className="text-[14px] font-semibold text-[var(--color-primary)]">{initials(employee.fullName)}</span>
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h1 className="text-[16px] font-semibold text-[var(--color-text-primary)]">
                {employee.employeeCode} — {employee.fullName}
              </h1>
              {statusBadge(employee.status)}
              {!employee.profileComplete && (
                <span className="inline-flex items-center gap-1 h-5 px-2 rounded-full text-[11px] font-medium bg-amber-50 text-amber-700 border border-amber-200">
                  <AlertCircle className="w-3 h-3" />
                  Profile Incomplete
                </span>
              )}
            </div>
            <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
              {employee.designationName ?? '—'}
            </p>
            <div className="flex items-center gap-4 mt-1.5 text-[12px] text-[var(--color-text-secondary)] flex-wrap">
              <span>{employee.workEmail}</span>
              {employee.mobileNumber && <span>{employee.mobileNumber}</span>}
              <span>Joined {employee.dateOfJoining.split('-').reverse().join('/')}</span>
              {employee.departmentName && <span>{employee.departmentName}</span>}
            </div>
          </div>
          {employee.status === 'Active' && (
            <div className="relative">
              <button
                onClick={() => { setKebabOpen(v => !v) }}
                className="w-8 h-8 rounded hover:bg-gray-100 flex items-center justify-center text-[var(--color-text-secondary)]"
                aria-label="More actions"
              >
                <MoreHorizontal className="w-4 h-4" />
              </button>
              {kebabOpen && (
                <>
                  <div className="fixed inset-0 z-10" onClick={() => { setKebabOpen(false) }} />
                  <div className="absolute right-0 top-9 z-20 w-52 bg-white border border-[var(--color-border)] rounded-lg shadow-lg py-1">
                    <button
                      onClick={() => { setKebabOpen(false); navigate(`/employees/${id}/exit/initiate`) }}
                      className="w-full text-left px-4 py-2 text-[13px] text-[var(--color-text-primary)] hover:bg-gray-50"
                    >
                      Initiate Exit Process
                    </button>
                  </div>
                </>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center border-b border-[var(--color-border)] bg-white px-4 overflow-hidden">
        {TABS.map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`h-10 px-4 text-[13px] font-medium border-b-2 transition-colors whitespace-nowrap ${
              tab === t.key
                ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
                : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div className="bg-white border border-[var(--color-border)] border-t-0 rounded-b-xl p-0">
        {tab === 'overview' && <EmployeeOverviewTab employee={employee} onSaved={() => void refetch()} />}
        {tab === 'salary' && <EmployeeSalaryTab employeeId={employee.id} />}
        {tab === 'tax' && <EmployeeTaxTab employeeId={employee.id} />}
        {tab === 'investments' && <ComingSoon />}
        {tab === 'payslips' && <EmployeePayslipsTab employeeId={employee.id} />}
      </div>
    </div>
  )
}
