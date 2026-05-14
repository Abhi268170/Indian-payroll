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

api.interceptors.response.use(
  r => r,
  error => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      useAuthStore.getState().logout()
    }
    return Promise.reject(error)
  },
)

export async function getToken(
  username: string,
  password: string,
  clientSecret: string,
): Promise<string> {
  const params = new URLSearchParams({
    grant_type: 'password',
    username,
    password,
    client_id: 'payroll-api',
    client_secret: clientSecret,
    scope: 'openid profile email offline_access payroll.api',
  })
  const resp = await axios.post(`${API_BASE}/connect/token`, params)
  return resp.data.access_token as string
}
