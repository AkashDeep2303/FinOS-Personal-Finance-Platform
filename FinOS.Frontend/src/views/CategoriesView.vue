<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold text-gray-900">Categories</h1><p class="mt-1 text-sm text-gray-500">Classify expenses so FinOS can explain monthly money flow accurately.</p></header>
    <form class="grid gap-3 rounded-xl border border-gray-200 bg-white p-5 md:grid-cols-3" @submit.prevent="createCategory">
      <input v-model.trim="form.name" required maxlength="100" placeholder="Category name" class="rounded-lg border border-gray-300 px-3 py-2">
      <select v-model="form.type" class="rounded-lg border border-gray-300 px-3 py-2"><option value="Expense">Expense</option><option value="Income">Income</option><option value="Transfer">Transfer</option></select>
      <select v-model="form.cashFlowClassification" :disabled="form.type !== 'Expense'" class="rounded-lg border border-gray-300 px-3 py-2"><option v-for="option in classifications" :key="option" :value="option">{{ option }}</option></select>
      <button :disabled="saving" class="rounded-lg bg-indigo-600 px-4 py-2 font-medium text-white disabled:opacity-50 md:col-span-3">{{ saving ? 'Creating...' : 'Create custom category' }}</button>
      <p v-if="actionError" class="text-sm text-red-600 md:col-span-3">{{ actionError }}</p>
    </form>
    <LoadingState v-if="store.loading && !store.categories.length" message="Loading categories..." />
    <ErrorState v-else-if="store.error" :message="store.error" @retry="store.load" />
    <EmptyState v-else-if="!store.categories.length" title="No categories available" description="Create a custom category to organize transactions." />
    <div v-else class="overflow-hidden rounded-xl border border-gray-200 bg-white">
      <div class="grid grid-cols-[minmax(0,1fr)_7rem_10rem] gap-3 border-b bg-gray-50 px-4 py-3 text-xs font-semibold uppercase text-gray-500"><span>Category</span><span>Type</span><span>Money-flow group</span></div>
      <div v-for="category in expenseFirst" :key="category.id" class="grid grid-cols-[minmax(0,1fr)_7rem_10rem] items-center gap-3 border-b px-4 py-3 last:border-b-0">
        <div><p class="font-medium text-gray-900">{{ category.name }}</p><p class="text-xs text-gray-500">{{ category.isSystem ? 'FinOS system category' : 'Custom category' }}</p></div>
        <span class="text-sm text-gray-600">{{ category.type }}</span>
        <span v-if="category.isSystem || category.type !== 'Expense'" class="text-sm text-gray-600">{{ category.cashFlowClassification }}</span>
        <select v-else v-model="category.cashFlowClassification" :disabled="savingId === category.id" class="rounded-lg border border-gray-300 px-2 py-1.5 text-sm" @change="saveClassification(category)"><option v-for="option in classifications" :key="option" :value="option">{{ option }}</option></select>
      </div>
    </div>
  </div>
</template>
<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useCategoriesStore } from '../stores/categories'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import LoadingState from '../components/LoadingState.vue'
const store = useCategoriesStore()
const classifications = ['Essential', 'Lifestyle', 'EMI', 'Investment', 'Other']
const form = reactive({ name: '', type: 'Expense', cashFlowClassification: 'Other' })
const saving = ref(false)
const savingId = ref(null)
const actionError = ref(null)
const expenseFirst = computed(() => [...store.categories].sort((a, b) => (a.type === 'Expense' ? 0 : 1) - (b.type === 'Expense' ? 0 : 1) || a.name.localeCompare(b.name)))
watch(() => form.type, type => { if (type !== 'Expense') form.cashFlowClassification = 'Other' })
async function createCategory() {
  saving.value = true; actionError.value = null
  try {
    await store.create({ ...form, parentId: null, icon: null, color: null, budgetAmount: 0, sortOrder: 0 })
    Object.assign(form, { name: '', type: 'Expense', cashFlowClassification: 'Other' })
  } catch (error) { actionError.value = error.response?.data?.message || 'Could not create the category.' } finally { saving.value = false }
}
async function saveClassification(category) {
  savingId.value = category.id; actionError.value = null
  try { await store.update(category) } catch (error) {
    actionError.value = error.response?.data?.message || 'Could not update the classification.'
    await store.load()
  } finally { savingId.value = null }
}
onMounted(store.load)
</script>
