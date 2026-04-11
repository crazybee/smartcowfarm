import * as signalR from '@microsoft/signalr'

const hubUrl = import.meta.env.VITE_SIGNALR_URL || '/api/hub'

const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Warning)
  .build()

export default {
  start() {
    if (connection.state === signalR.HubConnectionState.Disconnected) {
      return connection.start().catch(err => console.error('SignalR start error', err))
    }
  },
  stop() {
    return connection.stop()
  },
  onCowUpdate(callback) {
    connection.on('CowUpdate', callback)
  },
  onAlert(callback) {
    connection.on('Alert', callback)
  },
}
