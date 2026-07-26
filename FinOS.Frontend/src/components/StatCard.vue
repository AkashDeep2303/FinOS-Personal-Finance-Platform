<template>
  <div class="rounded-xl border border-gray-200 bg-white p-6 shadow-sm transition-shadow hover:shadow-md">
    <div class="flex items-center justify-between">
      <div>
        <p class="mb-1 text-sm text-gray-500">{{ title }}</p>
        <p class="text-2xl font-bold text-gray-900">{{ value }}</p>
      </div>
      <div class="flex h-12 w-12 items-center justify-center rounded-full text-2xl" :class="iconBgClass">{{ icon }}</div>
    </div>
    <div v-if="trendValue" class="mt-3 flex items-center">
      <span class="inline-flex items-center text-sm font-medium" :class="trendClass">
        <span class="mr-1">{{ trendIcon }}</span>{{ trendValue }}
      </span>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  title: { type: String, required: true },
  value: { type: [String, Number], required: true },
  icon: { type: String, default: '◩' },
  trend: { type: String, default: 'flat', validator: value => ['up', 'down', 'flat'].includes(value) },
  trendValue: { type: String, default: '' },
  color: { type: String, default: 'primary' }
})

const trendIcon = computed(() => props.trend === 'up' ? '↑' : props.trend === 'down' ? '↓' : '→')
const trendClass = computed(() => props.trend === 'up' ? 'text-green-600' : props.trend === 'down' ? 'text-red-600' : 'text-gray-600')
const iconBgClass = computed(() => ({
  primary: 'bg-primary-100', green: 'bg-green-100', red: 'bg-red-100',
  blue: 'bg-blue-100', amber: 'bg-amber-100', purple: 'bg-purple-100'
}[props.color] || 'bg-gray-100'))
</script>
