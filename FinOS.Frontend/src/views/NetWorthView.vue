<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">Net Worth</h1><p class="mt-1 text-sm text-gray-500">What you own minus what you owe, using periodic FinOS snapshots.</p></header>
    <DateRangeFilter v-model="months" :options="ranges" @change="load" />
    <LoadingState v-if="store.loading && !latest" message="Loading net worth history..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="load" />
    <EmptyState v-else-if="!latest" title="No net worth snapshots yet" description="Add accounts, investments, and loans, then refresh analytics to create your first snapshot." />
    <template v-else>
      <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Total Assets" :value="formatMoney(latest.totalAssets)" />
        <StatCard title="Total Liabilities" :value="formatMoney(latest.totalLiabilities)" />
        <StatCard title="Net Worth" :value="formatMoney(latest.netWorth)" />
        <StatCard title="Latest Change" :value="formatMoney(latest.changeFromPrevious || 0)" :change="latest.changePctFromPrevious || 0" />
      </section>
      <ChartCard title="Net Worth History" subtitle="Assets, liabilities, and net worth across saved snapshots"><div class="h-80"><Line :data="historyChart" :options="chartOptions" /></div></ChartCard>
      <div class="grid gap-6 lg:grid-cols-2">
        <ChartCard title="Asset Allocation"><div class="h-72"><Doughnut :data="assetChart" :options="doughnutOptions" /></div></ChartCard>
        <ChartCard title="Liability Breakdown"><div class="h-72"><Doughnut :data="liabilityChart" :options="doughnutOptions" /></div></ChartCard>
      </div>
      <FinancialMetricExplanation title="Understand net worth" description="Net worth is the value of all recorded assets less all recorded liabilities." :value="formatMoney(latest.netWorth)" calculation="Cash and bank + investments + property + gold + other assets − loans − credit cards − other liabilities." why-it-matters="Its direction over time is more useful than a single point-in-time value." improvement="Increase durable savings and investments, keep valuations current, and reduce expensive debt." />
    </template>
  </div>
</template>
<script setup>
import { computed, onMounted, ref } from 'vue'
import { Doughnut, Line } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, CategoryScale, LinearScale, LineElement, PointElement, Tooltip, Legend } from 'chart.js'
import { useAnalyticsStore } from '../stores/analytics'
import { formatMoney } from '../utils/formatters'
import StatCard from '../components/StatCard.vue'
import ChartCard from '../components/ChartCard.vue'
import DateRangeFilter from '../components/DateRangeFilter.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import LoadingState from '../components/LoadingState.vue'
import FinancialMetricExplanation from '../components/FinancialMetricExplanation.vue'
ChartJS.register(ArcElement, CategoryScale, LinearScale, LineElement, PointElement, Tooltip, Legend)
const store = useAnalyticsStore()
const months = ref(12)
const ranges = [{ label: '3M', value: 3 }, { label: '6M', value: 6 }, { label: '1Y', value: 12 }, { label: '3Y', value: 36 }, { label: '5Y', value: 60 }, { label: 'ALL', value: 120 }]
const latest = computed(() => store.netWorthTrend.at(-1))
const labels = computed(() => store.netWorthTrend.map(x => new Date(x.snapshotDate).toLocaleDateString('en-IN', { month: 'short', year: '2-digit' })))
const historyChart = computed(() => ({ labels: labels.value, datasets: [
  { label: 'Net Worth', data: store.netWorthTrend.map(x => x.netWorth), borderColor: '#4f46e5', tension: .3 },
  { label: 'Assets', data: store.netWorthTrend.map(x => x.totalAssets), borderColor: '#10b981', tension: .3 },
  { label: 'Liabilities', data: store.netWorthTrend.map(x => x.totalLiabilities), borderColor: '#ef4444', tension: .3 }
] }))
const assetChart = computed(() => ({ labels: ['Cash & Bank', 'Investments', 'Property', 'Gold', 'Other'], datasets: [{ data: latest.value ? [latest.value.cashAndBank, latest.value.investmentValue, latest.value.realEstateValue, latest.value.goldValue, latest.value.otherAssets] : [], backgroundColor: ['#06b6d4','#4f46e5','#10b981','#f59e0b','#94a3b8'] }] }))
const liabilityChart = computed(() => ({ labels: ['Loans', 'Credit Cards', 'Other'], datasets: [{ data: latest.value ? [latest.value.loanOutstanding, latest.value.creditCardOutstanding, latest.value.otherLiabilities] : [], backgroundColor: ['#ef4444','#f97316','#94a3b8'] }] }))
const chartOptions = { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
const doughnutOptions = chartOptions
const load = () => store.fetchNetWorthTrend({ months: months.value })
onMounted(load)
</script>
