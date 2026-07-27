<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">Financial Calculators</h1><p class="mt-1 text-sm text-gray-500">Deterministic tools for loans, investments, goals, and inflation.</p></header>
    <div class="grid gap-6 lg:grid-cols-[1fr_.8fr]">
      <form class="space-y-4 rounded-xl border border-gray-200 bg-white p-6" @submit.prevent="calculate">
        <label class="block text-sm font-medium">Calculator<select v-model="form.calculator" class="mt-1 w-full rounded-lg border-gray-300"><option value="emi">EMI</option><option value="creditcard">Credit Card Payoff</option><option value="refinance">Loan Refinance</option><option value="sip">SIP</option><option value="lumpsum">Lumpsum</option><option value="goal">Goal SIP</option><option value="inflation">Inflation</option><option value="fd">Fixed Deposit</option><option value="rd">Recurring Deposit</option><option value="cagr">CAGR</option><option value="emergencyfund">Emergency Fund</option><option value="xirr">XIRR</option></select></label>
        <div v-if="form.calculator !== 'xirr'" class="grid gap-4 sm:grid-cols-2">
          <label class="text-sm">{{ principalLabel }}<input v-model.number="form.principal" type="number" min="0" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label class="text-sm">{{ monthlyLabel }}<input v-model.number="form.monthlyAmount" type="number" min="0" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator !== 'emergencyfund'" class="text-sm">{{ form.calculator === 'refinance' ? 'Current annual rate (%)' : 'Annual rate (%)' }}<input v-model.number="form.annualRate" type="number" min="0" step=".1" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator !== 'creditcard'" class="text-sm">{{ form.calculator === 'emergencyfund' ? 'Target coverage (months)' : 'Duration (months)' }}<input v-model.number="form.months" type="number" min="1" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator === 'goal'" class="text-sm">Target amount<input v-model.number="form.targetAmount" type="number" min="0" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator === 'goal'" class="text-sm">Current amount<input v-model.number="form.currentAmount" type="number" min="0" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator === 'cagr'" class="text-sm">Ending value<input v-model.number="form.endingAmount" type="number" min="0" class="mt-1 w-full rounded-lg border-gray-300"></label>
          <label v-if="form.calculator === 'refinance'" class="text-sm">New annual rate (%)<input v-model.number="form.endingAmount" type="number" min="0" max="100" step=".1" class="mt-1 w-full rounded-lg border-gray-300"></label>
        </div>
        <div v-else class="space-y-3">
          <div class="flex items-center justify-between"><p class="text-sm font-medium">Dated cash flows</p><button type="button" class="text-sm font-medium text-indigo-700" @click="addCashFlow">Add cash flow</button></div>
          <p class="text-xs text-gray-500">Enter investments as negative amounts and redemptions or current value as positive amounts.</p>
          <div v-for="(flow, index) in cashFlows" :key="index" class="grid grid-cols-[1fr_1fr_auto] gap-2">
            <input v-model="flow.date" required type="date" class="rounded-lg border-gray-300 text-sm">
            <input v-model.number="flow.amount" required type="number" step=".01" class="rounded-lg border-gray-300 text-sm" placeholder="Amount">
            <button type="button" :disabled="cashFlows.length <= 2" class="px-2 text-gray-400 hover:text-red-600 disabled:opacity-30" aria-label="Remove cash flow" @click="cashFlows.splice(index, 1)">&times;</button>
          </div>
        </div>
        <p v-if="store.error" class="rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ store.error }}</p>
        <button :disabled="store.loading" class="rounded-lg bg-indigo-600 px-5 py-2.5 font-medium text-white disabled:opacity-50">{{ store.loading ? 'Calculating...' : 'Calculate' }}</button>
      </form>
      <section class="rounded-xl border border-gray-200 bg-white p-6">
        <h2 class="font-semibold text-gray-900">Result</h2>
        <EmptyState v-if="!result" title="Choose assumptions" description="Results and the formula used will appear here." />
        <div v-else class="mt-5 space-y-4">
          <div><p class="text-sm text-gray-500">{{ primaryLabel }}</p><p class="text-3xl font-bold">{{ formatPrimary(result) }}</p></div>
          <div><p class="text-sm text-gray-500">{{ secondaryLabel }}</p><p class="text-xl font-semibold">{{ formatMoney(result.secondaryResult) }}</p></div>
          <p class="rounded-lg bg-indigo-50 p-3 text-sm text-indigo-900">{{ result.formula }}</p>
          <p class="text-xs text-gray-500">The annual rate is an assumption, not a promised return.</p>
        </div>
      </section>
    </div>
  </div>
</template>
<script setup>
import { computed, reactive } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import { formatMoney } from '../utils/formatters'
import EmptyState from '../components/EmptyState.vue'
const store = useAnalyticsStore()
const result = computed(() => store.calculatorResult)
const form = reactive({ calculator: 'emi', principal: 1000000, monthlyAmount: 10000, annualRate: 8.5, months: 120, targetAmount: 2500000, currentAmount: 200000, endingAmount: 1500000 })
const today = new Date()
const previousYear = new Date(today)
previousYear.setFullYear(today.getFullYear() - 1)
const toDateInput = date => date.toISOString().slice(0, 10)
const cashFlows = reactive([
  { date: toDateInput(previousYear), amount: -100000 },
  { date: toDateInput(today), amount: 110000 }
])
const addCashFlow = () => cashFlows.push({ date: toDateInput(today), amount: 0 })
const principalLabel = computed(() => form.calculator === 'emergencyfund' ? 'Current emergency fund' : form.calculator === 'creditcard' ? 'Outstanding balance' : form.calculator === 'refinance' ? 'Outstanding principal' : 'Principal / present amount')
const monthlyLabel = computed(() => form.calculator === 'emergencyfund' ? 'Essential monthly expenses' : form.calculator === 'creditcard' ? 'Monthly payment' : form.calculator === 'refinance' ? 'Refinance fees' : 'Monthly amount')
const primaryLabel = computed(() => result.value?.resultUnit === 'PERCENT' ? 'Annualized return' : result.value?.resultUnit === 'MONTHS' ? 'Payoff duration' : form.calculator === 'emergencyfund' ? 'Target emergency fund' : form.calculator === 'refinance' ? 'New EMI' : 'Primary result')
const secondaryLabel = computed(() => form.calculator === 'emergencyfund' ? 'Remaining gap' : form.calculator === 'creditcard' ? 'Estimated interest' : form.calculator === 'refinance' ? 'Estimated net savings after fees' : 'Gain / interest / remaining gap')
const formatPrimary = value => value.resultUnit === 'PERCENT' ? `${Number(value.primaryResult).toFixed(2)}%` : value.resultUnit === 'MONTHS' ? `${value.primaryResult} months` : formatMoney(value.primaryResult)
const calculate = () => form.calculator === 'xirr'
  ? store.calculateXirr({ cashFlows: cashFlows.map(flow => ({ date: flow.date, amount: flow.amount })) })
  : store.calculateFinancialTool({ ...form })
</script>
