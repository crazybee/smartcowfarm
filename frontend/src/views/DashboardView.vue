<template>
  <div class="dashboard">
    <AlertBanner />
    <div class="top-grid">
      <div class="map-panel">
        <h2>🗺️ Farm Map</h2>
        <CowMap :cows="store.cows" :geofence="geofence" />
      </div>
      <div class="stats-panel">
        <h2>📊 Stats</h2>
        <div class="stat-card">
          <div class="stat-value">{{ store.cows.length }}</div>
          <div class="stat-label">Total Cows</div>
        </div>
        <div class="stat-card alert-stat">
          <div class="stat-value">{{ unresolvedAlertCount }}</div>
          <div class="stat-label">Active Alerts</div>
        </div>
        <div class="stat-card vax-stat">
          <div class="stat-value">{{ store.upcomingVaccinations.length }}</div>
          <div class="stat-label">Due Vaccinations</div>
        </div>
        <div class="last-updated" v-if="store.lastUpdated">
          Last updated: {{ store.lastUpdated.toLocaleTimeString() }}
        </div>
        <div class="connection-status" :class="{ connected: store.isConnected }">
          {{ store.isConnected ? '🟢 Connected' : '🔴 Disconnected' }}
        </div>
      </div>
    </div>
    <div class="health-cards-section">
      <h2>🐄 Cow Health</h2>
      <div class="health-cards-scroll">
        <HealthCard v-for="cow in store.cows" :key="cow.id" :cow="cow" />
        <div v-if="store.cows.length === 0" class="no-data">No cows found. Add some cows to get started.</div>
      </div>
    </div>
    <div class="vax-section">
      <VaxTable :records="vaccinations" @record-vaccination="showVaxModal = true" />
    </div>
    <div v-if="showVaxModal" class="modal-overlay" @click.self="showVaxModal = false">
      <div class="modal">
        <h3>Record Vaccination</h3>
        <form @submit.prevent="submitVax">
          <label>Cow ID <input v-model="vaxForm.cow_id" required /></label>
          <label>Vaccine Name <input v-model="vaxForm.vaccine_name" required /></label>
          <label>Date Administered <input type="date" v-model="vaxForm.last_administered" required /></label>
          <label>Next Due <input type="date" v-model="vaxForm.next_due" /></label>
          <div class="modal-actions">
            <button type="submit" class="btn-primary">Save</button>
            <button type="button" @click="showVaxModal = false">Cancel</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useCowStore } from '../stores/cowStore'
import AlertBanner from '../components/AlertBanner.vue'
import CowMap from '../components/CowMap.vue'
import HealthCard from '../components/HealthCard.vue'
import VaxTable from '../components/VaxTable.vue'
import signalrService from '../services/signalrService'
import apiService from '../services/apiService'

const store = useCowStore()
const vaccinations = ref([])
const showVaxModal = ref(false)
const vaxForm = ref({ cow_id: '', vaccine_name: '', last_administered: '', next_due: '' })

const geofence = ref([])

const unresolvedAlertCount = computed(() =>
  store.alerts.filter(a => !a.resolved).length
)

async function loadData() {
  await Promise.allSettled([store.fetchCows(), store.fetchAlerts()])
}

async function loadVaccinations() {
  try {
    const allVax = await Promise.all(
      store.cows.map(c => apiService.getVaccinations(c.id).catch(() => []))
    )
    vaccinations.value = allVax.flat()
  } catch (e) {
    vaccinations.value = []
  }
}

async function submitVax() {
  try {
    await apiService.addVaccination(vaxForm.value.cow_id, vaxForm.value)
    await loadVaccinations()
    showVaxModal.value = false
    vaxForm.value = { cow_id: '', vaccine_name: '', last_administered: '', next_due: '' }
  } catch (e) {
    console.error('submitVax error', e)
  }
}

onMounted(async () => {
  await loadData()
  await loadVaccinations()
  signalrService.onCowUpdate(payload => {
    store.updateCowTelemetry(payload)
    store.isConnected = true
  })
  signalrService.onAlert(alert => {
    store.alerts.push(alert)
  })
  signalrService.start()
    .then(() => { store.isConnected = true })
    .catch(() => { store.isConnected = false })
})

onUnmounted(() => {
  signalrService.stop()
  store.isConnected = false
})
</script>

<style scoped>
.dashboard { padding: 20px; max-width: 1400px; margin: 0 auto; }
.top-grid { display: grid; grid-template-columns: 2fr 1fr; gap: 20px; margin-bottom: 20px; }
.map-panel, .stats-panel { background: white; border-radius: 10px; padding: 16px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); }
h2 { margin: 0 0 12px; font-size: 1.1rem; }
.stat-card { background: #f0f9ff; border-radius: 8px; padding: 16px; margin-bottom: 10px; text-align: center; }
.alert-stat { background: #fef2f2; }
.vax-stat { background: #f0fdf4; }
.stat-value { font-size: 2rem; font-weight: 700; }
.stat-label { font-size: 0.85rem; color: #6b7280; margin-top: 4px; }
.last-updated, .connection-status { font-size: 0.8rem; color: #6b7280; text-align: center; margin-top: 6px; }
.connected { color: #16a34a; }
.health-cards-section { margin-bottom: 20px; }
.health-cards-scroll { display: flex; gap: 16px; overflow-x: auto; padding-bottom: 8px; }
.no-data { color: #9ca3af; padding: 20px; }
.vax-section { margin-bottom: 20px; }
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.5);
  display: flex; align-items: center; justify-content: center; z-index: 1000;
}
.modal { background: white; border-radius: 10px; padding: 24px; min-width: 380px; }
.modal h3 { margin: 0 0 16px; }
.modal form { display: flex; flex-direction: column; gap: 12px; }
.modal label { display: flex; flex-direction: column; gap: 4px; font-size: 0.9rem; }
.modal input { padding: 6px 10px; border: 1px solid #d1d5db; border-radius: 6px; }
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; }
.btn-primary { background: #3b82f6; color: white; border: none; padding: 6px 16px; border-radius: 6px; cursor: pointer; }
</style>
