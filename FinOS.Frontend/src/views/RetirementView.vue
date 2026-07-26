<template>
  <div class="space-y-6">
    <div>
      <p class="text-sm font-medium text-primary-700">Plan</p>
      <h1 class="text-2xl font-bold text-gray-900">Retirement Planner</h1>
      <p class="mt-1 text-sm text-gray-500">Model your retirement corpus using explicit inflation and return assumptions.</p>
    </div>

    <ErrorState v-if="store.error" :message="store.error" :retryable="false" />

    <div class="grid gap-6 xl:grid-cols-5">
      <form class="rounded-xl border border-gray-200 bg-white p-6 shadow-sm xl:col-span-2" @submit.prevent="calculate">
        <h2 class="text-lg font-semibold">Projection inputs</h2>
        <div class="mt-4 grid grid-cols-2 gap-4">
          <InputField v-model.number="form.currentAge" label="Current age" type="number" min="18" max="79" />
          <InputField v-model.number="form.retirementAge" label="Retirement age" type="number" min="19" max="80" />
          <InputField v-model.number="form.lifeExpectancy" label="Life expectancy" type="number" min="20" max="120" />
          <InputField v-model.number="form.currentRetirementCorpus" label="Current corpus" type="number" min="0" />
          <InputField v-model.number="form.monthlyRetirementContribution" label="Monthly contribution" type="number" min="0" />
          <InputField v-model.number="form.currentMonthlyExpense" label="Current monthly expense" type="number" min="0" />
          <InputField v-model.number="form.desiredRetirementExpense" label="Desired expense today" type="number" min="0" />
          <InputField v-model.number="form.annualInflationRate" label="Inflation %" type="number" min="0" max="25" step="0.1" />
          <InputField v-model.number="form.annualPreRetirementReturn" label="Pre-retirement return %" type="number" min="0" max="50" step="0.1" />
          <InputField v-model.number="form.annualPostRetirementReturn" label="Post-retirement return %" type="number" min="0" max="30" step="0.1" />
        </div>
        <button class="finos-btn-primary mt-5 w-full justify-center" :disabled="store.loading">
          {{ store.loading ? 'Calculating…' : 'Update projection' }}
        </button>
      </form>

      <div class="space-y-6 xl:col-span-3">
        <template v-if="result">
          <div class="grid grid-cols-2 gap-4">
            <MetricCard title="Target Corpus" :value="formatMoney(result.targetRetirementCorpus, { compact: true })" icon="◎" />
            <MetricCard title="Projected Corpus" :value="formatMoney(result.projectedRetirementCorpus, { compact: true })" icon="↗" color="green" />
            <MetricCard title="Retirement Gap" :value="formatMoney(result.retirementGap, { compact: true })" icon="△" :color="result.retirementGap ? 'red' : 'green'" />
            <MetricCard title="Readiness" :value="`${result.retirementReadinessScore} / 100`" icon="◷" color="purple" />
          </div>

          <ChartCard title="Projection summary">
            <div class="grid gap-4 text-sm sm:grid-cols-2">
              <Summary label="Years to retirement" :value="`${result.yearsToRetirement} years`" />
              <Summary label="Retirement duration" :value="`${result.retirementYears} years`" />
              <Summary label="First retirement-month expense" :value="formatMoney(result.firstMonthRetirementExpense)" />
              <Summary label="Required monthly contribution" :value="formatMoney(result.requiredMonthlyContribution)" />
            </div>
            <FinancialStatusBadge class="mt-5" :status="result.retirementReadinessScore >= 75 ? 'positive' : 'warning'" :label="result.status" />
          </ChartCard>

          <ChartCard title="Assumptions">
            <ul class="space-y-2 text-sm text-gray-600"><li v-for="item in result.assumptions" :key="item">• {{ item }}</li></ul>
          </ChartCard>
        </template>
        <EmptyState v-else title="Run your retirement projection" message="Enter your present corpus, contributions, expected retirement spending, inflation, and return assumptions." icon="◷" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineComponent, h, reactive } from 'vue'
import { useAnalyticsStore } from '../stores/analytics'
import { formatMoney } from '../utils/formatters'
import ChartCard from '../components/ChartCard.vue'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
import MetricCard from '../components/StatCard.vue'

const store = useAnalyticsStore()
const result = computed(() => store.retirementProjection)
const form = reactive({
  currentAge: 30, retirementAge: 60, lifeExpectancy: 85,
  currentRetirementCorpus: 500000, monthlyRetirementContribution: 15000,
  currentMonthlyExpense: 60000, desiredRetirementExpense: 60000,
  annualInflationRate: 6, annualPreRetirementReturn: 10, annualPostRetirementReturn: 7
})

const InputField = defineComponent({
  inheritAttrs: false,
  props: { modelValue: Number, label: String },
  emits: ['update:modelValue'],
  setup: (props, { attrs, emit }) => () => h('label', { class: 'text-sm text-gray-700' }, [
    h('span', { class: 'mb-1 block font-medium' }, props.label),
    h('input', { ...attrs, value: props.modelValue, class: 'finos-input', onInput: e => emit('update:modelValue', Number(e.target.value)) })
  ])
})
const Summary = defineComponent({
  props: { label: String, value: String },
  setup: props => () => h('div', { class: 'rounded-lg bg-gray-50 p-4' }, [
    h('p', { class: 'text-gray-500' }, props.label), h('p', { class: 'mt-1 font-semibold text-gray-900' }, props.value)
  ])
})

function calculate() {
  return store.projectRetirement({
    ...form,
    annualInflationRate: form.annualInflationRate / 100,
    annualPreRetirementReturn: form.annualPreRetirementReturn / 100,
    annualPostRetirementReturn: form.annualPostRetirementReturn / 100
  })
}
</script>
