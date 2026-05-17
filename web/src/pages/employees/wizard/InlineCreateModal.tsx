import { useState } from 'react'
import { X } from 'lucide-react'

interface Field {
  name: string
  label: string
  required: boolean
}

interface Props {
  title: string
  fields: Field[]
  onSave: (values: Record<string, string>) => Promise<void>
  onClose: () => void
}

const inputCls = 'w-full h-9 px-3 text-[13px] border border-[var(--color-border)] rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]'
const labelCls = 'block text-[12px] font-medium text-[var(--color-text-secondary)] mb-1'

export default function InlineCreateModal({ title, fields, onSave, onClose }: Props): React.ReactElement {
  const [values, setValues] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSave(): Promise<void> {
    const missing = fields.filter(f => f.required && !(values[f.name] ?? '').trim())
    if (missing.length > 0) {
      setError(`${missing[0]?.label ?? 'Field'} is required.`)
      return
    }
    setSaving(true)
    setError(null)
    try {
      await onSave(values)
    } catch {
      setError('Failed to save. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-sm p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">{title}</h3>
          <button
            type="button"
            onClick={onClose}
            className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="space-y-3">
          {fields.map(f => (
            <div key={f.name}>
              <label className={labelCls}>
                {f.label}
                {f.required && <span className="text-red-500 ml-0.5">*</span>}
              </label>
              <input
                className={inputCls}
                value={values[f.name] ?? ''}
                onChange={e => setValues(v => ({ ...v, [f.name]: e.target.value }))}
              />
            </div>
          ))}
        </div>

        {error && <p className="mt-2 text-[11px] text-red-600">{error}</p>}

        <div className="flex items-center gap-3 mt-5">
          <button
            type="button"
            disabled={saving}
            onClick={handleSave}
            className="h-8 px-4 bg-[var(--color-primary)] text-white text-[12px] font-medium rounded-lg disabled:opacity-50 hover:bg-[var(--color-primary-hover)] transition-colors"
          >
            {saving ? 'Saving…' : 'Save'}
          </button>
          <button
            type="button"
            onClick={onClose}
            className="h-8 px-3 text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  )
}
