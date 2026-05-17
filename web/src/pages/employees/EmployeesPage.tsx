import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { UserPlus, AlertCircle } from 'lucide-react'
import { api } from '@/lib/api'
import type { EmployeeListItemDto } from '@/types/api'

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

  const { data: employees = [], isLoading } = useQuery<EmployeeListItemDto[]>({
    queryKey: ['employees'],
    queryFn: () => api.get<EmployeeListItemDto[]>('/api/v1/employees').then(r => r.data),
  })

  const incomplete = employees.filter(e => !e.profileComplete && e.status === 'Active')

  const filtered = employees.filter(e => {
    const matchStatus = statusFilter === 'All' || e.status === statusFilter
    const q = search.toLowerCase()
    const matchSearch =
      !q ||
      e.fullName.toLowerCase().includes(q) ||
      e.workEmail.toLowerCase().includes(q) ||
      e.employeeCode.toLowerCase().includes(q) ||
      (e.departmentName ?? '').toLowerCase().includes(q)
    return matchStatus && matchSearch
  })

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[18px] font-semibold text-[var(--color-text-primary)]">Employees</h1>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">
            {employees.length} employee{employees.length !== 1 ? 's' : ''}
          </p>
        </div>
        <button
          onClick={() => navigate('/employees/new')}
          className="inline-flex items-center gap-1.5 h-9 px-4 bg-[var(--color-primary)] text-white text-[13px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] transition-colors"
        >
          <UserPlus className="w-3.5 h-3.5" />
          Add Employee
        </button>
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
              onClick={() => setStatusFilter(tab)}
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
          onChange={e => setSearch(e.target.value)}
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
      </div>
    </div>
  )
}
