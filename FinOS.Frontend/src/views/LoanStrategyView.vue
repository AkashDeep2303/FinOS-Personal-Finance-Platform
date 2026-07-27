<template>
  <div class="space-y-6">
    <div>
      <p class="text-sm font-medium text-primary-700">Debt</p>
      <h1 class="text-2xl font-bold text-gray-900">Loan Strategy Lab</h1>
      <p class="mt-1 text-sm text-gray-500">Compare prepayment, investing, and a split strategy without changing your loan.</p>
    </div>

    <ErrorState v-if="store.error" :message="store.error" :retryable="false" />

    <form class="grid gap-4 rounded-xl border border-gray-200 bg-white p-6 shadow-sm md:grid-cols-5" @submit.prevent="compare">
      <label class="text-sm font-medium text-gray-700">Loan
        <select v-model.number="form.loanId" required class="finos-input mt-1">
          <option :value="0" disabled>Select a loan</option>
          <option v-for="loan in store.activeLoans" :key="loan.id" :value="loan.id">{{ loan.name }} — {{ formatMoney(loan.outstandingAmount) }}</option>
        </select>
      </label>
      <InputField v-model.number="form.surplusAmount" label="Surplus amount" min="1" type="number" />
      <InputField v-model.number="form.splitPrepaymentAmount" label="Split: prepayment" min="0" :max="form.surplusAmount" type="number" />
      <InputField v-model.number="form.expectedReturnPct" label="Assumed return %" min="0" max="50" step="0.1" type="number" />
      <InputField v-model.number="form.horizonYears" label="Horizon (years)" min="1" max="50" type="number" />
      <button class="finos-btn-primary justify-center md:col-span-5" :disabled="store.loading">{{ store.loading ? 'Comparing…' : 'Compare strategies' }}</button>
    </form>

    <template v-if="result">
      <div class="grid gap-5 lg:grid-cols-3">
        <article v-for="option in result.options" :key="option.strategy" class="rounded-xl border border-gray-200 bg-white p-6 shadow-sm">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold">{{ option.strategy }}</h2>
            <FinancialStatusBadge :status="option.riskIndicator === 'High' ? 'warning' : 'info'" :label="`${option.riskIndicator} risk`" />
          </div>
          <dl class="mt-5 space-y-3 text-sm">
            <Row label="Prepayment" :value="formatMoney(option.prepaymentAmount)" />
            <Row label="Investment" :value="formatMoney(option.investmentAmount)" />
            <Row label="Interest saved" :value="formatMoney(option.interestSaved)" />
            <Row label="Tenure reduction" :value="`${option.tenureReductionMonths} months`" />
            <Row label="Investment future value" :value="formatMoney(option.investmentFutureValue)" />
            <Row label="Estimated investment gain" :value="formatMoney(option.estimatedInvestmentGain)" />
            <Row label="Liquidity retained" :value="formatMoney(option.liquidityRemaining)" />
          </dl>
          <div class="mt-5 rounded-lg bg-primary-50 p-4">
            <p class="text-xs font-medium uppercase tracking-wide text-primary-700">Projected net benefit</p>
            <p class="mt-1 text-xl font-bold text-primary-900">{{ formatMoney(option.projectedNetBenefit) }}</p>
          </div>
        </article>
      </div>
      <p class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">{{ result.disclaimer }}</p>
    </template>
    <EmptyState v-else title="Compare your surplus options" message="Select an active loan and enter a surplus amount. No real financial record will be modified." icon="⇄" />
  </div>
</template>

<script setup>
import { computed, defineComponent, h, onMounted, reactive } from 'vue'
import { useLoansStore } from '../stores/loans'
import { formatMoney } from '../utils/formatters'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'

const store = useLoansStore()
const result = computed(() => store.strategyComparison)
const form = reactive({ loanId: 0, surplusAmount: 100000, splitPrepaymentAmount: 50000, expectedReturnPct: 10, horizonYears: 5 })
const InputField = defineComponent({
  inheritAttrs: false, props: { modelValue: Number, label: String }, emits: ['update:modelValue'],
  setup: (props, { attrs, emit }) => () => h('label', { class: 'text-sm font-medium text-gray-700' }, [
    props.label, h('input', { ...attrs, value: props.modelValue, class: 'finos-input mt-1', onInput: e => emit('update:modelValue', Number(e.target.value)) })
  ])
})
const Row = defineComponent({
  props: { label: String, value: String },
  setup: props => () => h('div', { class: 'flex justify-between gap-3' }, [h('dt', { class: 'text-gray-500' }, props.label), h('dd', { class: 'font-medium text-gray-900' }, props.value)])
})
function compare() {
  return store.compareStrategy({
    loanId: form.loanId, surplusAmount: form.surplusAmount,
    splitPrepaymentAmount: form.splitPrepaymentAmount,
    expectedAnnualInvestmentReturn: form.expectedReturnPct / 100,
    investmentHorizonMonths: form.horizonYears * 12
  })
}
onMounted(async () => {
  await store.fetchLoans()
  if (store.activeLoans.length) form.loanId = store.activeLoans[0].id
})
</script>
