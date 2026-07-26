import api from './axios'

export const goalsApi = {
  getAll() {
    return api.get('/api/goals/goals/me')
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
  },

  getFundingAnalysis(availableMonthlySurplus) {
    return api.get('/api/goals/goal-planning/funding-analysis', { params: { availableMonthlySurplus } })
  }
}
