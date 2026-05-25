import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { UserPlus, AlertCircle, Upload } from 'lucide-react'
import { api } from '@/lib/api'
import type { EmployeeListItemDto } from '@/types/api'
import { Pagination, usePersistedPageSize } from '@/components/ui/Pagination'
import { useOnboardingStatus, navMissingLabel } from '@/hooks/useOnboardingStatus'

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

type StatusFilter = 'All' | 'Active' | 'Inactive' | 'Exited'

const STATUS_TABS: StatusFilter[] = ['All', 'Active', 'Inactive', 'Exited']

function statusBadge(status: string): React.ReactElement {
  const map: Record<string, string> = {
    Active: 'bg-emerald-50 text-emerald-700',
    Inactive: 'bg-gray-100 text-gray-600',
    Exited: 'bg-red-50 text-red-600',
  }
  return (
    <span className={`inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium ${map[status] ?? 'bg-gray-100 text-gray-600'}`}>
      {status}
    </span>
  )
}

function employmentTypeBadge(type: string): React.ReactElement {
  const map: Record<string, string> = {
    FullTime: 'bg-blue-50 text-blue-700',
    PartTime: 'bg-purple-50 text-purple-700',
    Contract: 'bg-amber-50 text-amber-700',
    Intern: 'bg-cyan-50 text-cyan-700',
  }
  const label: Record<string, string> = {
    FullTime: 'Full Time',
    PartTime: 'Part Time',
    Contract: 'Contract',
    Intern: 'Intern',
  }
  return (
    <span className={`inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium ${map[type] ?? 'bg-gray-100 text-gray-600'}`}>
      {label[type] ?? type}
    </span>
  )
}

function initials(name: string): string {
  return name.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase()
}

export default function EmployeesPage(): React.ReactElement {
  const navigate = useNavigate()
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = usePersistedPageSize('employees', 25)
  // People nav-gate: depts + desigs + work-locations + salary-structure must all
  // exist before an employee can be created (CreateEmployeeCommand validates FK
  // refs server-side; this disables the button up front for a friendlier UX).
  const { data: onboardingStatus } = useOnboardingStatus()
  const peopleGate = onboardingStatus?.navGates.people
  const addEmployeeBlocked = peopleGate ? !peopleGate.enabled : false
  const addEmployeeTooltip = addEmployeeBlocked
    ? `Complete first: ${(peopleGate?.missing ?? []).map(navMissingLabel).join(', ')}`
    : undefined

  const { data, isLoading } = useQuery<PagedResult<EmployeeListItemDto>>({
    queryKey: ['employees', page, pageSize, statusFilter, search],
    queryFn: () => api.get<PagedResult<EmployeeListItemDto>>('/api/v1/employees', {
      params: { page, pageSize, status: statusFilter === 'All' ? undefined : statusFilter, search: search || undefined },
    }).then(r => r.data),
    placeholderData: keepPreviousData,
  })

  const employees = data?.items ?? []
  const total = data?.total ?? 0
  const filtered = employees // server already filtered + paginated
  const incomplete = employees.filter(e => !e.profileComplete && e.status === 'Active')

  // Any state change that affects the result set jumps back to page 1.
  function resetToFirstPage(): void { setPage(1) }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[18px] font-semibold text-[var(--color-text-primary)]">Employees</h1>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">
            {total} employee{total !== 1 ? 's' : ''}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate('/employees/import')}
            className="inline-flex items-center gap-1.5 h-9 px-4 bg-white border border-[var(--color-border)] text-[var(--color-text-secondary)] text-[13px] font-medium rounded-lg hover:border-[var(--color-border-strong)] transition-colors"
          >
            <Upload className="w-3.5 h-3.5" />
            Import
          </button>
          <button
            onClick={() => navigate('/employees/new')}
            disabled={addEmployeeBlocked}
            title={addEmployeeTooltip}
            className="inline-flex items-center gap-1.5 h-9 px-4 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] transition-colors disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-[var(--color-primary)]"
          >
            <UserPlus className="w-3.5 h-3.5" />
            Add Employee
          </button>
        </div>
      </div>

      {/* Incomplete profiles banner */}
      {incomplete.length > 0 && (
        <div className="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
          <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
          <p className="text-[13px] text-amber-800">
            <span className="font-medium">{incomplete.length} employee{incomplete.length > 1 ? 's have' : ' has'} incomplete profiles.</span>
            {' '}Fill in personal details, payment info, and statutory details to enable payroll.
          </p>
        </div>
      )}

      {/* Filters row */}
      <div className="flex items-center gap-3">
        {/* Status tabs */}
        <div className="flex items-center h-9 bg-white border border-[var(--color-border)] rounded-lg p-1 gap-0.5">
          {STATUS_TABS.map(tab => (
            <button
              key={tab}
              onClick={() => { setStatusFilter(tab); resetToFirstPage() }}
              className={`h-7 px-3 rounded-md text-[12px] font-medium transition-colors ${
                statusFilter === tab
                  ? 'bg-[var(--color-primary)] text-white'
                  : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
              }`}
            >
              {tab}
            </button>
          ))}
        </div>

        {/* Search */}
        <input
          type="text"
          placeholder="Search by name, email, code, department…"
          value={search}
          onChange={e => { setSearch(e.target.value); resetToFirstPage() }}
          className="flex-1 h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]"
        />
      </div>

      {/* Table */}
      <div className="bg-white border border-[var(--color-border)] rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="py-12 text-center text-[13px] text-[var(--color-text-secondary)]">Loading…</div>
        ) : filtered.length === 0 ? (
          <div className="py-12 text-center">
            <p className="text-[13px] text-[var(--color-text-secondary)]">
              {employees.length === 0 ? 'No employees yet. Add your first employee.' : 'No employees match this filter.'}
            </p>
          </div>
        ) : (
          <table className="w-full text-[13px]">
            <thead className="border-b border-[var(--color-border)] bg-[var(--color-page-bg)]">
              <tr>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Employee</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Code</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Department</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Location</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Type</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Joined</th>
                <th className="text-left px-4 py-2.5 font-medium text-[var(--color-text-secondary)]">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {filtered.map(e => (
                <tr
                  key={e.id}
                  onClick={() => navigate(`/employees/${e.id}`)}
                  className="hover:bg-[var(--color-page-bg)] cursor-pointer transition-colors"
                >
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-[var(--color-primary)]/10 flex items-center justify-center flex-shrink-0">
                        <span className="text-[11px] font-semibold text-[var(--color-primary)]">
                          {initials(e.fullName)}
                        </span>
                      </div>
                      <div>
                        <div className="flex items-center gap-1.5">
                          <span className="font-medium text-[var(--color-text-primary)]">{e.fullName}</span>
                          {!e.profileComplete && (
                            <span className="inline-flex items-center h-4 px-1.5 rounded-sm bg-amber-50 border border-amber-200 text-[10px] font-medium text-amber-700">
                              Incomplete
                            </span>
                          )}
                        </div>
                        <span className="text-[12px] text-[var(--color-text-secondary)]">{e.workEmail}</span>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 font-mono text-[12px] text-[var(--color-text-secondary)]">{e.employeeCode}</td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">{e.departmentName ?? '—'}</td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">{e.workLocationName ?? '—'}</td>
                  <td className="px-4 py-3">{employmentTypeBadge(e.employmentType)}</td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {e.dateOfJoining.split('-').reverse().join('/')}
                  </td>
                  <td className="px-4 py-3">{statusBadge(e.status)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <Pagination
          page={page}
          pageSize={pageSize}
          total={total}
          onPageChange={setPage}
          onPageSizeChange={s => { setPageSize(s); setPage(1) }}
        />
      </div>
    </div>
  )
}
