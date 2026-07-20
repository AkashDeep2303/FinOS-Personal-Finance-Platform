import { defineStore } from 'pinia'
import { accountsApi } from '../api/accounts'

export const useAccountsStore = defineStore('accounts', {
  state: () => ({
    accounts: [],
    currentAccount: null,
    loading: false,
    error: null
  }),

  getters: {
    totalBalance: (state) => {
      return state.accounts.reduce((sum, acc) => sum + (acc.balance || 0), 0)
    },
    accountsByType: (state) => {
      const grouped = {}
      state.accounts.forEach(acc => {
        if (!grouped[acc.type]) grouped[acc.type] = []
        grouped[acc.type].push(acc)
      })
      return grouped
    },
    savingsAccounts: (state) => state.accounts.filter(a => a.type === 'Savings'),
    currentAccounts: (state) => state.accounts.filter(a => a.type === 'Current'),
    fixedDeposits: (state) => state.accounts.filter(a => a.type === 'FD'),
    mutualFunds: (state) => state.accounts.filter(a => a.type === 'MF')
  },

  actions: {
    async fetchAccounts() {
      this.loading = true
      this.error = null
      try {
        const response = await accountsApi.getAll()
        this.accounts = response.data?.data ?? []
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch accounts'
      } finally {
        this.loading = false
      }
    },

    async createAccount(accountData) {
      this.loading = true
      this.error = null
      try {
        const response = await accountsApi.create(accountData)
        this.accounts.push(response.data?.data ?? response.data)
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create account'
        throw err
      } finally {
        this.loading = false
      }
    },

    async updateAccount(id, accountData) {
      this.loading = true
      this.error = null
      try {
        const response = await accountsApi.update(id, accountData)
        const index = this.accounts.findIndex(a => a.id === id)
        if (index !== -1) this.accounts[index] = response.data
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update account'
        throw err
      } finally {
        this.loading = false
      }
    },

    async deleteAccount(id) {
      this.loading = true
      this.error = null
      try {
        await accountsApi.delete(id)
        this.accounts = this.accounts.filter(a => a.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete account'
        throw err
      } finally {
        this.loading = false
      }
    },

    async getAccount(id) {
      this.loading = true
      this.error = null
      try {
        const response = await accountsApi.getById(id)
        this.currentAccount = response.data?.data ?? response.data
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch account'
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})
