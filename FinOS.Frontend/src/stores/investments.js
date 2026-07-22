import { defineStore } from 'pinia'
import { investmentsApi } from '../api/investments'
import { useAuthStore } from './auth'

export const useInvestmentsStore = defineStore('investments', {
  state: () => ({
    investments: [],
    sipList: [],
    epfTracker: null,
    portfolioSummary: {
      totalInvested: 0,
      currentValue: 0,
      totalReturns: 0,
      returnsPercentage: 0
    },
    loading: false,
    error: null,
    activePortfolioId: null
  }),

  getters: {
    totalInvested: (state) => {
      return state.investments.reduce((sum, i) => sum + (i.investedAmount || 0), 0)
    },
    currentValue: (state) => {
      return state.investments.reduce((sum, i) => sum + (i.currentValue || 0), 0)
    },
    totalReturns: (state) => {
      return state.currentValue - state.totalInvested
    },
    returnsPercentage: (state) => {
      return state.totalInvested > 0
        ? ((state.currentValue - state.totalInvested) / state.totalInvested * 100).toFixed(2)
        : 0
    },
    investmentsByType: (state) => {
      const grouped = {}
      state.investments.forEach(inv => {
        if (!grouped[inv.type]) grouped[inv.type] = []
        grouped[inv.type].push(inv)
      })
      return grouped
    },
    activeSIPs: (state) => {
      return state.sipList.filter(s => s.isActive)
    },
    totalSIPMonthly: (state) => {
      return state.sipList
        .filter(s => s.isActive)
        .reduce((sum, s) => sum + (s.monthlyAmount || 0), 0)
    }
  },

  actions: {
    async fetchInvestments() {
      this.loading = true
      this.error = null
      try {
        const userId = useAuthStore().user?.id
        if (!userId) throw new Error('No authenticated user is available')

        let response = await investmentsApi.getAll(userId)
        let portfolios = Array.isArray(response.data?.data) ? response.data.data : []

        if (portfolios.length === 0) {
          const created = await investmentsApi.createPortfolio({
            userId,
            name: 'My Portfolio',
            currency: 'INR',
            isDefault: true
          })
          const portfolio = created.data?.data
          if (portfolio?.id) portfolios = [{ id: portfolio.id, name: portfolio.name, isDefault: true }]
        }

        const portfolio = portfolios.find(p => p.isDefault) || portfolios[0]
        this.activePortfolioId = portfolio?.id || null
        if (!this.activePortfolioId) {
          this.investments = []
          return
        }

        const summaryResponse = await investmentsApi.getSummary(this.activePortfolioId)
        const summary = summaryResponse.data?.data ?? {}
        this.investments = Array.isArray(summary.topHoldings) ? summary.topHoldings : []
        this.portfolioSummary = {
          totalInvested: summary.totalInvested ?? 0,
          currentValue: summary.currentValue ?? 0,
          totalReturns: summary.totalReturn ?? 0,
          returnsPercentage: summary.totalReturnPct ?? 0
        }
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to fetch investments'
      } finally {
        this.loading = false
      }
    },
    async fetchSIPList() {
      this.loading = true
      try {
        const userId = useAuthStore().user?.id
        if (!userId) throw new Error('No authenticated user is available')
        const response = await investmentsApi.getSIPs(userId)
        this.sipList = Array.isArray(response.data?.data) ? response.data.data : []
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to fetch SIP list'
      } finally {
        this.loading = false
      }
    },

    async fetchEPFTracker() {
      // The service has account-specific EPF endpoints only. Keep the dashboard
      // in an empty state until the user selects or creates an EPF account.
      this.epfTracker = null
    },

    async createInvestment(data) {
      this.loading = true
      this.error = null
      try {
        if (!this.activePortfolioId) await this.fetchInvestments()
        if (!this.activePortfolioId) throw new Error('No investment portfolio is available')

        const typeIds = {
          'Mutual Fund': 1,
          Stock: 2,
          FD: 3,
          Gold: 4,
          Crypto: 5,
          EPF: 6,
          PPF: 7,
          NPS: 8,
          'Real Estate': 9,
          Bond: 10
        }
        const investedAmount = Number(data.investedAmount) || 0
        const currentValue = Number(data.currentValue) || investedAmount
        const response = await investmentsApi.create({
          portfolioId: this.activePortfolioId,
          investmentTypeId: typeIds[data.type] || 1,
          symbol: data.symbol || data.name,
          name: data.name,
          quantity: 1,
          avgPurchasePrice: investedAmount,
          currentPrice: currentValue,
          fundHouse: data.fundHouse || null,
          notes: data.investmentDate ? 'Investment date: ' + data.investmentDate : null
        })
        const created = response.data?.data ?? response.data
        this.investments.push(created)
        return created
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create investment'
        throw err
      } finally {
        this.loading = false
      }
    },

    async updateInvestment(id, data) {
      this.loading = true
      this.error = null
      try {
        const response = await investmentsApi.update(id, data)
        const index = this.investments.findIndex(i => i.id === id)
        if (index !== -1) this.investments[index] = response.data
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update investment'
        throw err
      } finally {
        this.loading = false
      }
    },

    async deleteInvestment(id) {
      this.loading = true
      this.error = null
      try {
        await investmentsApi.delete(id)
        this.investments = this.investments.filter(i => i.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete investment'
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})
