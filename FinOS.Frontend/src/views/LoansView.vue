<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Loans</h1>
      <button @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium">
        <span class="mr-2">&#43;</span> Add Loan
      </button>
    </div>

    <!-- Loan Summary -->
    <div class="grid grid-cols-1 md:grid-cols-3 xl:grid-cols-5 gap-4">
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Outstanding</p>
        <p class="text-2xl font-bold text-red-600">{{ formatCurrency(loansStore.totalOutstanding) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Total Monthly EMI</p>
        <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(loansStore.totalEMI) }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Active Loans</p>
        <p class="text-2xl font-bold text-gray-900">{{ loansStore.activeLoans.length }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Debt-to-Income</p>
        <p class="text-2xl font-bold text-gray-900">{{ loansStore.debtOverview?.debtToIncomeRatioPct ?? 0 }}%</p>
        <p class="text-xs text-gray-500">{{ loansStore.debtOverview?.riskCategory || 'Not available' }}</p>
      </div>
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <p class="text-sm text-gray-500 mb-1">Weighted Interest</p>
        <p class="text-2xl font-bold text-gray-900">{{ loansStore.debtOverview?.weightedInterestRate ?? 0 }}%</p>
        <p class="text-xs text-gray-500">Debt-free {{ formatDate(loansStore.debtOverview?.debtFreeDate) }}</p>
      </div>
    </div>

    <FinancialMetricExplanation
      title="Understand debt-to-income"
      description="The share of recorded gross monthly income committed to recurring debt payments."
      :value="`${loansStore.debtOverview?.debtToIncomeRatioPct ?? 0}%`"
      :changed="`Monthly EMI commitment is ${formatCurrency(loansStore.debtOverview?.totalMonthlyEmi || 0)}.`"
      calculation="Total recurring monthly EMI divided by recorded monthly income, multiplied by 100."
      why-it-matters="A rising ratio reduces flexibility for savings, emergencies, and new obligations."
      improvement="Prioritize expensive debt, avoid adding obligations without sufficient surplus, and compare prepayment scenarios."
    />

    <!-- Loan List -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <div v-for="loan in loans" :key="loan.id"
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center space-x-3">
            <div class="w-10 h-10 rounded-full flex items-center justify-center text-lg"
              :class="getLoanColorClass(loan.type)">
              {{ getLoanIcon(loan.type) }}
            </div>
            <div>
              <h3 class="font-semibold text-gray-900">{{ loan.name }}</h3>
              <p class="text-xs text-gray-500">{{ loan.type }} &middot; {{ loan.lender || 'N/A' }}</p>
            </div>
          </div>
          <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
            :class="loan.status === 'Active' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
            {{ loan.status || 'Active' }}
          </span>
        </div>

        <div class="grid grid-cols-2 gap-3 mb-4">
          <div>
            <p class="text-xs text-gray-500">Loan Amount</p>
            <p class="text-sm font-bold text-gray-900">{{ formatCurrency(loan.principalAmount || loan.amount) }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500">Outstanding</p>
            <p class="text-sm font-bold text-red-600">{{ formatCurrency(loan.outstandingAmount) }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500">Monthly EMI</p>
            <p class="text-sm font-bold text-gray-900">{{ formatCurrency(loan.monthlyEMI) }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500">Interest Rate</p>
            <p class="text-sm font-bold text-gray-900">{{ loan.interestRate }}%</p>
          </div>
        </div>

        <!-- Progress Bar -->
        <div class="mb-3">
          <div class="flex items-center justify-between mb-1">
            <span class="text-xs text-gray-500">Repayment Progress</span>
            <span class="text-xs font-medium text-gray-700">{{ getRepaymentPercentage(loan) }}%</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="h-2 rounded-full bg-primary-600 transition-all"
              :style="{ width: getRepaymentPercentage(loan) + '%' }"></div>
          </div>
        </div>

        <div class="flex items-center space-x-2">
          <button @click="viewEMISchedule(loan)"
            class="flex-1 px-3 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-xs font-medium transition-colors">&#128197; EMI Schedule</button>
          <button @click="openPrepaymentCalc(loan)"
            class="flex-1 px-3 py-2 border border-primary-300 text-primary-700 rounded-lg hover:bg-primary-50 text-xs font-medium transition-colors">&#129518; Prepayment Calc</button>
          <button title="Close loan" @click="closeLoan(loan.id)"
            class="px-3 py-2 border border-red-300 text-red-700 rounded-lg hover:bg-red-50 text-xs font-medium transition-colors">Close</button>
        </div>
      </div>
    </div>

    <!-- EMI Schedule Modal -->
    <div v-if="showEMIModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-3xl max-h-[80vh] overflow-hidden">
        <div class="p-6 border-b border-gray-100 flex items-center justify-between">
          <h2 class="text-lg font-bold text-gray-900">EMI Schedule - {{ selectedLoan?.name }}</h2>
          <button @click="showEMIModal = false" class="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
        </div>
        <div class="overflow-auto max-h-[60vh]">
          <div v-if="loansStore.paymentAnalysis" class="grid grid-cols-2 gap-3 p-4 md:grid-cols-4">
            <div class="rounded-lg bg-gray-50 p-3"><p class="text-xs text-gray-500">Principal paid</p><b>{{ formatCurrency(loansStore.paymentAnalysis.principalPaid) }}</b></div>
            <div class="rounded-lg bg-gray-50 p-3"><p class="text-xs text-gray-500">Interest paid</p><b>{{ formatCurrency(loansStore.paymentAnalysis.interestPaid) }}</b></div>
            <div class="rounded-lg bg-gray-50 p-3"><p class="text-xs text-gray-500">Remaining interest</p><b>{{ formatCurrency(loansStore.paymentAnalysis.remainingInterest) }}</b></div>
            <div class="rounded-lg bg-gray-50 p-3"><p class="text-xs text-gray-500">Late payments</p><b>{{ loansStore.paymentAnalysis.latePayments }}</b></div>
          </div>
          <div class="border-y border-gray-100 p-4">
            <div class="flex items-center justify-between"><h3 class="font-semibold">Interest rate history</h3><button class="text-sm font-medium text-primary-700" @click="showRateForm=!showRateForm">Record change</button></div>
            <form v-if="showRateForm" class="mt-3 grid gap-2 sm:grid-cols-4" @submit.prevent="saveRateChange">
              <input v-model.number="rateForm.newRate" required min="0" max="100" step=".01" type="number" placeholder="New rate %" class="rounded-lg border-gray-300 text-sm">
              <input v-model="rateForm.effectiveDate" required type="date" class="rounded-lg border-gray-300 text-sm">
              <input v-model="rateForm.reason" maxlength="250" placeholder="Reason" class="rounded-lg border-gray-300 text-sm">
              <button class="rounded-lg bg-primary-600 px-3 py-2 text-sm text-white">Save</button>
            </form>
            <div v-if="loansStore.rateHistory.length" class="mt-3 space-y-1 text-sm text-gray-600"><p v-for="rate in loansStore.rateHistory" :key="rate.id">{{ formatDate(rate.effectiveDate) }}: {{ rate.previousRate }}% → {{ rate.newRate }}% <span v-if="rate.reason">· {{ rate.reason }}</span></p></div>
            <p v-else class="mt-2 text-sm text-gray-500">No rate changes recorded.</p>
          </div>
          <div class="border-b border-gray-100 p-4">
            <h3 class="font-semibold">Prepayment history</h3>
            <div v-if="loansStore.prepaymentHistory.length" class="mt-3 space-y-2">
              <div v-for="payment in loansStore.prepaymentHistory" :key="payment.id" class="grid grid-cols-2 gap-2 rounded-lg bg-gray-50 p-3 text-sm md:grid-cols-4">
                <div><p class="text-xs text-gray-500">Date</p><b>{{ formatDate(payment.prepaymentDate) }}</b></div>
                <div><p class="text-xs text-gray-500">Amount</p><b>{{ formatCurrency(payment.prepaymentAmount) }}</b></div>
                <div><p class="text-xs text-gray-500">Interest saved</p><b class="text-green-700">{{ formatCurrency(payment.interestSaved) }}</b></div>
                <div><p class="text-xs text-gray-500">Tenure reduced</p><b>{{ payment.tenureReduction || 0 }} months</b></div>
              </div>
            </div>
            <p v-else class="mt-2 text-sm text-gray-500">No executed prepayments recorded.</p>
          </div>
          <table class="w-full">
            <thead class="bg-gray-50 sticky top-0">
              <tr>
                <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">EMI #</th>
                <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Due Date</th>
                <th class="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">EMI (&#8377;)</th>
                <th class="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Principal</th>
                <th class="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Interest</th>
                <th class="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Balance</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(emi, index) in validEmiSchedule" :key="index" class="hover:bg-gray-50">
                <td class="px-4 py-3 text-sm text-gray-900">{{ emi.emiNumber || index + 1 }}</td>
                <td class="px-4 py-3 text-sm text-gray-600">{{ formatDate(emi.dueDate) }}</td>
                <td class="px-4 py-3 text-sm font-semibold text-right">{{ formatCurrency(emi.emiAmount) }}</td>
                <td class="px-4 py-3 text-sm text-right text-gray-600">{{ formatCurrency(emi.principal) }}</td>
                <td class="px-4 py-3 text-sm text-right text-red-600">{{ formatCurrency(emi.interest) }}</td>
                <td class="px-4 py-3 text-sm text-right font-medium">{{ formatCurrency(emi.remainingBalance) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Prepayment Calculator Modal -->
    <div v-if="showPrepaymentModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">Prepayment Calculator</h2>
            <button @click="showPrepaymentModal = false" class="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
          </div>
        </div>
        <form @submit.prevent="calculatePrepayment" class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Prepayment Amount (&#8377;)</label>
            <input v-model.number="prepaymentForm.amount" type="number" step="0.01" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select v-model="prepaymentForm.type"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="reduce_emi">Reduce EMI</option>
              <option value="reduce_tenure">Reduce Tenure</option>
            </select>
          </div>
          <button type="submit" :disabled="calculating"
            class="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
            {{ calculating ? 'Calculating...' : 'Calculate' }}
          </button>

          <!-- Results -->
          <div v-if="prepaymentResult" class="mt-4 p-4 bg-green-50 rounded-lg space-y-2">
            <h3 class="font-semibold text-green-800 text-sm">&#128176; Savings</h3>
            <div class="grid grid-cols-2 gap-2 text-sm">
              <div>
                <p class="text-gray-500">Interest Saved</p>
                <p class="font-bold text-green-700">{{ formatCurrency(prepaymentResult.interestSaved) }}</p>
              </div>
              <div>
                <p class="text-gray-500">EMIs Saved</p>
                <p class="font-bold text-green-700">{{ prepaymentResult.emisSaved }}</p>
              </div>
              <div>
                <p class="text-gray-500">New EMI</p>
                <p class="font-bold text-gray-900">{{ formatCurrency(prepaymentResult.newEMI) }}</p>
              </div>
              <div>
                <p class="text-gray-500">New Tenure</p>
                <p class="font-bold text-gray-900">{{ prepaymentResult.newTenureMonths }} months</p>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>

    <!-- Add Loan Modal -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md max-h-[90vh] overflow-auto">
        <div class="p-6 border-b border-gray-100">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">Add Loan</h2>
            <button @click="showAddModal = false" class="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
          </div>
        </div>
        <form @submit.prevent="saveLoan" class="p-6 space-y-4">
          <p v-if="loanError" class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ loanError }}</p>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Loan Name</label>
            <input v-model="loanForm.name" type="text" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="e.g., Home Loan - SBI" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Loan Type</label>
            <select v-model="loanForm.type" required
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option value="Home Loan">Home Loan</option>
              <option value="Car Loan">Car Loan</option>
              <option value="Personal Loan">Personal Loan</option>
              <option value="Education Loan">Education Loan</option>
              <option value="Gold Loan">Gold Loan</option>
              <option value="Business Loan">Business Loan</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Lender</label>
            <input v-model="loanForm.lender" type="text"
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm"
              placeholder="e.g., SBI, HDFC" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Linked Account</label>
            <select v-model.number="loanForm.accountId" required class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
              <option :value="null" disabled>Select an account</option>
              <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }}</option>
            </select>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Principal (&#8377;)</label>
              <input v-model.number="loanForm.principalAmount" type="number" min="0.01" step="0.01" required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Interest Rate (%)</label>
              <input v-model.number="loanForm.interestRate" type="number" min="0.01" step="0.01" required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Tenure (months)</label>
              <input v-model.number="loanForm.tenureMonths" type="number" min="1" required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
              <input v-model="loanForm.startDate" type="date" required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm" />
            </div>
          </div>
          <div class="flex space-x-3 pt-2">
            <button type="button" @click="showAddModal = false"
              class="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">&times;</button>
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
import { useLoansStore } from '../stores/loans'
import { useAccountsStore } from '../stores/accounts'
import FinancialMetricExplanation from '../components/FinancialMetricExplanation.vue'

const loansStore = useLoansStore()
const accountsStore = useAccountsStore()
const loans = computed(() => loansStore.loans)
const accounts = computed(() => accountsStore.accounts)

const showAddModal = ref(false)
const showEMIModal = ref(false)
const showPrepaymentModal = ref(false)
const selectedLoan = ref(null)
const emiSchedule = ref([])
const showRateForm = ref(false)
const rateForm = reactive({ newRate: 0, effectiveDate: new Date().toISOString().slice(0, 10), reason: '' })
const prepaymentResult = ref(null)
const calculating = ref(false)
const saving = ref(false)
const loanError = ref(null)

const loanForm = reactive({
  name: '', type: 'Home Loan', lender: '', accountId: null, principalAmount: 0,
  interestRate: 0, tenureMonths: 0, startDate: new Date().toISOString().slice(0, 10)
})

const prepaymentForm = reactive({ amount: 0, type: 'reduce_tenure' })

const validEmiSchedule = computed(() => Array.isArray(emiSchedule.value) ? emiSchedule.value.filter(Boolean) : [])

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

function getLoanIcon(type) {
  const icons = { 'Home Loan': 0x1F3E0, 'Car Loan': 0x1F697, 'Personal Loan': 0x1F464, 'Education Loan': 0x1F393, 'Gold Loan': 0x1F947, 'Business Loan': 0x1F3E2 }
  return String.fromCodePoint(icons[type] || 0x1F4B3)
}

function getLoanColorClass(type) {
  const classes = { 'Home Loan': 'bg-blue-100', 'Car Loan': 'bg-green-100', 'Personal Loan': 'bg-purple-100', 'Education Loan': 'bg-amber-100', 'Gold Loan': 'bg-yellow-100', 'Business Loan': 'bg-cyan-100' }
  return classes[type] || 'bg-gray-100'
}

function getRepaymentPercentage(loan) {
  const principal = loan.principalAmount || loan.amount || 0
  const outstanding = loan.outstandingAmount || 0
  if (!principal) return 0
  return Math.round(((principal - outstanding) / principal) * 100)
}

async function viewEMISchedule(loan) {
  selectedLoan.value = loan
  try {
    const [schedule] = await Promise.all([loansStore.fetchEMISchedule(loan.id), loansStore.fetchLoanAnalysis(loan.id)])
    emiSchedule.value = schedule || []
  } catch (err) {
    emiSchedule.value = []
  }
  showEMIModal.value = true
}

async function saveRateChange() {
  await loansStore.addRateChange(selectedLoan.value.id, { ...rateForm })
  showRateForm.value = false
}

function openPrepaymentCalc(loan) {
  selectedLoan.value = loan
  prepaymentResult.value = null
  prepaymentForm.amount = 0
  showPrepaymentModal.value = true
}

async function calculatePrepayment() {
  calculating.value = true
  try {
    const result = await loansStore.calculatePrepayment(selectedLoan.value.id, { ...prepaymentForm })
    prepaymentResult.value = result
  } catch (err) {
    console.error('Prepayment calculation failed:', err)
  } finally {
    calculating.value = false
  }
}

async function saveLoan() {
  saving.value = true
  loanError.value = null
  try {
    await loansStore.createLoan({ ...loanForm })
    showAddModal.value = false
    Object.assign(loanForm, { name: '', type: 'Home Loan', lender: '', accountId: null, principalAmount: 0, interestRate: 0, tenureMonths: 0, startDate: new Date().toISOString().slice(0, 10) })
  } catch (err) {
    const response = err.response?.data
    const validationErrors = response?.errors
    loanError.value = response?.message ||
      (validationErrors ? Object.values(validationErrors).flat()[0] : null) ||
      'Failed to save loan. Please verify the linked account and loan details.'
    console.error('Failed to save loan:', err)
  } finally {
    saving.value = false
  }
}

async function closeLoan(id) {
  if (confirm('Close this loan? This records the loan as closed; it does not delete its history.')) {
    await loansStore.closeLoan(id)
  }
}

onMounted(() => {
  Promise.allSettled([loansStore.fetchLoans(), loansStore.fetchDebtOverview()])
})
</script>
