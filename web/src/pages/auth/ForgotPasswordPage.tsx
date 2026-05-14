import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api } from '@/lib/api'

const schema = z.object({
  email: z.string().min(1, 'Required').email('Invalid email'),
})
type FormValues = z.infer<typeof schema>

export default function ForgotPasswordPage(): React.ReactElement {
  const [submitted, setSubmitted] = useState(false)
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900'
  const errorCls = 'mt-1 text-xs text-red-500'

  async function onSubmit(values: FormValues): Promise<void> {
    try {
      await api.post('/api/auth/forgot-password', { email: values.email })
    } catch {
      // Always show success — no enumeration
    }
    setSubmitted(true)
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="max-w-sm w-full">
        <h1 className="text-xl font-semibold text-gray-900 mb-6 text-center">Forgot Password</h1>

        {submitted ? (
          <div className="bg-white border border-gray-200 rounded-xl p-6 text-center space-y-3">
            <p className="text-sm text-gray-700">
              If that email exists, a reset link has been sent.
            </p>
            <Link to="/login" className="text-sm text-gray-500 hover:text-gray-700 underline">
              Back to login
            </Link>
          </div>
        ) : (
          <form
            onSubmit={handleSubmit(onSubmit)}
            className="bg-white border border-gray-200 rounded-xl p-6 space-y-4"
          >
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
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
              className="w-full bg-gray-900 text-white rounded-lg py-2 text-sm font-medium hover:bg-gray-700 disabled:opacity-50 transition-colors"
            >
              {isSubmitting ? 'Sending…' : 'Send Reset Link'}
            </button>

            <div className="text-center">
              <Link to="/login" className="text-xs text-gray-500 hover:text-gray-700">
                Back to login
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}
