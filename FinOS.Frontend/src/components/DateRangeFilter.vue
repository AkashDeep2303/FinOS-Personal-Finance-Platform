<template>
  <label class="inline-flex items-center gap-2 text-sm text-gray-600">
    <span class="sr-only">{{ label }}</span>
    <select :value="modelValue" class="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-500" @change="$emit('update:modelValue', $event.target.value)">
      <option v-for="option in options" :key="option.value" :value="option.value">{{ option.label }}</option>
    </select>
  </label>
</template>
<script setup>
defineProps({
  modelValue: { type: String, required: true },
  label: { type: String, default: 'Date range' },
  options: {
    type: Array,
    default: () => [
      { value: '1m', label: '1 Month' }, { value: '3m', label: '3 Months' },
      { value: '6m', label: '6 Months' }, { value: '1y', label: '1 Year' },
      { value: 'fy', label: 'Financial Year' }, { value: 'all', label: 'All Time' }
    ]
  }
})
defineEmits(['update:modelValue'])
</script>
