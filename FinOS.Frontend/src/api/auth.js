import api from './axios'

export const authApi = {
  login(credentials) {
    return api.post('/api/identity/login', credentials)
  },

  register(userData) {
    return api.post('/api/identity/register', userData)
  },

  refreshToken(refreshToken) {
    return api.post('/api/identity/refresh-token', { refreshToken })
  },
  logout(refreshToken) {
    return api.post('/api/identity/logout', refreshToken ? { refreshToken } : null)
  },
  getSessions() {
    return api.get('/api/identity/sessions')
  },
  revokeSession(sessionId) {
    return api.delete(`/api/identity/sessions/${sessionId}`)
  },
  revokeOtherSessions() {
    return api.post('/api/identity/sessions/revoke-others')
  },

  getProfile() {
    return api.get('/api/identity/users/me')
  },

  updateProfile(profileData) {
    return api.put('/api/identity/users/me', profileData)
  },

  changePassword(passwordData) {
    return api.put('/api/identity/users/me/change-password', passwordData)
  }
}
