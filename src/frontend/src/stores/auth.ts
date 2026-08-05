// Pinia auth store. Persists the JWT to sessionStorage under
// `ecommerce.token` so it survives reloads within the browser session.

import { defineStore } from 'pinia'
import { login as loginApi, register as registerApi } from '../api/auth'

const TOKEN_KEY = 'ecommerce.token'
const EMAIL_KEY = 'ecommerce.email'
const ROLES_KEY = 'ecommerce.roles'

interface AuthState {
  token: string | null
  email: string | null
  roles: string[]
  isAuthenticated: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: null,
    email: null,
    roles: [],
    isAuthenticated: false,
  }),

  actions: {
    /** Authenticate against the API and persist the token. */
    async login(email: string, password: string): Promise<void> {
      const res = await loginApi(email, password)
      this.token = res.token
      this.email = res.email
      this.roles = res.roles ?? []
      this.isAuthenticated = true

      sessionStorage.setItem(TOKEN_KEY, res.token)
      sessionStorage.setItem(EMAIL_KEY, res.email)
      sessionStorage.setItem(ROLES_KEY, JSON.stringify(this.roles))
    },

    /** Register a new customer account; on success the API returns a JWT (same as login). */
    async register(email: string, password: string): Promise<void> {
      const res = await registerApi(email, password)
      this.token = res.token
      this.email = res.email
      this.roles = res.roles ?? []
      this.isAuthenticated = true

      sessionStorage.setItem(TOKEN_KEY, res.token)
      sessionStorage.setItem(EMAIL_KEY, res.email)
      sessionStorage.setItem(ROLES_KEY, JSON.stringify(this.roles))
    },

    /** Clear all auth state and persisted credentials. */
    logout(): void {
      this.token = null
      this.email = null
      this.roles = []
      this.isAuthenticated = false
      sessionStorage.removeItem(TOKEN_KEY)
      sessionStorage.removeItem(EMAIL_KEY)
      sessionStorage.removeItem(ROLES_KEY)
    },

    /** Read persisted token from sessionStorage on app load. */
    hydrate(): void {
      const token = sessionStorage.getItem(TOKEN_KEY)
      const email = sessionStorage.getItem(EMAIL_KEY)
      const rolesRaw = sessionStorage.getItem(ROLES_KEY)

      if (token) {
        this.token = token
        this.email = email
        this.roles = rolesRaw ? safeParse(rolesRaw) : []
        this.isAuthenticated = true
      }
    },
  },
})

function safeParse(raw: string): string[] {
  try {
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []
  } catch {
    return []
  }
}

