import api from './axios'
export const taxApi = {
  profile: fy => api.get(`/api/corefinance/tax/profiles/${fy}`),
  rules: fy => api.get(`/api/corefinance/tax/rules/${fy}`),
  save: (fy, data) => api.put(`/api/corefinance/tax/profiles/${fy}`, data),
  calculate: fy => api.post(`/api/corefinance/tax/projections/${fy}/calculate`)
}
