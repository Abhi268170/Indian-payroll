import { useState } from 'react'
import { Navigate, useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { PayrollRunSummaryDto, PayrunEmployeeDto, PendingTasksDto } from '@/types/api'
import { Pagination, usePersistedPageSize } from '@/components/ui/Pagination'

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
import PayRunHeader from './components/PayRunHeader'
import PendingTasksBanner from './components/PendingTasksBanner'
import EmployeeSummaryTable from './components/EmployeeSummaryTable'
import VariableInputsPanel from './components/VariableInputsPanel'
import ApprovePayrollDialog from './components/ApprovePayrollDialog'
import RejectApprovalDialog from './components/RejectApprovalDialog'
import RecordPaymentDialog from './components/RecordPaymentDialog'
import DeletePaymentDialog from './components/DeletePaymentDialog'
import SkipEmployeeDialog from './components/SkipEmployeeDialog'
import PayslipPanel from './components/PayslipPanel'
import BankAdviceModal from './components/BankAdviceModal'
import ImportModal, { type ImportType } from './components/ImportModal'
import ExportModal from './components/ExportModal'
import PayRunTaxesTab from './tabs/PayRunTaxesTab'

type Tab = 'employees' | 'taxes' | 'insights'

interface VariableInputsState {
  employeeId: string
  employeeName: string
}

interface PayslipState {
  employeeId: string
  employeeName: string
}

interface SkipState {
  employeeId: string
  employeeName: string
}

const TABS: { key: Tab; label: string }[] = [
  { key: 'employees', label: 'Employee Summary' },
  { key: 'taxes', label: 'Taxes & Deductions' },
  { key: 'insights', label: 'Overall Insights' },
]

export default function PayRunDetailPage(): React.ReactElement {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [activeTab, setActiveTab] = useState<Tab>('employees')
  const [showMenu, setShowMenu] = useState(false)
  const [variableInputs, setVariableInputs] = useState<VariableInputsState | null>(null)
  const [payslipState, setPayslipState] = useState<PayslipState | null>(null)
  const [showApprove, setShowApprove] = useState(false)
  const [showReject, setShowReject] = useState(false)
  const [showRecordPayment, setShowRecordPayment] = useState(false)
  const [showDeletePayment, setShowDeletePayment] = useState(false)
  const [showBankAdvice, setShowBankAdvice] = useState(false)
  const [skipState, setSkipState] = useState<SkipState | null>(null)
  const [importType, setImportType] = useState<ImportType | null>(null)
  const [showExport, setShowExport] = useState(false)

  const runId = id ?? ''

  const { data: run, isLoading: runLoading, error: runError } = useQuery<PayrollRunSummaryDto>({
    queryKey: ['payroll-run', runId],
    queryFn: () => api.get<PayrollRunSummaryDto>(`/api/v1/payroll-runs/${runId}`).then(r => r.data),
    enabled: runId !== '',
    retry: false,
  })

  // Deep-link safety net (plan §4.5): API returns 404 for run ids absent from this
  // tenant — cross-tenant access surfaces as 404 too because schema-per-tenant +
  // JWT tenant_id binding routes the query through the wrong schema and finds
  // nothing. Bounce to the list rather than showing a broken detail shell.
  const runStatusCode = (runError as { response?: { status?: number } } | null)?.response?.status
  if (runStatusCode === 404) {
    return <Navigate to="/pay-runs" replace />
  }

  const [empPage, setEmpPage] = useState(1)
  const [empPageSize, setEmpPageSize] = usePersistedPageSize('payrun-employees', 25)

  const { data: employeesData } = useQuery<PagedResult<PayrunEmployeeDto>>({
    queryKey: ['run-employees', runId, empPage, empPageSize],
    queryFn: () => api.get<PagedResult<PayrunEmployeeDto>>(`/api/v1/payroll-runs/${runId}/employees`, {
      params: { page: empPage, pageSize: empPageSize },
    }).then(r => r.data),
    enabled: !!run,
    placeholderData: keepPreviousData,
  })
  const employees = employeesData?.items ?? []
  const employeesTotal = employeesData?.total ?? 0

  const { data: pendingTasks } = useQuery<PendingTasksDto>({
    queryKey: ['pending-tasks', runId],
    queryFn: () => api.get<PendingTasksDto>(`/api/v1/payroll-runs/${runId}/pending-tasks`).then(r => r.data),
    enabled: run?.status === 'Draft',
  })

  const deleteMutation = useMutation({
    mutationFn: () => api.delete(`/api/v1/payroll-runs/${runId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payroll-runs'] })
      void navigate('/pay-runs', { replace: true })
    },
  })

  const reEvaluateMutation = useMutation({
    mutationFn: () => api.post(`/api/v1/payroll-runs/${runId}/re-evaluate`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
      void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
      void queryClient.invalidateQueries({ queryKey: ['pending-tasks', runId] })
    },
  })

  function handleDownloadPayslip(employeeId: string, employeeName: string): void {
    setPayslipState({ employeeId, employeeName })
  }

  if (runLoading || !run) {
    return (
      <div className="flex items-center justify-center h-64">
        <span className="inline-block w-6 h-6 border-2 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <div>
      <PayRunHeader
        run={run}
        onApprove={() => { setShowApprove(true) }}
        onDelete={() => { deleteMutation.mutate() }}
        onRecordPayment={() => { setShowRecordPayment(true) }}
        onDeletePayment={() => { setShowDeletePayment(true) }}
        onRejectApproval={() => { setShowReject(true) }}
        onBankAdvice={() => { setShowBankAdvice(true) }}
        showMenu={showMenu}
        onToggleMenu={() => { setShowMenu(v => !v) }}
      />

      {pendingTasks && <PendingTasksBanner tasks={pendingTasks} />}

      {/* Tabs */}
      <div className="flex items-center gap-1 mb-4 border-b border-[var(--color-border)]">
        {TABS.map(tab => (
          <button
            key={tab.key}
            onClick={() => { setActiveTab(tab.key) }}
            className={`h-9 px-4 text-[13px] font-medium border-b-2 -mb-px transition-colors ${
              activeTab === tab.key
                ? 'border-[var(--color-primary)] text-[var(--color-primary)]'
                : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === 'employees' && (
        <>
          <EmployeeSummaryTable
            employees={employees}
            runStatus={run.status}
            runId={runId}
            onOpenVariableInputs={(empId, empName) => { setVariableInputs({ employeeId: empId, employeeName: empName }) }}
            onSkipEmployee={(empId, empName) => { setSkipState({ employeeId: empId, employeeName: empName }) }}
            onDownloadPayslip={handleDownloadPayslip}
            onReEvaluate={() => { reEvaluateMutation.mutate() }}
            isReEvaluating={reEvaluateMutation.isPending}
            onShowImport={type => { setImportType(type) }}
            onShowExport={() => { setShowExport(true) }}
          />
          <Pagination
            page={empPage}
            pageSize={empPageSize}
            total={employeesTotal}
            onPageChange={setEmpPage}
            onPageSizeChange={s => { setEmpPageSize(s); setEmpPage(1) }}
          />
        </>
      )}

      {activeTab === 'taxes' && <PayRunTaxesTab runId={runId} />}

      {activeTab === 'insights' && (
        <div className="flex items-center justify-center h-48 rounded-xl border border-dashed border-[var(--color-border)]">
          <p className="text-[13px] text-[var(--color-text-secondary)]">Overall Insights — coming soon</p>
        </div>
      )}

      {variableInputs && (
        <VariableInputsPanel
          runId={runId}
          employeeId={variableInputs.employeeId}
          employeeName={variableInputs.employeeName}
          onClose={() => { setVariableInputs(null) }}
        />
      )}

      {payslipState && (
        <PayslipPanel
          runId={runId}
          employeeId={payslipState.employeeId}
          employeeName={payslipState.employeeName}
          onClose={() => { setPayslipState(null) }}
        />
      )}

      {showApprove && (
        <ApprovePayrollDialog run={run} onClose={() => { setShowApprove(false) }} />
      )}

      {showReject && (
        <RejectApprovalDialog runId={runId} onClose={() => { setShowReject(false) }} />
      )}

      {showRecordPayment && (
        <RecordPaymentDialog run={run} onClose={() => { setShowRecordPayment(false) }} />
      )}

      {showDeletePayment && (
        <DeletePaymentDialog runId={runId} periodLabel={run.periodLabel} onClose={() => { setShowDeletePayment(false) }} />
      )}

      {showBankAdvice && (
        <BankAdviceModal runId={runId} periodLabel={run.periodLabel} onClose={() => { setShowBankAdvice(false) }} />
      )}

      {skipState && (
        <SkipEmployeeDialog
          runId={runId}
          employeeId={skipState.employeeId}
          employeeName={skipState.employeeName}
          periodLabel={run.periodLabel}
          onClose={() => { setSkipState(null) }}
        />
      )}

      {importType && (
        <ImportModal
          runId={runId}
          importType={importType}
          onClose={() => { setImportType(null) }}
          onSuccess={() => {
            void queryClient.invalidateQueries({ queryKey: ['run-employees', runId] })
            void queryClient.invalidateQueries({ queryKey: ['payroll-run', runId] })
          }}
        />
      )}

      {showExport && (
        <ExportModal
          runId={runId}
          periodLabel={run.periodLabel}
          onClose={() => { setShowExport(false) }}
        />
      )}
    </div>
  )
}
