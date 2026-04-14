output "hostname" {
  description = "Default hostname of the Function App."
  value       = azurerm_linux_function_app.main.default_hostname
}

output "app_name" {
  description = "Resource name of the Function App (used by Ansible/CLI)."
  value       = azurerm_linux_function_app.main.name
}

output "principal_id" {
  description = "System-assigned managed identity principal ID (for Key Vault access policy)."
  value       = azurerm_linux_function_app.main.identity[0].principal_id
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}
