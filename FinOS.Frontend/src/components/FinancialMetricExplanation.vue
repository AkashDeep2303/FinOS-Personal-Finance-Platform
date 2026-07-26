<template>
  <details class="rounded-xl border border-gray-200 bg-white">
    <summary class="cursor-pointer list-none px-5 py-4 font-medium text-gray-900">{{ heading }}</summary>
    <div class="grid gap-4 border-t border-gray-100 px-5 py-4 text-sm md:grid-cols-2">
      <div><p class="font-medium text-gray-900">What is this?</p><p class="mt-1 text-gray-600">{{ description }}</p></div>
      <div v-if="value"><p class="font-medium text-gray-900">Your value</p><p class="mt-1 text-lg font-semibold text-gray-900">{{ value }}</p></div>
      <div><p class="font-medium text-gray-900">How FinOS calculated it</p><p class="mt-1 text-gray-600">{{ calculation }}</p></div>
      <div v-if="whyItMatters"><p class="font-medium text-gray-900">Why it matters</p><p class="mt-1 text-gray-600">{{ whyItMatters }}</p></div>
      <div v-if="trend"><p class="font-medium text-gray-900">Historical trend</p><p class="mt-1 text-gray-600">{{ trend }}</p></div>
      <div v-if="changed"><p class="font-medium text-gray-900">What changed?</p><p class="mt-1 text-gray-600">{{ changed }}</p></div>
      <div v-if="improvement"><p class="font-medium text-gray-900">How to improve it</p><p class="mt-1 text-gray-600">{{ improvement }}</p></div>
      <router-link v-if="askLink" :to="askLink" class="font-medium text-primary-700">Ask FinOS about this &rarr;</router-link>
    </div>
  </details>
</template>
<script setup>
import { computed } from 'vue'
const props = defineProps({
  metric: { type: String, default: '' }, title: { type: String, default: '' },
  value: { type: String, default: '' }, trend: { type: String, default: '' }, changed: { type: String, default: '' },
  description: { type: String, required: true }, calculation: { type: String, required: true },
  whyItMatters: { type: String, default: '' }, improvement: { type: String, default: '' },
  askLink: { type: String, default: '/ai-assistant' }
})
const heading = computed(() => props.title || `Understand ${props.metric}`)
</script>
