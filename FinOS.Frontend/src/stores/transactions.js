import { defineStore } from 'pinia'
import { transactionsApi } from '../api/transactions'

const normalizeTransaction = (transaction) => ({
  ...transaction,
  date: transaction.transactionDate ?? transaction.date,
  category: transaction.categoryName ?? transaction.category
})

export const useTransactionsStore = defineStore('transactions', {
  state: () => ({
    transactions: [],
    currentTransaction: null,
    filters: {
      type: '',
      accountId: '',
      categoryId: '',
      startDate: '',
      endDate: '',
      search: '',
      page: 1,
      pageSize: 20
    },
    totalCount: 0,
    loading: false,
    error: null
  }),

  getters: {
    filteredTransactions: (state) => {
      let result = [...state.transactions]
      if (state.filters.type) {
        result = result.filter(t => t.type === state.filters.type)
      }
      if (state.filters.accountId) {
        result = result.filter(t => t.accountId === state.filters.accountId)
      }
      if (state.filters.categoryId) {
        result = result.filter(t => t.categoryId === state.filters.categoryId)
      }
      if (state.filters.search) {
        const search = state.filters.search.toLowerCase()
        result = result.filter(t =>
          t.description?.toLowerCase().includes(search) ||
          t.category?.toLowerCase().includes(search)
        )
      }
      return result
    },
    totalIncome: (state) => {
      return state.transactions
        .filter(t => t.type === 'Income')
        .reduce((sum, t) => sum + (t.amount || 0), 0)
    },
    totalExpense: (state) => {
      return state.transactions
        .filter(t => t.type === 'Expense')
        .reduce((sum, t) => sum + (t.amount || 0), 0)
    },
    recentTransactions: (state) => {
      return [...state.transactions]
        .sort((a, b) => new Date(b.date) - new Date(a.date))
        .slice(0, 10)
    }
  },

  actions: {
    async fetchTransactions(params = {}) {
      this.loading = true
      this.error = null
      try {
        const queryParams = { ...this.filters, ...params }
        const response = await transactionsApi.getAll(queryParams)
        const payload = response.data?.data ?? response.data ?? {}
        this.transactions = Array.isArray(payload.items) ? payload.items.map(normalizeTransaction) : []
        this.totalCount = payload.totalCount ?? this.transactions.length
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch transactions'
      } finally {
        this.loading = false
      }
    },

    async createTransaction(transactionData) {
      this.loading = true
      this.error = null
      try {
        const response = await transactionsApi.create(transactionData)
        const transaction = normalizeTransaction(response.data?.data ?? response.data)
        this.transactions.unshift(transaction)
        return transaction
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create transaction'
        throw err
      } finally {
        this.loading = false
      }
    },

    async updateTransaction(id, transactionData) {
      this.loading = true
      this.error = null
      try {
        const response = await transactionsApi.update(id, transactionData)
        const transaction = normalizeTransaction(response.data?.data ?? response.data)
        const index = this.transactions.findIndex(t => t.id === id)
        if (index !== -1) this.transactions[index] = transaction
        return transaction
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update transaction'
        throw err
      } finally {
        this.loading = false
      }
    },

    async deleteTransaction(id) {
      this.loading = true
      this.error = null
      try {
        await transactionsApi.delete(id)
        this.transactions = this.transactions.filter(t => t.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete transaction'
        throw err
      } finally {
        this.loading = false
      }
    },

    setFilters(newFilters) {
      this.filters = { ...this.filters, ...newFilters }
    },

    resetFilters() {
      this.filters = {
        type: '',
        accountId: '',
        categoryId: '',
        startDate: '',
        endDate: '',
        search: '',
        page: 1,
        pageSize: 20
      }
    }
  }
})
