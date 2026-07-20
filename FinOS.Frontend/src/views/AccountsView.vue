<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Accounts</h1>
      <button
        @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium"
      >
        <span class="mr-2">＋</span> Add Account
      </button>
    </div>

    <!-- Account Summary -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Balance</p>
        <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(accountsStore.totalBalance) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Accounts</p>
        <p class="text-2xl font-bold text-gray-900">{{ accounts.length }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Account Types</p>
        <p class="text-2xl font-bold text-gray-900">{{ Object.keys(accountsStore.accountsByType).length }}</p>
      </div>
    </div>

    <!-- Account List -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="account in accounts"
        :key="account.id"
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow"
      >
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center space-x-3">
            <div class="w-10 h-10 rounded-full flex items-center justify-center text-lg"
                 :class="getAccountColorClass(account.type)">
              {{ getAccountIcon(account.type) }}
            </div>
            <div>
              <h3 class="font-semibold text-gray-900">{{ account.name }}</h3>
              <p class="text-xs text-gray-500">{{ account.type }} · {{ account.bank || 'N/A' }}</p>
            </div>
          </div>
          <div class="flex space-x-1">
            <button @click="editAccount(account)" class="p-1 text-gray-400 hover:text-primary-600 transition-colors" title="Edit">✏️</button>
            <button @click="deleteAccount(account.id)" class="p-1 text-gray-400 hover:text-red-600 transition-colors" title="Delete">🗑️</button>
          </div>
        </div>
        <div class="border-t border-gray-100 pt-4">
          <p class="text-xs text-gray-500 mb-1">Available Balance</p>
          <p class="text-xl font-bold" :class="account.balance >= 0 ? 'text-gray-900' : 'text-red-600'">
            {{ formatCurrency(account.balance) }}
          </p>
          <p class="text-xs text-gray-400 mt-1">
            Last updated: {{ formatDate(account.updatedAt || account.createdAt) }}
          </p>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="accounts.length === 0 && !loading" class="text-center py-12">
      <p class="text-4xl mb-4">🏦</p>
      <h3 class="text-lg font-medium text-gray-900 mb-2">No accounts yet</h3>
      <p class="text-gray-500 mb-4">Add your bank accounts, wallets, and investments to get started.</p>
      <button @click="showAddModal = true" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium">
        Add Your First Account
      </button>
    </div>

    <!-- Add/Edit Account Modal -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">{{ editingAccount ? 'Edit Account' : 'Add Account' }}</h2>
            <button @click="closeModal" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
          </div>
        </div>
        <form @submit.prevent="saveAccount" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Account Name</label>
            <input v-model="form.name" type="text" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
              placeholder="e.g., HDFC Savings" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Account Type</label>
            <select v-model="form.type" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm bg-white">
              <option value="Savings">Savings Account</option>
              <option value="Current">Current Account</option>
              <option value="FD">Fixed Deposit</option>
              <option value="MF">Mutual Fund</option>
              <option value="Demat">Demat Account</option>
              <option value="Wallet">Digital Wallet</option>
              <option value="Cash">Cash</option>
              <option value="PPF">PPF</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Bank / Institution</label>
            <input v-model="form.bank" type="text"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
              placeholder="e.g., HDFC Bank" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Opening Balance (₹)</label>
            <input v-model.number="form.balance" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
              placeholder="0.00" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Account Number (last 4 digits)</label>
            <input v-model="form.accountNumber" type="text" maxlength="4"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
              placeholder="XXXX" />
          </div>
          <div class="flex space-x-3 pt-2">
            <button type="button" @click="closeModal"
              class="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
              Cancel
            </button>
            <button type="submit" :disabled="saving"
              class="flex-1 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
              {{ saving ? 'Saving...' : 'Save Account' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import { useAccountsStore } from '../stores/accounts'

const accountsStore = useAccountsStore()
const accounts = computed(() => accountsStore.accounts)
const loading = computed(() => accountsStore.loading)

const showAddModal = ref(false)
const editingAccount = ref(null)
const saving = ref(false)

const form = reactive({
  name: '',
  type: 'Savings',
  bank: '',
  balance: 0,
  accountNumber: ''
})

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency', currency: 'INR',
    minimumFractionDigits: 0, maximumFractionDigits: 0
  }).format(amount || 0)
}

function formatDate(date) {
  if (!date) return 'N/A'
  return format(new Date(date), 'dd MMM yyyy')
}

function getAccountIcon(type) {
  const icons = {
    Savings: '🏦', Current: '💳', FD: '📋', MF: '📊',
    Demat: '📈', Wallet: '📱', Cash: '💵', PPF: '🏛️'
  }
  return icons[type] || '🏦'
}

function getAccountColorClass(type) {
  const classes = {
    Savings: 'bg-blue-100', Current: 'bg-green-100', FD: 'bg-amber-100',
    MF: 'bg-purple-100', Demat: 'bg-pink-100', Wallet: 'bg-cyan-100',
    Cash: 'bg-emerald-100', PPF: 'bg-orange-100'
  }
  return classes[type] || 'bg-gray-100'
}

function editAccount(account) {
  editingAccount.value = account
  Object.assign(form, {
    name: account.name,
    type: account.type,
    bank: account.bank || '',
    balance: account.balance || 0,
    accountNumber: account.accountNumber || ''
  })
  showAddModal.value = true
}

function closeModal() {
  showAddModal.value = false
  editingAccount.value = null
  Object.assign(form, { name: '', type: 'Savings', bank: '', balance: 0, accountNumber: '' })
}

async function saveAccount() {
  saving.value = true
  try {
    if (editingAccount.value) {
      await accountsStore.updateAccount(editingAccount.value.id, { ...form })
    } else {
      await accountsStore.createAccount({ ...form })
    }
    closeModal()
  } catch (err) {
    console.error('Failed to save account:', err)
  } finally {
    saving.value = false
  }
}

async function deleteAccount(id) {
  if (confirm('Are you sure you want to delete this account?')) {
    await accountsStore.deleteAccount(id)
  }
}

onMounted(() => {
  accountsStore.fetchAccounts()
})
</script>
