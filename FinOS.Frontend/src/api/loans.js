import api from './axios'

export const loansApi = {
  getAll(userId) {
    return api.get(`/api/loan/loans/user/${userId}`)
  },

  getById(id) {
    return api.get(`/api/loan/${id}`)
  },

  create(data) {
    return api.post('/api/loan/loans', data)
  },

  update(id, data) {
    return api.put(`/api/loan/${id}`, data)
  },

  delete(id) {
    return api.delete(`/api/loan/${id}`)
  },

  getEMISchedule(loanId) {
    return api.get(`/api/loan/emischedule/loan/${loanId}`)
  },

  calculatePrepayment(loanId, data) {
    return api.post('/api/loan/prepayment/simulate', {
      loanId,
      prepaymentAmount: data.amount,
      prepaymentDate: new Date().toISOString(),
      strategy: data.type === 'reduce_emi' ? 0 : 1
    })
  }
}
