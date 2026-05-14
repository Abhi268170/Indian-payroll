import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { TenantDto } from '@/types/api'

export default function OrgDetailPage(): React.ReactElement {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()

  const { data: tenant, isLoading, isError, error } = useQuery<TenantDto>({
    queryKey: ['platform-tenant', id],
    queryFn: () => api.get<TenantDto>(`/api/tenants/${id}`).then(r => r.data),
    enabled: !!id,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform-tenants'] })
    queryClient.invalidateQueries({ queryKey: ['platform-tenant', id] })
  }

  const suspendMutation = useMutation({
    mutationFn: () => api.post(`/api/tenants/${id}/suspend`),
    onSuccess: invalidate,
  })

  const activateMutation = useMutation({
    mutationFn: () => api.post(`/api/tenants/${id}/activate`),
    onSuccess: invalidate,
  })

  const resendMutation = useMutation({
    mutationFn: () => api.post(`/api/tenants/${id}/resend-setup-email`),
  })

  const anyMutating = suspendMutation.isPending || activateMutation.isPending || resendMutation.isPending

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-sm text-gray-500">Loading…</p>
      </div>
    )
  }

  if (isError || !tenant) {
    const status = (error as { response?: { status?: number } })?.response?.status
    const msg = status ? `Error ${status}` : 'Request failed'
    return (
      <div className="flex flex-col items-center justify-center py-20 gap-2">
        <p className="text-sm text-red-500">Failed to load organisation. ({msg})</p>
        <Link to="/platform/orgs" className="text-sm text-gray-500 hover:text-gray-700">← Back</Link>
      </div>
    )
  }

  return (
    <div className="max-w-2xl">
      <div className="mb-6">
        <Link to="/platform/orgs" className="text-sm text-gray-500 hover:text-gray-700">
          ← All Orgs
        </Link>
      </div>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-xl font-semibold text-gray-900">{tenant.displayName}</h1>
          <p className="text-sm text-gray-500 mt-0.5">{tenant.slug}</p>
        </div>
        <span className={`mt-1 inline-block px-2.5 py-1 rounded-full text-xs font-medium ${
          tenant.isActive ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-600'
        }`}>
          {tenant.isActive ? 'Active' : 'Suspended'}
        </span>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl divide-y divide-gray-100 mb-6">
        <Row label="Display Name" value={tenant.displayName} />
        <Row label="Slug" value={<code className="text-xs bg-gray-100 px-2 py-0.5 rounded">{tenant.slug}</code>} />
        <Row label="Schema" value={<code className="text-xs bg-gray-100 px-2 py-0.5 rounded">{tenant.schema ?? '—'}</code>} />
        <Row label="Admin Email" value={tenant.adminEmail ?? '—'} />
        <Row label="Created" value={new Date(tenant.createdAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })} />
      </div>

      <div className="flex gap-3 flex-wrap">
        {tenant.isActive ? (
          <ActionButton
            label="Suspend Org"
            onClick={() => suspendMutation.mutate()}
            disabled={anyMutating}
            variant="danger"
          />
        ) : (
          <ActionButton
            label="Activate Org"
            onClick={() => activateMutation.mutate()}
            disabled={anyMutating}
            variant="primary"
          />
        )}
        <ActionButton
          label={resendMutation.isSuccess ? 'Email Sent ✓' : 'Resend Setup Email'}
          onClick={() => resendMutation.mutate()}
          disabled={anyMutating || resendMutation.isSuccess}
          variant="secondary"
        />
      </div>

      {suspendMutation.isError && (
        <p className="mt-3 text-sm text-red-500">Failed to suspend. Please try again.</p>
      )}
      {activateMutation.isError && (
        <p className="mt-3 text-sm text-red-500">Failed to activate. Please try again.</p>
      )}
      {resendMutation.isError && (
        <p className="mt-3 text-sm text-red-500">Failed to resend email. Please try again.</p>
      )}
    </div>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }): React.ReactElement {
  return (
    <div className="flex items-center px-4 py-3">
      <span className="w-40 text-sm text-gray-500 shrink-0">{label}</span>
      <span className="text-sm text-gray-900">{value}</span>
    </div>
  )
}

function ActionButton({
  label,
  onClick,
  disabled,
  variant,
}: {
  label: string
  onClick: () => void
  disabled: boolean
  variant: 'primary' | 'secondary' | 'danger'
}): React.ReactElement {
  const base = 'text-sm px-4 py-2 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed'
  const styles = {
    primary: 'bg-gray-900 text-white hover:bg-gray-700',
    secondary: 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50',
    danger: 'bg-red-600 text-white hover:bg-red-700',
  }
  return (
    <button className={`${base} ${styles[variant]}`} onClick={onClick} disabled={disabled}>
      {label}
    </button>
  )
}
