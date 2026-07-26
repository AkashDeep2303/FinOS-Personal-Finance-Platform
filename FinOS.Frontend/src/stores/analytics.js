import { defineStore } from 'pinia'
import { analyticsApi } from '../api/analytics'

const responseArray = (response) => Array.isArray(response.data?.data) ? response.data.data : []
const responseItems = (response) => Array.isArray(response.data?.data) ? response.data.data : (response.data?.data ? [response.data.data] : [])

export const useAnalyticsStore = defineStore('analytics', {
  state: () => ({
    incomeVsExpense: [],
    categoryBreakdown: [],
    netWorthTrend: [],
    financialScore: {
      score: 0,
      grade: '',
      factors: []
    },
    financialScoreHistory: [],
    monthlySummary: [],
    spendingTrends: [],
    commandCenter: null,
    cashFlow: null,
    retirementProjection: null,
    calculatorResult: null,
    scenarioResult: null,
    savedScenarios: [],
    advisorOpportunities: [],
    loading: false,
    error: null
  }),

  getters: {
    averageMonthlyIncome: (state) => {
      if (state.incomeVsExpense.length === 0) return 0
      const total = state.incomeVsExpense.reduce((sum, m) => sum + (m.income || 0), 0)
      return total / state.incomeVsExpense.length
    },
    averageMonthlyExpense: (state) => {
      if (state.incomeVsExpense.length === 0) return 0
      const total = state.incomeVsExpense.reduce((sum, m) => sum + (m.expense || 0), 0)
      return total / state.incomeVsExpense.length
    },
    savingsRate: (state) => {
      const avgIncome = state.incomeVsExpense.reduce((sum, m) => sum + (m.income || 0), 0)
      const avgExpense = state.incomeVsExpense.reduce((sum, m) => sum + (m.expense || 0), 0)
      return avgIncome > 0 ? ((avgIncome - avgExpense) / avgIncome * 100).toFixed(1) : 0
    },
    topCategories: (state) => {
      return [...state.categoryBreakdown]
        .sort((a, b) => (b.amount || 0) - (a.amount || 0))
        .slice(0, 5)
    }
  },

  actions: {
    async fetchCashFlow(params = {}) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getCashFlow(params)
        this.cashFlow = response.data?.data ?? null
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load cash-flow analytics'
      } finally {
        this.loading = false
      }
    },
    async fetchAdvisorOpportunities() {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getAdvisorOpportunities()
        this.advisorOpportunities = response.data?.data ?? []
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load FinOS Advisor'
      } finally {
        this.loading = false
      }
    },
    async calculateFinancialTool(input) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.calculateFinancialTool(input)
        this.calculatorResult = response.data?.data ?? null
        return this.calculatorResult
      } catch (err) {
        this.error = err.response?.data?.message || 'Calculation failed'
        throw err
      } finally {
        this.loading = false
      }
    },
    async calculateXirr(input) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.calculateXirr(input)
        this.calculatorResult = response.data?.data ?? null
        return this.calculatorResult
      } catch (err) {
        this.error = err.response?.data?.message || 'XIRR calculation failed'
        throw err
      } finally {
        this.loading = false
      }
    },
    async calculateScenario(input) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.calculateScenario(input)
        this.scenarioResult = response.data?.data ?? null
        return this.scenarioResult
      } catch (err) {
        this.error = err.response?.data?.message || 'Scenario calculation failed'
        throw err
      } finally {
        this.loading = false
      }
    },
    async fetchSavedScenarios() {
      const response = await analyticsApi.getSavedScenarios()
      this.savedScenarios = response.data?.data ?? []
    },
    async saveScenario(name, input) {
      const response = await analyticsApi.saveScenario({ name, scenario: input })
      await this.fetchSavedScenarios()
      return response.data?.data
    },
    async deleteScenario(id) {
      await analyticsApi.deleteScenario(id)
      this.savedScenarios = this.savedScenarios.filter(item => item.id !== id)
    },
    async projectRetirement(input) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.projectRetirement(input)
        this.retirementProjection = response.data?.data ?? null
        return this.retirementProjection
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to calculate retirement projection'
        throw err
      } finally {
        this.loading = false
      }
    },
    async fetchCommandCenter() {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getCommandCenter()
        this.commandCenter = response.data?.data ?? null
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch command center'
      } finally {
        this.loading = false
      }
    },
    async fetchDashboardData() {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getDashboard()
        const data = response.data
        this.incomeVsExpense = data.incomeVsExpense || []
        this.categoryBreakdown = data.categoryBreakdown || []
        this.netWorthTrend = data.netWorthTrend || []
        this.monthlySummary = data.monthlySummary || []
        this.financialScore = data.financialScore || this.financialScore
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch dashboard data'
      } finally {
        this.loading = false
      }
    },

    async fetchIncomeVsExpense(params = {}) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getIncomeVsExpense(params)
        this.incomeVsExpense = responseArray(response)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch income vs expense data'
      } finally {
        this.loading = false
      }
    },

    async fetchCategoryBreakdown(params = {}) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getCategoryBreakdown(params)
        this.categoryBreakdown = responseArray(response)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch category breakdown'
      } finally {
        this.loading = false
      }
    },

    async fetchNetWorthTrend(params = {}) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getNetWorthTrend(params)
        this.netWorthTrend = responseItems(response)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch net worth trend'
      } finally {
        this.loading = false
      }
    },

    async fetchFinancialScore() {
      this.loading = true
      try {
        const response = await analyticsApi.getFinancialScore()
        const history = responseArray(response)
        const latest = history.at(-1)
        this.financialScoreHistory = history
        this.financialScore = latest
          ? {
              ...latest,
              score: latest.overallScore,
              grade: latest.scoreGrade,
              factors: [
                { name: 'Savings Rate', score: latest.savingsRateScore, value: latest.savingsRatePct },
                { name: 'Debt Management', score: latest.debtToIncomeScore, value: latest.debtToIncomeRatio },
                { name: 'Emergency Fund', score: latest.emergencyFundScore, value: latest.emergencyFundMonths },
                { name: 'Investments', score: latest.investmentScore, value: latest.investmentToIncomeRatio },
                { name: 'Goals', score: latest.goalProgressScore, value: null }
              ]
            }
          : { score: 0, grade: '', factors: [] }
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch financial score'
      } finally {
        this.loading = false
      }
    },

    async fetchSpendingTrends(params = {}) {
      this.loading = true
      this.error = null
      try {
        const response = await analyticsApi.getSpendingTrends(params)
        this.spendingTrends = responseArray(response)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch spending trends'
      } finally {
        this.loading = false
      }
    }
  }
})
