<template><span>{{ formatted }}</span></template>
<script setup>
import { computed } from 'vue'
import { formatPercentage } from '../utils/formatters'
const props = defineProps({ value: { type: [Number, String], default: null }, fraction: Boolean, fallback: { type: String, default: '—' } })
const formatted = computed(() => formatPercentage(props.value, { fraction: props.fraction, fallback: props.fallback }))
</script>
