<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">AI Assistant</h1>
      <button @click="clearChat"
        class="text-sm text-gray-500 hover:text-gray-700 font-medium">
        🗑️ Clear Chat
      </button>
    </div>

    <!-- Chat Container -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 flex flex-col" style="height: calc(100vh - 220px)">
      <!-- Messages Area -->
      <div ref="messagesContainer" class="flex-1 overflow-y-auto p-6 space-y-4">
        <!-- Welcome Message -->
        <div v-if="!isLoading && messages.length === 0" class="text-center py-12">
          <p class="text-5xl mb-4">🤖</p>
          <h3 class="text-lg font-semibold text-gray-900 mb-2">FinOS AI Assistant</h3>
          <p class="text-gray-500 mb-6 max-w-md mx-auto">
            Ask me anything about your finances — spending insights, budget advice, investment suggestions, and more!
          </p>
          <div class="flex flex-wrap justify-center gap-2">
            <button v-for="suggestion in suggestions" :key="suggestion"
              @click="sendMessage(suggestion)"
              class="px-3 py-1.5 bg-primary-50 text-primary-700 rounded-full text-sm hover:bg-primary-100 transition-colors">
              {{ suggestion }}
            </button>
          </div>
        </div>

        <div v-if="isLoading" class="flex h-full items-center justify-center text-sm text-gray-500">
          Loading your conversation...
        </div>

        <!-- Chat Messages -->
        <div v-for="(msg, index) in messages" :key="index"
          class="flex" :class="msg.role === 'user' ? 'justify-end' : 'justify-start'">
          <div class="max-w-[75%] flex items-start space-x-2"
            :class="msg.role === 'user' ? 'flex-row-reverse space-x-reverse' : ''">
            <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm flex-shrink-0"
              :class="msg.role === 'user' ? 'bg-primary-600 text-white' : 'bg-gray-100'">
              {{ msg.role === 'user' ? '👤' : '🤖' }}
            </div>
            <div class="px-4 py-3 rounded-2xl text-sm leading-relaxed"
              :class="msg.role === 'user'
                ? 'bg-primary-600 text-white rounded-tr-sm'
                : 'bg-gray-100 text-gray-900 rounded-tl-sm'">
              <p class="whitespace-pre-wrap">{{ msg.content }}</p>
              <p class="text-xs mt-1 opacity-60">{{ formatTime(msg.timestamp) }}</p>
            </div>
          </div>
        </div>

        <!-- Typing Indicator -->
        <div v-if="isTyping" class="flex justify-start">
          <div class="flex items-start space-x-2">
            <div class="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center text-sm">🤖</div>
            <div class="bg-gray-100 px-4 py-3 rounded-2xl rounded-tl-sm">
              <div class="flex space-x-1">
                <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0ms"></div>
                <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 150ms"></div>
                <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 300ms"></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="border-t border-gray-200 p-4">
        <form @submit.prevent="handleSend" class="flex items-center space-x-3">
          <input
            v-model="inputMessage"
            type="text"
            placeholder="Ask about your finances..."
            class="flex-1 px-4 py-3 border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
            :disabled="isTyping || isLoading"
            @keydown.enter.prevent="handleSend"
          />
          <button
            type="submit"
            :disabled="!inputMessage.trim() || isTyping || isLoading"
            class="px-4 py-3 bg-primary-600 text-white rounded-xl hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"></path>
            </svg>
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onMounted } from 'vue'
import { format } from 'date-fns'
import { useAiAssistantStore } from '../stores/aiAssistant'

const aiAssistantStore = useAiAssistantStore()
const messages = computed(() => aiAssistantStore.messages)
const inputMessage = ref('')
const isTyping = computed(() => aiAssistantStore.sending)
const isLoading = computed(() => aiAssistantStore.loading)
const messagesContainer = ref(null)

const suggestions = [
  'How am I doing financially?',
  'Show my spending breakdown',
  'Budget tips for this month',
  'Investment advice',
  'How to save more?'
]

function formatTime(timestamp) {
  if (!timestamp) return ''
  return format(new Date(timestamp), 'HH:mm')
}

function scrollToBottom() {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
    }
  })
}

async function sendMessage(text) {
  if (!text.trim()) return

  inputMessage.value = ''

  try {
    await aiAssistantStore.send(text)
  } catch (err) {
    aiAssistantStore.messages.push({
      role: 'assistant',
      content: aiAssistantStore.error || 'I\'m sorry, I couldn\'t process your request. Please try again later.',
      timestamp: new Date().toISOString()
    })
  } finally {
    scrollToBottom()
  }
}

function handleSend() {
  sendMessage(inputMessage.value)
}

function clearChat() {
  aiAssistantStore.startNewConversation()
}

onMounted(async () => {
  try {
    await aiAssistantStore.initialize()
  } catch {
    // The store exposes a user-readable error when loading fails.
  }
  scrollToBottom()
})
</script>
