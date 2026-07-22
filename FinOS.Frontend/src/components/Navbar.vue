<template>
  <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 sticky top-0 z-30">
    <div class="flex items-center space-x-4">
      <button @click="$emit('toggle-sidebar')" class="text-gray-500 hover:text-gray-700 transition-colors lg:hidden">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>
      <nav class="flex items-center space-x-2 text-sm">
        <span class="text-gray-400">FinOS</span>
        <span class="text-gray-300">/</span>
        <span class="text-gray-900 font-medium">{{ currentPageName }}</span>
      </nav>
    </div>

    <div class="flex items-center space-x-4">
      <!-- Search -->
      <div class="hidden md:block relative">
        <input
          type="text"
          placeholder="Search..."
          class="w-48 pl-8 pr-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        />
        <svg class="w-4 h-4 absolute left-2.5 top-2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
        </svg>
      </div>

      <!-- Notification Bell -->
      <div class="relative" ref="notificationRef">
        <button
          type="button"
          @click="showNotifications = !showNotifications"
          aria-label="Notifications"
          :aria-expanded="showNotifications"
          class="relative p-2 text-gray-400 hover:text-gray-600 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
          </svg>
          <span class="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
        </button>
        <div
          v-if="showNotifications"
          class="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50"
          role="dialog"
          aria-label="Notifications"
        >
          <div class="px-4 py-2 border-b border-gray-100 flex items-center justify-between">
            <p class="text-sm font-semibold text-gray-900">Notifications</p>
            <button type="button" class="text-xs text-gray-400" @click="showNotifications = false">Close</button>
          </div>
          <p class="px-4 py-6 text-sm text-gray-500 text-center">No new notifications</p>
        </div>
      </div>

      <!-- User Dropdown -->
      <div class="relative" ref="dropdownRef">
        <button @click="showDropdown = !showDropdown"
          class="flex items-center space-x-2 hover:bg-gray-50 rounded-lg px-2 py-1.5 transition-colors">
          <div class="w-8 h-8 rounded-full bg-primary-600 flex items-center justify-center text-white text-sm font-medium">
            {{ userInitial }}
          </div>
          <span class="hidden md:block text-sm font-medium text-gray-700">{{ authStore.userName || 'User' }}</span>
          <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
          </svg>
        </button>

        <!-- Dropdown Menu -->
        <div v-if="showDropdown"
          class="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
          <div class="px-4 py-2 border-b border-gray-100">
            <p class="text-sm font-medium text-gray-900">{{ authStore.userName }}</p>
            <p class="text-xs text-gray-500">{{ authStore.userEmail }}</p>
          </div>
          <router-link to="/settings" @click="showDropdown = false"
            class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
            ⚙️ Settings
          </router-link>
          <button @click="handleLogout"
            class="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50">
            🚪 Logout
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

defineEmits(['toggle-sidebar'])

const authStore = useAuthStore()
const route = useRoute()
const showDropdown = ref(false)
const showNotifications = ref(false)
const dropdownRef = ref(null)
const notificationRef = ref(null)

const userInitial = computed(() => {
  const name = authStore.userName || 'U'
  return name.charAt(0).toUpperCase()
})

const currentPageName = computed(() => {
  const names = {
    '/dashboard': 'Dashboard',
    '/accounts': 'Accounts',
    '/transactions': 'Transactions',
    '/categories': 'Categories',
    '/budgets': 'Budgets',
    '/investments': 'Investments',
    '/loans': 'Loans',
    '/goals': 'Goals',
    '/analytics': 'Analytics',
    '/ai-assistant': 'AI Assistant',
    '/settings': 'Settings'
  }
  return names[route.path] || 'Dashboard'
})

function handleLogout() {
  showDropdown.value = false
  authStore.logout()
}

function handleClickOutside(event) {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target)) {
    showDropdown.value = false
  }
  if (notificationRef.value && !notificationRef.value.contains(event.target)) {
    showNotifications.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>
