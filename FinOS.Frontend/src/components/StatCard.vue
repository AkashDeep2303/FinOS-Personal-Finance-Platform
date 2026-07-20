<template>
  <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
    <div class="flex items-center justify-between">
      <div>
        <p class="text-sm text-gray-500 mb-1">{{ title }}</p>
        <p class="text-2xl font-bold text-gray-900">{{ value }}</p>
      </div>
      <div class="flex items-center space-x-3">
        <div class="w-12 h-12 rounded-full flex items-center justify-center text-2xl"
          :class="iconBgClass">
          {{ icon }}
        </div>
      </div>
    </div>
    <div v-if="trendValue" class="mt-3 flex items-center">
      <span class="inline-flex items-center text-sm font-medium"
        :class="trend === 'up' ? 'text-green-600' : 'text-red-600'">
        <span class="mr-1">{{ trend === 'up' ? '↑' : '↓' }}</span>
        {{ trendValue }}
      </span>
      <span class="text-xs text-gray-400 ml-2">vs last month</span>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  title: {
    type: String,
    required: true
  },
  value: {
    type: [String, Number],
    required: true
  },
  icon: {
    type: String,
    default: '📊'
  },
  trend: {
    type: String,
    default: 'up',
    validator: (value) => ['up', 'down'].includes(value)
  },
  trendValue: {
    type: String,
    default: ''
  },
  color: {
    type: String,
    default: 'primary'
  }
})

const iconBgClass = computed(() => {
  const classes = {
    primary: 'bg-primary-100',
    green: 'bg-green-100',
    red: 'bg-red-100',
    blue: 'bg-blue-100',
    amber: 'bg-amber-100',
    purple: 'bg-purple-100'
  }
  return classes[props.color] || 'bg-gray-100'
})
</script>
