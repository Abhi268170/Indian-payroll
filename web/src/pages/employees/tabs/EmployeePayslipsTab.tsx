import { useQuery } from '@tanstack/react-query'
import { FileText, Download } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'

async function downloadPayslip(payslipId: string, fileName: string): Promise<void> {
  const res = await api.get<Blob>(`/api/v1/payslips/${payslipId}/download`, { responseType: 'blob' })
  const url = URL.createObjectURL(res.data)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}

interface Props {
  employeeId: string
}

interface EmployeePayslipDto {
  id: string
  payPeriodYear: number
  payPeriodMonth: number
  generatedAt: string
  pdfStorageKey: string
  isPublished: boolean
  netPay: number
}

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

function formatPeriod(year: number, month: number): string {
  return `${MONTH_NAMES[month - 1] ?? 'Unknown'} ${year}`
}

function formatDateTime(iso: string): string {
  const d = new Date(iso)
  const date = d.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' })
  const time = d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
  return `${date}, ${time}`
}

export default function EmployeePayslipsTab({ employeeId }: Props): React.ReactElement {
  const { data, isLoading, error } = useQuery<EmployeePayslipDto[]>({
    queryKey: ['employee-payslips', employeeId],
    queryFn: () =>
      api.get<EmployeePayslipDto[]>(`/api/v1/employees/${employeeId}/payslips`).then(r => r.data),
  })

  if (isLoading) {
    return (
      <div className="p-8 text-center text-[13px] text-[var(--color-text-secondary)]">
        Loading…
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-8 text-center text-[13px] text-red-600">
        Failed to load payslips.
      </div>
    )
  }

  if (!data || data.length === 0) {
    return (
      <div className="p-12 flex flex-col items-center gap-3">
        <FileText className="w-8 h-8 text-[var(--color-text-secondary)] opacity-40" />
        <p className="text-[13px] text-[var(--color-text-secondary)]">No payslips available yet</p>
      </div>
    )
  }

  return (
    <div className="p-5">
      <table className="w-full text-[13px] border-collapse">
        <thead>
          <tr className="border-b border-[var(--color-border)]">
            <th className="text-left py-2.5 px-3 text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              Pay Period
            </th>
            <th className="text-left py-2.5 px-3 text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              Net Pay
            </th>
            <th className="text-left py-2.5 px-3 text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              Generated
            </th>
            <th className="text-left py-2.5 px-3 text-[11px] font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              Status
            </th>
            <th className="py-2.5 px-3" />
          </tr>
        </thead>
        <tbody>
          {data.map(p => (
            <tr
              key={p.id}
              className="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-surface-hover,#f8fafc)] transition-colors"
            >
              <td className="py-3 px-3 text-[var(--color-text-primary)] font-medium">
                {formatPeriod(p.payPeriodYear, p.payPeriodMonth)}
              </td>
              <td className="py-3 px-3 text-[var(--color-text-primary)]">
                {formatINR(p.netPay)}
              </td>
              <td className="py-3 px-3 text-[var(--color-text-secondary)]">
                {formatDateTime(p.generatedAt)}
              </td>
              <td className="py-3 px-3">
                {p.isPublished ? (
                  <span className="inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                    Published
                  </span>
                ) : (
                  <span className="inline-flex items-center h-5 px-2 rounded-full text-[11px] font-medium bg-gray-100 text-gray-600 border border-gray-200">
                    Draft
                  </span>
                )}
              </td>
              <td className="py-3 px-3 text-right">
                {p.isPublished ? (
                  <button
                    title="Download payslip"
                    className="inline-flex items-center gap-1.5 h-7 px-3 text-[12px] font-medium text-[var(--color-primary)] border border-[var(--color-primary)]/30 rounded-lg hover:bg-[var(--color-primary)]/5 transition-colors"
                    onClick={() => {
                      void downloadPayslip(p.id, `Payslip_${formatPeriod(p.payPeriodYear, p.payPeriodMonth)}.pdf`)
                    }}
                  >
                    <Download className="w-3.5 h-3.5" />
                    Download
                  </button>
                ) : (
                  <button
                    disabled
                    title="Payslip not yet published"
                    className="inline-flex items-center gap-1.5 h-7 px-3 text-[12px] font-medium text-[var(--color-text-secondary)] border border-[var(--color-border)] rounded-lg opacity-50 cursor-not-allowed"
                  >
                    <Download className="w-3.5 h-3.5" />
                    Download
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
