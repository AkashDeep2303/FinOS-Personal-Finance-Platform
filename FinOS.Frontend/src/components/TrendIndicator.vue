<template>
  <span class="inline-flex items-center gap-1 text-sm font-medium" :class="classes">
    <span aria-hidden="true">{{ direction === 'up' ? '↑' : direction === 'down' ? '↓' : '→' }}</span>
    <span>{{ value }}</span>
  </span>
</template>
<script setup>
import { computed } from 'vue'
const props = defineProps({ direction: { type: String, default: 'flat' }, value: { type: String, required: true }, inverse: Boolean })
const classes = computed(() => {
  if (props.direction === 'flat') return 'text-gray-600'
  const positive = props.inverse ? props.direction === 'down' : props.direction === 'up'
  return positive ? 'text-green-700' : 'text-red-700'
})
</script>
