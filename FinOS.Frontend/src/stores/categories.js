import { defineStore } from 'pinia'
import { categoriesApi } from '../api/categories'

const flatten = categories => categories.flatMap(category => [category, ...flatten(category.children ?? [])])

export const useCategoriesStore = defineStore('categories', {
  state: () => ({ categories: [], loading: false, error: null }),
  actions: {
    async load() {
      this.loading = true
      this.error = null
      try {
        const response = await categoriesApi.list()
        this.categories = flatten(response.data?.data ?? [])
      } catch (error) {
        this.error = error.response?.data?.message || 'Could not load categories.'
      } finally {
        this.loading = false
      }
    },
    async create(category) {
      const response = await categoriesApi.create(category)
      this.categories.push(response.data?.data)
    },
    async update(category) {
      const response = await categoriesApi.update(category.id, {
        name: category.name, icon: category.icon || null, color: category.color || null,
        budgetAmount: Number(category.budgetAmount || 0), isActive: category.isActive,
        sortOrder: Number(category.sortOrder || 0),
        cashFlowClassification: category.cashFlowClassification
      })
      const index = this.categories.findIndex(item => item.id === category.id)
      if (index >= 0) this.categories[index] = response.data?.data
    }
  }
})
