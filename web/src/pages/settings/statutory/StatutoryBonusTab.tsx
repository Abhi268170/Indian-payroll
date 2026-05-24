import { useState, type ReactElement } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Gift, Pencil } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { useToast } from '@/components/ui/useToast'
import type { StatutoryConfig } from '../StatutoryComponentsPage'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

interface Props {
  config: StatutoryConfig
}

export default function StatutoryBonusTab({ config }: Props): ReactElement {
  const [editing, setEditing] = useState(false)
  const qc = useQueryClient()
  const toast = useToast()

  const toggle = useMutation({
    mutationFn: (enabled: boolean) =>
      api.put('/api/v1/statutory/statutory-bonus', {
        enabled,
        bonusRate: config.bonusRate,
        bonusMode: config.bonusMode,
        bonusPayoutMonth: config.bonusPayoutMonth,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['statutory-config'] })
      toast.success(`Statutory Bonus ${config.statutoryBonusEnabled ? 'disabled' : 'enabled'}`)
    },
    onError: () => { toast.error('Failed to update statutory bonus') },
  })

  if (!config.statutoryBonusEnabled) {
    return (
      <div className="text-center py-16">
        <Gift className="w-10 h-10 mx-auto mb-4 text-[var(--color-text-muted)] opacity-40" />
        <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)] mb-1">
          Statutory Bonus
        </h3>
        <p className="text-[13px] text-[var(--color-text-muted)] mb-6 max-w-md mx-auto">
          Enable statutory bonus to compute annual bonus under the Payment of Bonus Act, 1965.
          Applicable to employees earning ≤ ₹21,000/month. Computed on{' '}
          <code className="bg-gray-100 px-1 rounded">max(₹7,000, state min wage) × 12</code>.
        </p>
        <Button
          onClick={() => { toggle.mutate(true) }}
          loading={toggle.isPending}
        >
          Enable Statutory Bonus
        </Button>
      </div>
    )
  }

  if (editing) {
    return (
      <BonusConfigForm
        config={config}
        onCancel={() => { setEditing(false) }}
        onSaved={() => { setEditing(false) }}
      />
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
            Statutory Bonus
          </h3>
          <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
            Enabled — Payment of Bonus Act, 1965
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => { setEditing(true) }}
          >
            <Pencil className="w-3.5 h-3.5 mr-1.5" />
            Edit
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => { toggle.mutate(false) }}
            loading={toggle.isPending}
          >
            Disable
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-x-12 gap-y-4">
        <ViewRow label="Bonus Rate" value={`${(config.bonusRate * 100).toFixed(2)}%`} />
        <ViewRow label="Payout Mode" value={config.bonusMode} />
        {config.bonusMode === 'Yearly' && (
          <ViewRow
            label="Payout Month"
            value={config.bonusPayoutMonth ? (MONTHS[config.bonusPayoutMonth - 1] ?? '—') : '—'}
          />
        )}
      </div>

      <div className="bg-[var(--color-primary-light)] rounded-lg p-4 text-[13px] text-[var(--color-text-secondary)]">
        <p>
          <strong className="text-[var(--color-primary)]">Bonus computation:</strong>{' '}
          8.33% to 20% of annual wages. Minimum bonus = 8.33% of{' '}
          <code className="bg-white/60 px-1 rounded">max(₹7,000, state min wage) × 12</code>.
          Payable within 8 months of financial year close. Employees with salary ≤ ₹21,000/month
          are eligible.
        </p>
      </div>
    </div>
  )
}

function BonusConfigForm({
  config,
  onCancel,
  onSaved,
}: {
  config: StatutoryConfig
  onCancel: () => void
  onSaved: () => void
}): ReactElement {
  const qc = useQueryClient()
  const toast = useToast()

  const [bonusRatePct, setBonusRatePct] = useState(String((config.bonusRate * 100).toFixed(2)))
  const [bonusMode, setBonusMode] = useState(config.bonusMode)
  const [bonusPayoutMonth, setBonusPayoutMonth] = useState(
    config.bonusPayoutMonth ? String(config.bonusPayoutMonth) : ''
  )

  const save = useMutation({
    mutationFn: () =>
      api.put('/api/v1/statutory/statutory-bonus', {
        enabled: true,
        bonusRate: parseFloat(bonusRatePct) / 100,
        bonusMode,
        bonusPayoutMonth: bonusMode === 'Yearly' && bonusPayoutMonth ? parseInt(bonusPayoutMonth, 10) : null,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['statutory-config'] })
      toast.success('Bonus configuration saved')
      onSaved()
    },
    onError: () => { toast.error('Failed to save bonus configuration') },
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Statutory Bonus Configuration
        </h3>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" onClick={onCancel}>Cancel</Button>
          <Button size="sm" loading={save.isPending} onClick={() => { save.mutate() }}>Save</Button>
        </div>
      </div>

      <div className="space-y-5">
        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
            Bonus Rate (%)
          </label>
          <input
            type="number"
            min={8.33}
            max={20}
            step={0.01}
            className="form-input w-32"
            value={bonusRatePct}
            onChange={e => { setBonusRatePct(e.target.value) }}
          />
          <p className="text-[12px] text-[var(--color-text-muted)] mt-1">
            Minimum 8.33% (statutory minimum). Maximum 20%.
          </p>
        </div>

        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-2">
            Payout Mode
          </label>
          <div className="space-y-2">
            {(['Monthly', 'Yearly'] as const).map(mode => (
              <label key={mode} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="bonusMode"
                  value={mode}
                  checked={bonusMode === mode}
                  onChange={() => { setBonusMode(mode) }}
                  className="accent-[var(--color-primary)]"
                />
                <span className="text-[13px] text-[var(--color-text-primary)]">{mode}</span>
              </label>
            ))}
          </div>
        </div>

        {bonusMode === 'Yearly' && (
          <div>
            <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
              Payout Month
            </label>
            <select
              className="form-input w-48"
              value={bonusPayoutMonth}
              onChange={e => { setBonusPayoutMonth(e.target.value) }}
            >
              <option value="">Select month</option>
              {MONTHS.map((m, i) => (
                <option key={m} value={i + 1}>{m}</option>
              ))}
            </select>
            <p className="text-[12px] text-[var(--color-text-muted)] mt-1">
              Bonus will be paid out in this month each year.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

function ViewRow({ label, value }: { label: string; value: string }): ReactElement {
  return (
    <div>
      <dt className="text-[12px] text-[var(--color-text-muted)] mb-0.5">{label}</dt>
      <dd className="text-[13px] text-[var(--color-text-primary)]">{value}</dd>
    </div>
  )
}
