import axios from 'axios'
import {
  normalizeAlert,
  normalizeCow,
  normalizeVaccination,
} from './modelTransforms'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const api = axios.create({ baseURL })

function toCowPayload(data) {
  return {
    gender: data.gender,
    birthDate: (data.birthDate ?? data.birth_date) || null,
    bodyTemp: data.bodyTemp ?? data.body_temp ?? 0,
    latitude: data.latitude ?? 0,
    longitude: data.longitude ?? 0,
    lastMilking: (data.lastMilking ?? data.last_milking) || null,
    nextVaxDue: (data.nextVaxDue ?? data.next_vax_due) || null,
  }
}

function toVaccinationPayload(data) {
  return {
    vaccineName: data.vaccineName ?? data.vaccine_name,
    administeredDate: data.administeredDate ?? data.last_administered,
    nextDueDate: (data.nextDueDate ?? data.next_due) || null,
  }
}

export default {
  getCows: () => api.get('/cows').then(r => r.data.map(normalizeCow)),
  getCow: (id) => api.get(`/cows/${id}`).then(r => normalizeCow(r.data)),
  createCow: (data) => api.post('/cows', toCowPayload(data)).then(r => normalizeCow(r.data)),
  updateCow: (id, data) => api.put(`/cows/${id}`, toCowPayload(data)).then(r => normalizeCow(r.data)),
  deleteCow: (id) => api.delete(`/cows/${id}`).then(r => r.data),
  getVaccinations: (cowId) => api.get(`/cows/${cowId}/vaccinations`).then(r => r.data.map(normalizeVaccination)),
  addVaccination: (cowId, data) => api.post(`/cows/${cowId}/vaccinations`, toVaccinationPayload(data)).then(r => normalizeVaccination(r.data)),
  getAlerts: () => api.get('/alerts').then(r => r.data.map(normalizeAlert)),
  resolveAlert: (id) => api.put(`/alerts/${id}/resolve`).then(r => normalizeAlert(r.data)),
}
