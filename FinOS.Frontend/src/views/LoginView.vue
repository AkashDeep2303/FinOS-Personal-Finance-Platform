<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-primary-800 to-slate-900 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full">
      <div class="text-center mb-8">
        <h1 class="text-4xl font-bold text-white tracking-tight">💰 FinOS</h1>
        <p class="mt-2 text-primary-200 text-sm">Personal Finance Manager</p>
      </div>
      <div class="bg-white rounded-2xl shadow-2xl p-8">
        <h2 class="text-2xl font-bold text-gray-900 text-center mb-6">Sign in to your account</h2>
        <div v-if="error" class="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {{ error }}
        </div>
        <form @submit.prevent="handleLogin" class="space-y-5">
          <div>
            <label for="email" class="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              required
              autocomplete="email"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
              placeholder="you@example.com"
            />
          </div>
          <div v-if="twoFactorRequired">
            <label for="twoFactorCode" class="block text-sm font-medium text-gray-700 mb-1">Authenticator code</label>
            <input id="twoFactorCode" v-model.trim="form.twoFactorCode" inputmode="numeric"
              autocomplete="one-time-code" pattern="[0-9]{6}" maxlength="6" required
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
              placeholder="6-digit code" />
          </div>
          <div>
            <label for="password" class="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              required
              autocomplete="current-password"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
              placeholder="••••••••"
            />
          </div>
          <div class="flex items-center justify-between">
            <label class="flex items-center">
              <input type="checkbox" v-model="form.rememberMe" class="rounded border-gray-300 text-primary-600 focus:ring-primary-500" />
              <span class="ml-2 text-sm text-gray-600">Remember me</span>
            </label>
            <a href="#" class="text-sm text-primary-600 hover:text-primary-700 font-medium">Forgot password?</a>
          </div>
          <button
            type="submit"
            :disabled="loading"
            class="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 font-medium text-sm disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <svg v-if="loading" class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            {{ loading ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>
        <p class="mt-6 text-center text-sm text-gray-600">
          Don't have an account?
          <router-link to="/register" class="text-primary-600 hover:text-primary-700 font-medium">Create one</router-link>
        </p>
      </div>
      <p class="mt-4 text-center text-xs text-primary-300">₹ Indian Rupee · All financial data in INR</p>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const loading = ref(false)
const error = ref(null)
const twoFactorRequired = ref(false)

const form = reactive({
  email: '',
  password: '',
  rememberMe: false,
  twoFactorCode: ''
})

async function handleLogin() {
  loading.value = true
  error.value = null
  try {
    const result = await authStore.login({
      email: form.email,
      password: form.password,
      twoFactorCode: twoFactorRequired.value ? form.twoFactorCode : null
    })
    if (result?.twoFactorRequired) {
      twoFactorRequired.value = true
      form.twoFactorCode = ''
    }
  } catch (err) {
    error.value = authStore.error || 'Login failed. Please check your credentials.'
  } finally {
    loading.value = false
  }
}
</script>
