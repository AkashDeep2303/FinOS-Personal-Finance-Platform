<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Settings</h1>

    <div v-if="successMessage" class="rounded-lg bg-green-50 px-4 py-3 text-sm text-green-700">{{ successMessage }}</div>
    <div v-if="errorMessage" class="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorMessage }}</div>

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
                <input v-model="profile.email" type="email" readonly
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg bg-gray-50 text-gray-600 text-sm" />
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

        <!-- Active Sessions -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200">
          <div class="flex items-center justify-between gap-4 p-6 border-b border-gray-100">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Active Sessions</h2>
              <p class="mt-1 text-sm text-gray-500">Review and revoke refresh sessions signed in to your account.</p>
            </div>
            <button
              v-if="otherSessionCount > 0"
              type="button"
              :disabled="revokingSessions"
              @click="revokeOtherSessions"
              class="shrink-0 px-4 py-2 border border-red-300 text-red-700 rounded-lg hover:bg-red-50 text-sm font-medium disabled:opacity-50">
              Sign out others
            </button>
          </div>
          <div class="p-6">
            <p v-if="sessionsLoading" class="text-sm text-gray-500">Loading sessions...</p>
            <p v-else-if="sessionsError" class="text-sm text-red-600">{{ sessionsError }}</p>
            <p v-else-if="authStore.sessions.length === 0" class="text-sm text-gray-500">
              No active refresh sessions were found.
            </p>
            <ul v-else class="divide-y divide-gray-100">
              <li
                v-for="session in authStore.sessions"
                :key="session.id"
                class="flex items-center justify-between gap-4 py-4 first:pt-0 last:pb-0">
                <div>
                  <div class="flex items-center gap-2">
                    <p class="text-sm font-medium text-gray-900">Signed in {{ formatSessionDate(session.createdAt) }}</p>
                    <span
                      v-if="session.isCurrent"
                      class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                      Current
                    </span>
                  </div>
                  <p class="mt-1 text-xs text-gray-500">Expires {{ formatSessionDate(session.expiresAt) }}</p>
                </div>
                <button
                  v-if="!session.isCurrent"
                  type="button"
                  :disabled="revokingSessionId === session.id"
                  @click="revokeSession(session.id)"
                  class="px-3 py-1.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm disabled:opacity-50">
                  {{ revokingSessionId === session.id ? 'Signing out...' : 'Sign out' }}
                </button>
              </li>
            </ul>
          </div>
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
                <option value="INR">&#8377; INR - Indian Rupee</option>
                <option value="USD">$ USD - US Dollar</option>
                <option value="EUR">&#8364; EUR - Euro</option>
                <option value="GBP">&#163; GBP - British Pound</option>
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
              &#128228; Export All Data
            </button>
            <button @click="confirmDeleteAccount"
              class="w-full px-4 py-2 border border-red-300 text-red-700 rounded-lg hover:bg-red-50 text-sm font-medium">
              &#128465;&#65039; Delete Account
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const saving = ref(false)
const changingPassword = ref(false)
const successMessage = ref('')
const errorMessage = ref('')
const sessionsLoading = ref(false)
const sessionsError = ref('')
const revokingSessionId = ref(null)
const revokingSessions = ref(false)
const otherSessionCount = computed(() => authStore.sessions.filter((session) => !session.isCurrent).length)

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
      phoneNumber: profile.phone || null,
      dateOfBirth: profile.dateOfBirth || null,
      bio: profile.bio || null,
      currency: preferences.currency,
      timeZone: preferences.timezone,
    })
    populateProfile()
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

function populateProfile() {
  const user = authStore.user
  if (!user) return
  Object.assign(profile, {
    name: user.name || user.fullName || [user.firstName, user.lastName].filter(Boolean).join(' '),
    email: user.email || '',
    phone: user.phoneNumber || user.phone || '',
    dateOfBirth: user.dateOfBirth ? String(user.dateOfBirth).slice(0, 10) : '',
    bio: user.bio || ''
  })
  Object.assign(preferences, {
    currency: user.currency || 'INR',
    timezone: user.timeZone || user.timezone || 'Asia/Kolkata'
  })
}

function formatSessionDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('en-IN', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value))
}

async function loadSessions() {
  sessionsLoading.value = true
  sessionsError.value = ''
  try {
    await authStore.fetchSessions()
  } catch {
    sessionsError.value = 'Active sessions could not be loaded.'
  } finally {
    sessionsLoading.value = false
  }
}

async function revokeSession(sessionId) {
  revokingSessionId.value = sessionId
  sessionsError.value = ''
  try {
    await authStore.revokeSession(sessionId)
    successMessage.value = 'Session signed out successfully.'
  } catch {
    sessionsError.value = 'The session could not be signed out.'
  } finally {
    revokingSessionId.value = null
  }
}

async function revokeOtherSessions() {
  revokingSessions.value = true
  sessionsError.value = ''
  try {
    await authStore.revokeOtherSessions()
    successMessage.value = 'All other sessions were signed out.'
  } catch {
    sessionsError.value = 'Other sessions could not be signed out.'
  } finally {
    revokingSessions.value = false
  }
}

onMounted(async () => {
  await Promise.all([authStore.fetchProfile(), loadSessions()])
  populateProfile()
})
</script>
