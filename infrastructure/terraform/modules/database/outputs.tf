output "server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "database_name" {
  value = azurerm_mssql_database.main.name
}

output "connection_string" {
  description = "ADO.NET connection string for EF Core."
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive   = true
}
