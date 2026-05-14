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
  user: DecodedToken | null
  login: (token: string) => void
  logout: () => void
  isAuthenticated: () => boolean
  hasRole: (...roles: string[]) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      user: null,

      login(token: string) {
        const user = decodeToken(token)
        set({ token, user })
      },

      logout() {
        set({ token: null, user: null })
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
      partialize: (state) => ({ token: state.token, user: state.user }),
    },
  ),
)
