<template>
  <div v-if="isAuthRoute" class="min-h-screen bg-gray-50">
    <router-view />
  </div>
  <div v-else class="min-h-screen flex bg-gray-50">
    <Sidebar :collapsed="sidebarCollapsed" @toggle="sidebarCollapsed = !sidebarCollapsed" />
    <div class="flex-1 flex flex-col min-h-screen transition-all duration-300"
         :class="sidebarCollapsed ? 'ml-16' : 'ml-64'">
      <Navbar @toggle-sidebar="sidebarCollapsed = !sidebarCollapsed" />
      <main class="flex-1 p-6 overflow-auto">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import Sidebar from './components/Sidebar.vue'
import Navbar from './components/Navbar.vue'

const sidebarCollapsed = ref(false)
const route = useRoute()

const isAuthRoute = computed(() => {
  return ['/login', '/register'].includes(route.path)
})
</script>
