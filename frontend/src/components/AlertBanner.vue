<template>
  <div v-if="hasAlerts" class="alert-banner" :class="{ flash: criticalAlerts.length > 0 }">
    <div class="alert-header">
      <strong>⚠️ Active Alerts ({{ unresolvedAlerts.length }})</strong>
    </div>
    <div class="alert-list">
      <div v-for="alert in unresolvedAlerts" :key="alert.id" class="alert-item">
        <span class="icon">{{ alertIcon(alert.type) }}</span>
        <span class="msg">{{ alert.message || alert.type }}</span>
        <span class="cow-id">Cow: {{ alert.cow_id }}</span>
        <button class="resolve-btn" @click="store.resolveAlert(alert.id)">Resolve</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useCowStore } from '../stores/cowStore'

const store = useCowStore()

const unresolvedAlerts = computed(() =>
  store.alerts.filter(a => !a.resolved)
)

const hasAlerts = computed(() => unresolvedAlerts.value.length > 0)

const criticalAlerts = computed(() =>
  unresolvedAlerts.value.filter(a => a.type === 'temperature')
)

function alertIcon(type) {
  const icons = { temperature: '🌡️', geofence: '📍', vaccination: '💉' }
  return icons[type] || '⚠️'
}
</script>

<style scoped>
.alert-banner {
  background: #fee2e2;
  border: 2px solid #ef4444;
  border-radius: 8px;
  padding: 12px 16px;
  margin-bottom: 16px;
}
.alert-header { font-size: 1rem; margin-bottom: 8px; color: #b91c1c; }
.alert-list { display: flex; flex-direction: column; gap: 6px; }
.alert-item {
  display: flex; align-items: center; gap: 10px;
  background: #fff; border-radius: 6px; padding: 6px 10px;
  border: 1px solid #fca5a5;
}
.icon { font-size: 1.2rem; }
.msg { flex: 1; font-size: 0.9rem; }
.cow-id { font-size: 0.8rem; color: #666; }
.resolve-btn {
  background: #ef4444; color: white; border: none;
  border-radius: 4px; padding: 3px 10px; cursor: pointer; font-size: 0.8rem;
}
.resolve-btn:hover { background: #b91c1c; }
.flash { animation: flash 1s infinite; }
@keyframes flash {
  0%, 100% { border-color: #ef4444; }
  50% { border-color: #fca5a5; background: #fef2f2; }
}
</style>
