import { useState, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { MapPin, Pencil, Plus, Trash2, Hash } from 'lucide-react'
import { api } from '@/lib/api'
import { Spinner } from '@/components/ui/Spinner'
import { Button } from '@/components/ui/Button'
import { useToast } from '@/components/ui/useToast'
import type { WorkLocation } from '../WorkLocationsPage'

interface PtRegistration {
  stateCode: string
  registrationNumber: string
}

interface PtSlab {
  stateCode: string
  effectiveDate: string
  frequency: string
  gender: string | null
  minGross: number
  maxGross: number | null
  ptAmount: number
  isFebruarySurcharge: boolean
}

const STATE_NAMES: Record<string, string> = {
  MH: 'Maharashtra', KA: 'Karnataka', AP: 'Andhra Pradesh',
  TS: 'Telangana', WB: 'West Bengal', TN: 'Tamil Nadu',
  KL: 'Kerala', GJ: 'Gujarat', MP: 'Madhya Pradesh',
  OR: 'Odisha', AS: 'Assam', SK: 'Sikkim',
  ML: 'Meghalaya', TR: 'Tripura', JH: 'Jharkhand',
}

function formatCurrency(amount: number): string {
  return `₹${amount.toLocaleString('en-IN')}`
}

interface SlabRow {
  minGross: string
  maxGross: string
  ptAmount: string
  gender: string
  isFebruarySurcharge: boolean
}

function ReviseModal({
  stateCode,
  stateName,
  onClose,
}: {
  stateCode: string
  stateName: string
  onClose: () => void
}): ReactElement {
  const today = new Date().toISOString().slice(0, 10)
  const qc = useQueryClient()
  const toast = useToast()

  const { data: existing, isLoading } = useQuery<PtSlab[]>({
    queryKey: ['pt-slabs', stateCode],
    queryFn: () => api.get<PtSlab[]>(`/api/v1/statutory/pt-slabs/${stateCode}?asOf=${today}`).then(r => r.data),
  })

  const [effectiveDate, setEffectiveDate] = useState(today)
  const [frequency, setFrequency] = useState('Monthly')
  const [deductionMonthsCsv, setDeductionMonthsCsv] = useState('')
  const [rows, setRows] = useState<SlabRow[]>([])
  const [initialized, setInitialized] = useState(false)

  if (!initialized && existing) {
    setRows(
      existing.length > 0
        ? existing.map(s => ({
            minGross: String(s.minGross),
            maxGross: s.maxGross != null ? String(s.maxGross) : '',
            ptAmount: String(s.ptAmount),
            gender: s.gender ?? '',
            isFebruarySurcharge: s.isFebruarySurcharge,
          }))
        : [{ minGross: '0', maxGross: '', ptAmount: '0', gender: '', isFebruarySurcharge: false }]
    )
    const first = existing[0]
    if (first?.frequency) setFrequency(first.frequency)
    setInitialized(true)
  }

  const save = useMutation({
    mutationFn: () =>
      api.post(`/api/v1/statutory/pt-slabs/${stateCode}`, {
        effectiveDate,
        frequency,
        deductionMonthsCsv: frequency !== 'Monthly' ? (deductionMonthsCsv || null) : null,
        slabs: rows.map(r => ({
          minGross: parseFloat(r.minGross) || 0,
          maxGross: r.maxGross ? parseFloat(r.maxGross) : null,
          ptAmount: parseFloat(r.ptAmount) || 0,
          gender: r.gender || null,
          isFebruarySurcharge: r.isFebruarySurcharge,
        })),
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['pt-slabs', stateCode] })
      toast.success('PT slabs updated')
      onClose()
    },
    onError: () => { toast.error('Failed to save PT slabs') },
  })

  function addRow(): void {
    setRows(prev => [...prev, { minGross: '', maxGross: '', ptAmount: '0', gender: '', isFebruarySurcharge: false }])
  }

  function removeRow(i: number): void {
    setRows(prev => prev.filter((_, idx) => idx !== i))
  }

  function updateRow(i: number, patch: Partial<SlabRow>): void {
    setRows(prev => prev.map((r, idx) => idx === i ? { ...r, ...patch } : r))
  }

  const hasGender = rows.some(r => r.gender !== '')

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="bg-white rounded-xl shadow-xl w-[720px] max-h-[85vh] flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <h3 className="text-[15px] font-semibold">{stateName} — Revise PT Slabs</h3>
          <button onClick={onClose} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-lg leading-none">×</button>
        </div>

        {isLoading ? (
          <div className="flex-1 flex items-center justify-center py-12"><Spinner /></div>
        ) : (
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-5">
            <div className="grid grid-cols-2 gap-5">
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-primary)] mb-1">
                  Effective Date
                </label>
                <input
                  type="date"
                  className="form-input w-full"
                  value={effectiveDate}
                  onChange={e => { setEffectiveDate(e.target.value) }}
                />
              </div>
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-primary)] mb-1">
                  Frequency
                </label>
                <select
                  className="form-input w-full"
                  value={frequency}
                  onChange={e => { setFrequency(e.target.value) }}
                >
                  <option value="Monthly">Monthly</option>
                  <option value="HalfYearly">Half-Yearly</option>
                  <option value="Annual">Annual</option>
                </select>
              </div>
            </div>
            {frequency !== 'Monthly' && (
              <div>
                <label className="block text-[12px] font-medium text-[var(--color-text-primary)] mb-1">
                  Deduction Months (comma-separated, e.g. 9,3)
                </label>
                <input
                  className="form-input w-full"
                  placeholder="e.g. 9,3 for September and March"
                  value={deductionMonthsCsv}
                  onChange={e => { setDeductionMonthsCsv(e.target.value) }}
                />
                <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
                  Month numbers 1–12. Common: Half-Yearly → 9,3 · Annual → 3
                </p>
              </div>
            )}

            <div>
              <p className="text-[11px] text-[var(--color-text-muted)] mb-2">
                New slabs apply from the effective date. Existing slabs are retained for historical pay run reproducibility.
              </p>
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="text-left text-[11px] uppercase tracking-wide text-[var(--color-text-muted)] border-b border-[var(--color-border)]">
                    <th className="pb-2 font-medium">Min Gross (₹)</th>
                    <th className="pb-2 font-medium">Max Gross (₹)</th>
                    {hasGender && <th className="pb-2 font-medium">Gender</th>}
                    <th className="pb-2 font-medium">PT Amount (₹)</th>
                    <th className="pb-2 font-medium">+Feb</th>
                    <th className="pb-2"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--color-border)]">
                  {rows.map((row, i) => (
                    <tr key={i}>
                      <td className="py-1.5 pr-2">
                        <input
                          type="number"
                          className="form-input w-24"
                          value={row.minGross}
                          onChange={e => { updateRow(i, { minGross: e.target.value }) }}
                        />
                      </td>
                      <td className="py-1.5 pr-2">
                        <input
                          type="number"
                          className="form-input w-24"
                          placeholder="No limit"
                          value={row.maxGross}
                          onChange={e => { updateRow(i, { maxGross: e.target.value }) }}
                        />
                      </td>
                      {hasGender && (
                        <td className="py-1.5 pr-2">
                          <select
                            className="form-input w-24"
                            value={row.gender}
                            onChange={e => { updateRow(i, { gender: e.target.value }) }}
                          >
                            <option value="">All</option>
                            <option value="Male">Male</option>
                            <option value="Female">Female</option>
                          </select>
                        </td>
                      )}
                      <td className="py-1.5 pr-2">
                        <input
                          type="number"
                          className="form-input w-24"
                          value={row.ptAmount}
                          onChange={e => { updateRow(i, { ptAmount: e.target.value }) }}
                        />
                      </td>
                      <td className="py-1.5 pr-2 text-center">
                        <input
                          type="checkbox"
                          checked={row.isFebruarySurcharge}
                          onChange={e => { updateRow(i, { isFebruarySurcharge: e.target.checked }) }}
                          className="h-4 w-4"
                        />
                      </td>
                      <td className="py-1.5">
                        <button
                          type="button"
                          onClick={() => { removeRow(i) }}
                          className="text-[var(--color-text-muted)] hover:text-[var(--color-error)]"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <button
                type="button"
                onClick={addRow}
                className="mt-2 flex items-center gap-1 text-[12px] text-[var(--color-primary)] hover:underline"
              >
                <Plus className="w-3.5 h-3.5" /> Add row
              </button>
            </div>
          </div>
        )}

        <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-[var(--color-border)]">
          <Button variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          <Button size="sm" loading={save.isPending} onClick={() => { save.mutate() }}>
            Save Slabs
          </Button>
        </div>
      </div>
    </div>
  )
}

function ViewSlabsModal({
  stateCode,
  stateName,
  onClose,
}: {
  stateCode: string
  stateName: string
  onClose: () => void
}): ReactElement {
  const today = new Date().toISOString().slice(0, 10)
  const { data: slabs, isLoading } = useQuery<PtSlab[]>({
    queryKey: ['pt-slabs', stateCode],
    queryFn: () => api.get<PtSlab[]>(`/api/v1/statutory/pt-slabs/${stateCode}?asOf=${today}`).then(r => r.data),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="bg-white rounded-xl shadow-xl w-[600px] max-h-[80vh] flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <h3 className="text-[15px] font-semibold">{stateName} — PT Slabs</h3>
          <button onClick={onClose} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-lg leading-none">×</button>
        </div>
        <div className="overflow-y-auto flex-1 px-6 py-4">
          {isLoading ? (
            <Spinner />
          ) : (
            <table className="w-full text-[13px]">
              <thead>
                <tr className="text-left text-[11px] uppercase tracking-wide text-[var(--color-text-muted)] border-b border-[var(--color-border)]">
                  <th className="pb-2 font-medium">Min Gross</th>
                  <th className="pb-2 font-medium">Max Gross</th>
                  {slabs?.some(s => s.gender) && <th className="pb-2 font-medium">Gender</th>}
                  <th className="pb-2 font-medium">PT Amount</th>
                  <th className="pb-2 font-medium">Frequency</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {slabs?.map((s, i) => (
                  <tr key={i}>
                    <td className="py-2">{formatCurrency(s.minGross)}</td>
                    <td className="py-2">{s.maxGross != null ? formatCurrency(s.maxGross) : 'No limit'}</td>
                    {slabs.some(x => x.gender) && <td className="py-2">{s.gender ?? 'All'}</td>}
                    <td className="py-2">{formatCurrency(s.ptAmount)}</td>
                    <td className="py-2">
                      {s.frequency}
                      {s.isFebruarySurcharge && (
                        <span className="ml-1 text-[11px] text-[var(--color-primary)]">+Feb</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}

function PtNumberModal({
  stateCode,
  stateName,
  existing,
  onClose,
}: {
  stateCode: string
  stateName: string
  existing: string
  onClose: () => void
}): ReactElement {
  const [value, setValue] = useState(existing)
  const qc = useQueryClient()
  const toast = useToast()

  const save = useMutation({
    mutationFn: () =>
      api.put(`/api/v1/statutory/pt-registrations/${stateCode}`, { registrationNumber: value }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['pt-registrations'] })
      toast.success('PT number updated')
      onClose()
    },
    onError: () => { toast.error('Failed to save PT number') },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="bg-white rounded-xl shadow-xl w-[420px]">
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <h3 className="text-[15px] font-semibold">{stateName} — PT Number</h3>
          <button onClick={onClose} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-lg leading-none">×</button>
        </div>
        <div className="px-6 py-4 space-y-4">
          <div>
            <label className="block text-[12px] font-medium text-[var(--color-text-primary)] mb-1">
              Professional Tax Registration Number
            </label>
            <input
              className="form-input w-full"
              placeholder="e.g. 27AAACV9847J1ZA"
              value={value}
              onChange={e => { setValue(e.target.value) }}
            />
          </div>
        </div>
        <div className="flex items-center justify-end gap-2 px-6 py-4 border-t border-[var(--color-border)]">
          <Button variant="secondary" size="sm" onClick={onClose}>Cancel</Button>
          <Button size="sm" loading={save.isPending} onClick={() => { save.mutate() }}>Save</Button>
        </div>
      </div>
    </div>
  )
}

export default function PtTab(): ReactElement {
  const [viewState, setViewState] = useState<string | null>(null)
  const [reviseState, setReviseState] = useState<string | null>(null)
  const [ptNumberState, setPtNumberState] = useState<string | null>(null)

  const { data: locations } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })

  const { data: ptRegistrations } = useQuery<PtRegistration[]>({
    queryKey: ['pt-registrations'],
    queryFn: () => api.get<PtRegistration[]>('/api/v1/statutory/pt-registrations').then(r => r.data),
  })

  const stateCodeMap: Record<string, string> = {
    Maharashtra: 'MH', Karnataka: 'KA', AndhraPradesh: 'AP',
    Telangana: 'TS', WestBengal: 'WB', TamilNadu: 'TN',
    Kerala: 'KL', Gujarat: 'GJ', MadhyaPradesh: 'MP',
    Odisha: 'OR', Assam: 'AS', Sikkim: 'SK',
    Meghalaya: 'ML', Tripura: 'TR', Jharkhand: 'JH',
  }

  const uniqueStates = [...new Set((locations ?? []).map(l => l.state))]
    .filter(s => stateCodeMap[s])

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-[15px] font-semibold text-[var(--color-text-primary)]">
          Professional Tax
        </h3>
        <p className="text-[13px] text-[var(--color-text-muted)] mt-0.5">
          PT is determined by employee's work location state. Configure work locations to enable PT.
        </p>
      </div>

      {uniqueStates.length === 0 ? (
        <div className="text-center py-12 text-[var(--color-text-muted)]">
          <MapPin className="w-8 h-8 mx-auto mb-3 opacity-40" />
          <p className="text-[13px]">No work locations with PT-applicable states configured.</p>
          <p className="text-[12px] mt-1">Add work locations in Settings → Work Locations first.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {uniqueStates.map(stateEnum => {
            const code = stateCodeMap[stateEnum] ?? stateEnum
            const stateName = STATE_NAMES[code] ?? stateEnum
            const locs = (locations ?? []).filter(l => l.state === stateEnum)
            const reg = (ptRegistrations ?? []).find(r => r.stateCode === code)
            return (
              <div
                key={stateEnum}
                className="border border-[var(--color-border)] rounded-lg p-4"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="text-[14px] font-semibold text-[var(--color-text-primary)]">
                      {stateName}
                    </div>
                    <div className="text-[12px] text-[var(--color-text-muted)] mt-0.5">
                      {locs.map(l => l.name).join(', ')}
                    </div>
                    <div className="flex items-center gap-4 mt-2">
                      <span className="flex items-center gap-1 text-[12px] text-[var(--color-text-muted)]">
                        <Hash className="w-3 h-3" />
                        {reg ? (
                          <span className="text-[var(--color-text-primary)]">{reg.registrationNumber}</span>
                        ) : (
                          <button
                            className="text-[var(--color-primary)] hover:underline"
                            onClick={() => { setPtNumberState(code) }}
                          >
                            Add PT Number
                          </button>
                        )}
                      </span>
                      {reg && (
                        <button
                          className="text-[12px] text-[var(--color-text-muted)] hover:text-[var(--color-primary)]"
                          onClick={() => { setPtNumberState(code) }}
                        >
                          Update PT Number
                        </button>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      className="text-[13px] text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] font-medium"
                      onClick={() => { setViewState(code) }}
                    >
                      View Slabs
                    </button>
                    <button
                      className="flex items-center gap-1 text-[13px] text-[var(--color-primary)] hover:underline font-medium"
                      onClick={() => { setReviseState(code) }}
                    >
                      <Pencil className="w-3.5 h-3.5" /> Revise Slabs
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {viewState && (
        <ViewSlabsModal
          stateCode={viewState}
          stateName={STATE_NAMES[viewState] ?? viewState}
          onClose={() => { setViewState(null) }}
        />
      )}
      {reviseState && (
        <ReviseModal
          stateCode={reviseState}
          stateName={STATE_NAMES[reviseState] ?? reviseState}
          onClose={() => { setReviseState(null) }}
        />
      )}
      {ptNumberState && (
        <PtNumberModal
          stateCode={ptNumberState}
          stateName={STATE_NAMES[ptNumberState] ?? ptNumberState}
          existing={(ptRegistrations ?? []).find(r => r.stateCode === ptNumberState)?.registrationNumber ?? ''}
          onClose={() => { setPtNumberState(null) }}
        />
      )}
    </div>
  )
}
