<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Investments</h1>
      <button @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium">
        <span class="mr-2">＋</span> Add Investment
      </button>
    </div>

    <!-- Portfolio Summary -->
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Invested</p>
        <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(investmentsStore.totalInvested) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Current Value</p>
        <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(investmentsStore.currentValue) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Returns</p>
        <p class="text-2xl font-bold" :class="investmentsStore.totalReturns >= 0 ? 'text-green-600' : 'text-red-600'">
          {{ investmentsStore.totalReturns >= 0 ? '+' : '' }}{{ formatCurrency(investmentsStore.totalReturns) }}
        </p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Returns %</p>
        <p class="text-2xl font-bold" :class="investmentsStore.totalReturns >= 0 ? 'text-green-600' : 'text-red-600'">
          {{ investmentsStore.returnsPercentage }}%
        </p>
      </div>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex space-x-8">
        <button @click="activeTab = 'portfolio'"
          class="py-2 px-1 border-b-2 font-medium text-sm transition-colors"
          :class="activeTab === 'portfolio' ? 'border-primary-600 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          Portfolio
        </button>
        <button @click="activeTab = 'sips'"
          class="py-2 px-1 border-b-2 font-medium text-sm transition-colors"
          :class="activeTab === 'sips' ? 'border-primary-600 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          SIP Tracker
        </button>
        <button @click="activeTab = 'epf'"
          class="py-2 px-1 border-b-2 font-medium text-sm transition-colors"
          :class="activeTab === 'epf' ? 'border-primary-600 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          EPF Tracker
        </button>
      </nav>
    </div>

    <!-- Portfolio Tab -->
    <div v-if="activeTab === 'portfolio'">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="inv in investments" :key="inv.id"
          class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center space-x-3">
              <div class="w-10 h-10 rounded-full flex items-center justify-center text-lg"
                :class="getInvestmentColorClass(inv.type)">
                {{ getInvestmentIcon(inv.type) }}
              </div>
              <div>
                <h3 class="font-semibold text-gray-900">{{ inv.name }}</h3>
                <p class="text-xs text-gray-500">{{ inv.type }}</p>
              </div>
            </div>
            <button @click="deleteInvestment(inv.id)" class="text-gray-400 hover:text-red-600" title="Delete">🗑️</button>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <p class="text-xs text-gray-500">Invested</p>
              <p class="text-sm font-bold text-gray-900">{{ formatCurrency(inv.investedAmount) }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500">Current</p>
              <p class="text-sm font-bold text-gray-900">{{ formatCurrency(inv.currentValue) }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500">Returns</p>
              <p class="text-sm font-bold" :class="getReturnClass(inv)">
                {{ getReturnPercentage(inv) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-gray-500">Date</p>
              <p class="text-sm text-gray-600">{{ formatDate(inv.investmentDate) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- SIP Tab -->
    <div v-if="activeTab === 'sips'">
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-lg font-semibold text-gray-900">Active SIPs</h2>
        <div class="text-sm text-gray-500">
          Total Monthly SIP: <strong class="text-primary-600">{{ formatCurrency(investmentsStore.totalSIPMonthly) }}</strong>
        </div>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase">Fund Name</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase">Type</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase">Monthly (₹)</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase">Total Invested</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase">Current Value</th>
              <th class="text-center px-6 py-3 text-xs font-medium text-gray-500 uppercase">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="sip in investmentsStore.sipList" :key="sip.id" class="hover:bg-gray-50">
              <td class="px-6 py-4 text-sm font-medium text-gray-900">{{ sip.fundName }}</td>
              <td class="px-6 py-4 text-sm text-gray-600">{{ sip.fundType }}</td>
              <td class="px-6 py-4 text-sm font-semibold text-right">{{ formatCurrency(sip.monthlyAmount) }}</td>
              <td class="px-6 py-4 text-sm text-right text-gray-600">{{ formatCurrency(sip.totalInvested) }}</td>
              <td class="px-6 py-4 text-sm font-semibold text-right">{{ formatCurrency(sip.currentValue) }}</td>
              <td class="px-6 py-4 text-center">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="sip.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
                  {{ sip.isActive ? 'Active' : 'Paused' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- EPF Tab -->
    <div v-if="activeTab === 'epf'">
      <div v-if="investmentsStore.epfTracker" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <p class="text-sm text-gray-500 mb-1">Employee Contribution</p>
          <p class="text-xl font-bold text-gray-900">{{ formatCurrency(investmentsStore.epfTracker.employeeContribution) }}</p>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <p class="text-sm text-gray-500 mb-1">Employer Contribution</p>
          <p class="text-xl font-bold text-gray-900">{{ formatCurrency(investmentsStore.epfTracker.employerContribution) }}</p>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <p class="text-sm text-gray-500 mb-1">Interest Earned</p>
          <p class="text-xl font-bold text-green-600">{{ formatCurrency(investmentsStore.epfTracker.interestEarned) }}</p>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <p class="text-sm text-gray-500 mb-1">Total Balance</p>
          <p class="text-xl font-bold text-gray-900">{{ formatCurrency(investmentsStore.epfTracker.totalBalance) }}</p>
        </div>
      </div>
      <div v-else class="text-center py-12 bg-white rounded-xl shadow-sm border border-gray-200">
        <p class="text-4xl mb-4">🏛️</p>
        <h3 class="text-lg font-medium text-gray-900 mb-2">EPF Tracker</h3>
        <p class="text-gray-500">Set up your EPF tracking to monitor your retirement savings.</p>
      </div>
    </div>

    <!-- Add Investment Modal -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">Add Investment</h2>
            <button @click="showAddModal = false" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
          </div>
        </div>
        <form @submit.prevent="saveInvestment" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Investment Name</label>
            <input v-model="form.name" type="text" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="e.g., HDFC Mid-Cap Fund" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select v-model="form.type" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="Mutual Fund">Mutual Fund</option>
              <option value="Stock">Stock</option>
              <option value="FD">Fixed Deposit</option>
              <option value="PPF">PPF</option>
              <option value="NPS">NPS</option>
              <option value="Bond">Bond</option>
              <option value="Gold">Gold</option>
              <option value="Real Estate">Real Estate</option>
              <option value="Crypto">Crypto</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Invested Amount (₹)</label>
            <input v-model.number="form.investedAmount" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Current Value (₹)</label>
            <input v-model.number="form.currentValue" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Investment Date</label>
            <input v-model="form.investmentDate" type="date" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div class="flex space-x-3 pt-2">
            <button type="button" @click="showAddModal = false"
              class="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">Cancel</button>
            <button type="submit" :disabled="saving"
              class="flex-1 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
              {{ saving ? 'Saving...' : 'Save' }}
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
import { useInvestmentsStore } from '../stores/investments'

const investmentsStore = useInvestmentsStore()
const investments = computed(() => investmentsStore.investments)
const activeTab = ref('portfolio')
const showAddModal = ref(false)
const saving = ref(false)

const form = reactive({
  name: '',
  type: 'Mutual Fund',
  investedAmount: 0,
  currentValue: 0,
  investmentDate: new Date().toISOString().slice(0, 10)
})

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

function getInvestmentIcon(type) {
  const icons = { 'Mutual Fund': '📊', Stock: '📈', FD: '📋', PPF: '🏛️', NPS: '🎯', Bond: '📜', Gold: '🥇', 'Real Estate': '🏠', Crypto: '₿' }
  return icons[type] || '💼'
}

function getInvestmentColorClass(type) {
  const classes = { 'Mutual Fund': 'bg-purple-100', Stock: 'bg-blue-100', FD: 'bg-amber-100', PPF: 'bg-orange-100', NPS: 'bg-cyan-100', Bond: 'bg-gray-100', Gold: 'bg-yellow-100', 'Real Estate': 'bg-green-100', Crypto: 'bg-pink-100' }
  return classes[type] || 'bg-gray-100'
}

function getReturnPercentage(inv) {
  if (!inv.investedAmount) return '0%'
  const pct = ((inv.currentValue - inv.investedAmount) / inv.investedAmount * 100).toFixed(2)
  return (pct >= 0 ? '+' : '') + pct + '%'
}

function getReturnClass(inv) {
  return inv.currentValue >= inv.investedAmount ? 'text-green-600' : 'text-red-600'
}

async function saveInvestment() {
  saving.value = true
  try {
    await investmentsStore.createInvestment({ ...form })
    showAddModal.value = false
    Object.assign(form, { name: '', type: 'Mutual Fund', investedAmount: 0, currentValue: 0, investmentDate: new Date().toISOString().slice(0, 10) })
  } catch (err) {
    console.error('Failed to save investment:', err)
  } finally {
    saving.value = false
  }
}

async function deleteInvestment(id) {
  if (confirm('Delete this investment?')) {
    await investmentsStore.deleteInvestment(id)
  }
}

onMounted(() => {
  investmentsStore.fetchInvestments()
  investmentsStore.fetchSIPList()
  investmentsStore.fetchEPFTracker()
})
</script>
