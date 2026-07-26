<template>
  <div class="space-y-6">
    <div>
      <p class="text-sm font-medium text-primary-700">Plan</p>
      <h1 class="text-2xl font-bold text-gray-900">Advanced Goal Funding</h1>
      <p class="mt-1 text-sm text-gray-500">Check whether your available monthly surplus can fund all active goals on time.</p>
    </div>

    <form class="flex flex-col gap-3 rounded-xl border border-gray-200 bg-white p-5 shadow-sm sm:flex-row sm:items-end" @submit.prevent="analyze">
      <label class="flex-1 text-sm font-medium text-gray-700">Available monthly investment surplus
        <input v-model.number="availableSurplus" class="finos-input mt-1" type="number" min="0" required />
      </label>
      <button class="finos-btn-primary justify-center" :disabled="store.loading">{{ store.loading ? 'Analyzing…' : 'Analyze funding' }}</button>
    </form>
    <ErrorState v-if="store.error" :message="store.error" :retryable="false" />

    <template v-if="analysis">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <MetricCard title="Available Surplus" :value="formatMoney(analysis.availableMonthlySurplus)" icon="₹" />
        <MetricCard title="Required Across Goals" :value="formatMoney(analysis.totalRequiredMonthlyContribution)" icon="◎" />
        <MetricCard title="Funding Deficit" :value="formatMoney(analysis.fundingDeficit)" icon="△" :color="analysis.hasConflict ? 'red' : 'green'" />
      </div>

      <div v-if="analysis.hasConflict" class="rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-900">
        Required contributions exceed available surplus by <strong>{{ formatMoney(analysis.fundingDeficit) }}</strong>.
        Review lower-priority goals, target dates, or planned contributions.
      </div>

      <div class="overflow-x-auto rounded-xl border border-gray-200 bg-white shadow-sm">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 text-left text-xs uppercase tracking-wide text-gray-500">
            <tr><th>Goal</th><th>Priority</th><th>Remaining</th><th>Required / month</th><th>Actual / month</th><th>Projected date</th><th>Status</th></tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="goal in analysis.goals" :key="goal.goalId">
              <td><p class="font-medium text-gray-900">{{ goal.name }}</p><p class="text-xs text-gray-500">{{ goal.category }}</p></td>
              <td>{{ goal.priority }}</td><td>{{ formatMoney(goal.remainingAmount) }}</td>
              <td>{{ formatMoney(goal.requiredMonthlyContribution) }}</td><td>{{ formatMoney(goal.actualMonthlyContribution) }}</td>
              <td>{{ formatIndianDate(goal.projectedCompletionDate) }}</td>
              <td><FinancialStatusBadge :status="statusTone(goal.status)" :label="goal.status" /></td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
    <EmptyState v-else title="Analyze all active goals" message="Enter the monthly amount available after expenses, EMIs, and essential reserves." icon="≋" />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useGoalsStore } from '../stores/goals'
import { formatIndianDate, formatMoney } from '../utils/formatters'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
import MetricCard from '../components/StatCard.vue'

const store = useGoalsStore()
const availableSurplus = ref(25000)
const analysis = computed(() => store.fundingAnalysis)
function analyze() { return store.fetchFundingAnalysis(availableSurplus.value) }
function statusTone(status) {
  return ['On Track', 'Ahead', 'Completed'].includes(status) ? 'positive' :
    ['Behind', 'Unfunded'].includes(status) ? 'negative' : 'warning'
}
</script>

<style scoped>
th,td{@apply px-4 py-3}
</style>
