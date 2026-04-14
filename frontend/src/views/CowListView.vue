<template>
  <div class="cow-list">
    <div class="page-header">
      <h1>🐄 Cow Management</h1>
      <button class="add-btn" @click="openAddModal">+ Add Cow</button>
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Gender</th>
            <th>Birth Date</th>
            <th>Body Temp</th>
            <th>Last Milking</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="cow in store.cows" :key="getCowId(cow)">
            <td>{{ getCowId(cow) }}</td>
            <td>{{ genderLabel(cow.gender) }}</td>
            <td>{{ cow.birth_date ? new Date(cow.birth_date).toLocaleDateString() : 'N/A' }}</td>
            <td :class="{ 'temp-high': (cow.body_temp || 0) > 39.5, 'temp-low': cow.body_temp != null && cow.body_temp < 38.0 }">
              {{ cow.body_temp != null ? `${cow.body_temp}°C` : 'N/A' }}
            </td>
            <td>{{ cow.last_milking ? new Date(cow.last_milking).toLocaleString() : 'N/A' }}</td>
            <td>
              <button class="edit-btn" @click="openEditModal(cow)">Edit</button>
              <button class="delete-btn" @click="confirmDelete(getCowId(cow))">Delete</button>
            </td>
          </tr>
          <tr v-if="store.cows.length === 0">
            <td colspan="6" class="empty">No cows found.</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h3>{{ editingCow ? 'Edit Cow' : 'Add Cow' }}</h3>
        <form @submit.prevent="submitForm">
          <label>Gender
            <select v-model="form.gender" required>
              <option value="female">Female</option>
              <option value="male">Male</option>
            </select>
          </label>
          <label>Birth Date <input type="date" v-model="form.birth_date" /></label>
          <label>Body Temp (°C) <input type="number" step="0.1" v-model.number="form.body_temp" /></label>
          <label>Last Milking <input type="datetime-local" v-model="form.last_milking" /></label>
          <div class="location-section">
            <span class="location-label">Location</span>
            <LocationPicker
              :modelValue="{ lat: form.latitude, lng: form.longitude }"
              @update:modelValue="v => { form.latitude = v.lat; form.longitude = v.lng }"
            />
          </div>
          <div class="modal-actions">
            <button type="submit" class="btn-primary">{{ editingCow ? 'Update' : 'Add' }}</button>
            <button type="button" @click="showModal = false">Cancel</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useCowStore } from '../stores/cowStore'
import apiService from '../services/apiService'
import { getCowId } from '../services/modelTransforms'
import LocationPicker from '../components/LocationPicker.vue'

const store = useCowStore()
const showModal = ref(false)
const editingCow = ref(null)
const form = ref({ gender: 'female', birth_date: '', body_temp: '', last_milking: '', latitude: 0, longitude: 0 })

onMounted(() => { if (!store.cows.length) store.fetchCows() })

function genderLabel(gender) {
  return gender === 'female' ? '♀️ Female' : gender === 'male' ? '♂️ Male' : 'Unknown'
}

function openAddModal() {
  editingCow.value = null
  form.value = { gender: 'female', birth_date: '', body_temp: '', last_milking: '', latitude: 0, longitude: 0 }
  showModal.value = true
}

function openEditModal(cow) {
  editingCow.value = cow
  form.value = {
    gender: cow.gender || 'female',
    birth_date: cow.birth_date ? cow.birth_date.slice(0, 10) : '',
    body_temp: cow.body_temp ?? '',
    last_milking: cow.last_milking ? cow.last_milking.slice(0, 16) : '',
    latitude: cow.latitude ?? 0,
    longitude: cow.longitude ?? 0,
  }
  showModal.value = true
}

async function submitForm() {
  try {
    if (editingCow.value) {
      await apiService.updateCow(getCowId(editingCow.value), form.value)
      await Promise.all([store.fetchCows(), store.fetchAlerts()])
    } else {
      await store.addCow(form.value)
    }
    showModal.value = false
  } catch (e) {
    console.error('submitForm error', e)
  }
}

async function confirmDelete(cowId) {
  if (confirm(`Delete cow ${cowId}?`)) {
    await store.deleteCow(cowId)
  }
}
</script>

<style scoped>
.cow-list { padding: 20px; max-width: 1200px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
h1 { margin: 0; }
.add-btn { background: #3b82f6; color: white; border: none; padding: 8px 18px; border-radius: 8px; cursor: pointer; font-size: 0.95rem; }
.table-wrap { background: white; border-radius: 10px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); overflow: hidden; }
.table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.table th, .table td { padding: 10px 14px; border-bottom: 1px solid #e5e7eb; text-align: left; }
.table th { background: #f9fafb; font-weight: 600; }
.table tr:hover { background: #f9fafb; }
.temp-high { color: #ef4444; font-weight: 700; }
.temp-low { color: #3b82f6; font-weight: 700; }
.edit-btn { background: #f59e0b; color: white; border: none; padding: 4px 10px; border-radius: 4px; cursor: pointer; margin-right: 6px; }
.delete-btn { background: #ef4444; color: white; border: none; padding: 4px 10px; border-radius: 4px; cursor: pointer; }
.empty { text-align: center; color: #9ca3af; padding: 24px; }
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; overflow-y: auto; padding: 20px; }
.modal { background: white; border-radius: 10px; padding: 24px; width: 100%; max-width: 520px; max-height: calc(100vh - 40px); overflow-y: auto; }
.modal h3 { margin: 0 0 16px; }
.modal form { display: flex; flex-direction: column; gap: 12px; }
.modal label { display: flex; flex-direction: column; gap: 4px; font-size: 0.9rem; }
.modal input, .modal select { padding: 6px 10px; border: 1px solid #d1d5db; border-radius: 6px; }
.location-section { display: flex; flex-direction: column; gap: 4px; }
.location-label { font-size: 0.9rem; font-weight: 500; }
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; }
.btn-primary { background: #3b82f6; color: white; border: none; padding: 6px 16px; border-radius: 6px; cursor: pointer; }
</style>
