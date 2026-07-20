<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Budgets</h1>
      <div class="flex items-center space-x-3">
        <input v-model="selectedMonth" type="month"
          class="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          @change="changeMonth" />
        <button @click="showAddModal = true"
          class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium">
          <span class="mr-2">＋</span> Add Budget
        </button>
      </div>
    </div>

    <!-- Budget Summary -->
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500 mb-1">Total Budget</p>
        <p class="text-xl font-bold text-gray-900">{{ formatCurrency(budgetsStore.totalBudget) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500 mb-1">Total Spent</p>
        <p class="text-xl font-bold text-red-600">{{ formatCurrency(budgetsStore.totalSpent) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500 mb-1">Remaining</p>
        <p class="text-xl font-bold" :class="budgetsStore.totalRemaining >= 0 ? 'text-green-600' : 'text-red-600'">
          {{ formatCurrency(budgetsStore.totalRemaining) }}
        </p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <p class="text-sm text-gray-500 mb-1">Utilization</p>
        <p class="text-xl font-bold text-gray-900">{{ budgetsStore.budgetUtilization }}%</p>
        <div class="w-full bg-gray-200 rounded-full h-2 mt-2">
          <div class="h-2 rounded-full transition-all"
            :class="budgetsStore.budgetUtilization >= 100 ? 'bg-red-500' : budgetsStore.budgetUtilization >= 80 ? 'bg-amber-500' : 'bg-green-500'"
            :style="{ width: Math.min(budgetsStore.budgetUtilization, 100) + '%' }"></div>
        </div>
      </div>
    </div>

    <!-- Over Budget Alert -->
    <div v-if="budgetsStore.overBudgetCategories.length > 0" class="bg-red-50 border border-red-200 rounded-xl p-4">
      <div class="flex items-center">
        <span class="text-2xl mr-3">⚠️</span>
        <div>
          <h3 class="font-semibold text-red-800">Over Budget Categories</h3>
          <p class="text-sm text-red-600">
            {{ budgetsStore.overBudgetCategories.map(b => b.category).join(', ') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Budget List with Progress Bars -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div v-for="budget in budgets" :key="budget.id"
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
        <div class="flex items-center justify-between mb-3">
          <div class="flex items-center space-x-3">
            <span class="text-xl">{{ getCategoryIcon(budget.category) }}</span>
            <div>
              <h3 class="font-semibold text-gray-900">{{ budget.category }}</h3>
              <p class="text-xs text-gray-500">{{ budget.period || 'Monthly' }}</p>
            </div>
          </div>
          <div class="flex space-x-1">
            <button @click="editBudget(budget)" class="p-1 text-gray-400 hover:text-primary-600" title="Edit">✏️</button>
            <button @click="deleteBudget(budget.id)" class="p-1 text-gray-400 hover:text-red-600" title="Delete">🗑️</button>
          </div>
        </div>

        <div class="mb-3">
          <div class="flex items-center justify-between mb-1">
            <span class="text-sm text-gray-600">
              {{ formatCurrency(budget.spent || 0) }} of {{ formatCurrency(budget.amount) }}
            </span>
            <span class="text-sm font-medium"
              :class="getSpentPercentage(budget) >= 100 ? 'text-red-600' : getSpentPercentage(budget) >= 80 ? 'text-amber-600' : 'text-green-600'">
              {{ getSpentPercentage(budget) }}%
            </span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-3">
            <div class="h-3 rounded-full transition-all"
              :class="getBarColor(budget)"
              :style="{ width: Math.min(getSpentPercentage(budget), 100) + '%' }">
            </div>
          </div>
        </div>

        <div class="flex items-center justify-between text-xs">
          <span class="text-gray-500">
            Remaining: <strong :class="getRemaining(budget) >= 0 ? 'text-green-600' : 'text-red-600'">
              {{ formatCurrency(getRemaining(budget)) }}
            </strong>
          </span>
          <span v-if="getSpentPercentage(budget) >= 100" class="text-red-500 font-medium">Over Budget!</span>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="budgets.length === 0" class="text-center py-12">
      <p class="text-4xl mb-4">📊</p>
      <h3 class="text-lg font-medium text-gray-900 mb-2">No budgets set</h3>
      <p class="text-gray-500 mb-4">Create budgets to track and control your spending.</p>
      <button @click="showAddModal = true" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium">
        Create Your First Budget
      </button>
    </div>

    <!-- Add/Edit Budget Modal -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">{{ editingBudget ? 'Edit Budget' : 'Add Budget' }}</h2>
            <button @click="closeModal" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
          </div>
        </div>
        <form @submit.prevent="saveBudget" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Category</label>
            <select v-model="form.category" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="Food & Dining">🍽️ Food & Dining</option>
              <option value="Transportation">🚗 Transportation</option>
              <option value="Shopping">🛍️ Shopping</option>
              <option value="Entertainment">🎬 Entertainment</option>
              <option value="Bills & Utilities">💡 Bills & Utilities</option>
              <option value="Healthcare">🏥 Healthcare</option>
              <option value="Education">📚 Education</option>
              <option value="Rent">🏠 Rent</option>
              <option value="EMI">💳 EMI</option>
              <option value="Insurance">🛡️ Insurance</option>
              <option value="Groceries">🛒 Groceries</option>
              <option value="Other">📦 Other</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Budget Amount (₹)</label>
            <input v-model.number="form.amount" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="0.00" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Period</label>
            <select v-model="form.period"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="Monthly">Monthly</option>
              <option value="Weekly">Weekly</option>
              <option value="Yearly">Yearly</option>
            </select>
          </div>
          <div class="flex space-x-3 pt-2">
            <button type="button" @click="closeModal"
              class="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
              Cancel
            </button>
            <button type="submit" :disabled="saving"
              class="flex-1 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
              {{ saving ? 'Saving...' : 'Save Budget' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useBudgetsStore } from '../stores/budgets'

const budgetsStore = useBudgetsStore()
const budgets = computed(() => budgetsStore.budgets)
const selectedMonth = ref(new Date().toISOString().slice(0, 7))

const showAddModal = ref(false)
const editingBudget = ref(null)
const saving = ref(false)

const form = reactive({
  category: 'Food & Dining',
  amount: 0,
  period: 'Monthly'
})

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency', currency: 'INR',
    minimumFractionDigits: 0, maximumFractionDigits: 0
  }).format(amount || 0)
}

function getCategoryIcon(category) {
  const icons = {
    'Food & Dining': '🍽️', 'Transportation': '🚗', 'Shopping': '🛍️',
    'Entertainment': '🎬', 'Bills & Utilities': '💡', 'Healthcare': '🏥',
    'Education': '📚', 'Rent': '🏠', 'EMI': '💳', 'Insurance': '🛡️',
    'Groceries': '🛒', 'Other': '📦'
  }
  return icons[category] || '📊'
}

function getSpentPercentage(budget) {
  if (!budget.amount) return 0
  return Math.round(((budget.spent || 0) / budget.amount) * 100)
}

function getBarColor(budget) {
  const pct = getSpentPercentage(budget)
  if (pct >= 100) return 'bg-red-500'
  if (pct >= 80) return 'bg-amber-500'
  return 'bg-green-500'
}

function getRemaining(budget) {
  return (budget.amount || 0) - (budget.spent || 0)
}

function changeMonth() {
  budgetsStore.setSelectedMonth(selectedMonth.value)
}

function editBudget(budget) {
  editingBudget.value = budget
  Object.assign(form, { category: budget.category, amount: budget.amount, period: budget.period || 'Monthly' })
  showAddModal.value = true
}

function closeModal() {
  showAddModal.value = false
  editingBudget.value = null
  Object.assign(form, { category: 'Food & Dining', amount: 0, period: 'Monthly' })
}

async function saveBudget() {
  saving.value = true
  try {
    if (editingBudget.value) {
      await budgetsStore.updateBudget(editingBudget.value.id, { ...form })
    } else {
      await budgetsStore.createBudget({ ...form, month: selectedMonth.value })
    }
    closeModal()
  } catch (err) {
    console.error('Failed to save budget:', err)
  } finally {
    saving.value = false
  }
}

async function deleteBudget(id) {
  if (confirm('Delete this budget?')) {
    await budgetsStore.deleteBudget(id)
  }
}

onMounted(() => {
  budgetsStore.fetchBudgets()
})
</script>
