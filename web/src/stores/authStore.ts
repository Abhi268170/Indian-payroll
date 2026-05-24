import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface DecodedToken {
  sub: string
  email: string
  role: string | string[]
  tenant_id?: string
  tenant_slug?: string
  exp: number
}

function decodeToken(token: string): DecodedToken {
  const payload = token.split('.')[1]
  if (!payload) throw new Error('Invalid token')
  return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as DecodedToken
}

interface AuthState {
  token: string | null
  refreshToken: string | null
  user: DecodedToken | null
  login: (token: string, refreshToken: string) => void
  setToken: (token: string) => void
  logout: () => void
  isAuthenticated: () => boolean
  hasRole: (...roles: string[]) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      refreshToken: null,
      user: null,

      login(token: string, refreshToken: string) {
        const user = decodeToken(token)
        set({ token, refreshToken, user })
      },

      setToken(token: string) {
        const user = decodeToken(token)
        set({ token, user })
      },

      logout() {
        set({ token: null, refreshToken: null, user: null })
      },

      isAuthenticated() {
        const { token, user } = get()
        if (!token || !user) return false
        return user.exp * 1000 > Date.now()
      },

      hasRole(...roles: string[]) {
        const { user } = get()
        if (!user) return false
        const userRoles = Array.isArray(user.role) ? user.role : [user.role]
        return roles.some(r => userRoles.includes(r))
      },
    }),
    {
      name: 'payroll-auth',
      partialize: (state) => ({ token: state.token, refreshToken: state.refreshToken, user: state.user }),
    },
  ),
)
