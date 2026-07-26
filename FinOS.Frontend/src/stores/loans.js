import { defineStore } from 'pinia'
import { loansApi } from '../api/loans'
import { useAuthStore } from './auth'
const currentUserId = () => {
  const user = useAuthStore().user
  if (user?.id ?? user?.userId) return user.id ?? user.userId
  try {
    const token = localStorage.getItem('finos_token')
    return token ? Number(JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))).sub) : undefined
  } catch {
    return undefined
  }
}

export const useLoansStore = defineStore('loans', {
  state: () => ({
    loans: [],
    currentLoan: null,
    emiSchedule: [],
    prepaymentResult: null,
    strategyComparison: null,
    debtOverview: null,
    paymentAnalysis: null,
    rateHistory: [],
    prepaymentHistory: [],
    loading: false,
    error: null
  }),

  getters: {
    totalOutstanding: (state) => {
      return state.loans.reduce((sum, l) => sum + (l.outstandingAmount || 0), 0)
    },
    totalEMI: (state) => {
      return state.loans.reduce((sum, l) => sum + (l.monthlyEMI || 0), 0)
    },
    activeLoans: (state) => {
      return state.loans.filter(l => l.status === 'Active')
    },
    upcomingEMIs: (state) => {
      return state.loans
        .filter(l => l.status === 'Active')
        .map(l => ({
          id: l.id,
          name: l.name,
          emiAmount: l.monthlyEMI,
          dueDate: l.nextDueDate,
          type: l.type
        }))
        .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate))
    },
    loansByType: (state) => {
      const grouped = {}
      state.loans.forEach(loan => {
        if (!grouped[loan.type]) grouped[loan.type] = []
        grouped[loan.type].push(loan)
      })
      return grouped
    }
  },

  actions: {
    async fetchDebtOverview() {
      try {
        const response = await loansApi.getDebtOverview()
        this.debtOverview = response.data?.data ?? null
        return this.debtOverview
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load debt overview'
        throw err
      }
    },

    async compareStrategy(input) {
      this.loading = true
      this.error = null
      try {
        const response = await loansApi.compareStrategy(input)
        this.strategyComparison = response.data?.data ?? null
        return this.strategyComparison
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to compare loan strategies'
        throw err
      } finally {
        this.loading = false
      }
    },
    async fetchLoans() {
      this.loading = true
      this.error = null
      try {
        const userId = currentUserId()
        if (!userId) throw new Error('No authenticated user is available')
        const response = await loansApi.getAll(userId)
        this.loans = Array.isArray(response.data?.data) ? response.data.data.map(loan => ({ ...loan, name: loan.lenderName, lender: loan.lenderName, type: (loan.loanTypeName || 'Loan').replace(/([a-z])([A-Z])/g, '$1 $2'), outstandingAmount: loan.outstandingPrincipal, monthlyEMI: loan.emi, nextDueDate: loan.nextEMIDate, status: loan.status === 0 ? 'Active' : String(loan.status) })) : []
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to fetch loans'
      } finally {
        this.loading = false
      }
    },

    async fetchLoanById(id) {
      this.loading = true
      this.error = null
      try {
        const response = await loansApi.getById(id)
        this.currentLoan = response.data
        return response.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch loan'
        throw err
      } finally {
        this.loading = false
      }
    },

    async fetchEMISchedule(loanId) {
      this.loading = true
      try {
        const response = await loansApi.getEMISchedule(loanId)
        const schedule = response.data?.data ?? response.data
        this.emiSchedule = Array.isArray(schedule) ? schedule.filter(Boolean).map(emi => ({ ...emi, dueDate: emi.emiDate, principal: emi.principalComponent, interest: emi.interestComponent, remainingBalance: emi.outstandingAfter })) : []
        return this.emiSchedule
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to fetch EMI schedule'
        throw err
      } finally {
        this.loading = false
      }
    },
    async fetchLoanAnalysis(loanId) {
      const [analysis, rates, prepayments] = await Promise.all([
        loansApi.getPaymentAnalysis(loanId),
        loansApi.getRateHistory(loanId),
        loansApi.getPrepaymentHistory(loanId)
      ])
      this.paymentAnalysis = analysis.data?.data ?? null
      this.rateHistory = rates.data?.data ?? []
      this.prepaymentHistory = prepayments.data?.data ?? []
      return this.paymentAnalysis
    },
    async addRateChange(loanId, data) {
      await loansApi.addRateChange(loanId, data)
      await Promise.all([this.fetchLoans(), this.fetchLoanAnalysis(loanId)])
    },

    async calculatePrepayment(loanId, prepaymentData) {
      this.loading = true
      try {
        const response = await loansApi.calculatePrepayment(loanId, prepaymentData)
        const result = response.data?.data ?? response.data
        this.prepaymentResult = result
        return result
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to calculate prepayment'
        throw err
      } finally {
        this.loading = false
      }
    },

    async createLoan(loanData) {
      this.loading = true
      this.error = null
      try {
        const loanTypeIds = { 'Home Loan': 1, 'Car Loan': 2, 'Personal Loan': 3, 'Education Loan': 4, 'Gold Loan': 6, 'Business Loan': 8 }
        const startDate = loanData.startDate
        const response = await loansApi.create({
          loanTypeId: loanTypeIds[loanData.type] ?? 1,
          accountId: loanData.accountId,
          lenderName: loanData.lender || loanData.name,
          principalAmount: loanData.principalAmount,
          interestRate: loanData.interestRate,
          interestType: 0,
          tenureMonths: loanData.tenureMonths,
          emiDayOfMonth: new Date(`${startDate}T00:00:00`).getDate(),
          startDate,
          disbursementDate: startDate,
          processingFee: 0,
          prepaymentPenaltyPct: 0,
          isPrepaymentAllowed: true
        })
        const created = response.data?.data ?? response.data
        await this.fetchLoans()
        return created
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create loan'
        throw err
      } finally {
        this.loading = false
      }
    },

    async closeLoan(id) {
      this.loading = true
      this.error = null
      try {
        await loansApi.close(id)
        await this.fetchLoans()
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to close loan'
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})
