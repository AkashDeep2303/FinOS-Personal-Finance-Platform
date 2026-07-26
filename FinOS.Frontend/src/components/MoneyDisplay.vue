<template><span>{{ formatted }}</span></template>
<script setup>
import { computed } from 'vue'
import { formatMoney } from '../utils/formatters'
const props = defineProps({ value: { type: [Number, String], default: null }, compact: Boolean, fallback: { type: String, default: '—' } })
const formatted = computed(() => formatMoney(props.value, { compact: props.compact, fallback: props.fallback }))
</script>
