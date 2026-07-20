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
    error: null
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
        const response = await investmentsApi.getAll(userId)
        const data = response.data?.data ?? []
        this.investments = Array.isArray(data) ? data : (data.investments ?? [])
        this.portfolioSummary = data.summary ?? this.portfolioSummary
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
        const response = await investmentsApi.create(data)
        this.investments.push(response.data)
        return response.data
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
