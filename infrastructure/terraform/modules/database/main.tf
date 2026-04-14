# SQL Server name must be globally unique (max 63 chars).
resource "azurerm_mssql_server" "main" {
  name                         = "sql-${var.name_prefix}-${var.suffix}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  tags                         = var.tags
}

resource "azurerm_mssql_database" "main" {
  name        = "sqldb-smartcowfarm"
  server_id   = azurerm_mssql_server.main.id
  collation   = "SQL_Latin1_General_CP1_CI_AS"
  sku_name    = var.database_sku
  max_size_gb = var.database_max_size_gb
  tags        = var.tags
}

# Allow Azure services (Functions, etc.) to reach the SQL server.
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
