import api from './axios'

export const loansApi = {
  getDebtOverview() {
    return api.get('/api/loan/debt/overview')
  },

  getAll(userId) {
    return api.get(`/api/loan/loans/user/${userId}`)
  },

  getById(id) {
    return api.get(`/api/loan/loans/${id}/summary`)
  },

  create(data) {
    return api.post('/api/loan/loans', data)
  },

  close(id) {
    return api.post(`/api/loan/loans/${id}/close`)
  },

  getEMISchedule(loanId) {
    return api.get(`/api/loan/emischedule/loan/${loanId}`)
  },
  getPaymentAnalysis(loanId) {
    return api.get(`/api/loan/debt/loans/${loanId}/payment-analysis`)
  },
  getRateHistory(loanId) {
    return api.get(`/api/loan/debt/loans/${loanId}/rate-history`)
  },
  addRateChange(loanId, data) {
    return api.post(`/api/loan/debt/loans/${loanId}/rate-history`, data)
  },
  getPrepaymentHistory(loanId) {
    return api.get(`/api/loan/prepayment/loan/${loanId}/history`)
  },

  calculatePrepayment(loanId, data) {
    return api.post('/api/loan/prepayment/simulate', {
      loanId,
      prepaymentAmount: data.amount,
      prepaymentDate: new Date().toISOString(),
      strategy: data.type === 'reduce_emi' ? 0 : 1
    })
  },

  compareStrategy(data) {
    return api.post('/api/loan/loan-strategy/compare', data)
  }
}
