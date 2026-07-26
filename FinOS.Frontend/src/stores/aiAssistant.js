import { defineStore } from 'pinia'
import { aiAssistantApi } from '../api/aiAssistant'
import { useAuthStore } from './auth'

const QUERY_TYPES = {
  affordability: 0,
  spending: 1,
  loan: 2,
  investment: 3,
  general: 4
}

function currentUserId() {
  const user = useAuthStore().user
  const storeUserId = user?.id ?? user?.userId
  if (storeUserId) return Number(storeUserId)

  try {
    const token = localStorage.getItem('finos_token')
    if (!token) return undefined

    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const claims = JSON.parse(atob(payload))
    const claimUserId = claims.sub
      ?? claims.nameid
      ?? claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']

    return claimUserId ? Number(claimUserId) : undefined
  } catch {
    return undefined
  }
}

function inferQueryType(content) {
  const text = content.toLowerCase()
  if (/\b(invest|sip|epf|mutual fund|portfolio|stock)\b/.test(text)) return QUERY_TYPES.investment
  if (/\b(loan|emi|prepay|debt)\b/.test(text)) return QUERY_TYPES.loan
  if (/\b(spend|expense|breakdown|transaction)\b/.test(text)) return QUERY_TYPES.spending
  if (/\b(afford|buy|purchase|can i)\b/.test(text)) return QUERY_TYPES.affordability
  return QUERY_TYPES.general
}

function normalizeMessage(message) {
  return {
    id: message.id,
    role: message.role === 0 || message.role === 'User' ? 'user' : 'assistant',
    content: message.content,
    timestamp: message.createdAt
  }
}

export const useAiAssistantStore = defineStore('aiAssistant', {
  state: () => ({
    conversation: null,
    messages: [],
    loading: false,
    sending: false,
    error: null
  }),

  actions: {
    async initialize() {
      this.loading = true
      this.error = null

      try {
        const userId = currentUserId()
        if (!userId) throw new Error('No authenticated user is available')

        const response = await aiAssistantApi.getConversations(userId, 1)
        const conversations = response.data?.data ?? []
        this.conversation = conversations[0] ?? null

        if (!this.conversation) {
          this.messages = []
          return
        }

        const messagesResponse = await aiAssistantApi.getMessages(this.conversation.id, userId)
        const messages = messagesResponse.data?.data ?? []
        this.messages = messages.map(normalizeMessage)
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'Failed to load AI conversation'
        throw err
      } finally {
        this.loading = false
      }
    },

    async ensureConversation(firstMessage) {
      if (this.conversation) return this.conversation

      const userId = currentUserId()
      if (!userId) throw new Error('No authenticated user is available')

      const title = firstMessage.trim().slice(0, 100) || 'Financial conversation'
      const response = await aiAssistantApi.createConversation({ userId, title })
      this.conversation = response.data?.data ?? response.data
      return this.conversation
    },

    async send(content) {
      const trimmedContent = content.trim()
      if (!trimmedContent || this.sending) return

      this.sending = true
      this.error = null

      try {
        const userId = currentUserId()
        if (!userId) throw new Error('No authenticated user is available')

        const conversation = await this.ensureConversation(trimmedContent)
        const response = await aiAssistantApi.sendMessage({
          conversationId: conversation.id,
          userId,
          content: trimmedContent,
          queryType: inferQueryType(trimmedContent)
        })

        const result = response.data?.data ?? response.data
        this.messages.push(
          normalizeMessage(result.userMessage),
          normalizeMessage(result.assistantMessage)
        )
      } catch (err) {
        this.error = err.response?.data?.message || err.message || 'The AI Assistant could not process your request'
        throw err
      } finally {
        this.sending = false
      }
    },

    startNewConversation() {
      this.conversation = null
      this.messages = []
      this.error = null
    }
  }
})
