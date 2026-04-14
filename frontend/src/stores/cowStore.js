import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiService from '../services/apiService'
import { getCowId } from '../services/modelTransforms'

export const useCowStore = defineStore('cow', () => {
  const cows = ref([])
  const alerts = ref([])
  const isConnected = ref(false)
  const lastUpdated = ref(null)

  async function fetchCows() {
    try {
      const data = await apiService.getCows()
      cows.value = data
      lastUpdated.value = new Date()
    } catch (e) {
      console.error('fetchCows error', e)
    }
  }

  async function fetchAlerts() {
    try {
      const data = await apiService.getAlerts()
      alerts.value = data
    } catch (e) {
      console.error('fetchAlerts error', e)
    }
  }

  function updateCowTelemetry(payload) {
    const payloadId = getCowId(payload)
    const cow = cows.value.find(c => getCowId(c) === payloadId)
    if (cow) {
      if (payload.body_temp !== undefined) cow.body_temp = payload.body_temp
      if (payload.latitude !== undefined) cow.latitude = payload.latitude
      if (payload.longitude !== undefined) cow.longitude = payload.longitude
      if (payload.location !== undefined) cow.location = payload.location
      lastUpdated.value = new Date()
    }
  }

  async function resolveAlert(alertId) {
    try {
      await apiService.resolveAlert(alertId)
      const idx = alerts.value.findIndex(a => a.id === alertId)
      if (idx !== -1) alerts.value[idx].resolved = true
    } catch (e) {
      console.error('resolveAlert error', e)
    }
  }

  async function addCow(cowData) {
    try {
      const created = await apiService.createCow(cowData)
      cows.value.push(created)
      await fetchAlerts()
    } catch (e) {
      console.error('addCow error', e)
    }
  }

  async function deleteCow(cowId) {
    try {
      await apiService.deleteCow(cowId)
      cows.value = cows.value.filter(c => getCowId(c) !== cowId)
    } catch (e) {
      console.error('deleteCow error', e)
    }
  }

  const cowsInAlert = computed(() => {
    const alertCowIds = new Set(
      alerts.value.filter(a => !a.resolved).map(a => a.cow_id)
    )
    return cows.value.filter(c => alertCowIds.has(getCowId(c)))
  })

  const alertsByType = computed(() => {
    return alerts.value.reduce((acc, alert) => {
      const type = alert.type || 'unknown'
      if (!acc[type]) acc[type] = []
      acc[type].push(alert)
      return acc
    }, {})
  })

  const upcomingVaccinations = computed(() => {
    const today = new Date()
    const sevenDaysLater = new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000)
    return cows.value.filter(c => {
      if (!c.next_vax_due) return false
      const due = new Date(c.next_vax_due)
      return due <= sevenDaysLater
    })
  })

  return {
    cows, alerts, isConnected, lastUpdated,
    fetchCows, fetchAlerts, updateCowTelemetry,
    resolveAlert, addCow, deleteCow,
    cowsInAlert, alertsByType, upcomingVaccinations,
  }
})
