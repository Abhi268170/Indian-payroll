import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'

const SLUG_REGEX = /^[a-z0-9]+(-[a-z0-9]+)*$/

const schema = z.object({
  displayName: z.string().min(1, 'Required').max(200),
  adminEmail: z.string().min(1, 'Required').email('Invalid email'),
  slug: z.string().min(1, 'Required').max(63).regex(SLUG_REGEX, 'Lowercase letters, numbers, hyphens only'),
})
type FormValues = z.infer<typeof schema>

function deriveSlug(name: string): string {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '')
}

export default function ProvisionOrgPage(): React.ReactElement {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [slugTouched, setSlugTouched] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const provision = useMutation({
    mutationFn: (v: FormValues) => api.post<{ id: string }>('/api/tenants', {
      displayName: v.displayName,
      adminEmail: v.adminEmail,
      slug: v.slug,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform-tenants'] })
      navigate('/platform/orgs')
    },
    onError: (err: unknown) => {
      const status = (err as { response?: { status?: number } }).response?.status
      if (status === 409) {
        setApiError('That slug is already taken. Choose a different one.')
      } else {
        setApiError('Provisioning failed. Please try again.')
      }
    },
  })

  function onNameChange(e: React.ChangeEvent<HTMLInputElement>): void {
    if (!slugTouched) setValue('slug', deriveSlug(e.target.value))
  }

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900'
  const errorCls = 'mt-1 text-xs text-red-500'

  return (
    <div className="max-w-md">
      <Link to="/platform/orgs" className="text-sm text-gray-500 hover:text-gray-700 mb-6 inline-block">
        ← Back to Organisations
      </Link>

      <h1 className="text-xl font-semibold text-gray-900 mb-6">Provision New Organisation</h1>

      <form
        onSubmit={handleSubmit(v => { setApiError(null); provision.mutate(v) })}
        className="bg-white border border-gray-200 rounded-xl p-6 space-y-5"
      >
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Organisation Name</label>
          <input
            {...register('displayName')}
            onChange={e => { register('displayName').onChange(e); onNameChange(e) }}
            className={inputCls}
            placeholder="Acme Corp"
          />
          {errors.displayName && <p className={errorCls}>{errors.displayName.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Admin Email</label>
          <input
            {...register('adminEmail')}
            type="email"
            className={inputCls}
            placeholder="admin@acme.com"
          />
          <p className="mt-1 text-xs text-gray-400">
            A welcome email with a set-password link will be sent to this address.
          </p>
          {errors.adminEmail && <p className={errorCls}>{errors.adminEmail.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
          <input
            {...register('slug')}
            onChange={e => { register('slug').onChange(e); setSlugTouched(true) }}
            className={inputCls}
            placeholder="acme-corp"
          />
          <p className="mt-1 text-xs text-gray-400">
            Used as the subdomain identifier. Auto-generated from name, editable.
          </p>
          {errors.slug && <p className={errorCls}>{errors.slug.message}</p>}
        </div>

        {apiError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{apiError}</p>}

        <button
          type="submit"
          disabled={isSubmitting || provision.isPending}
          className="w-full bg-gray-900 text-white rounded-lg py-2 text-sm font-medium hover:bg-gray-700 disabled:opacity-50 transition-colors"
        >
          {provision.isPending ? 'Provisioning…' : 'Provision Organisation'}
        </button>
      </form>
    </div>
  )
}
