import { useState } from 'react'
import { AlertTriangle, AlertCircle, ChevronDown, ChevronUp } from 'lucide-react'
import type { PendingTasksDto } from '@/types/api'

interface PendingTasksBannerProps {
  tasks: PendingTasksDto
}

export default function PendingTasksBanner({ tasks }: PendingTasksBannerProps): React.ReactElement | null {
  const [expanded, setExpanded] = useState(true)

  if (tasks.hardBlocks.length === 0 && tasks.softWarnings.length === 0) return null

  return (
    <div className="mb-4 rounded-xl border overflow-hidden border-[var(--color-border)]">
      <button
        onClick={() => { setExpanded(e => !e) }}
        className="w-full flex items-center justify-between px-4 py-3 bg-amber-50 border-b border-amber-100 text-left"
      >
        <div className="flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 text-amber-600" />
          <span className="text-[13px] font-medium text-amber-800">
            {tasks.hardBlocks.length > 0
              ? `${String(tasks.hardBlocks.length)} blocking issue${tasks.hardBlocks.length > 1 ? 's' : ''} must be resolved before approval`
              : `${String(tasks.softWarnings.length)} warning${tasks.softWarnings.length > 1 ? 's' : ''} — review before approving`}
          </span>
        </div>
        {expanded ? <ChevronUp className="w-4 h-4 text-amber-600" /> : <ChevronDown className="w-4 h-4 text-amber-600" />}
      </button>

      {expanded && (
        <div className="bg-white divide-y divide-[var(--color-border)]">
          {tasks.hardBlocks.map(item => (
            <div key={item.employeeId} className="flex items-center gap-3 px-4 py-2.5">
              <AlertCircle className="w-3.5 h-3.5 text-red-500 flex-shrink-0" />
              <span className="text-[12px] font-medium text-[var(--color-text-primary)]">{item.employeeCode}</span>
              <span className="text-[12px] text-red-600">{item.reason}</span>
            </div>
          ))}
          {tasks.softWarnings.map(item => (
            <div key={item.employeeId} className="flex items-center gap-3 px-4 py-2.5">
              <AlertTriangle className="w-3.5 h-3.5 text-amber-500 flex-shrink-0" />
              <span className="text-[12px] font-medium text-[var(--color-text-primary)]">{item.employeeCode}</span>
              <span className="text-[12px] text-amber-600">{item.reason}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
