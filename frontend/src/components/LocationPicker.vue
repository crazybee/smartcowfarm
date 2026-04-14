<template>
  <div class="location-picker">
    <p class="hint">Click the map to set the cow's location, or drag the pin</p>
    <div ref="mapEl" class="picker-map"></div>
    <div class="coord-row">
      <label>
        Latitude
        <input type="number" step="any" :value="lat" @input="onLatInput" placeholder="e.g. 51.505" />
      </label>
      <label>
        Longitude
        <input type="number" step="any" :value="lng" @input="onLngInput" placeholder="e.g. -0.09" />
      </label>
      <button type="button" class="locate-btn" title="Use my current location" @click="useMyLocation">📍</button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted, nextTick } from 'vue'
import L from 'leaflet'

delete L.Icon.Default.prototype._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: new URL('leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
  iconUrl: new URL('leaflet/dist/images/marker-icon.png', import.meta.url).href,
  shadowUrl: new URL('leaflet/dist/images/marker-shadow.png', import.meta.url).href,
})

const props = defineProps({
  modelValue: { type: Object, default: () => ({ lat: 0, lng: 0 }) },
})
const emit = defineEmits(['update:modelValue'])

const mapEl = ref(null)
let map = null
let marker = null

const lat = ref(props.modelValue?.lat ?? 0)
const lng = ref(props.modelValue?.lng ?? 0)

function placeMarker(newLat, newLng) {
  if (!map) return
  if (marker) {
    marker.setLatLng([newLat, newLng])
  } else {
    marker = L.marker([newLat, newLng], { draggable: true }).addTo(map)
    marker.on('dragend', () => {
      const pos = marker.getLatLng()
      lat.value = +pos.lat.toFixed(6)
      lng.value = +pos.lng.toFixed(6)
      emit('update:modelValue', { lat: lat.value, lng: lng.value })
    })
  }
}

onMounted(async () => {
  const hasCoords = lat.value !== 0 || lng.value !== 0
  const center = hasCoords ? [lat.value, lng.value] : [51.505, -0.09]
  const zoom = hasCoords ? 13 : 2

  map = L.map(mapEl.value, { center, zoom })
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
  }).addTo(map)

  if (hasCoords) placeMarker(lat.value, lng.value)

  map.on('click', (e) => {
    lat.value = +e.latlng.lat.toFixed(6)
    lng.value = +e.latlng.lng.toFixed(6)
    placeMarker(lat.value, lng.value)
    emit('update:modelValue', { lat: lat.value, lng: lng.value })
  })

  // Ensure Leaflet measures the container after layout is settled
  await nextTick()
  map.invalidateSize()
})

onUnmounted(() => {
  if (map) { map.remove(); map = null; marker = null }
})

watch(
  () => props.modelValue,
  (val) => {
    if (!val) return
    const newLat = val.lat ?? 0
    const newLng = val.lng ?? 0
    if (newLat === lat.value && newLng === lng.value) return
    lat.value = newLat
    lng.value = newLng
    if (map) {
      placeMarker(newLat, newLng)
      if (newLat !== 0 || newLng !== 0) map.setView([newLat, newLng], 13)
    }
  },
  { deep: true }
)

function onLatInput(e) {
  lat.value = parseFloat(e.target.value) || 0
  placeMarker(lat.value, lng.value)
  if (map) map.panTo([lat.value, lng.value])
  emit('update:modelValue', { lat: lat.value, lng: lng.value })
}

function onLngInput(e) {
  lng.value = parseFloat(e.target.value) || 0
  placeMarker(lat.value, lng.value)
  if (map) map.panTo([lat.value, lng.value])
  emit('update:modelValue', { lat: lat.value, lng: lng.value })
}

function useMyLocation() {
  if (!navigator.geolocation) return alert('Geolocation is not supported by your browser.')
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      lat.value = +pos.coords.latitude.toFixed(6)
      lng.value = +pos.coords.longitude.toFixed(6)
      placeMarker(lat.value, lng.value)
      if (map) map.setView([lat.value, lng.value], 14)
      emit('update:modelValue', { lat: lat.value, lng: lng.value })
    },
    () => alert('Unable to retrieve your location.')
  )
}
</script>

<style scoped>
.location-picker { display: flex; flex-direction: column; gap: 6px; }
.hint { margin: 0; font-size: 0.78rem; color: #6b7280; }
.picker-map { width: 100%; height: 240px; border-radius: 6px; border: 1px solid #d1d5db; cursor: crosshair; }
.coord-row { display: flex; gap: 8px; align-items: flex-end; }
.coord-row label { flex: 1; display: flex; flex-direction: column; gap: 3px; font-size: 0.85rem; font-weight: normal; }
.coord-row input { padding: 5px 8px; border: 1px solid #d1d5db; border-radius: 6px; width: 100%; box-sizing: border-box; }
.locate-btn { background: #f3f4f6; border: 1px solid #d1d5db; border-radius: 6px; padding: 6px 10px; cursor: pointer; font-size: 1.1rem; line-height: 1; white-space: nowrap; flex-shrink: 0; }
.locate-btn:hover { background: #e5e7eb; }
</style>
