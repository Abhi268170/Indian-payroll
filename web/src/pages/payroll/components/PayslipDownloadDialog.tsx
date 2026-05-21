import { useState } from 'react'
import { api } from '@/lib/api'

interface PayslipDownloadDialogProps {
  runId: string
  employeeId: string
  employeeName: string
  onClose: () => void
}

export default function PayslipDownloadDialog({ runId, employeeId, employeeName, onClose }: PayslipDownloadDialogProps): React.ReactElement {
  const [passwordProtected, setPasswordProtected] = useState(true)

  function handleDownload(): void {
    void api.get<Blob>(
      `/api/v1/payroll-runs/${runId}/employees/${employeeId}/payslip/pdf`,
      { responseType: 'blob' },
    ).then(res => {
      const url = URL.createObjectURL(res.data)
      const a = document.createElement('a')
      a.href = url
      a.download = `${employeeName}-payslip.pdf`
      a.click()
      URL.revokeObjectURL(url)
    })
    onClose()
  }

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div className="relative w-[380px] bg-white rounded-xl shadow-xl flex flex-col overflow-hidden">
        <div className="px-6 py-5 border-b border-[var(--color-border)]">
          <h2 className="text-[16px] font-semibold text-[var(--color-text-primary)]">Download Payslip</h2>
          <p className="text-[13px] text-[var(--color-text-secondary)] mt-1">{employeeName}</p>
        </div>

        <div className="px-6 py-5">
          <label className="flex items-start gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={passwordProtected}
              onChange={e => { setPasswordProtected(e.target.checked) }}
              className="mt-0.5 w-4 h-4 rounded border-[var(--color-border)] accent-[var(--color-primary)]"
            />
            <div>
              <p className="text-[13px] font-medium text-[var(--color-text-primary)]">Password protect PDF</p>
              <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
                The payslip will be protected with the employee's date of birth (DDMMYYYY).
              </p>
            </div>
          </label>
        </div>

        <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
          <button
            onClick={onClose}
            className="h-8 px-4 rounded-lg border border-[var(--color-border)] text-[13px] text-[var(--color-text-secondary)] hover:bg-[var(--color-page-bg)]"
          >
            Cancel
          </button>
          <button
            onClick={handleDownload}
            className="h-8 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)]"
          >
            Download
          </button>
        </div>
      </div>
    </div>
  )
}
