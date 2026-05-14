import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'
import type { DepartmentDto } from '@/types/api'

const schema = z.object({
  name: z.string().min(1, 'Required').max(200),
  code: z.string().max(50).optional(),
})
type FormValues = z.infer<typeof schema>

export default function DepartmentsPage(): React.ReactElement {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { data: departments, isLoading } = useQuery<DepartmentDto[]>({
    queryKey: ['departments'],
    queryFn: () => api.get<DepartmentDto[]>('/api/org/departments').then(r => r.data),
  })

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const create = useMutation({
    mutationFn: (v: FormValues) => api.post('/api/org/departments', { name: v.name, code: v.code ?? null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['departments'] }); reset(); setShowForm(false); setApiError(null) },
    onError: () => setApiError('Failed to create department.'),
  })

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-semibold text-gray-900">Departments</h1>
        <button onClick={() => setShowForm(f => !f)} className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 transition-colors">
          {showForm ? 'Cancel' : '+ New Department'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(v => create.mutate(v))} className="bg-white border border-gray-200 rounded-xl p-5 mb-6 space-y-4 max-w-md">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input {...register('name')} className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500" />
            {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Code (optional)</label>
            <input {...register('code')} className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500" placeholder="e.g. ENG" />
          </div>
          {apiError && <p className="text-xs text-red-600">{apiError}</p>}
          <button type="submit" disabled={isSubmitting || create.isPending} className="bg-violet-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-violet-700 disabled:opacity-50 transition-colors">
            Create Department
          </button>
        </form>
      )}

      {isLoading ? <p className="text-sm text-gray-500">Loading…</p> : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Name</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Code</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {departments?.length === 0 && <tr><td colSpan={2} className="px-4 py-6 text-center text-gray-400">No departments yet.</td></tr>}
              {departments?.map(d => (
                <tr key={d.id}>
                  <td className="px-4 py-3 text-gray-900">{d.name}</td>
                  <td className="px-4 py-3 text-gray-500">{d.code ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
