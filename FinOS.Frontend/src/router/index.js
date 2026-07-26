import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/LoginView.vue'),
    meta: { guest: true }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/RegisterView.vue'),
    meta: { guest: true }
  },
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: () => import('../views/DashboardView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/accounts',
    name: 'Accounts',
    component: () => import('../views/AccountsView.vue'),
    meta: { requiresAuth: true }
  },
  { path: '/credit-cards', name: 'CreditCards', component: () => import('../views/CreditCardsView.vue'), meta: { requiresAuth: true } },
  {
    path: '/transactions',
    name: 'Transactions',
    component: () => import('../views/TransactionsView.vue'),
    meta: { requiresAuth: true }
  },
  {
      path: '/categories',
      name: 'Categories',
      component: () => import('../views/CategoriesView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/budgets',
    name: 'Budgets',
    component: () => import('../views/BudgetsView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/subscriptions',
    name: 'Subscriptions',
    component: () => import('../views/SubscriptionsView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/investments',
    name: 'Investments',
    component: () => import('../views/InvestmentsView.vue'),
    meta: { requiresAuth: true }
  },
  { path: '/assets', name: 'Assets', component: () => import('../views/AssetsView.vue'), meta: { requiresAuth: true } },
  {
    path: '/loans',
    name: 'Loans',
    component: () => import('../views/LoansView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/loan-strategy',
    name: 'LoanStrategy',
    component: () => import('../views/LoanStrategyView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/goals',
    name: 'Goals',
    component: () => import('../views/GoalsView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/goal-planning',
    name: 'GoalPlanning',
    component: () => import('../views/GoalPlanningView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/retirement',
    name: 'Retirement',
    component: () => import('../views/RetirementView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/tax',
    name: 'TaxCenter',
    component: () => import('../views/TaxCenterView.vue'),
    meta: { requiresAuth: true }
  },
  { path: '/protection', name: 'Protection', component: () => import('../views/ProtectionView.vue'), meta: { requiresAuth: true } },
  {
    path: '/calculators',
    name: 'Calculators',
    component: () => import('../views/CalculatorsView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/scenario-lab',
    name: 'ScenarioLab',
    component: () => import('../views/ScenarioLabView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/analytics',
    name: 'Analytics',
    component: () => import('../views/AnalyticsView.vue'),
    meta: { requiresAuth: true }
  },
  { path: '/reports', name: 'Reports', component: () => import('../views/ReportsView.vue'), meta: { requiresAuth: true } },
  {
    path: '/net-worth',
    name: 'NetWorth',
    component: () => import('../views/NetWorthView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/cash-flow',
    name: 'CashFlow',
    component: () => import('../views/CashFlowView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/financial-health',
    name: 'FinancialHealth',
    component: () => import('../views/FinancialHealthView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/ai-assistant',
    name: 'AIAssistant',
    component: () => import('../views/AIAssistantView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/advisor',
    name: 'Advisor',
    component: () => import('../views/AdvisorView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import('../views/SettingsView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/data-center',
    name: 'DataCenter',
    component: () => import('../views/DataCenterView.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.meta.guest && authStore.isAuthenticated) {
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
