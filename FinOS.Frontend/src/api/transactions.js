import api from './axios'

const toNumberOrNull = (value) => value === '' || value == null ? null : Number(value)

const toRequest = ({ date, accountId, categoryId, transferAccountId, amount, ...transaction }) => ({
  ...transaction,
  accountId: toNumberOrNull(accountId),
  categoryId: toNumberOrNull(categoryId),
  transferAccountId: toNumberOrNull(transferAccountId),
  amount: Number(amount),
  transactionDate: date
})

export const transactionsApi = {
  getAll(params = {}) {
    return api.get('/api/corefinance/transactions/filter', { params })
  },

  getById(id) {
    return api.get(`/api/corefinance/transactions/${id}`)
  },

  getCategories() {
    return api.get('/api/corefinance/categories')
  },

  create(data) {
    return api.post('/api/corefinance/transactions', toRequest(data))
  },

  update(id, data) {
    return api.put(`/api/corefinance/transactions/${id}`, toRequest(data))
  },

  delete(id) {
    return api.delete(`/api/corefinance/transactions/${id}`)
  }
}