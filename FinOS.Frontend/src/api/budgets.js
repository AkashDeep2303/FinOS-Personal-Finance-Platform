import api from './axios'

export const budgetsApi = {
  getAll() {
    return api.get('/api/budget/budgets/me')
  },

  getById(id) {
    return api.get(`/api/budget/budgets/${id}`)
  },

  create(data) {
    return api.post('/api/budget/budgets', data)
  },

  update(id, data) {
    return api.put(`/api/budget/budgets/${id}`, data)
  },

  delete(id) {
    return api.delete(`/api/budget/budgets/${id}`)
  }
}
