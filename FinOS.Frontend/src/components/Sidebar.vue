<template>
  <aside
    class="fixed left-0 top-0 h-full bg-slate-800 text-white z-40 transition-all duration-300 flex flex-col"
    :class="collapsed ? 'w-16' : 'w-64'"
  >
    <!-- Logo -->
    <div class="flex items-center h-16 px-4 border-b border-slate-700">
      <span class="text-2xl">💰</span>
      <span v-if="!collapsed" class="ml-3 text-xl font-bold tracking-tight">FinOS</span>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 overflow-y-auto py-4 space-y-1 px-2">
      <router-link
        v-for="item in navItems"
        :key="item.path"
        :to="item.path"
        class="flex items-center px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200"
        :class="isActive(item.path)
          ? 'bg-primary-600 text-white shadow-lg shadow-primary-600/30'
          : 'text-slate-300 hover:bg-slate-700 hover:text-white'"
      >
        <span class="text-lg flex-shrink-0 w-8 text-center">{{ item.icon }}</span>
        <span v-if="!collapsed" class="ml-3 truncate">{{ item.label }}</span>
      </router-link>
    </nav>

    <!-- Collapse Toggle -->
    <div class="p-3 border-t border-slate-700">
      <button
        @click="$emit('toggle')"
        class="w-full flex items-center justify-center py-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-700 transition-colors"
      >
        <span class="text-lg">{{ collapsed ? '→' : '←' }}</span>
        <span v-if="!collapsed" class="ml-2 text-sm">Collapse</span>
      </button>
    </div>
  </aside>
</template>

<script setup>
import { useRoute } from 'vue-router'

defineProps({
  collapsed: {
    type: Boolean,
    default: false
  }
})

defineEmits(['toggle'])

const route = useRoute()

const navItems = [
  { path: '/dashboard', icon: '📊', label: 'Dashboard' },
  { path: '/accounts', icon: '🏦', label: 'Accounts' },
  { path: '/transactions', icon: '💳', label: 'Transactions' },
  { path: '/budgets', icon: '📋', label: 'Budgets' },
  { path: '/investments', icon: '📈', label: 'Investments' },
  { path: '/loans', icon: '🏠', label: 'Loans' },
  { path: '/goals', icon: '🎯', label: 'Goals' },
  { path: '/analytics', icon: '📉', label: 'Analytics' },
  { path: '/ai-assistant', icon: '🤖', label: 'AI Assistant' },
  { path: '/settings', icon: '⚙️', label: 'Settings' }
]

function isActive(path) {
  return route.path === path || route.path.startsWith(path + '/')
}
</script>
