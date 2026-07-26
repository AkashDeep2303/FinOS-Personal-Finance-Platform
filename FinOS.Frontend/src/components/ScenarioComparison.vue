<template>
  <div class="overflow-hidden rounded-xl border border-gray-200 bg-white">
    <div class="grid grid-cols-3 bg-gray-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
      <span>Metric</span><span class="text-right">Before</span><span class="text-right">After</span>
    </div>
    <div v-for="row in rows" :key="row.label" class="grid grid-cols-3 border-t border-gray-100 px-4 py-3 text-sm">
      <span class="font-medium text-gray-800">{{ row.label }}</span><span class="text-right text-gray-600">{{ row.before }}</span>
      <span class="text-right font-semibold" :class="row.status === 'negative' ? 'text-red-700' : row.status === 'positive' ? 'text-green-700' : 'text-gray-900'">{{ row.after }}</span>
    </div>
  </div>
</template>
<script setup>defineProps({ rows: { type: Array, required: true } })</script>
