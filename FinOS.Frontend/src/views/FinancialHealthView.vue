<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">Financial Health</h1><p class="mt-1 text-sm text-gray-500">A deterministic view of the financial areas that need attention.</p></header>
    <LoadingState v-if="store.loading && !score.score" message="Loading financial health..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="load" />
    <EmptyState v-else-if="!score.score" title="Not enough data for a score" description="Add income, expenses, investments, debt, and goal information to generate a meaningful health score." />
    <template v-else>
      <section class="rounded-2xl bg-gradient-to-r from-indigo-600 to-violet-600 p-7 text-white">
        <p class="text-sm font-medium text-indigo-100">Overall score</p>
        <div class="mt-2 flex items-end gap-3"><span class="text-5xl font-bold">{{ score.score }}</span><span class="pb-1 text-lg">/ 100 · {{ score.grade }}</span></div>
        <p v-if="previous" class="mt-3 text-sm text-indigo-100">Previous {{ previous.overallScore }}/100 · Change {{ signed(score.score - previous.overallScore) }}</p>
      </section>
      <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <article v-for="factor in score.factors" :key="factor.name" class="rounded-xl border border-gray-200 bg-white p-5">
          <div class="flex items-center justify-between"><h2 class="font-semibold text-gray-900">{{ factor.name }}</h2><FinancialStatusBadge :status="status(factor.score).tone" :label="status(factor.score).label" /></div>
          <p class="mt-3 text-3xl font-bold text-gray-900">{{ factor.score }}<span class="text-base text-gray-400">/100</span></p>
          <div class="mt-3 h-2 overflow-hidden rounded bg-gray-100"><div class="h-full rounded bg-indigo-500" :style="{ width: `${factor.score}%` }"></div></div>
          <p class="mt-3 text-sm text-gray-500">{{ explanation(factor) }}</p>
        </article>
      </section>
      <FinancialMetricExplanation title="Why this score?" description="FinOS combines independently calculated savings, debt, emergency-fund, investment, and goal-progress dimensions." :value="`${score.score}/100`" calculation="Each component is scored using deterministic thresholds; the overall score is the weighted result saved with its source figures." why-it-matters="Component scores reveal which action is most likely to improve resilience." improvement="Start with the lowest component while checking that the underlying data is complete and current." />
    </template>
  </div>
</template>
<script setup>
import { computed, onMounted } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import LoadingState from '../components/LoadingState.vue'
import ErrorState from '../components/ErrorState.vue'
import EmptyState from '../components/EmptyState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
import FinancialMetricExplanation from '../components/FinancialMetricExplanation.vue'
const store = useAnalyticsStore()
const score = computed(() => store.financialScore)
const previous = computed(() => store.financialScoreHistory.length > 1 ? store.financialScoreHistory.at(-2) : null)
const signed = value => `${value >= 0 ? '+' : ''}${value}`
const status = value => value >= 80
  ? { tone: 'positive', label: 'Healthy' }
  : value >= 60
    ? { tone: 'warning', label: 'Watch' }
    : { tone: 'negative', label: 'Needs attention' }
const explanation = factor => {
  if (factor.name === 'Emergency Fund') return `${Number(factor.value || 0).toFixed(1)} months of expense coverage recorded.`
  if (factor.name === 'Savings Rate') return `${Number(factor.value || 0).toFixed(1)}% of income is currently saved.`
  if (factor.name === 'Debt Management') return `Recorded debt-to-income ratio is ${Number(factor.value || 0).toFixed(1)}%.`
  if (factor.name === 'Investments') return `Investment-to-income ratio is ${Number(factor.value || 0).toFixed(1)}.`
  return 'Based on current recorded goal progress.'
}
const load = () => store.fetchFinancialScore()
onMounted(load)
</script>
