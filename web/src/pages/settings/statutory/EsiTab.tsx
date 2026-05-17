import { useState, type ReactElement } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { useToast } from '@/components/ui/useToast'
import type { StatutoryConfig } from '../StatutoryComponentsPage'

interface Props {
  config: StatutoryConfig
}

export default function EsiTab({ config }: Props): ReactElement {
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({
    enabled: config.esiEnabled,
    establishmentCode: config.esiEstablishmentCode ?? '',
    notifiedArea: config.esiNotifiedArea,
  })

  const qc = useQueryClient()
  const toast = useToast()

  const save = useMutation({
    mutationFn: () =>
      api.put('/api/v1/statutory/esi', {
        enabled: form.enabled,
        establishmentCode: form.establishmentCode || null,
        notifiedArea: form.notifiedArea,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['statutory-config'] })
      setEditing(false)
      toast.success('ESI settings saved')
    },
    onError: () => { toast.error('Failed to save ESI settings'); },
  })

  if (!editing) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
              Employee State Insurance
            </h3>
            <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
              Configure ESI deductions for eligible employees
            </p>
          </div>
          <Button variant="secondary" size="sm" onClick={() => { setEditing(true); }}>
            <Pencil className="w-3.5 h-3.5 mr-1.5" />
            Edit
          </Button>
        </div>

        <div className="grid grid-cols-2 gap-x-12 gap-y-4">
          <ViewRow label="ESI Status" value={config.esiEnabled ? 'Enabled' : 'Disabled'} />
          <ViewRow label="Establishment Code" value={config.esiEstablishmentCode ?? '—'} />
          <ViewRow
            label="ESI Notified Area"
            value={config.esiNotifiedArea ? 'Yes — ESI applicable' : 'No — ESI not applicable'}
          />
        </div>

        <div className="bg-[var(--color-primary-light)] rounded-lg p-4 text-[13px] text-[var(--color-text-secondary)]">
          <strong className="text-[var(--color-primary)]">Eligibility threshold:</strong>{' '}
          Employees earning ≤ ₹21,000/month gross (excluding overtime) are covered under ESI.
          Employee contributes 0.75%, employer contributes 3.25% of ESI wages.
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
          Employee State Insurance
        </h3>
        <div className="flex gap-2">
          <Button type="button" variant="secondary" size="sm" onClick={() => { setEditing(false); }}>
            Cancel
          </Button>
          <Button type="submit" size="sm" loading={save.isPending}>
            Save
          </Button>
        </div>
      </div>

      <div className="space-y-5">
        <CheckboxField
          id="esi-enabled"
          label="Enable ESI for this organisation"
          checked={form.enabled}
          onChange={v => { setForm(f => ({ ...f, enabled: v })); }}
        />

        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
            ESI Establishment Code
          </label>
          <input
            className="form-input w-72"
            placeholder="e.g. 31-00-12345-000-0001"
            value={form.establishmentCode}
            onChange={e => { setForm(f => ({ ...f, establishmentCode: e.target.value })); }}
          />
        </div>

        <CheckboxField
          id="notified-area"
          label="This establishment is in an ESI Notified Area"
          checked={form.notifiedArea}
          onChange={v => { setForm(f => ({ ...f, notifiedArea: v })); }}
        />
        <p className="text-[12px] text-[var(--color-text-muted)] -mt-2">
          ESI is mandatory only in notified areas. 89 of 691 districts are not yet notified.
        </p>
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
        onChange={e => { onChange(e.target.checked); }}
        className="mt-0.5 h-4 w-4 rounded border-[var(--color-border)] text-[var(--color-primary)] focus:ring-[var(--color-primary)]"
      />
      <span className="text-[13px] text-[var(--color-text-primary)]">{label}</span>
    </label>
  )
}
