import api from './axios'

export const categoriesApi = {
  list: () => api.get('/api/corefinance/categories'),
  create: category => api.post('/api/corefinance/categories', category),
  update: (id, category) => api.put(`/api/corefinance/categories/${id}`, category)
}
