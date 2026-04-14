export function getCowId(cow) {
  return cow?.id ?? cow?.cowId ?? cow?.cow_id ?? null
}

export function normalizeGender(gender) {
  if (gender == null) return null

  if (typeof gender === 'number') {
    return gender === 1 ? 'female' : gender === 0 ? 'male' : String(gender)
  }

  const value = String(gender).trim().toLowerCase()
  if (value === '1' || value === 'female') return 'female'
  if (value === '0' || value === 'male') return 'male'
  return value
}

export function normalizeAlertType(type) {
  if (type == null) return 'unknown'

  if (typeof type === 'number') {
    if (type === 0) return 'highTemperature'
    if (type === 1) return 'geofence'
    if (type === 2) return 'vaccination'
    if (type === 3) return 'lowTemperature'
  }

  const value = String(type).trim().toLowerCase()
  if (value === 'lowtemperature' || value === 'low') return 'lowTemperature'
  if (value.includes('temperature')) return 'highTemperature' // covers 'hightemperature' and legacy 'temperature'
  if (value.includes('geofence')) return 'geofence'
  if (value.includes('vaccination')) return 'vaccination'
  return value || 'unknown'
}

export function normalizeCow(cow) {
  if (!cow) return cow

  const latitude = cow.latitude ?? cow.location?.lat ?? cow.location?.latitude ?? (Array.isArray(cow.location) ? cow.location[0] : undefined)
  const longitude = cow.longitude ?? cow.location?.lng ?? cow.location?.longitude ?? (Array.isArray(cow.location) ? cow.location[1] : undefined)

  return {
    ...cow,
    id: getCowId(cow),
    cow_id: cow.cow_id ?? cow.cowId ?? getCowId(cow),
    gender: normalizeGender(cow.gender),
    birth_date: cow.birth_date ?? cow.birthDate ?? null,
    age: cow.age ?? null,
    body_temp: cow.body_temp ?? cow.bodyTemp ?? null,
    latitude: latitude ?? null,
    longitude: longitude ?? null,
    location: latitude != null && longitude != null ? [latitude, longitude] : null,
    last_milking: cow.last_milking ?? cow.lastMilking ?? null,
    next_vax_due: cow.next_vax_due ?? cow.nextVaxDue ?? null,
    created_at: cow.created_at ?? cow.createdAt ?? null,
    updated_at: cow.updated_at ?? cow.updatedAt ?? null,
    latest_alert: cow.latest_alert ?? (cow.latestAlert
      ? {
          type: normalizeAlertType(cow.latestAlert.type ?? cow.latestAlert.alertType),
          message: cow.latestAlert.message ?? '',
          created_at: cow.latestAlert.created_at ?? cow.latestAlert.createdAt ?? null,
        }
      : null),
  }
}

export function normalizeVaccination(record) {
  if (!record) return record

  return {
    ...record,
    id: record.id ?? record.recordId ?? record.record_id,
    record_id: record.record_id ?? record.recordId ?? record.id,
    cow_id: record.cow_id ?? record.cowId ?? null,
    vaccine_name: record.vaccine_name ?? record.vaccineName ?? '',
    last_administered: record.last_administered ?? record.administeredDate ?? null,
    next_due: record.next_due ?? record.nextDueDate ?? null,
  }
}

export function normalizeAlert(alert) {
  if (!alert) return alert

  return {
    ...alert,
    id: alert.id ?? alert.alertId ?? null,
    cow_id: alert.cow_id ?? alert.cowId ?? null,
    type: alert.type ?? normalizeAlertType(alert.alertType),
    resolved: alert.resolved ?? alert.isResolved ?? false,
    created_at: alert.created_at ?? alert.createdAt ?? null,
  }
}

export function normalizeTelemetryPayload(payload) {
  if (!payload) return payload

  const latitude = payload.latitude ?? payload.location?.lat ?? payload.location?.latitude ?? (Array.isArray(payload.location) ? payload.location[0] : undefined)
  const longitude = payload.longitude ?? payload.location?.lng ?? payload.location?.longitude ?? (Array.isArray(payload.location) ? payload.location[1] : undefined)

  return {
    id: payload.id ?? payload.cowId ?? payload.deviceId ?? null,
    body_temp: payload.body_temp ?? payload.bodyTemp ?? payload.temperature ?? null,
    latitude: latitude ?? null,
    longitude: longitude ?? null,
    location: latitude != null && longitude != null ? [latitude, longitude] : null,
  }
}
