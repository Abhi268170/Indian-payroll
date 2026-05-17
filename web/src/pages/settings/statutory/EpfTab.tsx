import { useState, type ReactElement } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { useToast } from '@/components/ui/useToast'
import type { StatutoryConfig } from '../StatutoryComponentsPage'

const CONTRIBUTION_RATES = [
  { value: 'ActualPfWage12', label: '12% of Actual PF Wage' },
  { value: 'RestrictedWage12', label: '12% of Restricted PF Wage (₹15,000)' },
  { value: 'Gross12', label: '12% of Gross' },
]

interface Props {
  config: StatutoryConfig
}

export default function EpfTab({ config }: Props): ReactElement {
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({
    enabled: config.epfEnabled,
    establishmentCode: config.epfEstablishmentCode ?? '',
    employeeContributionRate: config.epfEmployeeContributionRate,
    employerContributionRate: config.epfEmployerContributionRate,
    includeEmployerInCtc: config.epfIncludeEmployerInCtc,
    includeEdliInCtc: config.epfIncludeEdliInCtc,
    includeAdminInCtc: config.epfIncludeAdminInCtc,
    overrideAtEmployeeLevel: config.epfOverrideAtEmployeeLevel,
    proRateRestrictedPfWage: config.epfProRateRestrictedPfWage,
    considerSalaryOnLop: config.epfConsiderSalaryOnLop,
  })

  const qc = useQueryClient()
  const toast = useToast()

  const save = useMutation({
    mutationFn: () =>
      api.put('/api/v1/statutory/epf', {
        enabled: form.enabled,
        establishmentCode: form.establishmentCode || null,
        employeeContributionRate: form.employeeContributionRate,
        employerContributionRate: form.employerContributionRate,
        includeEmployerInCtc: form.includeEmployerInCtc,
        includeEdliInCtc: form.includeEdliInCtc,
        includeAdminInCtc: form.includeAdminInCtc,
        overrideAtEmployeeLevel: form.overrideAtEmployeeLevel,
        proRateRestrictedPfWage: form.proRateRestrictedPfWage,
        considerSalaryOnLop: form.considerSalaryOnLop,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['statutory-config'] })
      setEditing(false)
      toast.success('EPF settings saved')
    },
    onError: () => { toast.error('Failed to save EPF settings'); },
  })

  const rateLabel = (v: string): string =>
    CONTRIBUTION_RATES.find(r => r.value === v)?.label ?? v

  if (!editing) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
              Employee Provident Fund
            </h3>
            <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
              Configure EPF deductions and employer contributions
            </p>
          </div>
          <Button variant="secondary" size="sm" onClick={() => { setEditing(true); }}>
            <Pencil className="w-3.5 h-3.5 mr-1.5" />
            Edit
          </Button>
        </div>

        <div className="grid grid-cols-2 gap-x-12 gap-y-4">
          <ViewRow label="EPF Status" value={config.epfEnabled ? 'Enabled' : 'Disabled'} />
          <ViewRow label="Establishment Code" value={config.epfEstablishmentCode ?? '—'} />
          <ViewRow label="Employee Contribution" value={rateLabel(config.epfEmployeeContributionRate)} />
          <ViewRow label="Employer Contribution" value={rateLabel(config.epfEmployerContributionRate)} />
          <ViewRow label="Include Employer EPF in CTC" value={config.epfIncludeEmployerInCtc ? 'Yes' : 'No'} />
          <ViewRow label="Include EDLI in CTC" value={config.epfIncludeEdliInCtc ? 'Yes' : 'No'} />
          <ViewRow label="Include Admin Charges in CTC" value={config.epfIncludeAdminInCtc ? 'Yes' : 'No'} />
        </div>

        <div className="border-t border-[var(--color-border)] pt-4">
          <h4 className="text-[13px] font-semibold text-[var(--color-text-primary)] mb-3">
            PF Configuration when LOP Applied
          </h4>
          <div className="grid grid-cols-2 gap-x-12 gap-y-3">
            <ViewRow
              label="Pro-rate Restricted PF Wage"
              value={config.epfProRateRestrictedPfWage ? 'Enabled' : 'Disabled'}
            />
            <ViewRow
              label="Consider salary on LOP"
              value={config.epfConsiderSalaryOnLop ? 'Enabled' : 'Disabled'}
            />
          </div>
        </div>
      </div>
    )
  }

  return (
    <form
      className="space-y-6"
      onSubmit={e => {
        e.preventDefault()
        save.mutate()
      }}
    >
      <div className="flex items-center justify-between">
        <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Employee Provident Fund
        </h3>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => { setEditing(false); }}
          >
            Cancel
          </Button>
          <Button type="submit" size="sm" loading={save.isPending}>
            Save
          </Button>
        </div>
      </div>

      <div className="space-y-5">
        <CheckboxField
          id="epf-enabled"
          label="Enable EPF for this organisation"
          checked={form.enabled}
          onChange={v => { setForm(f => ({ ...f, enabled: v })); }}
        />

        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
            EPF Establishment Code
          </label>
          <input
            className="form-input w-72"
            placeholder="e.g. MH/PUN/0012345/000"
            value={form.establishmentCode}
            onChange={e => { setForm(f => ({ ...f, establishmentCode: e.target.value })); }}
          />
        </div>

        <div className="grid grid-cols-2 gap-5">
          <SelectField
            label="Employee Contribution Rate"
            value={form.employeeContributionRate}
            options={CONTRIBUTION_RATES}
            onChange={v => { setForm(f => ({ ...f, employeeContributionRate: v })); }}
          />
          <SelectField
            label="Employer Contribution Rate"
            value={form.employerContributionRate}
            options={CONTRIBUTION_RATES}
            onChange={v => { setForm(f => ({ ...f, employerContributionRate: v })); }}
          />
        </div>

        <div className="space-y-2">
          <CheckboxField
            id="include-employer"
            label="Include Employer EPF contribution in CTC"
            checked={form.includeEmployerInCtc}
            onChange={v => { setForm(f => ({ ...f, includeEmployerInCtc: v })); }}
          />
          <CheckboxField
            id="include-edli"
            label="Include EDLI contribution in CTC"
            checked={form.includeEdliInCtc}
            onChange={v => { setForm(f => ({ ...f, includeEdliInCtc: v })); }}
          />
          <CheckboxField
            id="include-admin"
            label="Include EPF Admin charges in CTC"
            checked={form.includeAdminInCtc}
            onChange={v => { setForm(f => ({ ...f, includeAdminInCtc: v })); }}
          />
          <CheckboxField
            id="override-employee"
            label="Allow EPF override at employee level"
            checked={form.overrideAtEmployeeLevel}
            onChange={v => { setForm(f => ({ ...f, overrideAtEmployeeLevel: v })); }}
          />
        </div>

        <div className="border-t border-[var(--color-border)] pt-4">
          <h4 className="text-[13px] font-semibold text-[var(--color-text-primary)] mb-3">
            PF Configuration when LOP Applied
          </h4>
          <div className="space-y-2">
            <CheckboxField
              id="pro-rate"
              label="Pro-rate Restricted PF Wage based on paid days"
              checked={form.proRateRestrictedPfWage}
              onChange={v => { setForm(f => ({ ...f, proRateRestrictedPfWage: v })); }}
            />
            <CheckboxField
              id="consider-lop"
              label="Consider all applicable salary components if PF wage < ₹15,000 after LOP"
              checked={form.considerSalaryOnLop}
              onChange={v => { setForm(f => ({ ...f, considerSalaryOnLop: v })); }}
            />
          </div>
        </div>
      </div>
    </form>
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

function CheckboxField({
  id,
  label,
  checked,
  onChange,
}: {
  id: string
  label: string
  checked: boolean
  onChange: (v: boolean) => void
}): ReactElement {
  return (
    <label htmlFor={id} className="flex items-start gap-2.5 cursor-pointer">
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={e => { onChange(e.target.checked) }}
        className="mt-0.5 h-4 w-4 rounded border-[var(--color-border)] text-[var(--color-primary)] focus:ring-[var(--color-primary)]"
      />
      <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
    </label>
  )
}

function SelectField({
  label,
  value,
  options,
  onChange,
}: {
  label: string
  value: string
  options: { value: string; label: string }[]
  onChange: (v: string) => void
}): ReactElement {
  return (
    <div>
      <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
        {label}
      </label>
      <select
        className="form-input w-full"
        value={value}
        onChange={e => { onChange(e.target.value); }}
      >
        {options.map(o => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  )
}
