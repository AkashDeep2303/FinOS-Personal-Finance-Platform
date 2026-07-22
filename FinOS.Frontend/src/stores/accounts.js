import { defineStore } from 'pinia'
import { accountsApi } from '../api/accounts'

const ACCOUNT_TYPE_IDS = {
  Savings: 1,
  Current: 2,
  FD: 5,
  MF: 5,
  Demat: 5,
  Wallet: 7,
  Cash: 6,
  PPF: 9
}

function normalizeAccount(account) {
  if (!account || typeof account !== 'object') return account
  const backendType = account.accountTypeName || account.accountType?.name || ''
  return {
    ...account,
    type: account.type || (backendType === 'Investment' ? 'MF' : backendType),
    bank: account.bank ?? account.institutionName ?? ''
  }
}

function unwrapAccounts(response) {
  const value = response?.data?.data ?? response?.data ?? []
  return Array.isArray(value) ? value.map(normalizeAccount) : []
}

function unwrapAccount(response) {
  return normalizeAccount(response?.data?.data ?? response?.data)
}

function toApiAccount(accountData, existing = {}) {
  const type = accountData.type || existing.type || 'Savings'
  return {
    accountTypeId: accountData.accountTypeId || existing.accountTypeId || ACCOUNT_TYPE_IDS[type] || 1,
    name: accountData.name?.trim() || '',
    institutionName: accountData.institutionName ?? accountData.bank ?? existing.institutionName ?? '',
    accountNumber: accountData.accountNumber ?? existing.accountNumber ?? null,
    balance: Number(accountData.balance ?? existing.balance ?? 0),
    creditLimit: Number(accountData.creditLimit ?? existing.creditLimit ?? 0),
    currency: accountData.currency || existing.currency || 'INR',
    color: accountData.color ?? existing.color ?? null,
    icon: accountData.icon ?? existing.icon ?? null,
    isIncludedInNetWorth: accountData.isIncludedInNetWorth ?? existing.isIncludedInNetWorth ?? true,
    notes: accountData.notes ?? existing.notes ?? null,
    isActive: accountData.isActive ?? existing.isActive ?? true
  }
}

export const useAccountsStore = defineStore('accounts', {
  state: () => ({
    accounts: [],
    currentAccount: null,
    loading: false,
    error: null
  }),

  getters: {
    totalBalance: (state) => state.accounts.reduce((sum, acc) => sum + (Number(acc.balance) || 0), 0),
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
        this.accounts = unwrapAccounts(await accountsApi.getAll())
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
        const response = await accountsApi.create(toApiAccount(accountData))
        this.accounts.push(unwrapAccount(response))
        return unwrapAccount(response)
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
        const existing = this.accounts.find(a => a.id === id) || {}
        const response = await accountsApi.update(id, toApiAccount(accountData, existing))
        const updated = unwrapAccount(response)
        const index = this.accounts.findIndex(a => a.id === id)
        if (index !== -1) this.accounts[index] = updated
        return updated
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
        this.currentAccount = unwrapAccount(response)
        return this.currentAccount
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch account'
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})