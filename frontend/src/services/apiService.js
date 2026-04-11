import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const api = axios.create({ baseURL })

export default {
  getCows: () => api.get('/cows').then(r => r.data),
  getCow: (id) => api.get(`/cows/${id}`).then(r => r.data),
  createCow: (data) => api.post('/cows', data).then(r => r.data),
  updateCow: (id, data) => api.put(`/cows/${id}`, data).then(r => r.data),
  deleteCow: (id) => api.delete(`/cows/${id}`).then(r => r.data),
  getVaccinations: (cowId) => api.get(`/cows/${cowId}/vaccinations`).then(r => r.data),
  addVaccination: (cowId, data) => api.post(`/cows/${cowId}/vaccinations`, data).then(r => r.data),
  getAlerts: () => api.get('/alerts').then(r => r.data),
  resolveAlert: (id) => api.put(`/alerts/${id}/resolve`).then(r => r.data),
}
