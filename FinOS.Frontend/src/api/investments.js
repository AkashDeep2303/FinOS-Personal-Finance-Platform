import api from './axios'

export const investmentsApi = {
  // The Investment service exposes Holdings/SIPs/EPF as separate controllers,
  // each at /api/investment/<resource>. There is no bare /api/investment route.
  // For "get all my investments" we use Holdings.GetAll, which is the most
  // representative list endpoint.
  getAll(userId) {
    return api.get(`/api/investment/portfolios/user/${userId}`)
  },

  getById(id) {
    return api.get(`/api/investment/holdings/${id}`)
  },

  create(data) {
    return api.post('/api/investment/holdings', data)
  },

  update(id, data) {
    return api.put(`/api/investment/holdings/${id}`, data)
  },

  delete(id) {
    return api.delete(`/api/investment/holdings/${id}`)
  },

  getSIPs(userId) {
    return api.get(`/api/investment/sips/user/${userId}`)
  },

  createSIP(data) {
    return api.post('/api/investment/sips', data)
  },

  getEPF() {
    return api.get('/api/investment/epf')
  },

  updateEPF(data) {
    return api.put('/api/investment/epf', data)
  }
}
