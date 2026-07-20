<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Transactions</h1>
      <button
        @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium"
      >
        <span class="mr-2">＋</span> Add Transaction
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Type</label>
          <select v-model="filters.type" @change="applyFilters"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 bg-white">
            <option value="">All Types</option>
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
            <option value="Transfer">Transfer</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Account</label>
          <select v-model="filters.accountId" @change="applyFilters"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500 bg-white">
            <option value="">All Accounts</option>
            <option v-for="acc in accountsStore.accounts" :key="acc.id" :value="acc.id">{{ acc.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">From Date</label>
          <input v-model="filters.startDate" @change="applyFilters" type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">To Date</label>
          <input v-model="filters.endDate" @change="applyFilters" type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
        </div>
      </div>
      <div class="mt-3 flex items-center justify-between">
        <div class="flex-1 max-w-xs">
          <input v-model="filters.search" @input="applyFilters" type="text" placeholder="Search transactions..."
            class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
        </div>
        <button @click="resetFilters" class="text-sm text-primary-600 hover:text-primary-700 font-medium ml-4">Reset Filters</button>
      </div>
    </div>

    <!-- Summary Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500">Total Income</p>
        <p class="text-xl font-bold text-green-600">+{{ formatCurrency(transactionsStore.totalIncome) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500">Total Expenses</p>
        <p class="text-xl font-bold text-red-600">-{{ formatCurrency(transactionsStore.totalExpense) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500">Net</p>
        <p class="text-xl font-bold" :class="netAmount >= 0 ? 'text-green-600' : 'text-red-600'">
          {{ formatCurrency(netAmount) }}
        </p>
      </div>
    </div>

    <!-- Transaction Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Account</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-if="filteredTransactions.length === 0">
              <td colspan="7" class="px-6 py-12 text-center text-gray-500">No transactions found</td>
            </tr>
            <tr v-for="txn in filteredTransactions" :key="txn.id" class="hover:bg-gray-50 transition-colors">
              <td class="px-6 py-4 text-sm text-gray-600 whitespace-nowrap">{{ formatDate(txn.date) }}</td>
              <td class="px-6 py-4 text-sm font-medium text-gray-900">{{ txn.description || '-' }}</td>
              <td class="px-6 py-4">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                  {{ txn.category || txn.categoryId }}
                </span>
              </td>
              <td class="px-6 py-4 text-sm text-gray-600">{{ txn.accountName || txn.accountId }}</td>
              <td class="px-6 py-4">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                  :class="txn.type === 'Income' ? 'bg-green-100 text-green-800' : txn.type === 'Transfer' ? 'bg-blue-100 text-blue-800' : 'bg-red-100 text-red-800'">
                  {{ txn.type }}
                </span>
              </td>
              <td class="px-6 py-4 text-sm font-semibold text-right whitespace-nowrap"
                :class="txn.type === 'Income' ? 'text-green-600' : 'text-red-600'">
                {{ txn.type === 'Income' ? '+' : '-' }}{{ formatCurrency(txn.amount) }}
              </td>
              <td class="px-6 py-4 text-right">
                <button @click="editTransaction(txn)" class="text-gray-400 hover:text-primary-600 mr-2" title="Edit">✏️</button>
                <button @click="deleteTransaction(txn.id)" class="text-gray-400 hover:text-red-600" title="Delete">🗑️</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Add Transaction Modal -->
    <TransactionModal
      v-if="showAddModal"
      :transaction="editingTransaction"
      @close="closeModal"
      @save="handleSave"
    />
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import { useTransactionsStore } from '../stores/transactions'
import { useAccountsStore } from '../stores/accounts'
import TransactionModal from '../components/TransactionModal.vue'

const transactionsStore = useTransactionsStore()
const accountsStore = useAccountsStore()

const showAddModal = ref(false)
const editingTransaction = ref(null)

const filters = reactive({
  type: '',
  accountId: '',
  startDate: '',
  endDate: '',
  search: ''
})

const filteredTransactions = computed(() => transactionsStore.filteredTransactions)
const netAmount = computed(() => transactionsStore.totalIncome - transactionsStore.totalExpense)

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency', currency: 'INR',
    minimumFractionDigits: 0, maximumFractionDigits: 0
  }).format(amount || 0)
}

function formatDate(date) {
  if (!date) return ''
  return format(new Date(date), 'dd MMM yyyy')
}

function applyFilters() {
  transactionsStore.setFilters({ ...filters })
  transactionsStore.fetchTransactions()
}

function resetFilters() {
  Object.assign(filters, { type: '', accountId: '', startDate: '', endDate: '', search: '' })
  transactionsStore.resetFilters()
  transactionsStore.fetchTransactions()
}

function editTransaction(txn) {
  editingTransaction.value = txn
  showAddModal.value = true
}

function closeModal() {
  showAddModal.value = false
  editingTransaction.value = null
}

async function handleSave(data) {
  if (editingTransaction.value) {
    await transactionsStore.updateTransaction(editingTransaction.value.id, data)
  } else {
    await transactionsStore.createTransaction(data)
  }
  closeModal()
}

async function deleteTransaction(id) {
  if (confirm('Delete this transaction?')) {
    await transactionsStore.deleteTransaction(id)
  }
}

onMounted(() => {
  transactionsStore.fetchTransactions()
  accountsStore.fetchAccounts()
})
</script>
