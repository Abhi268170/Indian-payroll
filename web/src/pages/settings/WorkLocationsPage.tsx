import { useState, useRef, useEffect, type ReactElement } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, MoreHorizontal, Users } from 'lucide-react'
import { api } from '@/lib/api'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { EmptyState } from '@/components/ui/EmptyState'
import { useToast } from '@/components/ui/useToast'
import WorkLocationFormPage from './WorkLocationFormPage'

export interface WorkLocation {
  id: string
  name: string
  addressLine1: string | null
  addressLine2: string | null
  state: string
  city: string | null
  pinCode: string | null
  ptRegistrationNumber: string | null
  isActive: boolean
  employeeCount: number
}

// Mirrors IndianState enum
export const INDIAN_STATES = [
  { value: 'AndhraPradesh', label: 'Andhra Pradesh' },
  { value: 'ArunachalPradesh', label: 'Arunachal Pradesh' },
  { value: 'Assam', label: 'Assam' },
  { value: 'Bihar', label: 'Bihar' },
  { value: 'Chhattisgarh', label: 'Chhattisgarh' },
  { value: 'Goa', label: 'Goa' },
  { value: 'Gujarat', label: 'Gujarat' },
  { value: 'Haryana', label: 'Haryana' },
  { value: 'HimachalPradesh', label: 'Himachal Pradesh' },
  { value: 'Jharkhand', label: 'Jharkhand' },
  { value: 'Karnataka', label: 'Karnataka' },
  { value: 'Kerala', label: 'Kerala' },
  { value: 'MadhyaPradesh', label: 'Madhya Pradesh' },
  { value: 'Maharashtra', label: 'Maharashtra' },
  { value: 'Manipur', label: 'Manipur' },
  { value: 'Meghalaya', label: 'Meghalaya' },
  { value: 'Mizoram', label: 'Mizoram' },
  { value: 'Nagaland', label: 'Nagaland' },
  { value: 'Odisha', label: 'Odisha' },
  { value: 'Punjab', label: 'Punjab' },
  { value: 'Rajasthan', label: 'Rajasthan' },
  { value: 'Sikkim', label: 'Sikkim' },
  { value: 'TamilNadu', label: 'Tamil Nadu' },
  { value: 'Telangana', label: 'Telangana' },
  { value: 'Tripura', label: 'Tripura' },
  { value: 'UttarPradesh', label: 'Uttar Pradesh' },
  { value: 'Uttarakhand', label: 'Uttarakhand' },
  { value: 'WestBengal', label: 'West Bengal' },
  { value: 'AndamanAndNicobar', label: 'Andaman & Nicobar Islands' },
  { value: 'Chandigarh', label: 'Chandigarh' },
  { value: 'DadraAndNagarHaveliAndDamanAndDiu', label: 'Dadra & Nagar Haveli and Daman & Diu' },
  { value: 'Delhi', label: 'Delhi' },
  { value: 'JammuAndKashmir', label: 'Jammu & Kashmir' },
  { value: 'Ladakh', label: 'Ladakh' },
  { value: 'Lakshadweep', label: 'Lakshadweep' },
  { value: 'Puducherry', label: 'Puducherry' },
] as const

export function stateLabel(value: string): string {
  return INDIAN_STATES.find(s => s.value === value)?.label ?? value
}

function ContextMenu({
  location,
  onToggleActive,
  onDelete,
}: {
  location: WorkLocation
  onToggleActive: () => void
  onDelete: () => void
}): ReactElement {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handler(e: MouseEvent): void {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    if (open) document.addEventListener('mousedown', handler)
    return () => { document.removeEventListener('mousedown', handler) }
  }, [open])

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
        aria-label="More options"
      >
        <MoreHorizontal className="w-4 h-4" />
      </button>
      {open && (
        <div className="absolute right-0 top-9 w-44 bg-white border border-[var(--color-border)] rounded-lg shadow-lg z-10 py-1">
          <button
            onClick={() => { setOpen(false); onToggleActive() }}
            className="w-full text-left px-4 py-2 text-[13px] text-[var(--color-text-primary)] hover:bg-gray-50 transition-colors"
          >
            {location.isActive ? 'Mark as Inactive' : 'Mark as Active'}
          </button>
          <button
            onClick={() => { setOpen(false); onDelete() }}
            className="w-full text-left px-4 py-2 text-[13px] text-[var(--color-error)] hover:bg-red-50 transition-colors"
          >
            Delete
          </button>
        </div>
      )}
    </div>
  )
}

function WorkLocationCard({
  location,
  onEdit,
  onToggleActive,
  onDelete,
}: {
  location: WorkLocation
  onEdit: () => void
  onToggleActive: () => void
  onDelete: () => void
}): ReactElement {
  const addressParts = [location.city, stateLabel(location.state), location.pinCode]
    .filter(Boolean)
    .join(', ')

  return (
    <div className="bg-white border border-[var(--color-border)] rounded-xl p-5 w-72 flex-shrink-0 relative">
      <div className="flex items-start justify-between mb-3">
        <h3 className="text-[14px] font-semibold text-[var(--color-text-primary)] pr-2 leading-snug">
          {location.name}
        </h3>
        <div className="flex items-center gap-0.5 flex-shrink-0">
          <button
            onClick={onEdit}
            title="Edit"
            className="inline-flex items-center justify-center w-8 h-8 rounded-full text-[var(--color-text-muted)] hover:bg-gray-100 transition-colors"
          >
            <Pencil className="w-3.5 h-3.5" />
          </button>
          <ContextMenu location={location} onToggleActive={onToggleActive} onDelete={onDelete} />
        </div>
      </div>

      <div className="space-y-0.5 mb-4">
        {location.addressLine1 && (
          <p className="text-[13px] text-[var(--color-text-secondary)]">{location.addressLine1}</p>
        )}
        {location.addressLine2 && (
          <p className="text-[13px] text-[var(--color-text-secondary)]">{location.addressLine2}</p>
        )}
        {addressParts && (
          <p className="text-[13px] text-[var(--color-text-secondary)]">{addressParts}</p>
        )}
      </div>

      <div className="flex items-center gap-1.5">
        <Users className="w-3.5 h-3.5 text-[var(--color-text-muted)]" />
        <span className="text-[12px] text-[var(--color-text-muted)]">
          {location.employeeCount} {location.employeeCount === 1 ? 'Employee' : 'Employees'}
        </span>
        {!location.isActive && (
          <span className="ml-auto text-[11px] bg-[var(--color-badge-grey-bg)] text-[var(--color-badge-grey-text)] px-2 py-0.5 rounded-full font-medium">
            Inactive
          </span>
        )}
      </div>
    </div>
  )
}

export default function WorkLocationsPage(): ReactElement {
  const [view, setView] = useState<'list' | 'new' | WorkLocation>('list')
  const { error: toastError, success: toastSuccess } = useToast()
  const qc = useQueryClient()

  const { data: locations = [], isLoading } = useQuery<WorkLocation[]>({
    queryKey: ['work-locations'],
    queryFn: () => api.get<WorkLocation[]>('/api/v1/work-locations').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/api/v1/work-locations/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
      toastSuccess('Work location deleted')
    },
    onError: () => {
      toastError('Cannot delete — employees are assigned to this location')
    },
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      api.post(`/api/v1/work-locations/${id}/${activate ? 'activate' : 'deactivate'}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['work-locations'] })
    },
    onError: () => {
      toastError('Failed to update status')
    },
  })

  function handleDelete(loc: WorkLocation): void {
    if (loc.employeeCount > 0) {
      toastError(`Cannot delete — ${loc.employeeCount} employee(s) assigned`)
      return
    }
    if (!confirm(`Delete "${loc.name}"? This cannot be undone.`)) return
    deleteMutation.mutate(loc.id)
  }

  if (view !== 'list') {
    return (
      <WorkLocationFormPage
        location={view === 'new' ? null : view}
        onSaved={() => {
          void qc.invalidateQueries({ queryKey: ['work-locations'] })
          setView('list')
        }}
        onCancel={() => setView('list')}
      />
    )
  }

  return (
    <div className="px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[20px] font-semibold text-[var(--color-text-primary)]">Work Locations</h1>
        <Button variant="primary" size="sm" onClick={() => setView('new')}>
          Add Work Location
        </Button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <Spinner />
        </div>
      ) : locations.length === 0 ? (
        <EmptyState
          heading="No work locations yet"
          subtext="Add offices and branches where your employees work"
          action={
            <Button variant="primary" size="sm" onClick={() => setView('new')}>
              Add Work Location
            </Button>
          }
        />
      ) : (
        <div className="flex flex-wrap gap-4">
          {locations.map(loc => (
            <WorkLocationCard
              key={loc.id}
              location={loc}
              onEdit={() => setView(loc)}
              onToggleActive={() => toggleMutation.mutate({ id: loc.id, activate: !loc.isActive })}
              onDelete={() => handleDelete(loc)}
            />
          ))}
        </div>
      )}
    </div>
  )
}
