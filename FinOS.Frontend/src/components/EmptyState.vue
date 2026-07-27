<template>
  <div class="rounded-xl border border-dashed border-gray-300 bg-white p-8 text-center">
    <div class="text-2xl" aria-hidden="true">{{ icon }}</div>
    <h3 class="mt-3 text-base font-semibold text-gray-900">{{ title }}</h3>
    <p class="mx-auto mt-1 max-w-md text-sm text-gray-500">{{ description || message }}</p>
    <slot name="action" />
  </div>
</template>
<script setup>
defineProps({
  title: { type: String, required: true },
  message: { type: String, default: '' },
  description: { type: String, default: '' },
  icon: { type: String, default: '○' }
})
</script>
