import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'
import AuthLayout from '@/components/layout/AuthLayout'

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

const inputCls = 'w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-[13px] focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]'
const errorCls = 'mt-1 text-xs text-[var(--color-error)]'

export default function SetPasswordPage(): React.ReactElement {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const email = params.get('email') ?? ''
  const [success, setSuccess] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  if (!token || !email) {
    return (
      <AuthLayout title="Set password" subtitle="Reset link is invalid or expired.">
        <div className="space-y-4">
          <div className="bg-[var(--color-error-bg)] border border-[var(--color-error)]/20 rounded-lg p-4">
            <p className="text-[13px] text-[var(--color-text-primary)]">
              Invalid or missing link parameters. Request a new reset link from the login page.
            </p>
          </div>
          <Link
            to="/login"
            className="block text-center text-[13px] text-[var(--color-primary)] hover:underline"
          >
            Back to login
          </Link>
        </div>
      </AuthLayout>
    )
  }

  if (success) {
    return (
      <AuthLayout title="Password set" subtitle="You can now sign in with your new password.">
        <div className="space-y-4">
          <div className="bg-[var(--color-success-bg)] border border-[var(--color-success)]/20 rounded-lg p-4">
            <p className="text-[13px] text-[var(--color-text-primary)]">
              Your password has been updated.
            </p>
          </div>
          <Link
            to="/login"
            className="block w-full text-center bg-[var(--color-primary)] text-white rounded-lg py-2 text-[13px] font-medium hover:bg-[var(--color-primary-hover)] transition-colors"
          >
            Go to login
          </Link>
        </div>
      </AuthLayout>
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
    <AuthLayout
      title="Set your password"
      subtitle={`For ${email}. Minimum 8 characters, mixed case, digit + special character.`}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">New Password</label>
          <input
            {...register('newPassword')}
            type="password"
            className={inputCls}
            autoComplete="new-password"
          />
          {errors.newPassword && <p className={errorCls}>{errors.newPassword.message}</p>}
        </div>

        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">Confirm Password</label>
          <input
            {...register('confirmPassword')}
            type="password"
            className={inputCls}
            autoComplete="new-password"
          />
          {errors.confirmPassword && <p className={errorCls}>{errors.confirmPassword.message}</p>}
        </div>

        {apiError && <p className="text-[13px] text-[var(--color-error)] bg-[var(--color-error-bg)] rounded-lg px-3 py-2">{apiError}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full bg-[var(--color-primary)] text-white rounded-lg py-2 text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {isSubmitting ? 'Setting password…' : 'Set Password'}
        </button>
      </form>
    </AuthLayout>
  )
}
