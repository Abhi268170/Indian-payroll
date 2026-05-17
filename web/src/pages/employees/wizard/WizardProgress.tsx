import { Check } from 'lucide-react'

const STEPS = [
  { n: 1, label: 'Basic Details' },
  { n: 2, label: 'Salary Details' },
  { n: 3, label: 'Personal Details' },
  { n: 4, label: 'Payment Information' },
]

interface Props {
  current: number
}

export default function WizardProgress({ current }: Props): React.ReactElement {
  return (
    <div className="flex items-center gap-0 mb-8">
      {STEPS.map((step, i) => {
        const done = step.n < current
        const active = step.n === current

        return (
          <div key={step.n} className="flex items-center flex-1 last:flex-none">
            <div className="flex items-center gap-2.5 flex-shrink-0">
              <div className={`w-7 h-7 rounded-full flex items-center justify-center text-[12px] font-semibold border-2 transition-colors ${
                done
                  ? 'bg-[var(--color-primary)] border-[var(--color-primary)] text-white'
                  : active
                    ? 'bg-white border-[var(--color-primary)] text-[var(--color-primary)]'
                    : 'bg-white border-[var(--color-border)] text-[var(--color-text-secondary)]'
              }`}>
                {done ? <Check className="w-3.5 h-3.5" /> : step.n}
              </div>
              <span className={`text-[12px] font-medium whitespace-nowrap ${
                active ? 'text-[var(--color-primary)]' : done ? 'text-[var(--color-text-primary)]' : 'text-[var(--color-text-secondary)]'
              }`}>
                {step.label}
              </span>
            </div>
            {i < STEPS.length - 1 && (
              <div className={`flex-1 h-px mx-3 ${done ? 'bg-[var(--color-primary)]' : 'bg-[var(--color-border)]'}`} />
            )}
          </div>
        )
      })}
    </div>
  )
}
