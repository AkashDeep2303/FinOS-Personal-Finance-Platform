import api from './axios'

export const subscriptionsApi = {
  list: () => api.get('/api/corefinance/subscriptions'),
  detect: () => api.post('/api/corefinance/subscriptions/detect'),
  confirm: (id, data = { isConfirmed: true }) => api.put(`/api/corefinance/subscriptions/${id}/confirm`, data)
}
