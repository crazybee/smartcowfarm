import * as signalR from '@microsoft/signalr'
import {
  normalizeAlert,
  normalizeTelemetryPayload,
} from './modelTransforms'

const signalREnabled = import.meta.env.VITE_SIGNALR_ENABLED === 'true'
const hubUrl = import.meta.env.VITE_SIGNALR_URL || '/api'

const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect([2000, 5000, 10000])
  .configureLogging(signalR.LogLevel.Critical)
  .build()

export default {
  start() {
    if (!signalREnabled) {
      console.info('[SignalR] Disabled in local dev (VITE_SIGNALR_ENABLED != true). Real-time updates inactive.')
      return Promise.resolve()
    }
    if (connection.state !== signalR.HubConnectionState.Disconnected) {
      return Promise.resolve()
    }
    return connection.start().catch(err => {
      console.warn('[SignalR] Connection failed:', err.message)
    })
  },

  stop() {
    return connection.stop()
  },

  onCowUpdate(callback) {
    connection.off('cowUpdate')
    connection.on('cowUpdate', payload => callback(normalizeTelemetryPayload(payload)))
  },

  onAlert(callback) {
    connection.off('cowAlerts')
    connection.on('cowAlerts', payload => {
      const alerts = Array.isArray(payload) ? payload : [payload]
      alerts
        .map(normalizeAlert)
        .filter(Boolean)
        .forEach(callback)
    })
  },
}
