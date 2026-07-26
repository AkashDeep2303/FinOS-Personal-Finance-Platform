import api from './axios'

export const aiAssistantApi = {
  getConversations(userId, count = 20) {
    return api.get('/api/aiassistant/conversations', {
      params: { userId, count }
    })
  },

  createConversation(data) {
    return api.post('/api/aiassistant/conversations', data)
  },

  getMessages(conversationId, userId) {
    return api.get(`/api/aiassistant/conversations/${conversationId}/messages`, {
      params: { userId }
    })
  },

  sendMessage(data) {
    return api.post('/api/aiassistant/messages', data)
  }
}
