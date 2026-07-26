<template>
  <section class="rounded-xl border border-gray-200 bg-white p-6 shadow-sm">
    <div class="mb-4 flex items-start justify-between gap-4">
      <div><h2 class="text-lg font-semibold text-gray-900">{{ title }}</h2><p v-if="subtitle" class="mt-1 text-sm text-gray-500">{{ subtitle }}</p></div>
      <slot name="actions" />
    </div>
    <slot />
  </section>
</template>
<script setup>defineProps({ title: { type: String, required: true }, subtitle: { type: String, default: '' } })</script>
