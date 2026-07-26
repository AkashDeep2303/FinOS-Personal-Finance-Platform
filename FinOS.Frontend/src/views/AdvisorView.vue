<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">FinOS Advisor</h1><p class="mt-1 text-sm text-gray-500">Deterministic opportunities and warnings based only on your recorded financial data.</p></header>
    <LoadingState v-if="store.loading && !items.length" message="Reviewing your financial position..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="store.fetchAdvisorOpportunities" />
    <EmptyState v-else-if="!items.length" title="No opportunities detected" description="Keep accounts, transactions, loans, investments, and goals current so FinOS can identify actionable changes." />
    <section v-else class="grid gap-4 lg:grid-cols-2">
      <InsightCard
        v-for="item in items"
        :key="item.code"
        :title="item.title"
        :explanation="item.explanation"
        :area="item.area"
        :calculation="item.calculation"
        :status="tone(item.severity)"
        :label="item.severity"
        :to="item.actionRoute"
        :action="item.actionLabel"
      />
    </section>
    <div class="rounded-xl border border-blue-200 bg-blue-50 p-4 text-sm text-blue-900">Advisor results come from backend calculations. AI may explain or prioritize these results, but it does not calculate tax, returns, debt, net worth, or affordability.</div>
  </div>
</template>
<script setup>
import { computed, onMounted } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import InsightCard from '../components/InsightCard.vue'
import LoadingState from '../components/LoadingState.vue'
import ErrorState from '../components/ErrorState.vue'
import EmptyState from '../components/EmptyState.vue'
const store = useAnalyticsStore()
const items = computed(() => store.advisorOpportunities)
const tone = severity => ({ Critical: 'negative', Warning: 'warning', Opportunity: 'positive' }[severity] || 'info')
onMounted(store.fetchAdvisorOpportunities)
</script>
