<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Dashboard</h1>
      <span class="text-sm text-gray-500">{{ currentDate }}</span>
    </div>

    <!-- Net Worth & Monthly Summary Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <StatCard
        title="Net Worth"
        :value="netWorth"
        icon="💰"
        trend="up"
        :trendValue="'+' + formatCurrency(monthlyChange)"
      />
      <StatCard
        title="Monthly Income"
        :value="monthlyIncome"
        icon="📈"
        trend="up"
        trendValue="+12.5%"
      />
      <StatCard
        title="Monthly Expenses"
        :value="monthlyExpenses"
        icon="📉"
        trend="down"
        trendValue="-3.2%"
      />
      <StatCard
        title="Savings Rate"
        :value="savingsRate + '%'"
        icon="🎯"
        :trend="savingsRate >= 20 ? 'up' : 'down'"
        :trendValue="savingsRate >= 20 ? 'On track' : 'Below target'"
      />
    </div>

    <!-- Main Content Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Recent Transactions -->
      <div class="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-gray-900">Recent Transactions</h2>
            <router-link to="/transactions" class="text-sm text-primary-600 hover:text-primary-700 font-medium">View all →</router-link>
          </div>
        </div>
        <div class="divide-y divide-gray-50">
          <div v-if="recentTransactions.length === 0" class="p-6 text-center text-gray-500">
            No recent transactions
          </div>
          <div
            v-for="txn in recentTransactions"
            :key="txn.id"
            class="flex items-center justify-between p-4 hover:bg-gray-50 transition-colors"
          >
            <div class="flex items-center space-x-3">
              <div
                class="w-10 h-10 rounded-full flex items-center justify-center text-lg"
                :class="txn.type === 'Income' ? 'bg-green-100' : 'bg-red-100'"
              >
                {{ txn.type === 'Income' ? '📥' : '📤' }}
              </div>
              <div>
                <p class="text-sm font-medium text-gray-900">{{ txn.description || txn.category }}</p>
                <p class="text-xs text-gray-500">{{ txn.category }} · {{ formatDate(txn.date) }}</p>
              </div>
            </div>
            <span
              class="text-sm font-semibold"
              :class="txn.type === 'Income' ? 'text-green-600' : 'text-red-600'"
            >
              {{ txn.type === 'Income' ? '+' : '-' }}{{ formatCurrency(txn.amount) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Budget Status -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-gray-900">Budget Status</h2>
            <router-link to="/budgets" class="text-sm text-primary-600 hover:text-primary-700 font-medium">View →</router-link>
          </div>
        </div>
        <div class="p-6 space-y-4">
          <div v-if="budgets.length === 0" class="text-center text-gray-500 text-sm">
            No budgets set
          </div>
          <div v-for="budget in budgets.slice(0, 5)" :key="budget.id" class="space-y-2">
            <div class="flex items-center justify-between">
              <span class="text-sm text-gray-700">{{ budget.category }}</span>
              <span class="text-xs text-gray-500">
                {{ formatCurrency(budget.spent || 0) }} / {{ formatCurrency(budget.amount) }}
              </span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div
                class="h-2 rounded-full transition-all"
                :class="getBudgetBarClass(budget)"
                :style="{ width: getBudgetPercentage(budget) + '%' }"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Investment & Loans Row -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Investment Summary -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-gray-900">Investment Summary</h2>
            <router-link to="/investments" class="text-sm text-primary-600 hover:text-primary-700 font-medium">View →</router-link>
          </div>
        </div>
        <div class="p-6">
          <div class="grid grid-cols-2 gap-4">
            <div class="text-center p-4 bg-green-50 rounded-lg">
              <p class="text-xs text-gray-500 mb-1">Total Invested</p>
              <p class="text-lg font-bold text-gray-900">{{ formatCurrency(totalInvested) }}</p>
            </div>
            <div class="text-center p-4 bg-blue-50 rounded-lg">
              <p class="text-xs text-gray-500 mb-1">Current Value</p>
              <p class="text-lg font-bold text-gray-900">{{ formatCurrency(currentInvestmentValue) }}</p>
            </div>
            <div class="text-center p-4 bg-primary-50 rounded-lg">
              <p class="text-xs text-gray-500 mb-1">Total Returns</p>
              <p class="text-lg font-bold" :class="investmentReturns >= 0 ? 'text-green-600' : 'text-red-600'">
                {{ investmentReturns >= 0 ? '+' : '' }}{{ formatCurrency(investmentReturns) }}
              </p>
            </div>
            <div class="text-center p-4 bg-amber-50 rounded-lg">
              <p class="text-xs text-gray-500 mb-1">Monthly SIP</p>
              <p class="text-lg font-bold text-gray-900">{{ formatCurrency(monthlySIP) }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Upcoming EMIs & Goal Progress -->
      <div class="space-y-6">
        <!-- Upcoming EMIs -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-semibold text-gray-900">Upcoming EMIs</h2>
              <router-link to="/loans" class="text-sm text-primary-600 hover:text-primary-700 font-medium">View →</router-link>
            </div>
          </div>
          <div class="divide-y divide-gray-50">
            <div v-if="upcomingEMIs.length === 0" class="p-6 text-center text-gray-500 text-sm">
              No active loans
            </div>
            <div
              v-for="emi in upcomingEMIs.slice(0, 3)"
              :key="emi.id"
              class="flex items-center justify-between p-4"
            >
              <div>
                <p class="text-sm font-medium text-gray-900">{{ emi.name }}</p>
                <p class="text-xs text-gray-500">Due: {{ formatDate(emi.dueDate) }}</p>
              </div>
              <span class="text-sm font-semibold text-red-600">{{ formatCurrency(emi.emiAmount) }}</span>
            </div>
          </div>
        </div>

        <!-- Goal Progress -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-semibold text-gray-900">Goal Progress</h2>
              <router-link to="/goals" class="text-sm text-primary-600 hover:text-primary-700 font-medium">View →</router-link>
            </div>
          </div>
          <div class="p-6 space-y-3">
            <div v-if="goals.length === 0" class="text-center text-gray-500 text-sm">
              No active goals
            </div>
            <div v-for="goal in goals.slice(0, 3)" :key="goal.id">
              <div class="flex items-center justify-between mb-1">
                <span class="text-sm text-gray-700">{{ goal.name }}</span>
                <span class="text-xs text-gray-500">{{ Math.round(goalProgress(goal)) }}%</span>
              </div>
              <div class="w-full bg-gray-200 rounded-full h-2">
                <div
                  class="h-2 rounded-full bg-primary-600 transition-all"
                  :style="{ width: goalProgress(goal) + '%' }"
                ></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import { useAccountsStore } from '../stores/accounts'
import { useTransactionsStore } from '../stores/transactions'
import { useBudgetsStore } from '../stores/budgets'
import { useInvestmentsStore } from '../stores/investments'
import { useLoansStore } from '../stores/loans'
import { useGoalsStore } from '../stores/goals'
import StatCard from '../components/StatCard.vue'

const accountsStore = useAccountsStore()
const transactionsStore = useTransactionsStore()
const budgetsStore = useBudgetsStore()
const investmentsStore = useInvestmentsStore()
const loansStore = useLoansStore()
const goalsStore = useGoalsStore()

const currentDate = computed(() => format(new Date(), 'EEEE, dd MMMM yyyy'))

const netWorth = computed(() => formatCurrency(accountsStore.totalBalance))
const monthlyIncome = computed(() => formatCurrency(transactionsStore.totalIncome))
const monthlyExpenses = computed(() => formatCurrency(transactionsStore.totalExpense))
const monthlyChange = computed(() => transactionsStore.totalIncome - transactionsStore.totalExpense)
const savingsRate = computed(() => {
  const income = transactionsStore.totalIncome
  if (income === 0) return 0
  return Math.round(((income - transactionsStore.totalExpense) / income) * 100)
})

const recentTransactions = computed(() => transactionsStore.recentTransactions)
const budgets = computed(() => budgetsStore.budgets)
const upcomingEMIs = computed(() => loansStore.upcomingEMIs)
const goals = computed(() => goalsStore.activeGoals)

const totalInvested = computed(() => investmentsStore.totalInvested)
const currentInvestmentValue = computed(() => investmentsStore.currentValue)
const investmentReturns = computed(() => investmentsStore.totalReturns)
const monthlySIP = computed(() => investmentsStore.totalSIPMonthly)

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(amount || 0)
}

function formatDate(date) {
  if (!date) return ''
  return format(new Date(date), 'dd MMM yyyy')
}

function getBudgetPercentage(budget) {
  if (!budget.amount) return 0
  return Math.min(Math.round(((budget.spent || 0) / budget.amount) * 100), 100)
}

function getBudgetBarClass(budget) {
  const pct = getBudgetPercentage(budget)
  if (pct >= 100) return 'bg-red-500'
  if (pct >= 80) return 'bg-amber-500'
  return 'bg-green-500'
}

function goalProgress(goal) {
  if (!goal.targetAmount) return 0
  return Math.min(((goal.currentAmount || 0) / goal.targetAmount) * 100, 100)
}

onMounted(async () => {
  await Promise.all([
    accountsStore.fetchAccounts(),
    transactionsStore.fetchTransactions(),
    budgetsStore.fetchBudgets(),
    investmentsStore.fetchInvestments(),
    investmentsStore.fetchSIPs(),
    loansStore.fetchLoans(),
    goalsStore.fetchGoals()
  ])
})
</script>
