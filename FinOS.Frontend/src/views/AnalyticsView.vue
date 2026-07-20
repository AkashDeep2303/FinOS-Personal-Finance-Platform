<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Analytics</h1>
      <div class="flex items-center space-x-3">
        <select v-model="dateRange" @change="fetchData"
          class="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 bg-white">
          <option value="3months">Last 3 Months</option>
          <option value="6months">Last 6 Months</option>
          <option value="1year">Last 1 Year</option>
          <option value="all">All Time</option>
        </select>
      </div>
    </div>

    <!-- Financial Score -->
    <div class="bg-gradient-to-r from-primary-600 to-primary-700 rounded-xl shadow-sm p-6 text-white">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-lg font-semibold opacity-90">Financial Health Score</h2>
          <p class="text-5xl font-bold mt-2">{{ financialScore.score || 0 }}</p>
          <p class="text-sm opacity-80 mt-1">Grade: {{ financialScore.grade || 'N/A' }}</p>
        </div>
        <div class="w-24 h-24 rounded-full border-4 border-white/30 flex items-center justify-center">
          <span class="text-3xl">{{ getGradeEmoji(financialScore.grade) }}</span>
        </div>
      </div>
      <div v-if="financialScore.factors && financialScore.factors.length" class="mt-4 grid grid-cols-2 gap-2">
        <div v-for="factor in financialScore.factors" :key="factor.name"
          class="bg-white/10 rounded-lg px-3 py-2">
          <p class="text-xs opacity-80">{{ factor.name }}</p>
          <p class="text-sm font-semibold">{{ factor.value }}</p>
        </div>
      </div>
    </div>

    <!-- Charts Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Income vs Expense Chart -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Income vs Expenses</h2>
        <div class="h-64">
          <Bar v-if="incomeVsExpenseData.labels.length" :data="incomeVsExpenseData" :options="barChartOptions" />
          <div v-else class="flex items-center justify-center h-full text-gray-400">
            No data available
          </div>
        </div>
      </div>

      <!-- Category Breakdown -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Expense by Category</h2>
        <div class="h-64 flex items-center justify-center">
          <Doughnut v-if="categoryData.labels.length" :data="categoryData" :options="doughnutOptions" />
          <div v-else class="text-gray-400">No data available</div>
        </div>
      </div>

      <!-- Net Worth Trend -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Net Worth Trend</h2>
        <div class="h-64">
          <Line v-if="netWorthData.labels.length" :data="netWorthData" :options="lineChartOptions" />
          <div v-else class="flex items-center justify-center h-full text-gray-400">
            No data available
          </div>
        </div>
      </div>

      <!-- Savings Rate Trend -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Savings Rate Trend</h2>
        <div class="h-64">
          <Line v-if="savingsRateData.labels.length" :data="savingsRateData" :options="lineChartOptions" />
          <div v-else class="flex items-center justify-center h-full text-gray-400">
            No data available
          </div>
        </div>
      </div>
    </div>

    <!-- Top Categories Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200">
      <div class="p-6 border-b border-gray-100">
        <h2 class="text-lg font-semibold text-gray-900">Top Spending Categories</h2>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase">Category</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase">Amount</th>
              <th class="text-right px-6 py-3 text-xs font-medium text-gray-500 uppercase">% of Total</th>
              <th class="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase">Trend</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="(cat, index) in topCategories" :key="index" class="hover:bg-gray-50">
              <td class="px-6 py-4 text-sm font-medium text-gray-900">{{ cat.category || cat.name }}</td>
              <td class="px-6 py-4 text-sm text-right font-semibold">{{ formatCurrency(cat.amount) }}</td>
              <td class="px-6 py-4 text-sm text-right text-gray-600">{{ cat.percentage || 0 }}%</td>
              <td class="px-6 py-4">
                <span :class="cat.trend === 'up' ? 'text-red-600' : 'text-green-600'" class="text-sm">
                  {{ cat.trend === 'up' ? '📈' : '📉' }} {{ cat.change || 0 }}%
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { Bar, Doughnut, Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'
import { useAnalyticsStore } from '../stores/analytics'

ChartJS.register(
  CategoryScale, LinearScale, BarElement, PointElement,
  LineElement, ArcElement, Title, Tooltip, Legend, Filler
)

const analyticsStore = useAnalyticsStore()
const dateRange = ref('6months')

const financialScore = computed(() => analyticsStore.financialScore)
const topCategories = computed(() => analyticsStore.topCategories)

const chartColors = [
  '#4f46e5', '#06b6d4', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#ec4899', '#14b8a6', '#f97316', '#6366f1'
]

const incomeVsExpenseData = computed(() => ({
  labels: analyticsStore.incomeVsExpense.map(d => d.month || d.label || ''),
  datasets: [
    {
      label: 'Income',
      data: analyticsStore.incomeVsExpense.map(d => d.income || 0),
      backgroundColor: '#10b981',
      borderRadius: 6
    },
    {
      label: 'Expenses',
      data: analyticsStore.incomeVsExpense.map(d => d.expense || 0),
      backgroundColor: '#ef4444',
      borderRadius: 6
    }
  ]
}))

const categoryData = computed(() => ({
  labels: analyticsStore.categoryBreakdown.map(d => d.category || d.name || ''),
  datasets: [{
    data: analyticsStore.categoryBreakdown.map(d => d.amount || 0),
    backgroundColor: chartColors.slice(0, analyticsStore.categoryBreakdown.length),
    borderWidth: 0
  }]
}))

const netWorthData = computed(() => ({
  labels: analyticsStore.netWorthTrend.map(d => d.month || d.date || ''),
  datasets: [{
    label: 'Net Worth',
    data: analyticsStore.netWorthTrend.map(d => d.netWorth || d.value || 0),
    borderColor: '#4f46e5',
    backgroundColor: 'rgba(79, 70, 229, 0.1)',
    fill: true,
    tension: 0.4,
    pointBackgroundColor: '#4f46e5',
    pointRadius: 4
  }]
}))

const savingsRateData = computed(() => {
  const data = analyticsStore.incomeVsExpense.map(d => {
    if (!d.income) return 0
    return Math.round(((d.income - (d.expense || 0)) / d.income) * 100)
  })
  return {
    labels: analyticsStore.incomeVsExpense.map(d => d.month || d.label || ''),
    datasets: [{
      label: 'Savings Rate %',
      data,
      borderColor: '#10b981',
      backgroundColor: 'rgba(16, 185, 129, 0.1)',
      fill: true,
      tension: 0.4,
      pointBackgroundColor: '#10b981',
      pointRadius: 4
    }]
  }
})

const barChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { position: 'bottom' } },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        callback: (value) => '₹' + (value / 1000).toFixed(0) + 'K'
      }
    }
  }
}

const doughnutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'right', labels: { boxWidth: 12, padding: 12 } },
    tooltip: {
      callbacks: {
        label: (ctx) => {
          const value = ctx.parsed || 0
          return ' ₹' + value.toLocaleString('en-IN')
        }
      }
    }
  }
}

const lineChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { position: 'bottom' } },
  scales: {
    y: {
      ticks: {
        callback: (value) => '₹' + (value / 1000).toFixed(0) + 'K'
      }
    }
  }
}

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency', currency: 'INR',
    minimumFractionDigits: 0, maximumFractionDigits: 0
  }).format(amount || 0)
}

function getGradeEmoji(grade) {
  const emojis = { 'A+': '🌟', 'A': '⭐', 'B+': '👍', 'B': '👌', 'C': '🤔', 'D': '⚠️', 'F': '🔴' }
  return emojis[grade] || '📊'
}

async function fetchData() {
  const params = { range: dateRange.value }
  await Promise.all([
    analyticsStore.fetchIncomeVsExpense(params),
    analyticsStore.fetchCategoryBreakdown(params),
    analyticsStore.fetchNetWorthTrend(params),
    analyticsStore.fetchFinancialScore()
  ])
}

onMounted(() => {
  fetchData()
})
</script>
