import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { EmployeeSalaryStructureDto } from '@/types/api'

interface Props {
  employeeId: string
}

function fmtDate(iso: string): string {
  return iso.split('-').reverse().join('/')
}

export default function EmployeeSalaryTab({ employeeId }: Props): React.ReactElement {
  const navigate = useNavigate()

  const { data, isLoading, error } = useQuery<EmployeeSalaryStructureDto>({
    queryKey: ['employee-salary', employeeId],
    queryFn: () =>
      api.get<EmployeeSalaryStructureDto>(`/api/v1/employees/${employeeId}/salary-structure`).then(r => r.data),
    retry: false,
  })

  if (isLoading) {
    return (
      <div className="p-8 text-center text-[13px] text-[var(--color-text-secondary)]">Loading…</div>
    )
  }

  if (error || !data) {
    return (
      <div className="p-8 text-center space-y-3">
        <p className="text-[13px] text-[var(--color-text-secondary)]">No salary structure assigned yet.</p>
        <button
          onClick={() => navigate(`/employees/${employeeId}/wizard/salary`)}
          className="h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg hover:bg-[var(--color-primary-hover)] transition-colors"
        >
          Assign Salary Structure
        </button>
      </div>
    )
  }

  return (
    <div className="p-5 space-y-5">
      {/* CTC card */}
      <div className="border border-[var(--color-border)] rounded-xl p-5">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-[12px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">Annual CTC</p>
            <p className="text-[24px] font-semibold text-[var(--color-text-primary)] mt-1">{formatINR(data.annualCTC)}</p>
            <p className="text-[13px] text-[var(--color-text-secondary)] mt-0.5">
              {formatINR(data.monthlyGross)} per month
            </p>
          </div>
          <div className="text-right">
            {data.templateName && (
              <p className="text-[12px] text-[var(--color-text-secondary)]">Template: {data.templateName}</p>
            )}
            <p className="text-[11px] text-[var(--color-text-secondary)] mt-1">Effective {fmtDate(data.effectiveFrom)}</p>
            <button
              onClick={() => navigate(`/employees/${employeeId}/wizard/salary?revise=1`)}
              className="mt-2 h-8 px-3 text-[12px] text-[var(--color-primary)] border border-[var(--color-primary)]/40 rounded-lg hover:bg-[var(--color-primary)]/5 transition-colors"
            >
              Revise
            </button>
          </div>
        </div>
      </div>

      {/* Component table */}
      {data.components.length > 0 && (
        <div className="border border-[var(--color-border)] rounded-xl overflow-hidden">
          <div className="px-5 py-3 border-b border-[var(--color-border)] bg-[var(--color-page-bg)]">
            <h3 className="text-[13px] font-semibold text-[var(--color-text-primary)]">Salary Structure</h3>
          </div>
          <table className="w-full text-[12px]">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Component</th>
                <th className="text-left px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Calculation</th>
                <th className="text-right px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Monthly</th>
                <th className="text-right px-5 py-2.5 font-medium text-[var(--color-text-secondary)]">Annual</th>
              </tr>
            </thead>
            <tbody>
              {data.components.map(c => (
                <tr key={c.componentId} className="border-b border-[var(--color-border)] last:border-0">
                  <td className="px-5 py-3 text-[var(--color-text-primary)]">{c.componentName}</td>
                  <td className="px-5 py-3 text-[var(--color-text-secondary)]">
                    {c.isResidual
                      ? 'Residual (Fixed Allowance)'
                      : c.formulaType === 'PercentOfCTC'
                        ? `${c.percentage}% of CTC`
                        : c.formulaType === 'PercentOfBasic'
                          ? `${c.percentage}% of Basic`
                          : c.formulaType === 'PercentOfGross'
                            ? `${c.percentage}% of Gross`
                            : 'Fixed'}
                  </td>
                  <td className="px-5 py-3 text-right text-[var(--color-text-primary)]">{formatINR(c.monthlyAmount)}</td>
                  <td className="px-5 py-3 text-right text-[var(--color-text-primary)]">{formatINR(c.annualAmount)}</td>
                </tr>
              ))}
            </tbody>
            {(data.employeeDeductions ?? []).length > 0 && (
              <tbody className="bg-[var(--color-page-bg)] border-t border-[var(--color-border)]">
                <tr>
                  <td colSpan={4} className="px-5 pt-2 text-[11px] uppercase tracking-wider text-[var(--color-text-muted)]">
                    Employee deductions
                  </td>
                </tr>
                {data.employeeDeductions.map(d => (
                  <tr key={d.code} className="text-[var(--color-text-secondary)]">
                    <td className="px-5 py-1.5" colSpan={2}>{d.name}</td>
                    <td className="px-5 py-1.5 text-right">−{formatINR(d.monthlyAmount)}</td>
                    <td className="px-5 py-1.5 text-right">−{formatINR(d.annualAmount)}</td>
                  </tr>
                ))}
                <tr className="font-semibold text-[var(--color-text-primary)] border-t border-[var(--color-border)]">
                  <td className="px-5 py-2" colSpan={2}>Take-home (net pay)</td>
                  <td className="px-5 py-2 text-right">{formatINR(data.netPayMonthly ?? 0)}</td>
                  <td className="px-5 py-2 text-right">{formatINR((data.netPayMonthly ?? 0) * 12)}</td>
                </tr>
              </tbody>
            )}
            {(data.employerContributions ?? []).length > 0 && (
              <tbody className="bg-[var(--color-page-bg)] border-t border-[var(--color-border)]">
                <tr>
                  <td colSpan={4} className="px-5 pt-2 text-[11px] uppercase tracking-wider text-[var(--color-text-muted)]">
                    Employer contributions (included in CTC)
                  </td>
                </tr>
                {data.employerContributions.map(ec => (
                  <tr key={ec.code} className="text-[var(--color-text-secondary)]">
                    <td className="px-5 py-1.5" colSpan={2}>{ec.name}</td>
                    <td className="px-5 py-1.5 text-right">{formatINR(ec.monthlyAmount)}</td>
                    <td className="px-5 py-1.5 text-right">{formatINR(ec.annualAmount)}</td>
                  </tr>
                ))}
              </tbody>
            )}
            {(data.benefits ?? []).length > 0 && (
              <tbody className="bg-[var(--color-page-bg)] border-t border-[var(--color-border)]">
                <tr>
                  <td colSpan={4} className="px-5 pt-2 text-[11px] uppercase tracking-wider text-[var(--color-text-muted)]">
                    Benefits (not in monthly gross)
                  </td>
                </tr>
                {data.benefits.map(b => (
                  <tr key={b.code} className="text-[var(--color-text-secondary)]">
                    <td className="px-5 py-1.5" colSpan={2}>{b.name}</td>
                    <td className="px-5 py-1.5 text-right">{formatINR(b.monthlyAmount)}</td>
                    <td className="px-5 py-1.5 text-right">{formatINR(b.annualAmount)}</td>
                  </tr>
                ))}
              </tbody>
            )}
            <tfoot>
              <tr className="bg-[var(--color-page-bg)] font-semibold border-t border-[var(--color-border)]">
                <td className="px-5 py-3 text-[var(--color-text-primary)]" colSpan={2}>Cost to Company</td>
                <td className="px-5 py-3 text-right text-[var(--color-text-primary)]">{formatINR(data.monthlyGross)}</td>
                <td className="px-5 py-3 text-right text-[var(--color-text-primary)]">{formatINR(data.annualCTC)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  )
}
