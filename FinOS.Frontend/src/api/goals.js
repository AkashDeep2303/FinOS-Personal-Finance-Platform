import api from './axios'

export const goalsApi = {
  getAll(userId) {
    return api.get(`/api/goals/goals/user/${userId}`)
  },

  getById(id) {
    return api.get(`/api/goals/${id}`)
  },

  create(data) {
    return api.post('/api/goals/goals', data)
  },

  update(id, data) {
    return api.put('/api/goals/goals', { ...data, id })
  },

  delete(id) {
    return api.delete(`/api/goals/goals/${id}`)
  },

  addContribution(goalId, data) {
    return api.post(`/api/goals/goals/${goalId}/contribute`, { ...data, goalId })
  }
}
