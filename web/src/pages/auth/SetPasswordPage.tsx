import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'

const schema = z.object({
  newPassword: z.string().min(8, 'Minimum 8 characters')
    .regex(/[A-Z]/, 'Must contain uppercase')
    .regex(/[a-z]/, 'Must contain lowercase')
    .regex(/[0-9]/, 'Must contain a digit')
    .regex(/[^a-zA-Z0-9]/, 'Must contain a special character'),
  confirmPassword: z.string(),
}).refine(d => d.newPassword === d.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
})
type FormValues = z.infer<typeof schema>

export default function SetPasswordPage(): React.ReactElement {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const email = params.get('email') ?? ''
  const [success, setSuccess] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900'
  const errorCls = 'mt-1 text-xs text-red-500'

  if (!token || !email) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="bg-white border border-gray-200 rounded-xl p-8 max-w-sm w-full text-center">
          <p className="text-sm text-red-600 mb-4">Invalid or missing link parameters.</p>
          <Link to="/login" className="text-sm text-gray-700 underline">Back to login</Link>
        </div>
      </div>
    )
  }

  if (success) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="bg-white border border-gray-200 rounded-xl p-8 max-w-sm w-full text-center">
          <p className="text-sm text-gray-900 font-medium mb-2">Password set successfully.</p>
          <p className="text-sm text-gray-500 mb-4">You can now log in with your new password.</p>
          <Link to="/login" className="text-sm text-gray-700 underline">Go to login</Link>
        </div>
      </div>
    )
  }

  async function onSubmit(values: FormValues): Promise<void> {
    setApiError(null)
    try {
      await api.post('/api/auth/set-password', {
        email,
        token,
        newPassword: values.newPassword,
      })
      setSuccess(true)
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { error?: string; errors?: string[] } } }).response?.data
      setApiError(data?.error ?? data?.errors?.join(', ') ?? 'Failed to set password. The link may have expired.')
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="max-w-sm w-full">
        <h1 className="text-xl font-semibold text-gray-900 mb-6 text-center">Set Your Password</h1>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="bg-white border border-gray-200 rounded-xl p-6 space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
            <input
              {...register('newPassword')}
              type="password"
              className={inputCls}
              autoComplete="new-password"
            />
            {errors.newPassword && <p className={errorCls}>{errors.newPassword.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
            <input
              {...register('confirmPassword')}
              type="password"
              className={inputCls}
              autoComplete="new-password"
            />
            {errors.confirmPassword && <p className={errorCls}>{errors.confirmPassword.message}</p>}
          </div>

          {apiError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{apiError}</p>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-gray-900 text-white rounded-lg py-2 text-sm font-medium hover:bg-gray-700 disabled:opacity-50 transition-colors"
          >
            {isSubmitting ? 'Setting password…' : 'Set Password'}
          </button>
        </form>
      </div>
    </div>
  )
}
