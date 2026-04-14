<template>
  <div ref="mapContainer" class="map-container"></div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import L from 'leaflet'
import { getCowId } from '../services/modelTransforms'

// Fix Leaflet default icon paths
delete L.Icon.Default.prototype._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: new URL('leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
  iconUrl: new URL('leaflet/dist/images/marker-icon.png', import.meta.url).href,
  shadowUrl: new URL('leaflet/dist/images/marker-shadow.png', import.meta.url).href,
})

const props = defineProps({
  cows: { type: Array, default: () => [] },
  geofence: { type: Array, default: () => [] },
})

const mapContainer = ref(null)
let map = null
let geofenceLayer = null
const markers = {}

function initMap() {
  map = L.map(mapContainer.value).setView([0, 0], 2)
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
  }).addTo(map)
}

function updateGeofence() {
  if (geofenceLayer) { map.removeLayer(geofenceLayer); geofenceLayer = null }
  if (props.geofence && props.geofence.length > 2) {
    geofenceLayer = L.polygon(props.geofence, { color: '#3b82f6', fillOpacity: 0.1 }).addTo(map)
    map.fitBounds(geofenceLayer.getBounds())
  }
}

function updateMarkers() {
  const seen = new Set()
  props.cows.forEach(cow => {
    if (!cow.location) return
    const [lat, lng] = Array.isArray(cow.location)
      ? cow.location
      : [cow.location.lat, cow.location.lng]
    if (lat == null || lng == null) return
    const cowId = getCowId(cow)
    if (!cowId) return
    seen.add(cowId)
    const isTempHigh = (cow.body_temp || 0) > 39.5
    const isTempLow = cow.body_temp != null && cow.body_temp < 38.0
    const color = isTempHigh ? '#ef4444' : isTempLow ? '#3b82f6' : '#22c55e'
    if (markers[cowId]) {
      markers[cowId].setLatLng([lat, lng])
      markers[cowId].setStyle({ color, fillColor: color })
    } else {
      const m = L.circleMarker([lat, lng], {
        radius: 10, color, fillColor: color, fillOpacity: 0.8,
      }).addTo(map)
      m.bindPopup(`
        <b>Cow ${cowId}</b><br/>
        Temp: ${cow.body_temp ?? 'N/A'}°C<br/>
        Last milking: ${cow.last_milking ? new Date(cow.last_milking).toLocaleString() : 'N/A'}
      `)
      markers[cowId] = m
    }
  })
  Object.keys(markers).forEach(id => {
    if (!seen.has(id)) { map.removeLayer(markers[id]); delete markers[id] }
  })
}

onMounted(() => {
  initMap()
  updateGeofence()
  updateMarkers()
})

onUnmounted(() => {
  if (map) { map.remove(); map = null }
})

watch(() => props.cows, updateMarkers, { deep: true })
watch(() => props.geofence, updateGeofence, { deep: true })
</script>

<style scoped>
.map-container { width: 100%; height: 400px; border-radius: 8px; overflow: hidden; }
</style>
