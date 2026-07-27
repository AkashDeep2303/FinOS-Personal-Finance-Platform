<template>
  <div class="space-y-6">
    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-end">
      <div>
        <p class="text-sm font-medium text-primary-700">Home</p>
        <h1 class="text-2xl font-bold text-gray-900">Financial Command Center</h1>
        <p class="mt-1 text-sm text-gray-500">Your current financial position, flow, health, and priorities.</p>
      </div>
      <span v-if="data?.asOfUtc" class="text-xs text-gray-500">Updated {{ formatIndianDate(data.asOfUtc, { long: true }) }}</span>
    </div>

    <LoadingState v-if="store.loading && !data" />
    <ErrorState v-else-if="store.error && !data" :message="store.error" @retry="load" />

    <template v-else-if="data">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Net Worth" :value="formatMoney(data.metrics.netWorth, { compact: true })" icon="₹"
          :trend="trendDirection(data.metrics.netWorthChange)" :trend-value="formatChange(data.metrics.netWorthChange)" />
        <MetricCard title="Cash Available" :value="formatMoney(data.metrics.cashAvailable, { compact: true })" icon="◫" color="blue" />
        <MetricCard title="Monthly Surplus" :value="formatMoney(data.metrics.monthlySurplus, { compact: true })" icon="↗"
          :trend="trendDirection(data.metrics.monthlySurplus)" trend-value="Income less expenses" color="green" />
        <MetricCard title="Financial Health" :value="data.metrics.financialHealthScore == null ? 'Not scored' : `${data.metrics.financialHealthScore} / 100`"
          icon="◎" color="purple" />
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Monthly Income" :value="formatMoney(data.metrics.monthlyIncome, { compact: true })" icon="+" color="green" />
        <MetricCard title="Monthly Expenses" :value="formatMoney(data.metrics.monthlyExpenses, { compact: true })" icon="−" />
        <MetricCard title="Savings Rate" :value="formatPercentage(data.metrics.savingsRatePct)" icon="%" color="blue" />
        <MetricCard title="Net Worth Change" :value="data.metrics.netWorthChange == null ? 'Not available' : formatMoney(data.metrics.netWorthChange, { compact: true })"
          :trend="trendDirection(data.metrics.netWorthChange)" :trend-value="formatPercentage(data.metrics.netWorthChangePct)" icon="Δ" />
      </div>

      <div class="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <ChartCard title="Monthly Money Flow" subtitle="Only values supported by recorded data are shown." class="xl:col-span-2">
          <div class="space-y-4">
            <FlowRow label="Income" :value="data.moneyFlow.income" :maximum="flowMaximum" color="bg-green-500" />
            <FlowRow label="Essential expenses" :value="data.moneyFlow.essentialExpenses" :maximum="flowMaximum" color="bg-red-500" />
            <FlowRow label="Lifestyle expenses" :value="data.moneyFlow.lifestyleExpenses" :maximum="flowMaximum" color="bg-orange-400" />
            <FlowRow label="EMIs" :value="data.moneyFlow.emiPayments" :maximum="flowMaximum" color="bg-amber-500" />
            <FlowRow label="Investments" :value="data.moneyFlow.investments" :maximum="flowMaximum" color="bg-blue-500" />
            <FlowRow label="Other / unclassified" :value="data.moneyFlow.otherExpenses" :maximum="flowMaximum" color="bg-gray-400" />
            <FlowRow label="Savings / Free Cash" :value="data.moneyFlow.freeCash" :maximum="flowMaximum" color="bg-primary-500" />
          </div>
          <p class="mt-5 text-xs text-gray-500">Expense groups follow each transaction category's configured cash-flow classification.</p>
        </ChartCard>

        <ChartCard title="Financial Health">
          <div class="flex items-center gap-5">
            <div class="flex h-24 w-24 items-center justify-center rounded-full border-8 border-primary-100 text-2xl font-bold text-primary-700">
              {{ data.financialHealth.overallScore ?? '—' }}
            </div>
            <div class="space-y-2 text-sm">
              <p>Grade <strong>{{ data.financialHealth.grade ?? 'Not available' }}</strong></p>
              <p>Savings rate <strong>{{ formatPercentage(data.financialHealth.savingsRatePct) }}</strong></p>
              <p>DTI <strong>{{ formatPercentage(data.financialHealth.debtToIncomeRatio, { fraction: true }) }}</strong></p>
              <p>Emergency fund <strong>{{ data.financialHealth.emergencyFundMonths?.toFixed(1) ?? '—' }} months</strong></p>
            </div>
          </div>
        </ChartCard>
      </div>

      <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <ChartCard title="Assets" :subtitle="formatMoney(data.balanceSheet.totalAssets)">
          <BreakdownList :items="data.balanceSheet.assets" empty-message="Create a net-worth snapshot to see asset details." />
        </ChartCard>
        <ChartCard title="Liabilities" :subtitle="formatMoney(data.balanceSheet.totalLiabilities)">
          <BreakdownList :items="data.balanceSheet.liabilities" empty-message="No recorded liabilities in the latest snapshot." />
        </ChartCard>
      </div>

      <section>
        <div class="mb-4 flex items-end justify-between">
          <div><h2 class="text-xl font-semibold text-gray-900">This Month's Financial Intelligence</h2><p class="text-sm text-gray-500">Deterministic observations based on recorded figures.</p></div>
        </div>
        <div v-if="data.insights.length" class="grid grid-cols-1 gap-4 lg:grid-cols-3">
          <InsightCard v-for="insight in data.insights" :key="insight.code" :title="insight.title"
            :explanation="insight.explanation" :area="insight.area" :calculation="insight.calculation"
            :status="insightStatus(insight.severity)" :label="insight.severity"
            :to="insight.actionRoute" :action="insight.actionLabel" />
        </div>
        <EmptyState v-else title="No urgent insights" message="FinOS has not detected a priority warning from the currently available data." icon="✓" />
      </section>

      <ChartCard title="Data Completeness" :subtitle="`${data.dataCompleteness.score}% of Command Center inputs available`">
        <div class="h-2 overflow-hidden rounded-full bg-gray-100"><div class="h-full rounded-full bg-primary-600" :style="{ width: `${data.dataCompleteness.score}%` }"></div></div>
        <div v-if="data.dataCompleteness.missing.length" class="mt-4">
          <p class="text-sm font-medium text-gray-800">Still needed</p>
          <ul class="mt-2 grid gap-2 text-sm text-gray-600 sm:grid-cols-2"><li v-for="item in data.dataCompleteness.missing" :key="item">• {{ item }}</li></ul>
        </div>
      </ChartCard>
    </template>
  </div>
</template>

<script setup>
import { computed, defineComponent, h, onMounted } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import { formatIndianDate, formatMoney, formatPercentage } from '../utils/formatters'
import MetricCard from '../components/StatCard.vue'
import ChartCard from '../components/ChartCard.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import InsightCard from '../components/InsightCard.vue'
import LoadingState from '../components/LoadingState.vue'

const store = useAnalyticsStore()
const data = computed(() => store.commandCenter)
const flowMaximum = computed(() => Math.max(data.value?.moneyFlow?.income || 0, data.value?.moneyFlow?.totalExpenses || 0, 1))

const FlowRow = defineComponent({
  props: { label: String, value: Number, maximum: Number, color: String },
  setup: props => () => h('div', [
    h('div', { class: 'mb-1 flex justify-between text-sm' }, [h('span', props.label), h('strong', formatMoney(props.value))]),
    h('div', { class: 'h-3 overflow-hidden rounded-full bg-gray-100' }, h('div', {
      class: `h-full rounded-full ${props.color}`,
      style: { width: `${Math.max(0, Math.min(100, props.value / props.maximum * 100))}%` }
    }))
  ])
})

const BreakdownList = defineComponent({
  props: { items: Array, emptyMessage: String },
  setup: props => () => props.items?.length
    ? h('div', { class: 'space-y-3' }, props.items.map(item => h('div', { class: 'flex justify-between text-sm' }, [h('span', item.name), h('strong', formatMoney(item.amount))])))
    : h('p', { class: 'text-sm text-gray-500' }, props.emptyMessage)
})

function trendDirection(value) { return value > 0 ? 'up' : value < 0 ? 'down' : 'flat' }
function formatChange(value) { return value == null ? '' : `${value >= 0 ? '+' : ''}${formatMoney(value)}` }
function insightStatus(severity) { return severity === 'high' ? 'negative' : severity === 'info' ? 'neutral' : 'warning' }
function load() { return store.fetchCommandCenter() }
onMounted(load)
</script>
