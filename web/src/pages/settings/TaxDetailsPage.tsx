import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { useToast } from '@/components/ui/useToast'

interface TaxDetails {
  pan: string | null
  tan: string | null
  aoAreaCode: string | null
  aoType: string | null
  aoRangeCode: string | null
  aoNumber: string | null
  deductorType: string | null
  deductorName: string | null
  deductorFathersName: string | null
  deductorDesignation: string | null
  deductorEmployeeId: string | null
}

interface EmployeePickerItem {
  id: string
  employeeCode: string
  fullName: string
  workEmail: string
}

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

const DEDUCTOR_TYPES = [
  'Company', 'Government', 'HUF', 'Individual', 'Partnership Firm', 'Trust', 'Others',
]

const AO_TYPES = ['A', 'B', 'C', 'H', 'W', 'S', 'E', 'F', 'G', 'L']

function ViewRow({ label, value }: { label: string; value: string | null }): ReactElement {
  return (
    <div>
      <dt className="text-[12px] text-[var(--color-text-muted)] mb-0.5">{label}</dt>
      <dd className="text-[13px] text-[var(--color-text-primary)]">{value ?? '—'}</dd>
    </div>
  )
}

function Field({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}): ReactElement {
  return (
    <div>
      <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">
        {label}
      </label>
      {children}
    </div>
  )
}

export default function TaxDetailsPage(): ReactElement {
  const [editing, setEditing] = useState(false)

  const { data, isLoading } = useQuery<TaxDetails>({
    queryKey: ['tax-details'],
    queryFn: () => api.get<TaxDetails>('/api/v1/org-profile/tax-details').then(r => r.data),
  })

  if (isLoading) {
    return <div className="flex items-center justify-center py-20"><Spinner /></div>
  }

  if (editing && data) {
    return <TaxDetailsForm data={data} onCancel={() => { setEditing(false) }} onSaved={() => { setEditing(false) }} />
  }

  return (
    <div className="px-8 py-8 max-w-2xl">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Tax Details</h1>
          <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
            TDS filing details for Form 24Q / Form 16 generation
          </p>
        </div>
        <Button variant="secondary" size="sm" onClick={() => { setEditing(true) }}>
          <Pencil className="w-3.5 h-3.5 mr-1.5" />
          Edit
        </Button>
      </div>

      <div className="space-y-6">
        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">Registration Numbers</h2>
          <div className="grid grid-cols-2 gap-x-12 gap-y-4">
            <ViewRow label="PAN" value={data?.pan ?? null} />
            <ViewRow label="TAN (Tax Deduction Account Number)" value={data?.tan ?? null} />
          </div>
        </section>

        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">AO Code</h2>
          <p className="text-[12px] text-[var(--color-text-muted)] mb-4">
            Assessing Officer code required for TDS returns. Available on your TAN allotment letter.
          </p>
          <div className="grid grid-cols-2 gap-x-12 gap-y-4">
            <ViewRow label="Area Code" value={data?.aoAreaCode ?? null} />
            <ViewRow label="AO Type" value={data?.aoType ?? null} />
            <ViewRow label="Range Code" value={data?.aoRangeCode ?? null} />
            <ViewRow label="AO Number" value={data?.aoNumber ?? null} />
          </div>
        </section>

        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">Deductor Details</h2>
          <div className="grid grid-cols-2 gap-x-12 gap-y-4">
            <ViewRow label="Deductor Type" value={data?.deductorType ?? null} />
            <ViewRow label="Name of Deductor" value={data?.deductorName ?? null} />
            {data?.deductorType !== 'Company' && data?.deductorType !== null && (
              <>
                <ViewRow label="Father's Name of Deductor" value={data?.deductorFathersName ?? null} />
                <ViewRow label="Designation of Deductor" value={data?.deductorDesignation ?? null} />
              </>
            )}
            <div className="col-span-2">
              <DeductorEmployeeView employeeId={data?.deductorEmployeeId ?? null} />
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

function DeductorEmployeeView({ employeeId }: { employeeId: string | null }): ReactElement {
  const { data } = useQuery<EmployeePickerItem | null>({
    queryKey: ['employee-summary', employeeId],
    queryFn: async () => {
      if (!employeeId) return null
      const res = await api.get<EmployeePickerItem>(`/api/v1/employees/${employeeId}`)
      return res.data
    },
    enabled: !!employeeId,
  })
  const label = !employeeId
    ? '— (required for exit initiation)'
    : data
      ? `${data.fullName} (${data.employeeCode})`
      : 'Loading…'
  return (
    <div>
      <dt className="text-[12px] text-[var(--color-text-muted)] mb-0.5">Tax Deductor Employee</dt>
      <dd className="text-[13px] text-[var(--color-text-primary)]">{label}</dd>
      <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
        Used as the responsible signatory for Form 16 / Form 24Q. Required before initiating any employee exit.
      </p>
    </div>
  )
}

function TaxDetailsForm({
  data,
  onCancel,
  onSaved,
}: {
  data: TaxDetails
  onCancel: () => void
  onSaved: () => void
}): ReactElement {
  const qc = useQueryClient()
  const toast = useToast()

  const [form, setForm] = useState({
    tan: data.tan ?? '',
    aoAreaCode: data.aoAreaCode ?? '',
    aoType: data.aoType ?? '',
    aoRangeCode: data.aoRangeCode ?? '',
    aoNumber: data.aoNumber ?? '',
    deductorType: data.deductorType ?? '',
    deductorName: data.deductorName ?? '',
    deductorFathersName: data.deductorFathersName ?? '',
    deductorDesignation: data.deductorDesignation ?? '',
    deductorEmployeeId: data.deductorEmployeeId ?? '',
  })

  const save = useMutation({
    mutationFn: () =>
      api.put('/api/v1/org-profile/tax-details', {
        tan: form.tan || null,
        aoAreaCode: form.aoAreaCode || null,
        aoType: form.aoType || null,
        aoRangeCode: form.aoRangeCode || null,
        aoNumber: form.aoNumber || null,
        deductorType: form.deductorType || null,
        deductorName: form.deductorName || null,
        deductorFathersName: form.deductorFathersName || null,
        deductorDesignation: form.deductorDesignation || null,
        deductorEmployeeId: form.deductorEmployeeId || null,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['tax-details'] })
      toast.success('Tax details saved')
      onSaved()
    },
    onError: () => { toast.error('Failed to save tax details') },
  })

  const isNonCompany = form.deductorType && form.deductorType !== 'Company'

  return (
    <div className="px-8 py-8 max-w-2xl">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Tax Details</h1>
          <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">TDS filing details for Form 24Q / Form 16 generation</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" onClick={onCancel}>Cancel</Button>
          <Button size="sm" loading={save.isPending} onClick={() => { save.mutate() }}>Save</Button>
        </div>
      </div>

      <div className="space-y-6">
        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">Registration Numbers</h2>
          <div className="grid grid-cols-2 gap-5">
            <Field label="TAN (Tax Deduction Account Number)">
              <input
                className="form-input w-full uppercase"
                placeholder="e.g. MUMB12345A"
                maxLength={10}
                value={form.tan}
                onChange={e => { setForm(f => ({ ...f, tan: e.target.value.toUpperCase() })) }}
              />
            </Field>
            <div>
              <label className="block text-[13px] font-medium text-[var(--color-text-muted)] mb-1">PAN</label>
              <div className="h-9 px-3 flex items-center border border-[var(--color-border)] rounded-lg bg-gray-50 text-[13px] text-[var(--color-text-muted)]">
                {data.pan ?? 'Set in Company Profile'}
              </div>
              <p className="text-[11px] text-[var(--color-text-muted)] mt-1">Edit PAN via Settings → Company Profile</p>
            </div>
          </div>
        </section>

        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-1">AO Code</h2>
          <p className="text-[12px] text-[var(--color-text-muted)] mb-4">
            Assessing Officer code required for TDS returns. Available on your TAN allotment letter.
          </p>
          <div className="grid grid-cols-2 gap-5">
            <Field label="Area Code">
              <input
                className="form-input w-full uppercase"
                placeholder="e.g. MUM"
                maxLength={3}
                value={form.aoAreaCode}
                onChange={e => { setForm(f => ({ ...f, aoAreaCode: e.target.value.toUpperCase() })) }}
              />
            </Field>
            <Field label="AO Type">
              <select
                className="form-input w-full"
                value={form.aoType}
                onChange={e => { setForm(f => ({ ...f, aoType: e.target.value })) }}
              >
                <option value="">Select</option>
                {AO_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </Field>
            <Field label="Range Code">
              <input
                className="form-input w-full"
                placeholder="e.g. 3"
                maxLength={3}
                value={form.aoRangeCode}
                onChange={e => { setForm(f => ({ ...f, aoRangeCode: e.target.value })) }}
              />
            </Field>
            <Field label="AO Number">
              <input
                className="form-input w-full"
                placeholder="e.g. 1"
                maxLength={5}
                value={form.aoNumber}
                onChange={e => { setForm(f => ({ ...f, aoNumber: e.target.value })) }}
              />
            </Field>
          </div>
        </section>

        <section className="bg-white border border-[var(--color-border)] rounded-xl p-6">
          <h2 className="text-[14px] font-semibold text-[var(--color-text-primary)] mb-4">Deductor Details</h2>
          <div className="space-y-4">
            <Field label="Deductor Type">
              <select
                className="form-input w-60"
                value={form.deductorType}
                onChange={e => { setForm(f => ({ ...f, deductorType: e.target.value })) }}
              >
                <option value="">Select type</option>
                {DEDUCTOR_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </Field>
            <Field label="Name of Deductor">
              <input
                className="form-input w-full"
                placeholder="Organisation name as per TAN"
                maxLength={200}
                value={form.deductorName}
                onChange={e => { setForm(f => ({ ...f, deductorName: e.target.value })) }}
              />
            </Field>
            {isNonCompany && (
              <>
                <Field label="Father's Name of Deductor">
                  <input
                    className="form-input w-full"
                    maxLength={200}
                    value={form.deductorFathersName}
                    onChange={e => { setForm(f => ({ ...f, deductorFathersName: e.target.value })) }}
                  />
                </Field>
                <Field label="Designation of Deductor">
                  <input
                    className="form-input w-full"
                    maxLength={200}
                    value={form.deductorDesignation}
                    onChange={e => { setForm(f => ({ ...f, deductorDesignation: e.target.value })) }}
                  />
                </Field>
              </>
            )}
            <Field label="Tax Deductor Employee">
              <DeductorEmployeePicker
                value={form.deductorEmployeeId}
                onChange={id => { setForm(f => ({ ...f, deductorEmployeeId: id })) }}
              />
              <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
                Required signatory for Form 16 / Form 24Q. Must be assigned before initiating any employee exit.
              </p>
            </Field>
          </div>
        </section>
      </div>
    </div>
  )
}

function DeductorEmployeePicker({
  value, onChange,
}: { value: string; onChange: (id: string) => void }): ReactElement {
  const [search, setSearch] = useState('')
  const { data } = useQuery<PagedResult<EmployeePickerItem>>({
    queryKey: ['employees', 'deductor-picker', search],
    queryFn: () => api.get<PagedResult<EmployeePickerItem>>('/api/v1/employees', {
      params: { page: 1, pageSize: 25, status: 'Active', search: search || undefined },
    }).then(r => r.data),
  })
  const items = data?.items ?? []
  return (
    <div className="space-y-2">
      <input
        type="text"
        className="form-input w-full"
        placeholder="Search by name, email or code…"
        value={search}
        onChange={e => { setSearch(e.target.value) }}
      />
      <select
        className="form-input w-full"
        value={value}
        onChange={e => { onChange(e.target.value) }}
      >
        <option value="">— No employee assigned —</option>
        {items.map(emp => (
          <option key={emp.id} value={emp.id}>{emp.fullName} ({emp.employeeCode})</option>
        ))}
      </select>
    </div>
  )
}
