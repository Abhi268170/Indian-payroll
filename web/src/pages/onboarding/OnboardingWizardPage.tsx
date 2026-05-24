import { type ReactElement, useEffect } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { Check, Circle, AlertTriangle, ArrowLeft, ArrowRight, ExternalLink } from 'lucide-react'
import { useOnboardingStatus, stepLabel, type OnboardingStepId, type OnboardingStepDto } from '@/hooks/useOnboardingStatus'

interface StepRouteEntry {
  id: OnboardingStepId
  // Settings page that owns the actual form. For now the wizard deep-links here in a new
  // tab; Phase C will inline the form components directly inside the wizard.
  settingsUrl: string
  ordinal: number
}

const STEPS: StepRouteEntry[] = [
  { id: 'org-profile',       settingsUrl: '/settings/org-profile',       ordinal: 1 },
  { id: 'tax-details',       settingsUrl: '/settings/tax-details',       ordinal: 2 },
  { id: 'work-locations',    settingsUrl: '/settings/work-locations',    ordinal: 3 },
  { id: 'org-structure',     settingsUrl: '/settings/departments',       ordinal: 4 },
  { id: 'pay-schedule',      settingsUrl: '/settings/pay-schedule',      ordinal: 5 },
  { id: 'statutory',         settingsUrl: '/settings/statutory',         ordinal: 6 },
  { id: 'salary-structure',  settingsUrl: '/settings/salary-structures', ordinal: 7 },
  { id: 'deductor-employee', settingsUrl: '/settings/tax-details',       ordinal: 8 },
  { id: 'first-employee',    settingsUrl: '/employees/new',              ordinal: 9 },
]

function pickActiveStepId(steps: OnboardingStepDto[]): OnboardingStepId {
  // First incomplete required step, else first incomplete optional step, else first step.
  const requiredOpen = steps.find(s => s.required && !s.complete)
  if (requiredOpen) return requiredOpen.id
  const optionalOpen = steps.find(s => !s.complete)
  if (optionalOpen) return optionalOpen.id
  return steps[0]?.id ?? 'org-profile'
}

export default function OnboardingWizardPage(): ReactElement {
  const { stepId } = useParams<{ stepId?: OnboardingStepId }>()
  const navigate = useNavigate()
  const { data: status, isLoading, error } = useOnboardingStatus()

  // Redirect to the active step if the URL has no stepId.
  useEffect(() => {
    if (!stepId && status) {
      void navigate(`/onboarding/${pickActiveStepId(status.steps)}`, { replace: true })
    }
  }, [stepId, status, navigate])

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--color-page-bg)]">
        <p className="text-[13px] text-[var(--color-text-secondary)]">Loading setup…</p>
      </div>
    )
  }
  if (error || !status) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--color-page-bg)]">
        <p className="text-[13px] text-red-600">Could not load setup status. Try refreshing.</p>
      </div>
    )
  }

  // If setup is complete and the user landed here by mistake, send them to the dashboard.
  if (status.setupComplete && !stepId) {
    return <Navigate to="/dashboard" replace />
  }

  const activeStepId: OnboardingStepId = stepId ?? pickActiveStepId(status.steps)
  const stepMeta = STEPS.find(s => s.id === activeStepId)
  const statusForStep = status.steps.find(s => s.id === activeStepId)
  const completedRequired = status.steps.filter(s => s.required && s.complete).length
  const totalRequired = status.steps.filter(s => s.required).length

  function gotoStep(next: OnboardingStepId): void {
    void navigate(`/onboarding/${next}`)
  }

  function adjacentStep(direction: -1 | 1): OnboardingStepId | null {
    if (!stepMeta) return null
    const target = STEPS.find(s => s.ordinal === stepMeta.ordinal + direction)
    return target?.id ?? null
  }

  return (
    <div className="min-h-screen bg-[var(--color-page-bg)] flex flex-col">
      {/* Top bar */}
      <header className="h-14 bg-white border-b border-[var(--color-border)] flex items-center justify-between px-6 flex-shrink-0">
        <div>
          <h1 className="text-[15px] font-semibold text-[var(--color-text-primary)]">Set up your organisation</h1>
          <p className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">
            {completedRequired} / {totalRequired} required steps complete
          </p>
        </div>
        {status.setupComplete && (
          <Link
            to="/dashboard"
            className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)]"
          >
            Go to dashboard <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        )}
      </header>

      <div className="flex-1 grid grid-cols-[260px_1fr] min-h-0">
        {/* Step rail */}
        <aside className="bg-white border-r border-[var(--color-border)] overflow-y-auto">
          <ol className="py-3">
            {STEPS.map(entry => {
              const s = status.steps.find(x => x.id === entry.id)
              const active = entry.id === activeStepId
              return (
                <li key={entry.id}>
                  <button
                    type="button"
                    onClick={() => { gotoStep(entry.id) }}
                    className={`flex items-start gap-3 w-full text-left px-4 py-2.5 text-[13px] transition-colors border-l-2 ${
                      active
                        ? 'border-[var(--color-primary)] bg-[var(--color-primary)]/5'
                        : 'border-transparent hover:bg-[var(--color-page-bg)]'
                    }`}
                  >
                    <span className="mt-0.5">
                      {s?.complete
                        ? <Check className="w-4 h-4 text-emerald-600" />
                        : active
                          ? <Circle className="w-4 h-4 text-[var(--color-primary)] fill-[var(--color-primary)]/20" />
                          : <Circle className="w-4 h-4 text-[var(--color-text-secondary)]" />}
                    </span>
                    <span className="flex-1">
                      <span className="block text-[11px] text-[var(--color-text-secondary)]">Step {entry.ordinal}</span>
                      <span className="block font-medium text-[var(--color-text-primary)]">{stepLabel(entry.id)}</span>
                      {!s?.required && (
                        <span className="block text-[10px] text-[var(--color-text-secondary)] mt-0.5">Optional</span>
                      )}
                    </span>
                  </button>
                </li>
              )
            })}
          </ol>
        </aside>

        {/* Step panel */}
        <section className="overflow-y-auto p-8">
          <div className="max-w-2xl">
            <StepPanel stepId={activeStepId} stepMeta={stepMeta} stepStatus={statusForStep} />

            <div className="mt-8 flex items-center justify-between">
              {adjacentStep(-1) ? (
                <button
                  type="button"
                  onClick={() => { gotoStep(adjacentStep(-1) as OnboardingStepId) }}
                  className="inline-flex items-center gap-1.5 h-9 px-4 text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                >
                  <ArrowLeft className="w-3.5 h-3.5" />
                  Back
                </button>
              ) : <span />}
              {adjacentStep(1) ? (
                <button
                  type="button"
                  onClick={() => { gotoStep(adjacentStep(1) as OnboardingStepId) }}
                  className="inline-flex items-center gap-1.5 h-9 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)]"
                >
                  Next step
                  <ArrowRight className="w-3.5 h-3.5" />
                </button>
              ) : (
                <Link
                  to="/dashboard"
                  className="inline-flex items-center gap-1.5 h-9 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)]"
                >
                  Finish
                  <Check className="w-3.5 h-3.5" />
                </Link>
              )}
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

interface StepPanelProps {
  stepId: OnboardingStepId
  stepMeta: StepRouteEntry | undefined
  stepStatus: OnboardingStepDto | undefined
}

function StepPanel({ stepId, stepMeta, stepStatus }: StepPanelProps): ReactElement {
  // Phase B: each step deep-links the user to the existing settings page in a new tab and
  // shows a status pill. Phase C will inline the form components directly.
  const complete = stepStatus?.complete ?? false
  const required = stepStatus?.required ?? true
  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] p-6">
      <div className="flex items-start justify-between gap-4 mb-4">
        <div>
          <h2 className="text-[18px] font-semibold text-[var(--color-text-primary)]">{stepLabel(stepId)}</h2>
          <p className="text-[12px] text-[var(--color-text-secondary)] mt-1">{stepHelp(stepId)}</p>
        </div>
        {complete ? (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700 text-[11px] font-medium">
            <Check className="w-3 h-3" /> Complete
          </span>
        ) : required ? (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-amber-50 text-amber-700 text-[11px] font-medium">
            <AlertTriangle className="w-3 h-3" /> Required
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-gray-100 text-gray-600 text-[11px] font-medium">
            Optional
          </span>
        )}
      </div>

      {stepId === 'pay-schedule' && (
        <div className="mb-4 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2.5">
          <AlertTriangle className="w-3.5 h-3.5 text-amber-600 flex-shrink-0 mt-0.5" />
          <p className="text-[12px] text-amber-800">
            Work week and salary calculation method become locked after your first payroll run. Pay date can still be changed later.
          </p>
        </div>
      )}

      {stepId === 'deductor-employee' && stepStatus?.details && (stepStatus.details as { blockedBy?: string }).blockedBy === 'first-employee' && (
        <div className="mb-4 flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 px-3 py-2.5">
          <AlertTriangle className="w-3.5 h-3.5 text-blue-600 flex-shrink-0 mt-0.5" />
          <p className="text-[12px] text-blue-800">
            Add your first employee before assigning a Tax Deductor.
          </p>
        </div>
      )}

      <p className="text-[13px] text-[var(--color-text-secondary)] mb-4">
        Open the configuration form, fill in the required fields, then come back to this checklist. Status refreshes automatically.
      </p>

      <Link
        to={stepMeta?.settingsUrl ?? '/settings'}
        target="_blank"
        rel="noopener"
        className="inline-flex items-center gap-1.5 h-9 px-4 rounded-lg bg-[var(--color-primary)] text-white text-[13px] font-medium hover:bg-[var(--color-primary-hover)]"
      >
        Open {stepLabel(stepId)} <ExternalLink className="w-3.5 h-3.5" />
      </Link>
    </div>
  )
}

function stepHelp(id: OnboardingStepId): string {
  switch (id) {
    case 'org-profile': return 'Company name, PAN, GSTIN, and registered address. Required.'
    case 'tax-details': return 'TAN, AO code, and deductor information for Form 24Q / Form 16. Optional, but skipping disables tax filings.'
    case 'work-locations': return 'At least one office with state. Drives Professional Tax and Labour Welfare Fund calculations.'
    case 'org-structure': return 'At least one department and one designation. Required for adding employees.'
    case 'pay-schedule': return 'Work week, salary calculation method, pay date, and first pay period.'
    case 'statutory': return 'Toggle EPF, ESI, Professional Tax, LWF, and Statutory Bonus. Defaults match Indian compliance.'
    case 'salary-structure': return 'At least one salary structure template (CTC breakdown).'
    case 'deductor-employee': return 'Pick the employee who signs Form 16. Required before initiating an employee exit.'
    case 'first-employee': return 'Add at least one fully onboarded employee with date of birth, father’s name, bank account, and salary structure.'
  }
}
