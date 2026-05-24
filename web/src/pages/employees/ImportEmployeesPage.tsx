import { useCallback, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Upload, Download, X, AlertCircle, CheckCircle } from 'lucide-react'
import { api } from '@/lib/api'

function apiErrorMessage(err: unknown): string | undefined {
  if (typeof err !== 'object' || err === null) return undefined
  const resp = (err as Record<string, unknown>).response
  if (typeof resp !== 'object' || resp === null) return undefined
  const data = (resp as Record<string, unknown>).data
  if (typeof data !== 'object' || data === null) return undefined
  const msg = (data as Record<string, unknown>).error
  return typeof msg === 'string' ? msg : undefined
}

interface ImportRowError {
  rowNumber: number
  employeeNumber: string | null
  message: string
}

interface ValidationResult {
  newRows: unknown[]
  updateRows: unknown[]
  skippedRows: unknown[]
  errors: ImportRowError[]
}

interface CommitResult {
  created: number
  updated: number
  skipped: number
}

type PageState =
  | { phase: 'idle' }
  | { phase: 'validating' }
  | { phase: 'validated'; result: ValidationResult; file: File; overwrite: boolean }
  | { phase: 'importing'; file: File; overwrite: boolean }
  | { phase: 'done'; result: CommitResult }
  | { phase: 'error'; message: string }

export default function ImportEmployeesPage(): React.ReactElement {
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [state, setState] = useState<PageState>({ phase: 'idle' })
  const [overwrite, setOverwrite] = useState(false)
  const [dragOver, setDragOver] = useState(false)

  const validate = useCallback(async (file: File, overwriteExisting: boolean): Promise<void> => {
    setState({ phase: 'validating' })
    try {
      const form = new FormData()
      form.append('file', file)
      form.append('overwriteExisting', String(overwriteExisting))
      const res = await api.post<ValidationResult>('/api/v1/employees/import/validate', form)
      setState({ phase: 'validated', result: res.data, file, overwrite: overwriteExisting })
    } catch (err: unknown) {
      setState({ phase: 'error', message: apiErrorMessage(err) ?? 'Failed to validate the file. Check the format and try again.' })
    }
  }, [])

  const onFileSelected = useCallback((file: File): void => {
    if (!file.name.endsWith('.xlsx')) {
      setState({ phase: 'error', message: 'Only .xlsx files are accepted. Download the template above.' })
      return
    }
    void validate(file, overwrite)
  }, [overwrite, validate])

  const onDrop = useCallback((e: React.DragEvent<HTMLDivElement>): void => {
    e.preventDefault()
    setDragOver(false)
    const file = e.dataTransfer.files[0]
    if (file) onFileSelected(file)
  }, [onFileSelected])

  const onInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>): void => {
    const file = e.target.files?.[0]
    if (file) onFileSelected(file)
    e.target.value = ''
  }, [onFileSelected])

  const onOverwriteChange = useCallback((checked: boolean): void => {
    setOverwrite(checked)
    if (state.phase === 'validated') {
      void validate(state.file, checked)
    }
  }, [state, validate])

  const onRemoveFile = useCallback((): void => {
    setState({ phase: 'idle' })
  }, [])

  const onImport = useCallback(async (): Promise<void> => {
    if (state.phase !== 'validated') return
    const { file, overwrite: ow } = state
    setState({ phase: 'importing', file, overwrite: ow })
    try {
      const form = new FormData()
      form.append('file', file)
      form.append('overwriteExisting', String(ow))
      const res = await api.post<CommitResult>('/api/v1/employees/import/commit', form)
      setState({ phase: 'done', result: res.data })
    } catch (err: unknown) {
      setState({ phase: 'error', message: apiErrorMessage(err) ?? 'Import failed. Please try again.' })
    }
  }, [state])

  const downloadErrorReport = useCallback((): void => {
    if (state.phase !== 'validated') return
    const { errors } = state.result
    const lines = [
      'Row,EmployeeNumber,Error',
      ...errors.map(e => `${String(e.rowNumber)},"${e.employeeNumber ?? ''}","${e.message.replace(/"/g, '""')}"`),
    ]
    const blob = new Blob([lines.join('\n')], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'import-errors.csv'
    a.click()
    URL.revokeObjectURL(url)
  }, [state])

  const readyCount = state.phase === 'validated'
    ? state.result.newRows.length + state.result.updateRows.length
    : 0
  const hasErrors = state.phase === 'validated' && state.result.errors.length > 0
  const isValidating = state.phase === 'validating' || state.phase === 'importing'

  return (
    <div className="p-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl font-semibold text-[var(--color-text-primary)]">Import Employees</h1>
          <p className="text-sm text-[var(--color-text-secondary)] mt-0.5">
            Upload an XLSX file to add or update multiple employees at once.
          </p>
        </div>
        <button
          onClick={() => {
            void api.get<Blob>('/api/v1/employees/import/template', { responseType: 'blob' }).then(res => {
              const url = URL.createObjectURL(res.data)
              const a = document.createElement('a')
              a.href = url
              a.download = 'employee-import-template.xlsx'
              a.click()
              URL.revokeObjectURL(url)
            })
          }}
          className="flex items-center gap-1.5 px-3 py-2 bg-white border border-[var(--color-border)] text-sm font-medium text-[var(--color-text-secondary)] rounded-md hover:border-[var(--color-border-strong)]"
        >
          <Download size={14} />
          Download Template
        </button>
      </div>

      {/* Done screen */}
      {state.phase === 'done' && (
        <div className="bg-white rounded-lg border border-[var(--color-border)] p-10 text-center">
          <CheckCircle size={40} className="mx-auto mb-4 text-[var(--color-success)]" />
          <div className="text-lg font-semibold text-[var(--color-text-primary)] mb-1">
            Import Complete
          </div>
          <div className="text-sm text-[var(--color-text-secondary)] mb-6">
            {state.result.created > 0 && <span>{state.result.created} employee{state.result.created !== 1 ? 's' : ''} added. </span>}
            {state.result.updated > 0 && <span>{state.result.updated} updated. </span>}
            {state.result.skipped > 0 && <span>{state.result.skipped} skipped (already exist). </span>}
          </div>
          <div className="flex gap-3 justify-center">
            <button
              onClick={() => { void navigate('/employees') }}
              className="px-4 py-2 bg-[var(--color-primary)] text-white text-sm font-medium rounded-md hover:bg-[var(--color-primary-hover)]"
            >
              View Employees
            </button>
            <button
              onClick={() => { setState({ phase: 'idle' }) }}
              className="px-4 py-2 bg-white border border-[var(--color-border)] text-sm font-medium text-[var(--color-text-secondary)] rounded-md hover:border-[var(--color-border-strong)]"
            >
              Import More
            </button>
          </div>
        </div>
      )}

      {/* Upload + validation screen */}
      {state.phase !== 'done' && (
        <div className="bg-white rounded-lg border border-[var(--color-border)] p-6 space-y-5">

          {/* File area */}
          {state.phase === 'idle' || state.phase === 'error' ? (
            <div
              onDrop={onDrop}
              onDragOver={e => { e.preventDefault(); setDragOver(true) }}
              onDragLeave={() => { setDragOver(false) }}
              onClick={() => { fileInputRef.current?.click() }}
              className={`
                border-2 border-dashed rounded-lg p-10 text-center cursor-pointer transition-colors
                ${dragOver
                  ? 'border-[var(--color-primary)] bg-[var(--color-primary-light)]'
                  : 'border-[var(--color-border-strong)] hover:border-[var(--color-primary)] hover:bg-[var(--color-primary-light)]'}
              `}
            >
              <Upload size={28} className="mx-auto mb-3 text-[var(--color-text-muted)]" />
              <div className="text-sm font-medium text-[var(--color-text-primary)]">
                Drag and drop your XLSX file here, or click to browse
              </div>
              <div className="text-xs text-[var(--color-text-muted)] mt-1">
                Only .xlsx files. Download the template above — do not rename columns.
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept=".xlsx"
                className="hidden"
                onChange={onInputChange}
              />
            </div>
          ) : (
            <div className="flex items-center gap-3 px-4 py-3 bg-[var(--color-page-bg)] rounded-lg border border-[var(--color-border)]">
              <div className="flex-1 text-sm font-medium text-[var(--color-text-primary)]">
                {state.phase === 'validating' || state.phase === 'importing'
                  ? 'Processing...'
                  : (state as { file: File }).file.name}
              </div>
              {(state.phase === 'validated') && (
                <button
                  onClick={onRemoveFile}
                  className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]"
                >
                  <X size={16} />
                </button>
              )}
            </div>
          )}

          {/* Overwrite checkbox */}
          <label className="flex items-center gap-2 cursor-pointer w-fit">
            <input
              type="checkbox"
              checked={overwrite}
              onChange={e => { onOverwriteChange(e.target.checked) }}
              disabled={isValidating}
              className="w-4 h-4 rounded border-[var(--color-border-strong)] accent-[var(--color-primary)]"
            />
            <span className="text-sm text-[var(--color-text-secondary)]">
              Update existing employees
              <span className="text-[var(--color-text-muted)] ml-1">(unchecked = skip duplicates)</span>
            </span>
          </label>

          {/* Validating spinner */}
          {(state.phase === 'validating' || state.phase === 'importing') && (
            <div className="text-sm text-[var(--color-text-secondary)] flex items-center gap-2">
              <svg className="animate-spin w-4 h-4 text-[var(--color-primary)]" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
              </svg>
              {state.phase === 'validating' ? 'Checking your file...' : 'Importing employees...'}
            </div>
          )}

          {/* Error banner */}
          {state.phase === 'error' && (
            <div className="flex items-start gap-2 p-3 bg-[var(--color-error-bg)] border border-red-200 rounded-lg text-sm text-[var(--color-error)]">
              <AlertCircle size={16} className="mt-0.5 shrink-0" />
              {state.message}
            </div>
          )}

          {/* Validation summary */}
          {state.phase === 'validated' && (
            <div className="space-y-4">
              <div className="flex gap-3 flex-wrap">
                {readyCount > 0 && (
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-[var(--color-success-bg)] text-[var(--color-success)] text-sm font-medium rounded-full">
                    <CheckCircle size={13} />
                    {readyCount} {readyCount === 1 ? 'row' : 'rows'} ready
                  </span>
                )}
                {state.result.skippedRows.length > 0 && (
                  <span className="inline-flex items-center px-3 py-1 bg-[var(--color-badge-grey-bg)] text-[var(--color-text-secondary)] text-sm font-medium rounded-full">
                    {state.result.skippedRows.length} will be skipped (already exist)
                  </span>
                )}
                {state.result.errors.length > 0 && (
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-[var(--color-error-bg)] text-[var(--color-error)] text-sm font-medium rounded-full">
                    <AlertCircle size={13} />
                    {state.result.errors.length} {state.result.errors.length === 1 ? 'row has' : 'rows have'} errors
                  </span>
                )}
              </div>

              {/* Error table */}
              {state.result.errors.length > 0 && (
                <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-[var(--color-page-bg)] border-b border-[var(--color-border)]">
                        <th className="text-left px-3 py-2.5 text-xs font-semibold text-[var(--color-text-secondary)] w-14">Row</th>
                        <th className="text-left px-3 py-2.5 text-xs font-semibold text-[var(--color-text-secondary)] w-32">Employee No</th>
                        <th className="text-left px-3 py-2.5 text-xs font-semibold text-[var(--color-text-secondary)]">Error</th>
                      </tr>
                    </thead>
                    <tbody>
                      {state.result.errors.map((err, i) => (
                        <tr key={i} className="border-b border-[var(--color-border)] last:border-0">
                          <td className="px-3 py-2.5 text-[var(--color-text-secondary)]">{err.rowNumber}</td>
                          <td className="px-3 py-2.5 text-[var(--color-text-secondary)]">{err.employeeNumber ?? '—'}</td>
                          <td className="px-3 py-2.5 text-[var(--color-text-primary)]">{err.message}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {/* Actions */}
              <div className="flex items-center gap-3 pt-1">
                {state.result.errors.length > 0 && (
                  <button
                    onClick={downloadErrorReport}
                    className="flex items-center gap-1.5 px-3 py-2 bg-white border border-[var(--color-border)] text-sm font-medium text-[var(--color-text-secondary)] rounded-md hover:border-[var(--color-border-strong)]"
                  >
                    <Download size={14} />
                    Download Error Report
                  </button>
                )}
                <button
                  onClick={() => void onImport()}
                  disabled={hasErrors || readyCount === 0}
                  className={`
                    px-4 py-2 text-sm font-medium rounded-md transition-colors
                    ${hasErrors || readyCount === 0
                      ? 'bg-[var(--color-border)] text-[var(--color-text-disabled)] cursor-not-allowed'
                      : 'bg-[var(--color-primary)] text-white hover:bg-[var(--color-primary-hover)]'}
                  `}
                >
                  Import {readyCount} {readyCount === 1 ? 'Employee' : 'Employees'}
                </button>
                {hasErrors && (
                  <span className="text-xs text-[var(--color-text-muted)]">
                    Fix all errors to enable import
                  </span>
                )}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
