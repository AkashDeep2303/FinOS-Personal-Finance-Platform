import { defineStore } from 'pinia'
import { taxApi } from '../api/tax'
export const useTaxStore = defineStore('tax', {
  state: () => ({ profile: null, rules: [], comparison: null, loading: false, error: null }),
  actions: {
    async load(fy) {
      this.loading = true; this.error = null
      try {
        const [profile, rules] = await Promise.all([taxApi.profile(fy), taxApi.rules(fy)])
        this.profile = profile.data?.data ?? null
        this.rules = rules.data?.data ?? []
      } catch (e) { this.error = e.response?.data?.message || 'Failed to load tax profile' }
      finally { this.loading = false }
    },
    async save(fy, regime, input) {
      const response = await taxApi.save(fy, { preferredRegime: regime, inputJson: JSON.stringify(input) })
      this.profile = response.data?.data
    },
    async calculate(fy) {
      this.loading = true; this.error = null
      try {
        const response = await taxApi.calculate(fy)
        this.comparison = response.data?.data ?? null
      } catch (e) { this.error = e.response?.data?.message || 'Failed to calculate tax projection' }
      finally { this.loading = false }
    }
  }
})
