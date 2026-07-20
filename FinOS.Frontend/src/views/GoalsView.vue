<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Goals</h1>
      <button @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium">
        <span class="mr-2">＋</span> Add Goal
      </button>
    </div>

    <!-- Goal Summary -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Target</p>
        <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(goalsStore.totalTargetAmount) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Saved</p>
        <p class="text-2xl font-bold text-green-600">{{ formatCurrency(goalsStore.totalSavedAmount) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Active Goals</p>
        <p class="text-2xl font-bold text-gray-900">{{ goalsStore.activeGoals.length }}</p>
      </div>
    </div>

    <!-- Goal Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="goal in goals" :key="goal.id"
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center space-x-3">
            <div class="w-12 h-12 rounded-full flex items-center justify-center text-2xl"
              :class="getGoalColorClass(goal.priority)">
              {{ getGoalIcon(goal.category || goal.name) }}
            </div>
            <div>
              <h3 class="font-semibold text-gray-900">{{ goal.name }}</h3>
              <div class="flex items-center space-x-2">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="goal.status === 'Completed' ? 'bg-green-100 text-green-800' : 'bg-blue-100 text-blue-800'">
                  {{ goal.status || 'Active' }}
                </span>
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="getPriorityClass(goal.priority)">
                  {{ goal.priority || 'Medium' }}
                </span>
              </div>
            </div>
          </div>
          <div class="flex space-x-1">
            <button @click="openContribution(goal)" class="p-1 text-gray-400 hover:text-green-600" title="Add Contribution">➕</button>
            <button @click="deleteGoal(goal.id)" class="p-1 text-gray-400 hover:text-red-600" title="Delete">🗑️</button>
          </div>
        </div>

        <!-- Progress -->
        <div class="mb-4">
          <div class="flex items-center justify-between mb-1">
            <span class="text-sm text-gray-600">{{ formatCurrency(goal.currentAmount || 0) }} saved</span>
            <span class="text-sm font-medium text-gray-700">{{ Math.round(getGoalProgress(goal)) }}%</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-3">
            <div class="h-3 rounded-full transition-all"
              :class="getGoalProgress(goal) >= 100 ? 'bg-green-500' : 'bg-primary-600'"
              :style="{ width: Math.min(getGoalProgress(goal), 100) + '%' }">
            </div>
          </div>
          <div class="flex items-center justify-between mt-1">
            <span class="text-xs text-gray-500">Target: {{ formatCurrency(goal.targetAmount) }}</span>
            <span class="text-xs" :class="getRemaining(goal) > 0 ? 'text-amber-600' : 'text-green-600'">
              {{ getRemaining(goal) > 0 ? formatCurrency(getRemaining(goal)) + ' to go' : '🎉 Achieved!' }}
            </span>
          </div>
        </div>

        <!-- Details -->
        <div class="grid grid-cols-2 gap-2 text-sm">
          <div>
            <p class="text-gray-500 text-xs">Target Date</p>
            <p class="font-medium text-gray-900">{{ formatDate(goal.targetDate) }}</p>
          </div>
          <div>
            <p class="text-gray-500 text-xs">Monthly Needed</p>
            <p class="font-medium text-gray-900">{{ formatCurrency(getMonthlyNeeded(goal)) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="goals.length === 0" class="text-center py-12">
      <p class="text-4xl mb-4">🎯</p>
      <h3 class="text-lg font-medium text-gray-900 mb-2">No goals set</h3>
      <p class="text-gray-500 mb-4">Set financial goals and track your progress towards achieving them.</p>
      <button @click="showAddModal = true" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium">
        Create Your First Goal
      </button>
    </div>

    <!-- Add Goal Modal -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">Add Goal</h2>
            <button @click="showAddModal = false" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
          </div>
        </div>
        <form @submit.prevent="saveGoal" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Goal Name</label>
            <input v-model="goalForm.name" type="text" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="e.g., Emergency Fund" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Category</label>
            <select v-model="goalForm.category"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="Emergency Fund">🆘 Emergency Fund</option>
              <option value="Vacation">✈️ Vacation</option>
              <option value="Home">🏠 Home</option>
              <option value="Car">🚗 Car</option>
              <option value="Wedding">💒 Wedding</option>
              <option value="Education">🎓 Education</option>
              <option value="Retirement">🏖️ Retirement</option>
              <option value="Gadget">📱 Gadget</option>
              <option value="Other">🎯 Other</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Target Amount (₹)</label>
            <input v-model.number="goalForm.targetAmount" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Current Amount (₹)</label>
            <input v-model.number="goalForm.currentAmount" type="number" step="0.01"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Target Date</label>
            <input v-model="goalForm.targetDate" type="date" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Priority</label>
            <select v-model="goalForm.priority"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="High">🔴 High</option>
              <option value="Medium">🟡 Medium</option>
              <option value="Low">🟢 Low</option>
            </select>
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

    <!-- Add Contribution Modal -->
    <div v-if="showContributionModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-sm">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">Add Contribution</h2>
            <button @click="showContributionModal = false" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
          </div>
          <p class="text-sm text-gray-500 mt-1">{{ selectedGoal?.name }}</p>
        </div>
        <form @submit.prevent="saveContribution" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Amount (₹)</label>
            <input v-model.number="contributionForm.amount" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="Enter contribution amount" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Date</label>
            <input v-model="contributionForm.date" type="date" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Note (optional)</label>
            <input v-model="contributionForm.note" type="text"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="e.g., Monthly savings" />
          </div>
          <div class="flex space-x-3 pt-2">
            <button type="button" @click="showContributionModal = false"
              class="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">Cancel</button>
            <button type="submit" :disabled="saving"
              class="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm font-medium disabled:opacity-50">
              {{ saving ? 'Adding...' : 'Add ₹' + (contributionForm.amount || 0).toLocaleString('en-IN') }}
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
import { useGoalsStore } from '../stores/goals'

const goalsStore = useGoalsStore()
const goals = computed(() => goalsStore.goals)

const showAddModal = ref(false)
const showContributionModal = ref(false)
const selectedGoal = ref(null)
const saving = ref(false)

const goalForm = reactive({
  name: '', category: 'Emergency Fund', targetAmount: 0, currentAmount: 0,
  targetDate: '', priority: 'Medium'
})

const contributionForm = reactive({
  amount: 0,
  date: new Date().toISOString().slice(0, 10),
  note: ''
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

function getGoalIcon(name) {
  const icons = { 'Emergency Fund': '🆘', Vacation: '✈️', Home: '🏠', Car: '🚗', Wedding: '💒', Education: '🎓', Retirement: '🏖️', Gadget: '📱' }
  return icons[name] || '🎯'
}

function getGoalColorClass(priority) {
  const classes = { High: 'bg-red-100', Medium: 'bg-amber-100', Low: 'bg-green-100' }
  return classes[priority] || 'bg-blue-100'
}

function getPriorityClass(priority) {
  const classes = { High: 'bg-red-100 text-red-800', Medium: 'bg-amber-100 text-amber-800', Low: 'bg-green-100 text-green-800' }
  return classes[priority] || 'bg-gray-100 text-gray-800'
}

function getGoalProgress(goal) {
  if (!goal.targetAmount) return 0
  return ((goal.currentAmount || 0) / goal.targetAmount) * 100
}

function getRemaining(goal) {
  return (goal.targetAmount || 0) - (goal.currentAmount || 0)
}

function getMonthlyNeeded(goal) {
  if (!goal.targetDate) return 0
  const remaining = getRemaining(goal)
  if (remaining <= 0) return 0
  const monthsLeft = Math.max(1, Math.ceil((new Date(goal.targetDate) - new Date()) / (1000 * 60 * 60 * 24 * 30)))
  return remaining / monthsLeft
}

function openContribution(goal) {
  selectedGoal.value = goal
  contributionForm.amount = 0
  contributionForm.date = new Date().toISOString().slice(0, 10)
  contributionForm.note = ''
  showContributionModal.value = true
}

async function saveGoal() {
  saving.value = true
  try {
    await goalsStore.createGoal({ ...goalForm })
    showAddModal.value = false
    Object.assign(goalForm, { name: '', category: 'Emergency Fund', targetAmount: 0, currentAmount: 0, targetDate: '', priority: 'Medium' })
  } catch (err) {
    console.error('Failed to save goal:', err)
  } finally {
    saving.value = false
  }
}

async function saveContribution() {
  saving.value = true
  try {
    await goalsStore.addContribution(selectedGoal.value.id, { ...contributionForm })
    showContributionModal.value = false
  } catch (err) {
    console.error('Failed to add contribution:', err)
  } finally {
    saving.value = false
  }
}

async function deleteGoal(id) {
  if (confirm('Delete this goal?')) {
    await goalsStore.deleteGoal(id)
  }
}

onMounted(() => {
  goalsStore.fetchGoals()
})
</script>
