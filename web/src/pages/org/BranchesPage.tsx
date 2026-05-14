import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'
import type { BranchDto } from '@/types/api'

const INDIAN_STATES = [
  'AN','AP','AR','AS','BR','CG','CH','DD','DL','DN',
  'GA','GJ','HP','HR','JH','JK','KA','KL','LA','LD',
  'MH','ML','MN','MP','MZ','NL','OR','PB','PY','RJ',
  'SK','TG','TN','TR','UP','UT','WB',
]

const schema = z.object({
  name: z.string().min(1, 'Required').max(200),
  state: z.string().min(1, 'Select a state'),
})
type FormValues = z.infer<typeof schema>

export default function BranchesPage(): React.ReactElement {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { data: branches, isLoading } = useQuery<BranchDto[]>({
    queryKey: ['branches'],
    queryFn: () => api.get<BranchDto[]>('/api/org/branches').then(r => r.data),
  })

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const create = useMutation({
    mutationFn: (v: FormValues) => api.post('/api/org/branches', v),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['branches'] })
      reset()
      setShowForm(false)
      setApiError(null)
    },
    onError: () => setApiError('Failed to create branch.'),
  })

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-semibold text-gray-900">Branches</h1>
        <button
          onClick={() => setShowForm(f => !f)}
          className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 transition-colors"
        >
          {showForm ? 'Cancel' : '+ New Branch'}
        </button>
      </div>

      {showForm && (
        <form
          onSubmit={handleSubmit(v => create.mutate(v))}
          className="bg-white border border-gray-200 rounded-xl p-5 mb-6 space-y-4 max-w-md"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input
              {...register('name')}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
            />
            {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">State</label>
            <select
              {...register('state')}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
            >
              <option value="">Select state</option>
              {INDIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
            {errors.state && <p className="mt-1 text-xs text-red-500">{errors.state.message}</p>}
          </div>
          {apiError && <p className="text-xs text-red-600">{apiError}</p>}
          <button
            type="submit"
            disabled={isSubmitting || create.isPending}
            className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 disabled:opacity-50 transition-colors"
          >
            Create Branch
          </button>
        </form>
      )}

      {isLoading ? (
        <p className="text-sm text-gray-500">Loading…</p>
      ) : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Name</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">State</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {branches?.length === 0 && (
                <tr><td colSpan={2} className="px-4 py-6 text-center text-gray-400">No branches yet.</td></tr>
              )}
              {branches?.map(b => (
                <tr key={b.id}>
                  <td className="px-4 py-3 text-gray-900">{b.name}</td>
                  <td className="px-4 py-3 text-gray-500">{b.state}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
