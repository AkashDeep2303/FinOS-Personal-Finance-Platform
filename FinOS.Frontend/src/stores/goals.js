import { defineStore } from 'pinia'
import { goalsApi } from '../api/goals'
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

export const useGoalsStore = defineStore('goals', {
  state: () => ({
    goals: [],
    currentGoal: null,
    fundingAnalysis: null,
    loading: false,
    error: null
  }),

  getters: {
    totalTargetAmount: (state) => {
      return state.goals.reduce((sum, g) => sum + (g.targetAmount || 0), 0)
    },
    totalSavedAmount: (state) => {
      return state.goals.reduce((sum, g) => sum + (g.currentAmount || 0), 0)
    },
    activeGoals: (state) => {
      return state.goals.filter(g => g.status === 'Active')
    },
    completedGoals: (state) => {
      return state.goals.filter(g => g.status === 'Completed')
    },
    goalsByPriority: (state) => {
      const grouped = { High: [], Medium: [], Low: [] }
      state.goals.forEach(g => {
        if (grouped[g.priority]) grouped[g.priority].push(g)
      })
      return grouped
    }
  },

  actions: {
    async fetchFundingAnalysis(availableMonthlySurplus) {
      this.loading = true
      this.error = null
      try {
        const response = await goalsApi.getFundingAnalysis(availableMonthlySurplus)
        this.fundingAnalysis = response.data?.data ?? null
        return this.fundingAnalysis
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to analyze goal funding'
        throw err
      } finally {
        this.loading = false
      }
    },
    async fetchGoals() {
      this.loading = true
      this.error = null
      try {
        const userId = currentUserId()
        if (!userId) throw new Error('No authenticated user is available')
        const response = await goalsApi.getAll()
        const statuses = ['Active', 'Paused', 'Completed', 'Cancelled']
        const priorities = ['Low', 'Medium', 'High', 'Critical']
        this.goals = Array.isArray(response.data?.data)
          ? response.data.data.map(goal => ({ ...goal, status: statuses[goal.status] ?? goal.status, priority: priorities[goal.priority] ?? goal.priority }))
          : []
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to fetch goals'
      } finally {
        this.loading = false
      }
    },

    async createGoal(goalData) {
      this.loading = true
      this.error = null
      try {
        const priorities = { Low: 0, Medium: 1, High: 2, Critical: 3 }
        const categories = { 'Emergency Fund': 'Emergency', Vacation: 'Travel', Home: 'Purchase', Car: 'Purchase', Wedding: 'Wedding', Education: 'Education', Retirement: 'Retirement', Gadget: 'Purchase', Other: 'Other' }
        const response = await goalsApi.create({
          goalTemplateId: null,
          name: goalData.name,
          description: null,
          category: categories[goalData.category] ?? 'Other',
          targetAmount: goalData.targetAmount,
          monthlyContribution: 0,
          startDate: new Date().toISOString().slice(0, 10),
          targetDate: goalData.targetDate || null,
          priority: priorities[goalData.priority] ?? 1,
          linkedAccountIds: null,
          icon: null,
          color: null,
          isAutoContribute: false
        })
        const created = response.data?.data ?? response.data
        await this.fetchGoals()
        return created
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create goal'
        throw err
      } finally {
        this.loading = false
      }
    },

    async updateGoal(id, goalData) {
      this.loading = true
      this.error = null
      try {
        const response = await goalsApi.update(id, goalData)
        const index = this.goals.findIndex(g => g.id === id)
        if (index !== -1) this.goals[index] = response.data
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update goal'
        throw err
      } finally {
        this.loading = false
      }
    },

    async deleteGoal(id) {
      this.loading = true
      this.error = null
      try {
        await goalsApi.delete(id)
        this.goals = this.goals.filter(g => g.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete goal'
        throw err
      } finally {
        this.loading = false
      }
    },

    async addContribution(goalId, contributionData) {
      this.loading = true
      this.error = null
      try {
        const response = await goalsApi.addContribution(goalId, {
          amount: contributionData.amount,
          contributionDate: contributionData.date,
          source: 'Manual',
          notes: contributionData.note || null
        })
        const contribution = response.data?.data ?? response.data
        await this.fetchGoals()
        return contribution
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to add contribution'
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})
