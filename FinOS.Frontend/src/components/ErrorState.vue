<template>
  <div class="rounded-xl border border-red-200 bg-red-50 p-6" role="alert">
    <h3 class="font-semibold text-red-900">{{ title }}</h3>
    <p class="mt-1 text-sm text-red-700">{{ message }}</p>
    <button v-if="retryable" class="finos-btn-secondary mt-4 bg-white" type="button" @click="$emit('retry')">Try again</button>
  </div>
</template>
<script setup>
defineProps({ title: { type: String, default: 'We could not load this data' }, message: { type: String, default: 'Please try again. Your financial records have not been changed.' }, retryable: { type: Boolean, default: true } })
defineEmits(['retry'])
</script>
