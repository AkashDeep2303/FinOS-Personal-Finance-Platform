<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">Cash Flow</h1><p class="mt-1 text-sm text-gray-500">Understand how income becomes spending, EMIs, investments, and savings.</p></header>
    <section class="flex flex-wrap items-end gap-3 rounded-xl border border-gray-200 bg-white p-4">
      <label class="text-sm text-gray-600">Range
        <select v-model="range" class="mt-1 block rounded-lg border border-gray-300 px-3 py-2" @change="load">
          <option value="3">3M</option><option value="6">6M</option><option value="12">1Y</option><option value="FY">FY</option><option value="Custom">Custom</option>
        </select>
      </label>
      <template v-if="range === 'Custom'">
        <label class="text-sm text-gray-600">From<input v-model="startDate" type="date" class="mt-1 block rounded-lg border border-gray-300 px-3 py-2"></label>
        <label class="text-sm text-gray-600">To<input v-model="endDate" type="date" :max="today" class="mt-1 block rounded-lg border border-gray-300 px-3 py-2"></label>
        <button class="rounded-lg bg-indigo-600 px-4 py-2 font-medium text-white" @click="load">Apply</button>
      </template>
    </section>
    <LoadingState v-if="store.loading && !data" message="Loading cash flow..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="load" />
    <EmptyState v-else-if="!data?.series?.some(row => row.income || row.expenses)" title="No cash-flow history yet" description="Import or add transactions to generate monthly cash-flow analytics." />
    <template v-else>
      <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Total Income" :value="formatMoney(metrics.income)" />
        <StatCard title="Total Expenses" :value="formatMoney(metrics.expenses)" />
        <StatCard title="Average Surplus" :value="formatMoney(metrics.averageSurplus)" />
        <StatCard title="Savings Rate" :value="formatPercentage(metrics.savingsRatePct)" />
      </section>
      <section class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        <StatCard title="Expense Ratio" :value="formatPercentage(metrics.expenseRatioPct)" />
        <StatCard title="EMI Ratio" :value="formatPercentage(metrics.emiRatioPct)" />
        <StatCard title="Fixed Cost Ratio" :value="formatPercentage(metrics.fixedCostRatioPct)" />
        <StatCard title="Lifestyle Ratio" :value="formatPercentage(metrics.lifestyleCostRatioPct)" />
        <StatCard title="Investment Rate" :value="formatPercentage(metrics.investmentRatePct)" />
        <StatCard title="Income Volatility" :value="formatPercentage(metrics.incomeVolatilityPct)" />
        <StatCard title="Expense Volatility" :value="formatPercentage(metrics.expenseVolatilityPct)" />
        <StatCard title="Latest Surplus" :value="formatMoney(metrics.monthlySurplus)" />
      </section>
      <ChartCard title="Income vs expenses"><div class="h-80"><Bar :data="flowChart" :options="chartOptions" /></div></ChartCard>
      <ChartCard title="Monthly surplus"><div class="h-72"><Line :data="surplusChart" :options="chartOptions" /></div></ChartCard>
      <ChartCard title="Expense allocation"><div class="h-80"><Bar :data="allocationChart" :options="stackedOptions" /></div></ChartCard>
      <FinancialMetricExplanation title="Understand savings rate" description="The share of recorded income remaining after recorded expenses." :value="formatPercentage(metrics.savingsRatePct)" calculation="(Income − Expenses) ÷ Income × 100." why-it-matters="A sustainable positive rate creates capacity for emergencies, goals, investment, and debt reduction." improvement="Review fixed and lifestyle costs, automate savings, and direct irregular income intentionally." />
      <FinancialMetricExplanation title="Understand volatility" description="How much monthly income or expenses vary relative to their average." :value="`Income ${formatPercentage(metrics.incomeVolatilityPct)} · Expenses ${formatPercentage(metrics.expenseVolatilityPct)}`" calculation="Population standard deviation ÷ monthly average × 100." why-it-matters="Higher volatility requires a larger liquidity buffer and more conservative commitments." improvement="Separate irregular expenses into sinking funds and avoid relying on unusually high-income months." />
    </template>
  </div>
</template>
<script setup>
import { computed, onMounted, ref } from 'vue'
import { Bar, Line } from 'vue-chartjs'
import { Chart as ChartJS, BarElement, CategoryScale, LinearScale, LineElement, PointElement, Tooltip, Legend } from 'chart.js'
import { useAnalyticsStore } from '../stores/analytics'
import { formatMoney, formatPercentage } from '../utils/formatters'
import StatCard from '../components/StatCard.vue'
import ChartCard from '../components/ChartCard.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import LoadingState from '../components/LoadingState.vue'
import FinancialMetricExplanation from '../components/FinancialMetricExplanation.vue'
ChartJS.register(BarElement, CategoryScale, LinearScale, LineElement, PointElement, Tooltip, Legend)
const store = useAnalyticsStore()
const range = ref('12')
const today = new Date().toISOString().slice(0, 10)
const startDate = ref(new Date(Date.now() - 365 * 86400000).toISOString().slice(0, 10))
const endDate = ref(today)
const data = computed(() => store.cashFlow)
const metrics = computed(() => data.value?.metrics ?? {})
const rows = computed(() => data.value?.series ?? [])
const labels = computed(() => rows.value.map(x => `${String(x.yearMonth).slice(4)}/${String(x.yearMonth).slice(0, 4)}`))
const flowChart = computed(() => ({ labels: labels.value, datasets: [{ label: 'Income', data: rows.value.map(x => x.income), backgroundColor: '#10b981' }, { label: 'Expenses', data: rows.value.map(x => x.expenses), backgroundColor: '#ef4444' }] }))
const surplusChart = computed(() => ({ labels: labels.value, datasets: [{ label: 'Surplus', data: rows.value.map(x => x.surplus), borderColor: '#4f46e5', backgroundColor: '#c7d2fe', tension: .3 }] }))
const allocationChart = computed(() => ({ labels: labels.value, datasets: [
  ['Essential', 'essentialExpenses', '#ef4444'], ['Lifestyle', 'lifestyleExpenses', '#fb923c'],
  ['EMI', 'emiPayments', '#f59e0b'], ['Investments', 'investments', '#3b82f6'],
  ['Other', 'otherExpenses', '#9ca3af']
].map(([label, key, color]) => ({ label, data: rows.value.map(x => x[key]), backgroundColor: color })) }))
const chartOptions = { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
const stackedOptions = { ...chartOptions, scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true } } }
function rangeQuery() {
  if (range.value === 'FY') {
    const now = new Date()
    const year = now.getMonth() < 3 ? now.getFullYear() - 1 : now.getFullYear()
    return { startDate: `${year}-04-01`, endDate: today }
  }
  if (range.value === 'Custom') return { startDate: startDate.value, endDate: endDate.value }
  return { months: Number(range.value) }
}
const load = () => store.fetchCashFlow(rangeQuery())
onMounted(load)
</script>
