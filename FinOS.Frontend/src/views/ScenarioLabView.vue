<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">What If? Scenario Lab</h1><p class="mt-1 text-sm text-gray-500">Compare a hypothetical decision without changing real financial data.</p></header>
    <form class="rounded-xl border border-gray-200 bg-white p-6" @submit.prevent="run">
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <label class="text-sm">Scenario<select v-model="form.scenarioType" class="mt-1 w-full rounded-lg border-gray-300"><option>Buy a House</option><option>Buy a Car</option><option>Job Loss</option><option>Increase SIP</option><option>Custom</option></select></label>
        <label v-for="field in fields" :key="field.key" class="text-sm">{{ field.label }}<input v-model.number="form[field.key]" type="number" :min="field.allowNegative ? undefined : 0" class="mt-1 w-full rounded-lg border-gray-300"></label>
      </div>
      <div class="mt-5 flex flex-wrap gap-3"><button :disabled="store.loading" class="rounded-lg bg-indigo-600 px-5 py-2.5 font-medium text-white disabled:opacity-50">Run scenario</button><button v-if="result" type="button" class="rounded-lg border border-indigo-300 px-5 py-2.5 font-medium text-indigo-700" @click="save">Save scenario</button></div>
    </form>
    <ErrorState v-if="store.error" :message="store.error" @retry="run" />
    <section v-if="result" class="space-y-5">
      <div class="flex items-center gap-3"><h2 class="text-xl font-bold">Verdict</h2><FinancialStatusBadge :status="tone" :label="result.verdict" /></div>
      <ScenarioComparison :rows="comparison" />
      <div class="rounded-xl border border-gray-200 bg-white p-5"><h3 class="font-semibold">Why?</h3><ul class="mt-2 list-disc space-y-1 pl-5 text-sm text-gray-600"><li v-for="reason in result.reasons" :key="reason">{{ reason }}</li></ul><p class="mt-3 text-xs text-gray-500">Results use only the supplied assumptions and do not alter accounts, goals, loans, or investments.</p></div>
    </section>
    <section class="rounded-xl border border-gray-200 bg-white p-5">
      <h2 class="font-semibold">Saved scenarios</h2>
      <p v-if="!store.savedScenarios.length" class="mt-2 text-sm text-gray-500">No saved simulations yet.</p>
      <div v-else class="mt-3 divide-y">
        <div v-for="item in store.savedScenarios" :key="item.id" class="flex items-center justify-between py-3">
          <button class="text-left" @click="loadSaved(item)"><b class="block">{{ item.name }}</b><span class="text-xs text-gray-500">{{ item.scenarioType }} · {{ item.verdict }}</span></button>
          <button class="text-sm text-red-600" @click="store.deleteScenario(item.id)">Delete</button>
        </div>
      </div>
    </section>
  </div>
</template>
<script setup>
import { computed, reactive, onMounted } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import { formatMoney, formatPercentage } from '../utils/formatters'
import ScenarioComparison from '../components/ScenarioComparison.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
import ErrorState from '../components/ErrorState.vue'
const store = useAnalyticsStore()
const result = computed(() => store.scenarioResult)
const form = reactive({ scenarioType: 'Buy a House', currentNetWorth: 3000000, monthlyIncome: 150000, monthlyExpenses: 70000, monthlyDebtPayments: 20000, liquidAssets: 600000, oneTimeCost: 500000, monthlyIncomeChange: 0, monthlyExpenseChange: 10000, newMonthlyDebtPayment: 35000, horizonMonths: 12 })
const fields = [
  { key: 'currentNetWorth', label: 'Current net worth' }, { key: 'monthlyIncome', label: 'Monthly income' },
  { key: 'monthlyExpenses', label: 'Monthly expenses' }, { key: 'monthlyDebtPayments', label: 'Current EMIs' },
  { key: 'liquidAssets', label: 'Liquid assets' }, { key: 'oneTimeCost', label: 'One-time cost' },
  { key: 'monthlyIncomeChange', label: 'Income change', allowNegative: true }, { key: 'monthlyExpenseChange', label: 'Expense change', allowNegative: true },
  { key: 'newMonthlyDebtPayment', label: 'New EMI' }, { key: 'horizonMonths', label: 'Horizon months' }
]
const comparison = computed(() => result.value ? [
  { label: 'Net Worth', before: formatMoney(result.value.currentNetWorth), after: formatMoney(result.value.scenarioNetWorth) },
  { label: 'Monthly Surplus', before: formatMoney(result.value.currentMonthlySurplus), after: formatMoney(result.value.scenarioMonthlySurplus) },
  { label: 'Savings Rate', before: formatPercentage(result.value.currentSavingsRatePct), after: formatPercentage(result.value.scenarioSavingsRatePct) },
  { label: 'DTI', before: formatPercentage(result.value.currentDtiPct), after: formatPercentage(result.value.scenarioDtiPct) },
  { label: 'Emergency Fund', before: `${result.value.currentEmergencyFundMonths} months`, after: `${result.value.scenarioEmergencyFundMonths} months` }
] : [])
const tone = computed(() => result.value?.verdict === 'Comfortable' ? 'positive' : result.value?.verdict === 'High Risk' ? 'negative' : 'warning')
const run = () => store.calculateScenario({ ...form })
const save = () => {
  const name = window.prompt('Scenario name', form.scenarioType)
  if (name?.trim()) store.saveScenario(name.trim(), { ...form })
}
const loadSaved = item => {
  Object.assign(form, item.input)
  store.scenarioResult = item.result
}
onMounted(() => store.fetchSavedScenarios())
</script>
