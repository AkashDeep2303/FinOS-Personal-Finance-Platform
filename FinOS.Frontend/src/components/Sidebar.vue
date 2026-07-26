<template>
  <aside
    class="fixed left-0 top-0 z-40 flex h-full flex-col bg-slate-900 text-white transition-all duration-300"
    :class="collapsed ? 'w-16' : 'w-64'"
  >
    <div class="flex h-16 items-center border-b border-slate-800 px-4">
      <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-primary-500 font-bold" aria-hidden="true">₹</span>
      <div v-if="!collapsed" class="ml-3">
        <div class="text-lg font-bold tracking-tight">FinOS</div>
        <div class="text-[10px] uppercase tracking-widest text-slate-400">Financial OS</div>
      </div>
    </div>

    <nav class="custom-scrollbar flex-1 overflow-y-auto px-2 py-4" aria-label="Primary navigation">
      <section v-for="group in navGroups" :key="group.label" class="mb-4">
        <button
          v-if="!collapsed"
          type="button"
          class="flex w-full items-center justify-between px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-slate-500 hover:text-slate-300"
          :aria-expanded="isOpen(group.label)"
          @click="toggleGroup(group.label)"
        >
          <span>{{ group.label }}</span><span aria-hidden="true">{{ isOpen(group.label) ? '−' : '+' }}</span>
        </button>
        <div v-show="collapsed || isOpen(group.label)" class="mt-1 space-y-1">
          <router-link
            v-for="item in group.items"
            :key="item.path"
            :to="item.path"
            class="flex items-center rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
            :title="collapsed ? item.label : undefined"
            :class="isActive(item.path) ? 'bg-primary-600 text-white' : 'text-slate-300 hover:bg-slate-800 hover:text-white'"
          >
            <span class="w-8 flex-shrink-0 text-center" aria-hidden="true">{{ item.icon }}</span>
            <span v-if="!collapsed" class="ml-2 truncate">{{ item.label }}</span>
          </router-link>
        </div>
      </section>
    </nav>

    <div class="border-t border-slate-800 p-3">
      <button type="button" class="flex w-full items-center justify-center rounded-lg py-2 text-slate-400 hover:bg-slate-800 hover:text-white" @click="$emit('toggle')">
        <span aria-hidden="true">{{ collapsed ? '→' : '←' }}</span>
        <span v-if="!collapsed" class="ml-2 text-sm">Collapse</span>
      </button>
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'

defineProps({ collapsed: { type: Boolean, default: false } })
defineEmits(['toggle'])

const route = useRoute()
const openGroups = ref(new Set(['Home', 'Money', 'Wealth', 'Debt', 'Plan', 'Insights', 'Ask FinOS']))

const navGroups = [
  { label: 'Home', items: [{ path: '/dashboard', icon: '⌂', label: 'Command Center' }] },
  { label: 'Money', items: [
    { path: '/accounts', icon: '▣', label: 'Accounts' },
    { path: '/credit-cards', icon: '▤', label: 'Credit Cards' },
    { path: '/transactions', icon: '↔', label: 'Transactions' },
    { path: '/budgets', icon: '◫', label: 'Budget' }
  ] },
  { label: 'Wealth', items: [{ path: '/investments', icon: '↗', label: 'Investments' }, { path: '/assets', icon: '◇', label: 'Other Assets' }] },
  { label: 'Debt', items: [
    { path: '/loans', icon: '⌂', label: 'Loans' },
    { path: '/loan-strategy', icon: '⇄', label: 'Strategy Lab' }
  ] },
  { label: 'Plan', items: [
    { path: '/goals', icon: '◎', label: 'Goals' },
    { path: '/goal-planning', icon: '≋', label: 'Goal Funding' },
    { path: '/retirement', icon: '◷', label: 'Retirement' }
    ,{ path: '/tax', icon: '₹', label: 'Tax' },
    { path: '/protection', icon: '◇', label: 'Protection' }
  ] },
  { label: 'Insights', items: [{ path: '/analytics', icon: '◩', label: 'Analytics & Health' }, { path: '/reports', icon: '▦', label: 'Reports' }] },
  { label: 'Ask FinOS', items: [{ path: '/ai-assistant', icon: '✦', label: 'AI Assistant' }] },
  { label: 'Settings', items: [{ path: '/settings', icon: '⚙', label: 'Preferences' }] }
]

navGroups.find(group => group.label === 'Wealth')?.items.unshift(
  { path: '/net-worth', icon: 'Σ', label: 'Net Worth' }
)
navGroups.find(group => group.label === 'Insights')?.items.push(
  { path: '/cash-flow', icon: '↝', label: 'Cash Flow' },
  { path: '/financial-health', icon: '♡', label: 'Financial Health' }
)
navGroups.find(group => group.label === 'Money')?.items.push(
  { path: '/subscriptions', icon: '↻', label: 'Bills & Subscriptions' }
)
navGroups.find(group => group.label === 'Money')?.items.splice(3, 0,
  { path: '/categories', icon: '#', label: 'Categories' }
)
navGroups.find(group => group.label === 'Ask FinOS')?.items.push(
  { path: '/advisor', icon: '!', label: 'Advisor' }
)

navGroups.splice(navGroups.length - 2, 0, {
  label: 'Tools',
  items: [
    { path: '/calculators', icon: 'ƒ', label: 'Calculators' },
    { path: '/scenario-lab', icon: '?', label: 'Scenario Lab' }
  ]
})

navGroups.splice(navGroups.length - 1, 0, {
  label: 'Data',
  items: [{ path: '/data-center', icon: 'D', label: 'Data Center' }]
})

function isOpen(label) {
  return openGroups.value.has(label)
}

function toggleGroup(label) {
  const next = new Set(openGroups.value)
  next.has(label) ? next.delete(label) : next.add(label)
  openGroups.value = next
}

function isActive(path) {
  return route.path === path || route.path.startsWith(`${path}/`)
}
</script>
