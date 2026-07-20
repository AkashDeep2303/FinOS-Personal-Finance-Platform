import { defineStore } from 'pinia'
import { authApi } from '../api/auth'
import router from '../router'

/**
 * Parse the standardised ApiResponse<AuthResponse> envelope returned by the
 * Identity service. The HTTP body looks like:
 *
 *   {
 *     "success": true,
 *     "data": {
 *       "userId": 123,
 *       "email": "...",
 *       "firstName": "...",
 *       "lastName": "...",
 *       "fullName": "...",
 *       "accessToken": "eyJ...",
 *       "refreshToken": "abc...",
 *       "accessTokenExpiry": "...",
 *       "refreshTokenExpiry": "...",
 *       "roles": ["User"],
 *       "twoFactorRequired": false
 *     },
 *     "message": "Login successful",
 *     "timestamp": "..."
 *   }
 *
 * axios exposes the body as `response.data`, so the inner AuthResponse is at
 * `response.data.data`. We also support the legacy flat shape (`response.data.token`)
 * in case some endpoint still returns it.
 *
 * @param {object} response  axios response object
 * @returns {{accessToken:string, refreshToken:string, user:object}}
 */
function _applyAuthResponse(response) {
  const body = response?.data ?? {}
  const inner = body?.data ?? body ?? {}  // envelope.data, or fall back to body itself

  const accessToken = inner.accessToken ?? inner.token ?? inner.AccessToken
  const refreshToken = inner.refreshToken ?? inner.RefreshToken

  if (!accessToken) {
    console.error('[auth] No access token in response. Body keys:', Object.keys(body), 'Inner keys:', Object.keys(inner))
    throw new Error('Authentication failed: no access token returned by server.')
  }

  const user = {
    id: inner.userId ?? inner.id,
    email: inner.email ?? body.email,
    firstName: inner.firstName,
    lastName: inner.lastName,
    name: inner.fullName ?? [inner.firstName, inner.lastName].filter(Boolean).join(' '),
    roles: inner.roles ?? []
  }

  return { accessToken, refreshToken, user }
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    token: localStorage.getItem('finos_token') || null,
    refreshToken: localStorage.getItem('finos_refresh_token') || null,
    loading: false,
    error: null
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
    userName: (state) => state.user?.name || state.user?.firstName || '',
    userEmail: (state) => state.user?.email || ''
  },

  actions: {
    async login(credentials) {
      this.loading = true
      this.error = null
      try {
        const response = await authApi.login(credentials)
        const { accessToken, refreshToken, user } = _applyAuthResponse(response)

        this.token = accessToken
        this.refreshToken = refreshToken
        this.user = user

        localStorage.setItem('finos_token', accessToken)
        if (refreshToken) localStorage.setItem('finos_refresh_token', refreshToken)

        const redirect = router.currentRoute.value.query.redirect || '/dashboard'
        router.push(redirect)
      } catch (err) {
        const body = err?.response?.data
        this.error = body?.message || body?.errors?.[Object.keys(body?.errors ?? {})[0]]?.[0] || 'Login failed. Please try again.'
        throw err
      } finally {
        this.loading = false
      }
    },

    async register(userData) {
      this.loading = true
      this.error = null
      try {
        const response = await authApi.register(userData)
        const { accessToken, refreshToken, user } = _applyAuthResponse(response)

        this.token = accessToken
        this.refreshToken = refreshToken
        this.user = user

        localStorage.setItem('finos_token', accessToken)
        if (refreshToken) localStorage.setItem('finos_refresh_token', refreshToken)

        router.push('/dashboard')
      } catch (err) {
        const body = err?.response?.data
        this.error = body?.message || body?.errors?.[Object.keys(body?.errors ?? {})[0]]?.[0] || 'Registration failed. Please try again.'
        throw err
      } finally {
        this.loading = false
      }
    },

    async refreshAccessToken() {
      if (!this.refreshToken) {
        this.logout()
        throw new Error('No refresh token available')
      }
      try {
        const response = await authApi.refreshToken(this.refreshToken)
        const { accessToken, refreshToken } = _applyAuthResponse(response)

        this.token = accessToken
        if (refreshToken) this.refreshToken = refreshToken

        localStorage.setItem('finos_token', accessToken)
        if (refreshToken) localStorage.setItem('finos_refresh_token', refreshToken)

        return accessToken
      } catch (err) {
        this.logout()
        throw err
      }
    },

    async fetchProfile() {
      try {
        const response = await authApi.getProfile()
        // Profile endpoint returns ApiResponse<UserProfileDto> â€” inner data has the user fields
        const body = response?.data ?? {}
        const inner = body?.data ?? body
        this.user = {
          ...this.user,
          ...inner,
          name: inner?.fullName ?? [inner?.firstName, inner?.lastName].filter(Boolean).join(' ')
        }
      } catch (err) {
        console.error('Failed to fetch profile:', err)
      }
    },

    async updateProfile(profileData) {
      try {
        const response = await authApi.updateProfile(profileData)
        const body = response?.data ?? {}
        const inner = body?.data ?? body
        this.user = {
          ...this.user,
          ...inner,
          name: inner?.fullName ?? [inner?.firstName, inner?.lastName].filter(Boolean).join(' ')
        }
      } catch (err) {
        throw err
      }
    },

    async changePassword(passwordData) {
      await authApi.changePassword(passwordData)
    },
    logout() {
      this.user = null
      this.token = null
      this.refreshToken = null
      localStorage.removeItem('finos_token')
      localStorage.removeItem('finos_refresh_token')
      router.push('/login')
    }
  }
})
