<template>
  <div class="health-card" :class="{ 'has-alert': hasAlert }">
    <div class="card-header">
      <span class="cow-id">🐄 {{ shortId }}</span>
      <span v-if="hasAlert" class="alert-badge">⚠️</span>
      <span class="gender-icon">{{ cow.gender === 'female' ? '♀️' : '♂️' }}</span>
    </div>
    <div class="card-body">
      <div class="stat">
        <span class="label">Age</span>
        <span class="value">{{ age }}</span>
      </div>
      <div class="stat">
        <span class="label">Temp</span>
        <span class="value" :class="{ 'temp-high': isTempHigh }">
          {{ cow.body_temp != null ? `${cow.body_temp}°C` : 'N/A' }}
        </span>
      </div>
      <div class="stat">
        <span class="label">Location</span>
        <span class="value">{{ locationStr }}</span>
      </div>
      <div class="stat">
        <span class="label">Last Milking</span>
        <span class="value">{{ lastMilkingStr }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useCowStore } from '../stores/cowStore'

const props = defineProps({
  cow: { type: Object, required: true },
})

const store = useCowStore()

const shortId = computed(() => {
  const id = String(props.cow.id || '')
  return id.length > 8 ? id.slice(0, 8) + '…' : id
})

const isTempHigh = computed(() => (props.cow.body_temp || 0) > 39.5)

const hasAlert = computed(() =>
  store.alerts.some(a => !a.resolved && a.cow_id === props.cow.id)
)

const age = computed(() => {
  if (!props.cow.birth_date) return 'N/A'
  const diff = Date.now() - new Date(props.cow.birth_date).getTime()
  const years = Math.floor(diff / (365.25 * 24 * 60 * 60 * 1000))
  return `${years}y`
})

const locationStr = computed(() => {
  const loc = props.cow.location
  if (!loc) return 'N/A'
  const lat = Array.isArray(loc) ? loc[0] : loc.lat
  const lng = Array.isArray(loc) ? loc[1] : loc.lng
  if (lat == null || lng == null) return 'N/A'
  return `${Number(lat).toFixed(4)}, ${Number(lng).toFixed(4)}`
})

const lastMilkingStr = computed(() => {
  if (!props.cow.last_milking) return 'N/A'
  return new Date(props.cow.last_milking).toLocaleString()
})
</script>

<style scoped>
.health-card {
  background: white; border-radius: 10px; padding: 14px; min-width: 180px;
  box-shadow: 0 1px 4px rgba(0,0,0,0.1); border: 2px solid transparent;
  transition: border-color 0.2s;
}
.has-alert { border-color: #ef4444; }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.cow-id { font-weight: 700; font-size: 1rem; }
.alert-badge { background: #ef4444; color: white; border-radius: 50%; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; }
.gender-icon { font-size: 1rem; }
.card-body { display: flex; flex-direction: column; gap: 6px; }
.stat { display: flex; justify-content: space-between; font-size: 0.85rem; }
.label { color: #6b7280; }
.value { font-weight: 500; }
.temp-high { color: #ef4444; font-weight: 700; }
</style>
