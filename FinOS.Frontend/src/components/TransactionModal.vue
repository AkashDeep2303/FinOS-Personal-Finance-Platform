<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
    <div class="bg-white rounded-2xl shadow-2xl w-full max-w-lg">
      <div class="p-6 border-b border-gray-100">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-gray-900">{{ transaction ? 'Edit Transaction' : 'Add Transaction' }}</h2>
          <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
        </div>
      </div>
      <form @submit.prevent="handleSubmit" class="p-6 space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Transaction Type</label>
          <div class="grid grid-cols-3 gap-2">
            <button type="button" @click="form.type = 'Income'" class="py-2 px-3 rounded-lg text-sm font-medium transition-colors" :class="form.type === 'Income' ? 'bg-green-100 text-green-800 border-2 border-green-500' : 'bg-gray-50 text-gray-600 border border-gray-200 hover:bg-gray-100'">Income</button>
            <button type="button" @click="form.type = 'Expense'" class="py-2 px-3 rounded-lg text-sm font-medium transition-colors" :class="form.type === 'Expense' ? 'bg-red-100 text-red-800 border-2 border-red-500' : 'bg-gray-50 text-gray-600 border border-gray-200 hover:bg-gray-100'">Expense</button>
            <button type="button" @click="form.type = 'Transfer'" class="py-2 px-3 rounded-lg text-sm font-medium transition-colors" :class="form.type === 'Transfer' ? 'bg-blue-100 text-blue-800 border-2 border-blue-500' : 'bg-gray-50 text-gray-600 border border-gray-200 hover:bg-gray-100'">Transfer</button>
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Amount (INR)</label>
          <input v-model.number="form.amount" type="number" step="0.01" required class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" placeholder="0.00" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Account</label>
          <select v-model.number="form.accountId" required class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm bg-white">
            <option value="">Select Account</option>
            <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }} (INR {{ (account.balance || 0).toLocaleString('en-IN') }})</option>
          </select>
        </div>
        <div v-if="form.type === 'Transfer'">
          <label class="block text-sm font-medium text-gray-700 mb-1">Transfer To</label>
          <select v-model.number="form.transferAccountId" required class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm bg-white">
            <option value="">Select Destination Account</option>
            <option v-for="account in destinationAccounts" :key="account.id" :value="account.id">{{ account.name }} (INR {{ (account.balance || 0).toLocaleString('en-IN') }})</option>
          </select>
        </div>
        <div v-if="form.type !== 'Transfer'">
          <label class="block text-sm font-medium text-gray-700 mb-1">Category</label>
          <select v-model.number="form.categoryId" required class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm bg-white">
            <option value="">Select Category</option>
            <option v-for="category in categoriesForType" :key="category.id" :value="category.id">{{ category.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Date</label>
          <input v-model="form.date" type="date" required class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
          <textarea v-model="form.description" rows="2" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" placeholder="Add a note about this transaction..."></textarea>
        </div>
        <div class="flex space-x-3 pt-2">
          <button type="button" @click="$emit('close')" class="flex-1 px-4 py-2.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">Cancel</button>
          <button type="submit" :disabled="saving" class="flex-1 px-4 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">{{ saving ? 'Saving...' : (transaction ? 'Update' : 'Add Transaction') }}</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref, onMounted } from 'vue'
import { useAccountsStore } from '../stores/accounts'
import { transactionsApi } from '../api/transactions'

const props = defineProps({ transaction: { type: Object, default: null } })
const emit = defineEmits(['close', 'save'])
const accountsStore = useAccountsStore()
const accounts = accountsStore.accounts
const saving = ref(false)
const categories = ref([])
const form = reactive({
  type: 'Expense', amount: 0, accountId: '', transferAccountId: '', categoryId: '',
  date: new Date().toISOString().slice(0, 10), description: ''
})
const categoriesForType = computed(() => categories.value.filter(category => category.type === form.type && category.isActive))
const destinationAccounts = computed(() => accounts.filter(account => account.id !== form.accountId))

onMounted(async () => {
  try { categories.value = (await transactionsApi.getCategories()).data?.data ?? [] } catch { categories.value = [] }
  if (props.transaction) {
    Object.assign(form, {
      type: props.transaction.type || 'Expense', amount: props.transaction.amount || 0,
      accountId: props.transaction.accountId || '', transferAccountId: props.transaction.transferAccountId || '', categoryId: props.transaction.categoryId || '',
      date: (props.transaction.transactionDate || props.transaction.date) ? new Date(props.transaction.transactionDate || props.transaction.date).toISOString().slice(0, 10) : new Date().toISOString().slice(0, 10),
      description: props.transaction.description || ''
    })
  }
})

async function handleSubmit() {
  saving.value = true
  try {
    const payload = { ...form }
    if (payload.type !== 'Transfer') payload.transferAccountId = null
    if (payload.type === 'Transfer') payload.categoryId = null
    emit('save', payload)
  } finally { saving.value = false }
}
</script>