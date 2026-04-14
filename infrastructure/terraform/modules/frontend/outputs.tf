output "url" {
  description = "Default URL of the Static Web App."
  value       = "https://${azurerm_static_web_app.main.default_host_name}"
}

output "name" {
  value = azurerm_static_web_app.main.name
}

output "api_key" {
  description = "Deployment API key used by the SWA CLI / Azure DevOps task."
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}
