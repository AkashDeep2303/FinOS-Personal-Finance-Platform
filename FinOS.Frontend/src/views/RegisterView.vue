<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-primary-800 to-slate-900 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full">
      <div class="text-center mb-8">
        <h1 class="text-4xl font-bold text-white tracking-tight">💰 FinOS</h1>
        <p class="mt-2 text-primary-200 text-sm">Create your account</p>
      </div>
      <div class="bg-white rounded-2xl shadow-2xl p-8">
        <h2 class="text-2xl font-bold text-gray-900 text-center mb-6">Get started for free</h2>
        <div v-if="error" class="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {{ error }}
        </div>
        <form @submit.prevent="handleRegister" class="space-y-4">
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label for="firstName" class="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input
                id="firstName"
                v-model.trim="form.firstName"
                type="text"
                required
                maxlength="50"
                autocomplete="given-name"
                class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
                placeholder="Rahul"
              />
            </div>
            <div>
              <label for="lastName" class="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input
                id="lastName"
                v-model.trim="form.lastName"
                type="text"
                required
                maxlength="50"
                autocomplete="family-name"
                class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
                placeholder="Sharma"
              />
            </div>
          </div>
          <div>
            <label for="email" class="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              required
              autocomplete="email"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
              placeholder="rahul@example.com"
            />
          </div>
          <div>
            <label for="password" class="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              required
              minlength="8"
              maxlength="100"
              pattern="(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,100}"
              title="Use 8–100 characters with uppercase, lowercase, a number, and a special character."
              autocomplete="new-password"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
              placeholder="Min 8 characters"
            />
            <p class="mt-1 text-xs text-gray-500">Use uppercase, lowercase, a number, and a special character.</p>
          </div>
          <div>
            <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
            <input
              id="confirmPassword"
              v-model="form.confirmPassword"
              type="password"
              required
              autocomplete="new-password"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm"
              placeholder="Re-enter password"
            />
          </div>
          <div>
            <label for="currency" class="block text-sm font-medium text-gray-700 mb-1">Preferred Currency</label>
            <select
              id="currency"
              v-model="form.currency"
              class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors text-sm bg-white"
            >
              <option value="INR">₹ INR - Indian Rupee</option>
              <option value="USD">$ USD - US Dollar</option>
              <option value="EUR">€ EUR - Euro</option>
              <option value="GBP">£ GBP - British Pound</option>
            </select>
          </div>
          <div class="flex items-start">
            <input
              id="terms"
              v-model="form.agreeTerms"
              type="checkbox"
              required
              class="rounded border-gray-300 text-primary-600 focus:ring-primary-500 mt-1"
            />
            <label for="terms" class="ml-2 text-sm text-gray-600">
              I agree to the <a href="#" class="text-primary-600 hover:text-primary-700">Terms of Service</a> and <a href="#" class="text-primary-600 hover:text-primary-700">Privacy Policy</a>
            </label>
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
            {{ loading ? 'Creating account...' : 'Create Account' }}
          </button>
        </form>
        <p class="mt-6 text-center text-sm text-gray-600">
          Already have an account?
          <router-link to="/login" class="text-primary-600 hover:text-primary-700 font-medium">Sign in</router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const loading = ref(false)
const error = ref(null)

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  currency: 'INR',
  agreeTerms: false
})

async function handleRegister() {
  if (form.password !== form.confirmPassword) {
    error.value = 'Passwords do not match'
    return
  }
  if (!form.agreeTerms) {
    error.value = 'You must agree to the terms and conditions'
    return
  }

  loading.value = true
  error.value = null
  try {
    await authStore.register({
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email,
      password: form.password,
      confirmPassword: form.confirmPassword,
      currency: form.currency
    })
  } catch (err) {
    error.value = authStore.error || 'Registration failed. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>
