<template>
  <article class="rounded-xl border bg-white p-5 shadow-sm" :class="borderClass">
    <div class="flex items-start justify-between gap-3">
      <div><p class="text-xs font-semibold uppercase tracking-wide text-gray-500">{{ area }}</p><h3 class="mt-1 font-semibold text-gray-900">{{ title }}</h3></div>
      <FinancialStatusBadge :status="status" :label="label" />
    </div>
    <p class="mt-3 text-sm text-gray-600">{{ explanation }}</p>
    <p v-if="calculation" class="mt-2 rounded-lg bg-gray-50 px-3 py-2 text-xs text-gray-600">{{ calculation }}</p>
    <router-link v-if="to" :to="to" class="mt-4 inline-flex text-sm font-medium text-primary-700 hover:text-primary-800">{{ action }} →</router-link>
  </article>
</template>
<script setup>
import { computed } from 'vue'
import FinancialStatusBadge from './FinancialStatusBadge.vue'
const props = defineProps({
  title: { type: String, required: true }, explanation: { type: String, required: true },
  area: { type: String, default: 'Financial intelligence' }, calculation: { type: String, default: '' },
  status: { type: String, default: 'info' }, label: { type: String, default: 'Insight' },
  to: { type: String, default: '' }, action: { type: String, default: 'Review' }
})
const borderClass = computed(() => ({ warning: 'border-amber-200', negative: 'border-red-200', positive: 'border-green-200', info: 'border-blue-200' }[props.status] || 'border-gray-200'))
</script>
