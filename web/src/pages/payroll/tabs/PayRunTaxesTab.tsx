import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { PayRunTaxLineDto } from '@/types/api'

interface Props {
  runId: string
}

function WorksheetRow({ row }: { row: PayRunTaxLineDto }): React.ReactElement {
  const [expanded, setExpanded] = useState(false)

  return (
    <>
      <tr
        className="border-b border-[var(--color-border)] hover:bg-[var(--color-surface)] transition-colors cursor-pointer"
        onClick={() => setExpanded(e => !e)}
      >
        <td className="px-4 py-3 w-6">
          {expanded
            ? <ChevronDown size={14} className="text-[var(--color-text-secondary)]" />
            : <ChevronRight size={14} className="text-[var(--color-text-secondary)]" />
          }
        </td>
        <td className="px-3 py-3">
          <div className="font-medium text-[var(--color-text-primary)]">{row.employeeName}</div>
          <div className="text-[11px] text-[var(--color-text-secondary)]">{row.employeeCode}</div>
        </td>
        <td className="px-3 py-3 text-right text-[var(--color-text-primary)] tabular-nums">
          {formatINR(row.annualProjectedIncome)}
        </td>
        <td className="px-3 py-3 text-right text-[var(--color-text-primary)] tabular-nums">
          {formatINR(row.taxableIncome)}
        </td>
        <td className="px-3 py-3 text-right text-[var(--color-text-primary)] tabular-nums">
          {formatINR(row.annualTaxLiability)}
        </td>
        <td className="px-3 py-3 text-right font-semibold text-[var(--color-text-primary)] tabular-nums">
          {formatINR(row.tdsThisMonth)}
        </td>
        <td className="px-3 py-3 text-center">
          {row.hasPanOverride ? (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-amber-50 text-amber-700 border border-amber-200">
              <AlertTriangle size={11} />
              §206AA
            </span>
          ) : (
            <span className="text-[11px] text-[var(--color-text-secondary)]">OK</span>
          )}
        </td>
      </tr>
      {expanded && (
        <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)]">
          <td />
          <td colSpan={6} className="px-3 py-3">
            <div className="grid grid-cols-3 gap-x-8 gap-y-2 text-[12px]">
              <WorksheetLine label="Annual Projected Income" value={row.annualProjectedIncome} />
              <WorksheetLine label="Standard Deduction" value={-row.standardDeduction} />
              <WorksheetLine label="Taxable Income" value={row.taxableIncome} bold />
              <WorksheetLine label="Tax (Slab)" value={row.taxBeforeRebate} />
              {row.rebate87A > 0 && <WorksheetLine label="Rebate u/s 87A" value={-row.rebate87A} />}
              {row.surcharge > 0 && <WorksheetLine label="Surcharge" value={row.surcharge} />}
              {row.cess > 0 && <WorksheetLine label="Health & Education Cess (4%)" value={row.cess} />}
              <WorksheetLine label="Annual Tax Liability" value={row.annualTaxLiability} bold />
              <WorksheetLine label="TDS This Month" value={row.tdsThisMonth} bold highlight />
            </div>
          </td>
        </tr>
      )}
    </>
  )
}

function WorksheetLine({
  label,
  value,
  bold,
  highlight,
}: {
  label: string
  value: number
  bold?: boolean
  highlight?: boolean
}): React.ReactElement {
  return (
    <div className="flex items-center justify-between">
      <span className={bold ? 'font-medium text-[var(--color-text-primary)]' : 'text-[var(--color-text-secondary)]'}>
        {label}
      </span>
      <span className={`tabular-nums ${highlight ? 'font-semibold text-[var(--color-primary)]' : bold ? 'font-medium text-[var(--color-text-primary)]' : 'text-[var(--color-text-primary)]'}`}>
        {formatINR(Math.abs(value))}{value < 0 ? ' (-)' : ''}
      </span>
    </div>
  )
}

export default function PayRunTaxesTab({ runId }: Props): React.ReactElement {
  const { data: taxes = [], isLoading } = useQuery<PayRunTaxLineDto[]>({
    queryKey: ['run-taxes', runId],
    queryFn: () =>
      api
        .get<PayRunTaxLineDto[]>(`/api/v1/payroll-runs/${runId}/taxes`)
        .then(r => r.data),
    enabled: runId !== '',
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <span className="inline-block w-6 h-6 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (taxes.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 rounded-xl border border-dashed border-[var(--color-border)]">
        <p className="text-[13px] text-[var(--color-text-secondary)]">
          No TDS data — run payroll computation first.
        </p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-[var(--color-border)]">
      <table className="w-full text-[13px]">
        <thead>
          <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)]">
            <th className="px-4 py-3 w-6" />
            <th className="px-3 py-3 text-left font-medium text-[var(--color-text-secondary)]">Employee</th>
            <th className="px-3 py-3 text-right font-medium text-[var(--color-text-secondary)]">Annual Projected</th>
            <th className="px-3 py-3 text-right font-medium text-[var(--color-text-secondary)]">Taxable Income</th>
            <th className="px-3 py-3 text-right font-medium text-[var(--color-text-secondary)]">Annual Tax</th>
            <th className="px-3 py-3 text-right font-medium text-[var(--color-text-secondary)]">TDS This Month</th>
            <th className="px-3 py-3 text-center font-medium text-[var(--color-text-secondary)]">PAN</th>
          </tr>
        </thead>
        <tbody>
          {taxes.map(row => (
            <WorksheetRow key={row.employeeId} row={row} />
          ))}
        </tbody>
      </table>
    </div>
  )
}
