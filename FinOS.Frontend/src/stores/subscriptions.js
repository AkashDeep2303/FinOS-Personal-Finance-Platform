import { defineStore } from 'pinia'
import { subscriptionsApi } from '../api/subscriptions'

export const useSubscriptionsStore = defineStore('subscriptions', {
  state: () => ({ items: [], loading: false, error: null }),
  getters: {
    monthlyCost: state => state.items.filter(x => x.isActive).reduce((sum, x) => {
      const factors = { Daily: 30, Weekly: 52 / 12, Monthly: 1, Quarterly: 1 / 3, Yearly: 1 / 12 }
      return sum + Number(x.amount || 0) * (factors[x.frequency] ?? 1)
    }, 0),
    annualCost() { return this.monthlyCost * 12 }
  },
  actions: {
    async load() {
      this.loading = true
      this.error = null
      try {
        const response = await subscriptionsApi.list()
        this.items = response.data?.data ?? []
      } catch (error) {
        this.error = error.response?.data?.message || 'Failed to load subscriptions'
      } finally {
        this.loading = false
      }
    },
    async detect() {
      this.loading = true
      try {
        await subscriptionsApi.detect()
        await this.load()
      } catch (error) {
        this.error = error.response?.data?.message || 'Subscription detection failed'
      } finally {
        this.loading = false
      }
    },
    async confirm(item) {
      await subscriptionsApi.confirm(item.id, { isConfirmed: true, categoryId: item.categoryId })
      await this.load()
    }
  }
})
