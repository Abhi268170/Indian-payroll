import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'

const DAYS: { key: string; label: string }[] = [
  { key: 'Sunday',    label: 'Sun' },
  { key: 'Monday',    label: 'Mon' },
  { key: 'Tuesday',   label: 'Tue' },
  { key: 'Wednesday', label: 'Wed' },
  { key: 'Thursday',  label: 'Thu' },
  { key: 'Friday',    label: 'Fri' },
  { key: 'Saturday',  label: 'Sat' },
]

const DEFAULT_WORK_DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']

const MONTH_NAMES = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
]

const DOW_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

function walkBackToWorkingDay(year: number, month: number, day: number, workWeekDays: string[]): number {
  if (workWeekDays.length === 0) return day
  for (let d = day; d >= 1; d--) {
    const dow = new Date(year, month - 1, d).getDay()
    const name = DOW_NAMES[dow]
    if (name !== undefined && workWeekDays.includes(name)) return d
  }
  return 1
}

function getUpcomingPayDates(
  startMonth: number,
  startYear: number,
  payDateType: string,
  payDateDay: number | null,
  workWeekDays: string[],
  count: number,
): string[] {
  const dates: string[] = []
  let month = startMonth
  let year = startYear
  for (let i = 0; i < count; i++) {
    let day: number
    if (payDateType === 'LastDay') {
      day = new Date(year, month, 0).getDate()
    } else {
      const daysInMonth = new Date(year, month, 0).getDate()
      day = Math.min(payDateDay ?? 1, daysInMonth)
    }
    day = walkBackToWorkingDay(year, month, day, workWeekDays)
    dates.push(`${day} ${MONTH_NAMES[month - 1]} ${year}`)
    month++
    if (month > 12) { month = 1; year++ }
  }
  return dates
}

interface PayScheduleDto {
  workWeekDays: string[]
  salaryCalculationMethod: 'ActualDays' | 'FixedDays'
  fixedWorkingDaysPerMonth: number | null
  payDateType: 'LastDay' | 'SpecificDay'
  payDateDay: number | null
  firstPayPeriodMonth: number | null
  firstPayPeriodYear: number | null
  isLocked: boolean
}

// Outer component: handles data fetching and loading state.
// Inner PayScheduleForm is re-keyed on each successful save so its
// useState initializers re-run with the fresh query result.
export default function PaySchedulesPage(): ReactElement {
  const [formKey, setFormKey] = useState(0)

  const { data: schedule, isLoading } = useQuery<PayScheduleDto | null>({
    queryKey: ['pay-schedule'],
    queryFn: async () => {
      const res = await api.get<PayScheduleDto>('/api/v1/pay-schedule', {
        validateStatus: s => s === 200 || s === 204,
      })
      return res.status === 204 ? null : res.data
    },
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner />
      </div>
    )
  }

  return (
    <PayScheduleForm
      key={formKey}
      initialSchedule={schedule ?? null}
      onSaved={() => { setFormKey(k => k + 1) }}
    />
  )
}

// Inner form: initializes controlled state from props at mount time.
// No useEffect needed — props are already resolved when this mounts.
function PayScheduleForm({
  initialSchedule,
  onSaved,
}: {
  initialSchedule: PayScheduleDto | null
  onSaved: () => void
}): ReactElement {
  const qc = useQueryClient()
  const { success: toastSuccess, error: toastError } = useToast()

  const isLocked = initialSchedule?.isLocked ?? false

  const [workWeekDays, setWorkWeekDays] = useState<string[]>(
    initialSchedule?.workWeekDays ?? DEFAULT_WORK_DAYS
  )
  const [calcMethod, setCalcMethod] = useState<'ActualDays' | 'FixedDays'>(
    initialSchedule?.salaryCalculationMethod ?? 'ActualDays'
  )
  const [fixedDays, setFixedDays] = useState<string>(
    initialSchedule?.fixedWorkingDaysPerMonth?.toString() ?? '26'
  )
  const [payDateType, setPayDateType] = useState<'LastDay' | 'SpecificDay'>(
    initialSchedule?.payDateType ?? 'LastDay'
  )
  const [payDateDay, setPayDateDay] = useState<string>(
    initialSchedule?.payDateDay?.toString() ?? '1'
  )
  const [firstPayPeriodMonth, setFirstPayPeriodMonth] = useState<string>(
    initialSchedule?.firstPayPeriodMonth?.toString() ?? ''
  )
  const [firstPayPeriodYear, setFirstPayPeriodYear] = useState<string>(
    initialSchedule?.firstPayPeriodYear?.toString() ?? ''
  )

  const saveMutation = useMutation({
    mutationFn: () =>
      api.put('/api/v1/pay-schedule', {
        workWeekDays,
        salaryCalculationMethod: calcMethod,
        fixedWorkingDaysPerMonth: calcMethod === 'FixedDays' ? parseInt(fixedDays, 10) : null,
        payDateType,
        payDateDay: payDateType === 'SpecificDay' ? parseInt(payDateDay, 10) : null,
        firstPayPeriodMonth: firstPayPeriodMonth ? parseInt(firstPayPeriodMonth, 10) : null,
        firstPayPeriodYear: firstPayPeriodYear ? parseInt(firstPayPeriodYear, 10) : null,
      }),
    onSuccess: async () => {
      toastSuccess('Pay schedule saved')
      await qc.invalidateQueries({ queryKey: ['pay-schedule'] })
      onSaved()
    },
    onError: (err: unknown) => {
      const msg = extractError(err)
      toastError(msg ?? 'Failed to save pay schedule')
    },
  })

  function toggleDay(key: string): void {
    if (isLocked) return
    setWorkWeekDays(prev =>
      prev.includes(key) ? prev.filter(d => d !== key) : [...prev, key]
    )
  }

  return (
    <div className="px-8 py-8 max-w-2xl">
      <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)] mb-8">
        Pay Schedule
      </h1>

      {isLocked && (
        <div className="mb-6 flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-3">
          <AlertCircle className="w-4 h-4 text-amber-600 mt-0.5 flex-shrink-0" />
          <p className="text-[13px] text-amber-800">
            Work week and salary calculation method are locked after a payroll run has been processed.
            Pay date can still be changed.
          </p>
        </div>
      )}

      <div className="space-y-6">
        {/* Work Week */}
        <Section
          title="Work Week"
          helpText="These days will be considered when calculating payable days and loss of pay."
        >
          <div className="flex gap-2">
            {DAYS.map(day => {
              const selected = workWeekDays.includes(day.key)
              return (
                <button
                  key={day.key}
                  type="button"
                  onClick={() => { toggleDay(day.key) }}
                  disabled={isLocked}
                  className={[
                    'w-11 h-10 rounded-lg text-[13px] font-medium border transition-colors',
                    selected
                      ? 'bg-[var(--color-primary)] text-white border-[var(--color-primary)]'
                      : 'bg-white text-[var(--color-text-secondary)] border-[var(--color-border)] hover:border-[var(--color-primary)] hover:text-[var(--color-primary)]',
                    isLocked ? 'opacity-60 cursor-not-allowed' : 'cursor-pointer',
                  ].join(' ')}
                >
                  {day.label}
                </button>
              )
            })}
          </div>
        </Section>

        {/* Salary Calculation Method */}
        <Section title="Salary Calculation Method">
          <div className="space-y-3">
            <RadioOption
              id="method-actual"
              name="calcMethod"
              value="ActualDays"
              checked={calcMethod === 'ActualDays'}
              disabled={isLocked}
              onChange={() => { setCalcMethod('ActualDays') }}
              label="Actual days in a month"
              description="Daily rate = Monthly salary ÷ calendar days in the month (28/29/30/31)"
            />
            <RadioOption
              id="method-fixed"
              name="calcMethod"
              value="FixedDays"
              checked={calcMethod === 'FixedDays'}
              disabled={isLocked}
              onChange={() => { setCalcMethod('FixedDays') }}
              label="Based on fixed working days per month"
              description="Salary is prorated based on a fixed number of working days each month."
            />
            {calcMethod === 'FixedDays' && (
              <div className="ml-6 mt-2">
                <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">
                  Fixed working days per month
                </label>
                <input
                  type="number"
                  min={1}
                  max={31}
                  value={fixedDays}
                  disabled={isLocked}
                  onChange={e => { setFixedDays(e.target.value) }}
                  className="w-24 h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] disabled:opacity-60"
                />
              </div>
            )}
          </div>
        </Section>

        {/* Pay Date */}
        <Section title="Pay Date">
          <div className="space-y-3">
            <RadioOption
              id="date-last"
              name="payDateType"
              value="LastDay"
              checked={payDateType === 'LastDay'}
              onChange={() => { setPayDateType('LastDay') }}
              label="On the last day of every month"
            />
            <RadioOption
              id="date-specific"
              name="payDateType"
              value="SpecificDay"
              checked={payDateType === 'SpecificDay'}
              onChange={() => { setPayDateType('SpecificDay') }}
              label="On a specific day of every month"
            />
            {payDateType === 'SpecificDay' && (
              <div className="ml-6 mt-2 flex items-center gap-2">
                <span className="text-[13px] text-[var(--color-text-secondary)]">On day</span>
                <select
                  value={payDateDay}
                  onChange={e => { setPayDateDay(e.target.value) }}
                  className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white"
                >
                  {Array.from({ length: 30 }, (_, i) => i + 1).map(d => (
                    <option key={d} value={d}>{d}</option>
                  ))}
                </select>
                <span className="text-[13px] text-[var(--color-text-secondary)]">of every month</span>
              </div>
            )}
          </div>
          <p className="mt-4 text-[12px] text-[var(--color-text-muted)]">
            If the selected pay date falls on a non-working day or holiday, payment will be processed on the previous working day.
          </p>
        </Section>

        {/* First Payroll Setup */}
        {!isLocked && (
          <Section title="First Payroll Setup" helpText="Set the month and year for your first payroll run. This helps preview upcoming pay dates.">
            <div className="flex items-center gap-4">
              <div>
                <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Month</label>
                <select
                  value={firstPayPeriodMonth}
                  onChange={e => { setFirstPayPeriodMonth(e.target.value) }}
                  className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white"
                >
                  <option value="">Select month</option>
                  {['January','February','March','April','May','June','July','August','September','October','November','December']
                    .map((m, i) => <option key={m} value={i + 1}>{m}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-[12px] text-[var(--color-text-secondary)] mb-1">Year</label>
                <select
                  value={firstPayPeriodYear}
                  onChange={e => { setFirstPayPeriodYear(e.target.value) }}
                  className="h-9 px-3 border border-[var(--color-border)] rounded-lg text-[13px] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-primary)] bg-white"
                >
                  <option value="">Select year</option>
                  {Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - 1 + i).map(y => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
              </div>
            </div>
            {firstPayPeriodMonth && firstPayPeriodYear && payDateType && (
              <div className="mt-4">
                <p className="text-[12px] text-[var(--color-text-muted)] mb-2">Upcoming pay dates:</p>
                <div className="flex gap-3">
                  {getUpcomingPayDates(
                    parseInt(firstPayPeriodMonth, 10),
                    parseInt(firstPayPeriodYear, 10),
                    payDateType,
                    payDateType === 'SpecificDay' ? parseInt(payDateDay, 10) : null,
                    workWeekDays,
                    3
                  ).map((d, i) => (
                    <div key={i} className="px-3 py-1.5 bg-[var(--color-primary-light)] rounded-lg text-[12px] text-[var(--color-primary)] font-medium">
                      {d}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Section>
        )}

        <div>
          <Button
            type="button"
            variant="primary"
            size="sm"
            loading={saveMutation.isPending}
            onClick={() => { saveMutation.mutate() }}
            disabled={workWeekDays.length === 0}
          >
            Save
          </Button>
          {workWeekDays.length === 0 && (
            <p className="mt-2 text-[12px] text-[var(--color-error)]">
              Select at least one working day.
            </p>
          )}
        </div>
      </div>
    </div>
  )
}

function Section({
  title,
  helpText,
  children,
}: {
  title: string
  helpText?: string
  children: React.ReactNode
}): ReactElement {
  return (
    <div className="bg-white border border-[var(--color-border)] rounded-xl p-6">
      <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-1">{title}</h2>
      {helpText != null
        ? <p className="text-[12px] text-[var(--color-text-muted)] mb-4">{helpText}</p>
        : <div className="mb-4" />}
      {children}
    </div>
  )
}

function RadioOption({
  id,
  name,
  value,
  checked,
  disabled = false,
  onChange,
  label,
  description,
}: {
  id: string
  name: string
  value: string
  checked: boolean
  disabled?: boolean
  onChange: () => void
  label: string
  description?: string
}): ReactElement {
  return (
    <label
      htmlFor={id}
      className={[
        'flex items-start gap-3',
        disabled ? 'opacity-60 cursor-not-allowed' : 'cursor-pointer',
      ].join(' ')}
    >
      <input
        id={id}
        type="radio"
        name={name}
        value={value}
        checked={checked}
        disabled={disabled}
        onChange={onChange}
        className="mt-0.5 accent-[var(--color-primary)]"
      />
      <div>
        <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
        {description != null && (
          <p className="text-[12px] text-[var(--color-text-muted)] mt-0.5">{description}</p>
        )}
      </div>
    </label>
  )
}

function extractError(err: unknown): string | null {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const res = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response
    return res?.data?.error ?? res?.data?.errors?.[0] ?? null
  }
  return null
}
