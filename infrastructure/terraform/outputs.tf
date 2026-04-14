output "resource_group_name" {
  description = "Name of the main resource group."
  value       = azurerm_resource_group.main.name
}

output "functions_hostname" {
  description = "Default hostname of the Azure Functions app."
  value       = module.functions.hostname
}

output "functions_app_name" {
  description = "Name of the Azure Functions app (used by Ansible for deployment)."
  value       = module.functions.app_name
}

output "acr_login_server" {
  description = "Azure Container Registry login server URL."
  value       = module.container_registry.login_server
}

output "acr_name" {
  description = "Azure Container Registry resource name."
  value       = module.container_registry.name
}

output "static_web_app_url" {
  description = "Default URL of the Azure Static Web App (frontend)."
  value       = module.frontend.url
}

output "static_web_app_api_key" {
  description = "Deployment API key for the Static Web App."
  value       = module.frontend.api_key
  sensitive   = true
}

output "static_web_app_name" {
  description = "Name of the Azure Static Web App resource."
  value       = module.frontend.name
}

output "iothub_hostname" {
  description = "IoT Hub hostname for device connection strings."
  value       = module.iothub.hostname
}

output "key_vault_uri" {
  description = "Key Vault URI."
  value       = azurerm_key_vault.main.vault_uri
}

output "app_insights_instrumentation_key" {
  description = "Application Insights instrumentation key."
  value       = module.monitoring.app_insights_instrumentation_key
  sensitive   = true
}

output "app_insights_connection_string" {
  description = "Application Insights connection string."
  value       = module.monitoring.app_insights_connection_string
  sensitive   = true
}

output "deployment_suffix" {
  description = "Random suffix shared by all globally-unique resource names."
  value       = random_string.suffix.result
}
