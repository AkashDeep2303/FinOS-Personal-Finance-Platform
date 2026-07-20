<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Settings</h1>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Profile Section -->
      <div class="lg:col-span-2 space-y-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <h2 class="text-lg font-semibold text-gray-900">Profile Information</h2>
          </div>
          <form @submit.prevent="saveProfile" class="p-6 space-y-4">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                <input v-model="profile.name" type="text" required
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
                <input v-model="profile.email" type="email" required
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Phone Number</label>
                <input v-model="profile.phone" type="tel"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                  placeholder="+91 XXXXX XXXXX" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Date of Birth</label>
                <input v-model="profile.dateOfBirth" type="date"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Bio</label>
              <textarea v-model="profile.bio" rows="3"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                placeholder="Tell us about yourself..."></textarea>
            </div>
            <div class="flex justify-end">
              <button type="submit" :disabled="saving"
                class="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
                {{ saving ? 'Saving...' : 'Save Changes' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Change Password -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <h2 class="text-lg font-semibold text-gray-900">Change Password</h2>
          </div>
          <form @submit.prevent="changePassword" class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
              <input v-model="passwordForm.currentPassword" type="password" required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
            </div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                <input v-model="passwordForm.newPassword" type="password" required
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Confirm New Password</label>
                <input v-model="passwordForm.confirmPassword" type="password" required
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm" />
              </div>
            </div>
            <div class="flex justify-end">
              <button type="submit" :disabled="changingPassword"
                class="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50">
                {{ changingPassword ? 'Changing...' : 'Change Password' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Sidebar Settings -->
      <div class="space-y-6">
        <!-- Preferences -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <h2 class="text-lg font-semibold text-gray-900">Preferences</h2>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Currency</label>
              <select v-model="preferences.currency"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
                <option value="INR">Ã¢â€šÂ¹ INR - Indian Rupee</option>
                <option value="USD">$ USD - US Dollar</option>
                <option value="EUR">Ã¢â€šÂ¬ EUR - Euro</option>
                <option value="GBP">Ã‚Â£ GBP - British Pound</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Timezone</label>
              <select v-model="preferences.timezone"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
                <option value="Asia/Kolkata">Asia/Kolkata (IST +5:30)</option>
                <option value="America/New_York">America/New_York (EST -5:00)</option>
                <option value="Europe/London">Europe/London (GMT +0:00)</option>
                <option value="Asia/Dubai">Asia/Dubai (GST +4:00)</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Date Format</label>
              <select v-model="preferences.dateFormat"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
                <option value="dd/MM/yyyy">DD/MM/YYYY</option>
                <option value="MM/dd/yyyy">MM/DD/YYYY</option>
                <option value="yyyy-MM-dd">YYYY-MM-DD</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Financial Year Start</label>
              <select v-model="preferences.financialYearStart"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 text-sm bg-white">
                <option value="April">April (India)</option>
                <option value="January">January</option>
                <option value="July">July (Australia)</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Notifications -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-100">
            <h2 class="text-lg font-semibold text-gray-900">Notifications</h2>
          </div>
          <div class="p-6 space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-gray-900">Budget Alerts</p>
                <p class="text-xs text-gray-500">Notify when spending exceeds budget</p>
              </div>
              <button @click="notifications.budgetAlert = !notifications.budgetAlert"
                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                :class="notifications.budgetAlert ? 'bg-primary-600' : 'bg-gray-200'">
                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                  :class="notifications.budgetAlert ? 'translate-x-6' : 'translate-x-1'"></span>
              </button>
            </div>
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-gray-900">EMI Reminders</p>
                <p class="text-xs text-gray-500">Remind before EMI due dates</p>
              </div>
              <button @click="notifications.emiReminder = !notifications.emiReminder"
                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                :class="notifications.emiReminder ? 'bg-primary-600' : 'bg-gray-200'">
                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                  :class="notifications.emiReminder ? 'translate-x-6' : 'translate-x-1'"></span>
              </button>
            </div>
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-gray-900">Goal Milestones</p>
                <p class="text-xs text-gray-500">Celebrate when you reach milestones</p>
              </div>
              <button @click="notifications.goalMilestone = !notifications.goalMilestone"
                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                :class="notifications.goalMilestone ? 'bg-primary-600' : 'bg-gray-200'">
                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                  :class="notifications.goalMilestone ? 'translate-x-6' : 'translate-x-1'"></span>
              </button>
            </div>
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-gray-900">Weekly Summary</p>
                <p class="text-xs text-gray-500">Get a weekly financial summary</p>
              </div>
              <button @click="notifications.weeklySummary = !notifications.weeklySummary"
                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                :class="notifications.weeklySummary ? 'bg-primary-600' : 'bg-gray-200'">
                <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                  :class="notifications.weeklySummary ? 'translate-x-6' : 'translate-x-1'"></span>
              </button>
            </div>
          </div>
        </div>

        <!-- Danger Zone -->
        <div class="bg-white rounded-xl shadow-sm border border-red-200">
          <div class="p-6 border-b border-red-100">
            <h2 class="text-lg font-semibold text-red-600">Danger Zone</h2>
          </div>
          <div class="p-6 space-y-3">
            <button @click="exportData"
              class="w-full px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm font-medium">
              Ã°Å¸â€œÂ¥ Export All Data
            </button>
            <button @click="confirmDeleteAccount"
              class="w-full px-4 py-2 border border-red-300 text-red-700 rounded-lg hover:bg-red-50 text-sm font-medium">
              Ã°Å¸â€”â€˜Ã¯Â¸Â Delete Account
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const saving = ref(false)
const changingPassword = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const profile = reactive({
  name: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  bio: ''
})

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const preferences = reactive({
  currency: 'INR',
  timezone: 'Asia/Kolkata',
  dateFormat: 'dd/MM/yyyy',
  financialYearStart: 'April'
})

const notifications = reactive({
  budgetAlert: true,
  emiReminder: true,
  goalMilestone: true,
  weeklySummary: false
})

async function saveProfile() {
  saving.value = true
  try {
    const [firstName = '', ...lastNameParts] = profile.name.trim().split(/\s+/)
    await authStore.updateProfile({
      firstName,
      lastName: lastNameParts.join(' '),
      phoneNumber: profile.phone || null
    })
    successMessage.value = 'Profile updated successfully!'
  } catch (err) {
    errorMessage.value = 'Failed to update profile'
  } finally {
    saving.value = false
  }
}

async function changePassword() {
  if (passwordForm.newPassword !== passwordForm.confirmPassword) {
    errorMessage.value = 'Passwords do not match'
    return
  }
  changingPassword.value = true
  try {
    await authStore.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
      confirmNewPassword: passwordForm.confirmPassword
    })
    Object.assign(passwordForm, { currentPassword: '', newPassword: '', confirmPassword: '' })
    successMessage.value = 'Password changed successfully!'
  } catch (err) {
    errorMessage.value = 'Failed to change password'
  } finally {
    changingPassword.value = false
  }
}

function exportData() {
  alert('Data export will be available soon. Your data will be downloaded as a JSON file.')
}

function confirmDeleteAccount() {
  if (confirm('Are you sure you want to delete your account? This action cannot be undone.')) {
    alert('Account deletion request submitted. You will receive a confirmation email.')
  }
}

onMounted(() => {
  if (authStore.user) {
    Object.assign(profile, {
      name: authStore.user.name || '',
      email: authStore.user.email || '',
      phone: authStore.user.phoneNumber || '',
      dateOfBirth: authStore.user.dateOfBirth || '',
      bio: authStore.user.bio || ''
    })
  }
})
</script>
