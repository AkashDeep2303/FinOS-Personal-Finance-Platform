import api from './axios'
import { useAuthStore } from '../stores/auth'

function tokenUserId() {
  try {
    const token = localStorage.getItem('finos_token')
    if (!token) return undefined
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return Number(payload.sub) || undefined
  } catch {
    return undefined
  }
}

function query(params = {}) {
  const months = Number.parseInt(String(params.range || params.months || '6'), 10) || 6
  const user = useAuthStore().user
  const userId = user?.id ?? user?.userId ?? tokenUserId()
  return {
    ...params,
    userId,
    months,
    yearMonth: params.yearMonth || Number(new Date().toISOString().slice(0, 7).replace('-', ''))
  }
}

export const analyticsApi = {
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