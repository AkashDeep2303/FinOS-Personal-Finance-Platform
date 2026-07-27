<template>
  <div class="flex items-center justify-center gap-3 rounded-xl border border-gray-200 bg-white p-8 text-sm text-gray-600" role="status">
    <span class="h-5 w-5 animate-spin rounded-full border-2 border-primary-200 border-t-primary-600" aria-hidden="true"></span>
    <span>{{ message }}</span>
  </div>
</template>
<script setup>defineProps({ message: { type: String, default: 'Loading your financial data…' } })</script>
