import { type ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, Circle, AlertTriangle, ChevronRight, Sparkles, ExternalLink } from 'lucide-react'
import { api } from '@/lib/api'
import { useOnboardingStatus, stepLabel, type OnboardingStatusDto, type OnboardingStepDto, type OnboardingStepId } from '@/hooks/useOnboardingStatus'

interface StepRoute {
  id: OnboardingStepId
  ordinal: number
  settingsUrl: string
  primaryCtaLabel: string
}

const STEPS: StepRoute[] = [
  { id: 'org-profile',       ordinal: 1, settingsUrl: '/settings/org-profile',       primaryCtaLabel: 'Complete now' },
  { id: 'tax-details',       ordinal: 2, settingsUrl: '/settings/tax-details',       primaryCtaLabel: 'Complete now' },
  { id: 'work-locations',    ordinal: 3, settingsUrl: '/settings/work-locations',    primaryCtaLabel: 'Complete now' },
  { id: 'org-structure',     ordinal: 4, settingsUrl: '/settings/departments',       primaryCtaLabel: 'Complete now' },
  { id: 'pay-schedule',      ordinal: 5, settingsUrl: '/settings/pay-schedule',      primaryCtaLabel: 'Complete now' },
  { id: 'statutory',         ordinal: 6, settingsUrl: '/settings/statutory',         primaryCtaLabel: 'Complete now' },
  { id: 'salary-structure',  ordinal: 7, settingsUrl: '/settings/salary-structures', primaryCtaLabel: 'Complete now' },
  { id: 'deductor-employee', ordinal: 8, settingsUrl: '/settings/tax-details',       primaryCtaLabel: 'Assign deductor' },
  { id: 'first-employee',    ordinal: 9, settingsUrl: '/employees/new',              primaryCtaLabel: 'Add employee' },
]

const STEPS_WITH_DEFAULTS: ReadonlySet<OnboardingStepId> = new Set<OnboardingStepId>([
  'work-locations', 'org-structure', 'salary-structure',
])

function withReturnQuery(url: string, stepId: OnboardingStepId): string {
  // Setting `?return=dashboard&step=<id>` lets SettingsLayout swap "Close Settings"
  // for "Back to setup" + show a return banner. Sidebar nav inside Settings
  // preserves the query (see SettingsLayout's sidebarQuery handling).
  const sep = url.includes('?') ? '&' : '?'
  return `${url}${sep}return=dashboard&step=${encodeURIComponent(stepId)}`
}

export default function SetupChecklistCard(): ReactElement | null {
  const { data: status, isLoading } = useOnboardingStatus()
  if (isLoading) return null
  if (!status) return null
  if (status.setupComplete) return null  // Card disappears once setup is done

  const requiredSteps = status.steps.filter(s => s.required)
  const completedRequired = requiredSteps.filter(s => s.complete).length
  const totalRequired = requiredSteps.length
  const percent = totalRequired === 0 ? 0 : Math.round((completedRequired / totalRequired) * 100)

  return (
    <div className="bg-white rounded-xl border border-[var(--color-border)] overflow-hidden">
      <div className="px-5 py-4 border-b border-[var(--color-border)] bg-gradient-to-r from-[var(--color-primary)]/8 to-transparent">
        <div className="flex items-center justify-between mb-2">
          <div>
            <h2 className="text-[15px] font-semibold text-[var(--color-text-primary)]">Get started</h2>
            <p className="text-[12px] text-[var(--color-text-secondary)] mt-0.5">
              Complete these steps to run your first payroll.
            </p>
          </div>
          <span className="text-[13px] font-semibold text-[var(--color-text-primary)]">
            {completedRequired} / {totalRequired}
          </span>
        </div>
        <div className="w-full h-1.5 bg-[var(--color-border)] rounded-full overflow-hidden">
          <div className="h-full bg-[var(--color-primary)] transition-all" style={{ width: `${percent}%` }} />
        </div>
      </div>

      <ul className="divide-y divide-[var(--color-border)]">
        {STEPS.map(meta => {
          const s = status.steps.find(x => x.id === meta.id)
          if (!s) return null
          return <SetupStepRow key={meta.id} meta={meta} step={s} />
        })}
      </ul>
    </div>
  )
}

interface RowProps {
  meta: StepRoute
  step: OnboardingStepDto
}

function SetupStepRow({ meta, step }: RowProps): ReactElement {
  const qc = useQueryClient()
  const seedMutation = useMutation({
    mutationFn: async () => {
      const res = await api.post<OnboardingStatusDto>(`/api/v1/onboarding/seed-defaults/${meta.id}`)
      return res.data
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['onboarding-status'] })
      await qc.invalidateQueries({ queryKey: ['payroll-run-preflight'] })
    },
  })

  const settingsHref = withReturnQuery(meta.settingsUrl, meta.id)
  const showApplyDefaults = !step.complete && STEPS_WITH_DEFAULTS.has(meta.id)
  const details = step.details as { blockedBy?: string; locked?: boolean } | undefined
  const blockedByFirstEmployee = meta.id === 'deductor-employee' && details?.blockedBy === 'first-employee'
  const isLocked = meta.id === 'pay-schedule' && details?.locked === true

  return (
    <li>
      <div className="flex items-center gap-3 px-5 py-3.5 hover:bg-[var(--color-page-bg)] transition-colors">
        {/* Status icon */}
        <span className="flex-shrink-0">
          {step.complete
            ? <Check className="w-4 h-4 text-emerald-600" />
            : <Circle className="w-4 h-4 text-[var(--color-text-secondary)]" />}
        </span>

        {/* Step label + sub-info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-[12px] text-[var(--color-text-secondary)] font-medium">
              {meta.ordinal}.
            </span>
            <span className={`text-[13px] font-medium ${step.complete ? 'text-[var(--color-text-secondary)]' : 'text-[var(--color-text-primary)]'}`}>
              {stepLabel(meta.id)}
            </span>
            {!step.required && (
              <span className="text-[10px] uppercase tracking-wide text-[var(--color-text-secondary)] font-medium">
                Optional
              </span>
            )}
            {meta.id === 'pay-schedule' && !isLocked && (
              <span title="Work week and salary calc method lock after the first paid payroll run." className="inline-flex items-center gap-0.5 text-[10px] text-amber-700">
                <AlertTriangle className="w-3 h-3" /> Locks after 1st run
              </span>
            )}
            {isLocked && (
              <span className="text-[10px] uppercase tracking-wide text-[var(--color-text-secondary)] font-medium">
                Locked
              </span>
            )}
          </div>
          {blockedByFirstEmployee && (
            <p className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">
              Add your first employee before assigning a Tax Deductor.
            </p>
          )}
        </div>

        {/* Right-aligned actions */}
        <div className="flex items-center gap-2 flex-shrink-0">
          {step.complete ? (
            <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-emerald-50 text-emerald-700 text-[11px] font-medium">
              <Check className="w-3 h-3" /> Completed
            </span>
          ) : (
            <>
              {showApplyDefaults && (
                <button
                  type="button"
                  onClick={() => { seedMutation.mutate() }}
                  disabled={seedMutation.isPending}
                  className="inline-flex items-center gap-1 h-7 px-2.5 rounded-md text-[12px] font-medium text-[var(--color-primary)] hover:bg-[var(--color-primary)]/10 disabled:opacity-50"
                  title="Create a sensible default for this step in one click."
                >
                  <Sparkles className="w-3 h-3" />
                  {seedMutation.isPending ? 'Applying…' : 'Apply defaults'}
                </button>
              )}
              <Link
                to={settingsHref}
                className="inline-flex items-center gap-1 h-7 px-3 rounded-md bg-[var(--color-primary)] text-white text-[12px] font-medium hover:bg-[var(--color-primary-hover)]"
              >
                {meta.primaryCtaLabel}
                {meta.primaryCtaLabel.includes('employee') ? <ChevronRight className="w-3 h-3" /> : <ExternalLink className="w-3 h-3" />}
              </Link>
            </>
          )}
        </div>
      </div>
    </li>
  )
}
