import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { getToken } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'
import AuthLayout from '@/components/layout/AuthLayout'

const schema = z.object({
  username: z.string().email('Enter a valid email'),
  password: z.string().min(1, 'Required'),
})

type FormValues = z.infer<typeof schema>

const inputCls = 'w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-[13px] focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]'
const errorCls = 'mt-1 text-xs text-[var(--color-error)]'

export default function LoginPage(): React.ReactElement {
  const navigate = useNavigate()
  const login = useAuthStore(s => s.login)
  const [error, setError] = useState<string | null>(null)

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(values: FormValues): Promise<void> {
    setError(null)
    try {
      const { accessToken, refreshToken } = await getToken(values.username, values.password)
      login(accessToken, refreshToken)
      navigate('/', { replace: true })
    } catch {
      setError('Invalid credentials or server error.')
    }
  }

  return (
    <AuthLayout title="Sign in" subtitle="Sign in to your account">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">Email</label>
          <input
            {...register('username')}
            type="email"
            autoComplete="username"
            className={inputCls}
            placeholder="you@company.com"
          />
          {errors.username && <p className={errorCls}>{errors.username.message}</p>}
        </div>

        <div>
          <label className="block text-[13px] font-medium text-[var(--color-text-primary)] mb-1">Password</label>
          <input
            {...register('password')}
            type="password"
            autoComplete="current-password"
            className={inputCls}
          />
          {errors.password && <p className={errorCls}>{errors.password.message}</p>}
        </div>

        {error && <p className="text-[13px] text-[var(--color-error)] bg-[var(--color-error-bg)] rounded-lg px-3 py-2">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full bg-[var(--color-primary)] text-white rounded-lg py-2 text-[13px] font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors"
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>

        <div className="text-center">
          <Link to="/forgot-password" className="text-[12px] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
            Forgot password?
          </Link>
        </div>
      </form>
    </AuthLayout>
  )
}
