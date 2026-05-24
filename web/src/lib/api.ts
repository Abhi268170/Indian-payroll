import axios from 'axios'
import { useAuthStore } from '@/stores/authStore'

export const API_BASE = import.meta.env['VITE_API_URL'] ?? 'http://localhost:5000'

export const api = axios.create({ baseURL: API_BASE })

api.interceptors.request.use(config => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers['Authorization'] = `Bearer ${token}`
  }
  return config
})

// Track in-flight refresh to avoid concurrent refresh storms
let refreshPromise: Promise<string> | null = null

async function doRefresh(): Promise<string> {
  const { refreshToken, logout } = useAuthStore.getState()
  if (!refreshToken) {
    logout()
    throw new Error('No refresh token')
  }

  const params = new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: refreshToken,
    client_id: 'payroll-api',
    client_secret: import.meta.env['VITE_CLIENT_SECRET'] ?? 'dev-client-secret-2024',
  })

  try {
    const resp = await axios.post(`${API_BASE}/connect/token`, params)
    const newAccessToken = resp.data.access_token as string
    const newRefreshToken = resp.data.refresh_token as string | undefined
    const { setToken, login } = useAuthStore.getState()
    if (newRefreshToken) {
      login(newAccessToken, newRefreshToken)
    } else {
      setToken(newAccessToken)
    }
    return newAccessToken
  } catch {
    logout()
    throw new Error('Session expired')
  }
}

// Side-effect interceptor: after any successful write under /api/v1/*, invalidate the
// onboarding-status + payroll-run-preflight caches so the wizard rail and Pay Runs
// preflight reflect new state without each page needing to opt-in. GET requests, auth
// endpoints, and /api/v1/onboarding writes themselves are excluded.
api.interceptors.response.use(
  async r => {
    try {
      const method = (r.config.method ?? 'get').toLowerCase()
      const url = r.config.url ?? ''
      const isWrite = method !== 'get' && method !== 'head'
      const isV1 = url.startsWith('/api/v1/') || url.includes('/api/v1/')
      const isOnboardingWrite = url.includes('/api/v1/onboarding')
      const isJobs = url.includes('/api/v1/jobs/')
      if (isWrite && isV1 && !isOnboardingWrite && !isJobs) {
        const { queryClient } = await import('./queryClient')
        queryClient.invalidateQueries({ queryKey: ['onboarding-status'] })
        queryClient.invalidateQueries({ queryKey: ['payroll-run-preflight'] })
      }
    } catch {
      // Never let cache-invalidation failures break the original response.
    }
    return r
  },
  async error => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401) {
      return Promise.reject(error)
    }

    // Don't retry refresh calls themselves — would infinite loop
    const originalUrl = error.config?.url ?? ''
    if (originalUrl.includes('/connect/token')) {
      useAuthStore.getState().logout()
      return Promise.reject(error)
    }

    try {
      // Coalesce concurrent 401s into a single refresh call
      refreshPromise ??= doRefresh().finally(() => { refreshPromise = null })
      const newToken = await refreshPromise

      // Retry original request with new token
      const config = error.config!
      config.headers['Authorization'] = `Bearer ${newToken}`
      return api.request(config)
    } catch {
      return Promise.reject(error)
    }
  },
)

export async function getToken(
  username: string,
  password: string,
): Promise<{ accessToken: string; refreshToken: string }> {
  const params = new URLSearchParams({
    grant_type: 'password',
    username,
    password,
    client_id: 'payroll-api',
    client_secret: import.meta.env['VITE_CLIENT_SECRET'] ?? 'dev-client-secret-2024',
    scope: 'profile email roles offline_access payroll.api',
  })
  const resp = await axios.post(`${API_BASE}/connect/token`, params)
  return {
    accessToken: resp.data.access_token as string,
    refreshToken: resp.data.refresh_token as string,
  }
}
