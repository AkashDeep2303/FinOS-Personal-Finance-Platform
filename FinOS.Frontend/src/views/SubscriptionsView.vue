<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between"><header><h1 class="text-2xl font-bold text-gray-900">Bills & Subscriptions</h1><p class="mt-1 text-sm text-gray-500">Recurring costs detected from your transaction history.</p></header><button class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white" @click="store.detect">Detect recurring costs</button></div>
    <LoadingState v-if="store.loading && !store.items.length" message="Loading subscriptions..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="store.load" />
    <EmptyState v-else-if="!store.items.length" title="No subscriptions detected" description="Add recurring transactions, then run detection to identify likely bills and subscriptions." />
    <template v-else>
      <div class="grid gap-4 sm:grid-cols-2"><StatCard title="Monthly recurring cost" :value="formatMoney(store.monthlyCost)" /><StatCard title="Annual recurring cost" :value="formatMoney(store.annualCost)" /></div>
      <div class="overflow-hidden rounded-xl border border-gray-200 bg-white">
        <table class="w-full"><thead class="bg-gray-50 text-left text-xs uppercase text-gray-500"><tr><th class="p-4">Merchant</th><th class="p-4">Amount</th><th class="p-4">Frequency</th><th class="p-4">Next expected</th><th class="p-4">Status</th></tr></thead>
          <tbody class="divide-y divide-gray-100"><tr v-for="item in store.items" :key="item.id"><td class="p-4 font-medium">{{ item.merchantName }}</td><td class="p-4">{{ formatMoney(item.amount) }}</td><td class="p-4">{{ item.frequency }}</td><td class="p-4">{{ formatIndianDate(item.nextExpectedDate) }}</td><td class="p-4"><FinancialStatusBadge v-if="item.isConfirmed" status="positive" label="Confirmed" /><button v-else class="text-sm font-medium text-indigo-600" @click="store.confirm(item)">Confirm</button></td></tr></tbody>
        </table>
      </div>
    </template>
  </div>
</template>
<script setup>
import { onMounted } from 'vue'
import { useSubscriptionsStore } from '../stores/subscriptions'
import { formatMoney, formatIndianDate } from '../utils/formatters'
import StatCard from '../components/StatCard.vue'
import LoadingState from '../components/LoadingState.vue'
import ErrorState from '../components/ErrorState.vue'
import EmptyState from '../components/EmptyState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
const store = useSubscriptionsStore()
onMounted(store.load)
</script>
