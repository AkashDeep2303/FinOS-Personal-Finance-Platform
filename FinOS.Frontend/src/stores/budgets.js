import { defineStore } from 'pinia'
import { budgetsApi } from '../api/budgets'
import { useAuthStore } from './auth'
const currentUserId = () => {
  const user = useAuthStore().user
  if (user?.id ?? user?.userId) return user.id ?? user.userId
  try {
    const token = localStorage.getItem('finos_token')
    return token ? Number(JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))).sub) : undefined
  } catch {
    return undefined
  }
}

export const useBudgetsStore = defineStore('budgets', {
  state: () => ({
    budgets: [],
    currentBudget: null,
    loading: false,
    error: null,
    selectedMonth: new Date().toISOString().slice(0, 7)
  }),

  getters: {
    totalBudget: (state) => {
      return state.budgets.reduce((sum, b) => sum + (b.amount || 0), 0)
    },
    totalSpent: (state) => {
      return state.budgets.reduce((sum, b) => sum + (b.spent || 0), 0)
    },
    totalRemaining: (state) => {
      return state.budgets.reduce((sum, b) => sum + ((b.amount || 0) - (b.spent || 0)), 0)
    },
    overBudgetCategories: (state) => {
      return state.budgets.filter(b => (b.spent || 0) > (b.amount || 0))
    },
    budgetUtilization: (state) => {
      if (state.budgets.length === 0) return 0
      const totalBudget = state.budgets.reduce((sum, b) => sum + (b.amount || 0), 0)
      const totalSpent = state.budgets.reduce((sum, b) => sum + (b.spent || 0), 0)
      return totalBudget > 0 ? Math.round((totalSpent / totalBudget) * 100) : 0
    }
  },

  actions: {
    async fetchBudgets(month) {
      this.loading = true
      this.error = null
      try {
        const userId = currentUserId()
        if (!userId) throw new Error('No authenticated user is available')
        const response = await budgetsApi.getAll()
        this.budgets = Array.isArray(response.data?.data) ? response.data.data.map(budget => ({ ...budget, category: budget.name, period: budget.periodTypeDisplay, amount: budget.totalBudgetAmount, spent: budget.totalSpentAmount })) : []
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to fetch budgets'
      } finally {
        this.loading = false
      }
    },

    async createBudget(budgetData) {
      this.loading = true
      this.error = null
      try {
        const periodTypes = { Weekly: 0, Monthly: 1, Quarterly: 2, Yearly: 3 }
        const response = await budgetsApi.create({
          userId: currentUserId(),
          name: budgetData.category,
          periodType: periodTypes[budgetData.period] ?? 1,
          startDate: `${budgetData.month}-01`,
          totalBudgetAmount: budgetData.amount,
          currency: 'INR',
          rolloverEnabled: false,
          alertThresholdPct: 80,
          isTemplate: false,
          categories: []
        })
        const created = response.data?.data ?? response.data
        await this.fetchBudgets(this.selectedMonth)
        return created
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create budget'
        throw err
      } finally {
        this.loading = false
      }
    },

    async updateBudget(id, budgetData) {
      this.loading = true
      this.error = null
      try {
        const periodTypes = { Weekly: 0, Monthly: 1, Quarterly: 2, Yearly: 3 }
        const response = await budgetsApi.update(id, {
          name: budgetData.category,
          periodType: periodTypes[budgetData.period] ?? 1,
          totalBudgetAmount: budgetData.amount
        })
        const updated = response.data?.data ?? response.data
        await this.fetchBudgets(this.selectedMonth)
        return updated
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update budget'
        throw err
      } finally {
        this.loading = false
      }
    },

    async deleteBudget(id) {
      this.loading = true
      this.error = null
      try {
        await budgetsApi.delete(id)
        this.budgets = this.budgets.filter(b => b.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete budget'
        throw err
      } finally {
        this.loading = false
      }
    },

    setSelectedMonth(month) {
      this.selectedMonth = month
      this.fetchBudgets(month)
    }
  }
})
