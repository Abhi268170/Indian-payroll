import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { X, Download, Send } from 'lucide-react'
import { api } from '@/lib/api'
import { formatINR } from '@/lib/format'
import type { PayslipData, PayslipComponentDto } from '@/types/api'
import PayslipDownloadDialog from './PayslipDownloadDialog'

interface PayslipPanelProps {
  runId: string
  employeeId: string
  employeeName: string
  onClose: () => void
}

export default function PayslipPanel({ runId, employeeId, employeeName, onClose }: PayslipPanelProps): React.ReactElement {
  const [showDownload, setShowDownload] = useState(false)

  const { data, isLoading } = useQuery<PayslipData>({
    queryKey: ['payslip-data', runId, employeeId],
    queryFn: () => api.get<PayslipData>(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/payslip`).then(r => r.data),
  })

  const sendMutation = useMutation({
    mutationFn: () => api.post(`/api/v1/payroll-runs/${runId}/employees/${employeeId}/payslip/send`, {}),
  })

  const earnings = data?.components.filter((c: PayslipComponentDto) => c.isEarning) ?? []
  const deductions = data?.components.filter((c: PayslipComponentDto) => !c.isEarning) ?? []

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      <div className="absolute inset-0 bg-black/20" onClick={() => { onClose() }} />

      <div className="relative w-[520px] h-full bg-white shadow-2xl flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-[var(--color-border)]">
          <div>
            <h3 className="text-[14px] font-semibold text-[var(--color-text-primary)]">{employeeName}</h3>
            <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">{data?.periodLabel ?? '—'}</p>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => { setShowDownload(true) }}
              className="h-7 px-2.5 flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
            >
              <Download className="w-3.5 h-3.5" />
              Download
            </button>
            <button
              onClick={() => { sendMutation.mutate() }}
              disabled={sendMutation.isPending}
              className="h-7 px-2.5 flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] text-[12px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)] disabled:opacity-60"
            >
              <Send className="w-3.5 h-3.5" />
              {sendMutation.isPending ? 'Sending…' : 'Send'}
            </button>
            <button onClick={() => { onClose() }} className="w-7 h-7 flex items-center justify-center rounded-md hover:bg-[var(--color-page-bg)]">
              <X className="w-4 h-4 text-[var(--color-text-secondary)]" />
            </button>
          </div>
        </div>

        {isLoading && (
          <div className="flex-1 flex items-center justify-center">
            <span className="inline-block w-5 h-5 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
          </div>
        )}

        {data && (
          <div className="flex-1 overflow-y-auto">
            {/* Company header */}
            <div className="bg-[#1e293b] px-5 py-4 text-white">
              <p className="text-[15px] font-semibold">{data.companyName}</p>
              {data.companyAddress && <p className="text-[12px] opacity-70 mt-0.5">{data.companyAddress}</p>}
              <p className="text-[12px] opacity-70 mt-1">Pay Slip for {data.periodLabel}</p>
            </div>

            {/* Payment info banner */}
            <div className="bg-emerald-50 border-b border-emerald-100 px-5 py-3 flex items-center justify-between">
              <div>
                <p className="text-[11px] text-emerald-600 font-medium uppercase tracking-wide">Net Pay</p>
                <p className="text-[20px] font-bold text-emerald-700">{formatINR(data.netPay)}</p>
                <p className="text-[11px] text-emerald-600">{data.netPayInWords}</p>
              </div>
              {data.maskedBankAccount && (
                <div className="text-right">
                  <p className="text-[11px] text-[var(--color-text-secondary)]">Bank Account</p>
                  <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{data.maskedBankAccount}</p>
                  {data.ifscCode && <p className="text-[11px] text-[var(--color-text-secondary)]">{data.ifscCode}</p>}
                </div>
              )}
            </div>

            <div className="px-5 py-4 space-y-5">
              {/* Employee details */}
              <section>
                <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                  <div>
                    <p className="text-[11px] text-[var(--color-text-secondary)]">Employee Code</p>
                    <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{data.employeeCode}</p>
                  </div>
                  <div>
                    <p className="text-[11px] text-[var(--color-text-secondary)]">Designation</p>
                    <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{data.designation}</p>
                  </div>
                  <div>
                    <p className="text-[11px] text-[var(--color-text-secondary)]">Department</p>
                    <p className="text-[13px] font-medium text-[var(--color-text-primary)]">{data.department}</p>
                  </div>
                  <div>
                    <p className="text-[11px] text-[var(--color-text-secondary)]">Pay Day</p>
                    <p className="text-[13px] font-medium text-[var(--color-text-primary)]">
                      {data.payDay ? new Date(data.payDay).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'}
                    </p>
                  </div>
                </div>
              </section>

              {/* Earnings table */}
              <section>
                <h4 className="text-[12px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-2">Earnings</h4>
                <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                  <table className="w-full">
                    <thead>
                      <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                        <th className="px-3 py-2 text-left text-[11px] font-semibold text-[var(--color-text-secondary)]">Component</th>
                        <th className="px-3 py-2 text-right text-[11px] font-semibold text-[var(--color-text-secondary)]">Amount</th>
                        <th className="px-3 py-2 text-right text-[11px] font-semibold text-[var(--color-text-secondary)]">YTD</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[var(--color-border)]">
                      {earnings.map((c: PayslipComponentDto) => (
                        <tr key={c.componentCode}>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">{c.componentName}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(c.amount)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">{formatINR(c.ytdAmount)}</td>
                        </tr>
                      ))}
                      <tr className="bg-[var(--color-page-bg)] font-semibold">
                        <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Gross Pay</td>
                        <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.grossPay)}</td>
                        <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">{formatINR(data.ytdGross)}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>

              {/* Deductions table */}
              <section>
                <h4 className="text-[12px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-2">Deductions</h4>
                <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                  <table className="w-full">
                    <thead>
                      <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                        <th className="px-3 py-2 text-left text-[11px] font-semibold text-[var(--color-text-secondary)]">Component</th>
                        <th className="px-3 py-2 text-right text-[11px] font-semibold text-[var(--color-text-secondary)]">Amount</th>
                        <th className="px-3 py-2 text-right text-[11px] font-semibold text-[var(--color-text-secondary)]">YTD</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[var(--color-border)]">
                      {deductions.map((c: PayslipComponentDto) => (
                        <tr key={c.componentCode}>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">{c.componentName}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(c.amount)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">{formatINR(c.ytdAmount)}</td>
                        </tr>
                      ))}
                      {data.employeePf > 0 && (
                        <tr className="divide-y divide-[var(--color-border)]">
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Employee PF</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.employeePf)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">{formatINR(data.ytdPf)}</td>
                        </tr>
                      )}
                      {data.employeeEsi > 0 && (
                        <tr>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Employee ESI</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.employeeEsi)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">—</td>
                        </tr>
                      )}
                      {data.ptAmount > 0 && (
                        <tr>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Professional Tax</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.ptAmount)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">—</td>
                        </tr>
                      )}
                      {data.lwfEmployeeAmount > 0 && (
                        <tr>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Labour Welfare Fund</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.lwfEmployeeAmount)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">—</td>
                        </tr>
                      )}
                      {data.tdsAmount > 0 && (
                        <tr>
                          <td className="px-3 py-2 text-[13px] text-[var(--color-text-primary)]">Income Tax (TDS)</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-primary)]">{formatINR(data.tdsAmount)}</td>
                          <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">{formatINR(data.ytdTds)}</td>
                        </tr>
                      )}
                      <tr className="bg-[var(--color-page-bg)]">
                        <td className="px-3 py-2 text-[13px] font-semibold text-[var(--color-text-primary)]">Total Deductions</td>
                        <td className="px-3 py-2 text-[13px] text-right font-semibold text-[var(--color-text-primary)]">
                          {formatINR(data.employeePf + data.employeeEsi + data.ptAmount + data.lwfEmployeeAmount + data.tdsAmount)}
                        </td>
                        <td className="px-3 py-2 text-[13px] text-right text-[var(--color-text-secondary)]">—</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>
          </div>
        )}

        {/* Net pay footer */}
        {data && (
          <div className="border-t border-[var(--color-border)] px-5 py-4 bg-[var(--color-page-bg)]">
            <div className="flex items-center justify-between">
              <div>
                <span className="text-[13px] font-semibold text-[var(--color-text-primary)]">Net Pay</span>
                <span className="ml-3 text-[11px] text-[var(--color-text-secondary)]">YTD: {formatINR(data.ytdNetPay)}</span>
              </div>
              <span className="text-[16px] font-bold text-[var(--color-primary)]">{formatINR(data.netPay)}</span>
            </div>
          </div>
        )}
      </div>

      {showDownload && (
        <PayslipDownloadDialog
          runId={runId}
          employeeId={employeeId}
          employeeName={employeeName}
          onClose={() => { setShowDownload(false) }}
        />
      )}
    </div>
  )
}

