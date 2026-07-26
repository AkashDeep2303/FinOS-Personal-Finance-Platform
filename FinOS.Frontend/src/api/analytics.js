import api from './axios'

function query(params = {}) {
  const months = Number.parseInt(String(params.range || params.months || '6'), 10) || 6
  return {
    ...params,
    months,
    yearMonth: params.yearMonth || Number(new Date().toISOString().slice(0, 7).replace('-', ''))
  }
}

export const analyticsApi = {
  getCommandCenter() {
    return api.get('/api/analytics/command-center')
  },
  getCashFlow(params = {}) {
    return api.get('/api/analytics/cash-flow', { params })
  },
  getAdvisorOpportunities() {
    return api.get('/api/analytics/advisor/opportunities')
  },
  projectRetirement(data) {
    return api.post('/api/analytics/retirement/project', data)
  },
  calculateFinancialTool(data) {
    return api.post('/api/analytics/decision-tools/calculate', data)
  },
  calculateXirr(data) {
    return api.post('/api/analytics/decision-tools/xirr', data)
  },
  calculateScenario(data) {
    return api.post('/api/analytics/decision-tools/scenario', data)
  },
  getSavedScenarios() {
    return api.get('/api/analytics/decision-tools/scenarios')
  },
  saveScenario(data) {
    return api.post('/api/analytics/decision-tools/scenarios', data)
  },
  deleteScenario(id) {
    return api.delete(`/api/analytics/decision-tools/scenarios/${id}`)
  },
  getDashboard() {
    return api.get('/api/analytics/monthlyaggregates', { params: query() })
  },
  getIncomeVsExpense(params = {}) {
    return api.get('/api/analytics/spending/income-vs-expense', { params: query(params) })
  },
  getCategoryBreakdown(params = {}) {
    return api.get('/api/analytics/spending/category-breakdown', { params: query(params) })
  },
  getNetWorthTrend(params = {}) {
    return api.get('/api/analytics/networth/trend', { params: query(params) })
  },
  getFinancialScore(params = {}) {
    return api.get('/api/analytics/financialscore/history', { params: query(params) })
  },
  getSpendingTrends(params = {}) {
    return api.get('/api/analytics/spending/trends', { params: query(params) })
  }
}
