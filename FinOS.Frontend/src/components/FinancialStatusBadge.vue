<template><span class="finos-badge" :class="classes">{{ label }}</span></template>
<script setup>
import { computed } from 'vue'
const props = defineProps({ status: { type: String, default: 'neutral' }, label: { type: String, required: true } })
const classes = computed(() => ({
  positive: 'bg-green-100 text-green-800',
  warning: 'bg-amber-100 text-amber-800',
  negative: 'bg-red-100 text-red-800',
  info: 'bg-blue-100 text-blue-800',
  neutral: 'bg-gray-100 text-gray-700'
}[props.status] || 'bg-gray-100 text-gray-700'))
</script>
