import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'
import AuthLayout from '@/components/layout/AuthLayout'

const schema = z.object({
  email: z.string().min(1, 'Required').email('Invalid email'),
})
type FormValues = z.infer<typeof schema>

const inputCls = 'w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-[13px] focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]'
const errorCls = 'mt-1 text-xs text-[var(--color-error)]'

export default function ForgotPasswordPage(): React.ReactElement {
  const [submitted, setSubmitted] = useState(false)
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(values: FormValues): Promise<void> {
    try {
      await api.post('/api/auth/forgot-password', { email: values.email })
    } catch {
      // Always show success — no enumeration
    }
    setSubmitted(true)
  }

  return (
    <AuthLayout
      title="Forgot password"
      subtitle="Enter your work email and we'll send a reset link."
    >
      {submitted ? (
        <div className="space-y-4">
          <div className="bg-[var(--color-success-bg)] border border-[var(--color-success)]/20 rounded-lg p-4">
            <p className="text-[13px] text-[var(--color-text-primary)]">
              If that email exists, a reset link has been sent.
            </p>
          </div>
          <Link
            to="/login"
            className="block text-center text-[13px] text-[var(--color-primary)] hover:underline"
          >
            Back to login
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">Email</label>
            <input
              {...register('email')}
              type="email"
              className={inputCls}
              placeholder="you@example.com"
              autoComplete="email"
            />
            {errors.email && <p className={errorCls}>{errors.email.message}</p>}
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-[var(--color-primary)] text-white rounded-lg py-2 text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
          >
            {isSubmitting ? 'Sending…' : 'Send Reset Link'}
          </button>

          <div className="text-center">
            <Link to="/login" className="text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
              Back to login
            </Link>
          </div>
        </form>
      )}
    </AuthLayout>
  )
}
