import api from './axios'

export const accountsApi = {
  getAll() {
    return api.get('/api/corefinance/accounts')
  },

  getById(id) {
    return api.get(`/api/corefinance/accounts/${id}`)
  },

  create(data) {
    return api.post('/api/corefinance/accounts', data)
  },

  update(id, data) {
    return api.put(`/api/corefinance/accounts/${id}`, data)
  },

  delete(id) {
    return api.delete(`/api/corefinance/accounts/${id}`)
  }
}
