output "hostname" {
  description = "IoT Hub hostname for device connection strings."
  value       = azurerm_iothub.main.hostname
}

output "event_hub_events_endpoint" {
  value = azurerm_iothub.main.event_hub_events_endpoint
}

output "event_hub_events_path" {
  value = azurerm_iothub.main.event_hub_events_path
}

output "event_hub_connection_string" {
  description = "Event Hub-compatible connection string for the Azure Functions EventHub trigger (IoTHubConnectionString)."
  # Format: Endpoint=sb://<ns>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<path>
  value     = "Endpoint=${azurerm_iothub.main.event_hub_events_endpoint};SharedAccessKeyName=${azurerm_iothub_shared_access_policy.functions_read.name};SharedAccessKey=${azurerm_iothub_shared_access_policy.functions_read.primary_key};EntityPath=${azurerm_iothub.main.event_hub_events_path}"
  sensitive = true
}

output "iothub_name" {
  value = azurerm_iothub.main.name
}
