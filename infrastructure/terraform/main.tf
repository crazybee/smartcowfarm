data "azurerm_client_config" "current" {}

# Shared random suffix keeps every globally-unique name consistent per
# deployment while avoiding DNS conflicts across environments.
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}

locals {
  name_prefix = "${var.project_name}-${var.environment}"
  # Short prefix for resources with tight name-length limits (Key Vault, ACR, Storage).
  short_prefix = "${var.project_name}${substr(var.environment, 0, 1)}"
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    Owner       = var.owner
  }
}

# ─── Resource Group ───────────────────────────────────────────────────────────

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name_prefix}"
  location = var.location
  tags     = local.common_tags
}

# ─── Monitoring ───────────────────────────────────────────────────────────────

module "monitoring" {
  source              = "./modules/monitoring"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  name_prefix         = local.name_prefix
  tags                = local.common_tags
}

# ─── Database ─────────────────────────────────────────────────────────────────

module "database" {
  source               = "./modules/database"
  resource_group_name  = azurerm_resource_group.main.name
  location             = azurerm_resource_group.main.location
  name_prefix          = local.name_prefix
  suffix               = random_string.suffix.result
  sql_admin_login      = var.sql_admin_login
  sql_admin_password   = var.sql_admin_password
  database_sku         = var.database_sku
  database_max_size_gb = var.database_max_size_gb
  tags                 = local.common_tags
}

# ─── IoT Hub ──────────────────────────────────────────────────────────────────

module "iothub" {
  source              = "./modules/iothub"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  name_prefix         = local.name_prefix
  sku_name            = var.iothub_sku_name
  sku_capacity        = var.iothub_sku_capacity
  tags                = local.common_tags
}

# ─── SignalR ──────────────────────────────────────────────────────────────────

module "signalr" {
  source              = "./modules/signalr"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  name_prefix         = local.name_prefix
  sku_name            = var.signalr_sku_name
  capacity            = var.signalr_capacity
  tags                = local.common_tags
}

# ─── Container Registry ───────────────────────────────────────────────────────

module "container_registry" {
  source              = "./modules/container_registry"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  short_prefix        = local.short_prefix
  suffix              = random_string.suffix.result
  sku                 = var.acr_sku
  tags                = local.common_tags
}

# ─── Key Vault ────────────────────────────────────────────────────────────────
# Name limit: 3–24 chars. Pattern: kv-<short_prefix><suffix> e.g. kv-cowfarmd-abc123

resource "azurerm_key_vault" "main" {
  name                       = "kv-${local.short_prefix}-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  tags                       = local.common_tags
}

# Allow the pipeline service principal to seed/manage secrets.
resource "azurerm_key_vault_access_policy" "deployer" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = ["Backup", "Delete", "Get", "List", "Purge", "Recover", "Restore", "Set"]
}

# Seed connection-string secrets so Functions can reference them via
# @Microsoft.KeyVault(SecretUri=...) without storing plaintext in app settings.
resource "azurerm_key_vault_secret" "sql_cs" {
  name         = "SqlConnectionString"
  value        = module.database.connection_string
  key_vault_id = azurerm_key_vault.main.id
  depends_on   = [azurerm_key_vault_access_policy.deployer]
}

resource "azurerm_key_vault_secret" "iothub_cs" {
  name         = "IoTHubConnectionString"
  value        = module.iothub.event_hub_connection_string
  key_vault_id = azurerm_key_vault.main.id
  depends_on   = [azurerm_key_vault_access_policy.deployer]
}

resource "azurerm_key_vault_secret" "signalr_cs" {
  name         = "AzureSignalRConnectionString"
  value        = module.signalr.connection_string
  key_vault_id = azurerm_key_vault.main.id
  depends_on   = [azurerm_key_vault_access_policy.deployer]
}

# ─── Functions App ────────────────────────────────────────────────────────────

module "functions" {
  source                           = "./modules/functions"
  resource_group_name              = azurerm_resource_group.main.name
  location                         = azurerm_resource_group.main.location
  name_prefix                      = local.name_prefix
  short_prefix                     = local.short_prefix
  suffix                           = random_string.suffix.result
  tags                             = local.common_tags
  sku_name                         = var.functions_sku_name
  app_insights_connection_string   = module.monitoring.app_insights_connection_string
  app_insights_instrumentation_key = module.monitoring.app_insights_instrumentation_key
  acr_login_server                 = module.container_registry.login_server
  acr_admin_username               = module.container_registry.admin_username
  acr_admin_password               = module.container_registry.admin_password
  image_tag                        = var.image_tag
  sql_secret_id                    = azurerm_key_vault_secret.sql_cs.versionless_id
  iothub_secret_id                 = azurerm_key_vault_secret.iothub_cs.versionless_id
  signalr_secret_id                = azurerm_key_vault_secret.signalr_cs.versionless_id
  geofence_coordinates             = var.geofence_coordinates
  auto_create_database             = var.auto_create_database
}

# Grant the Functions system-assigned identity read access to Key Vault.
resource "azurerm_key_vault_access_policy" "functions" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.functions.principal_id

  secret_permissions = ["Get"]
  depends_on         = [module.functions]
}

# ─── Frontend ─────────────────────────────────────────────────────────────────

module "frontend" {
  source              = "./modules/frontend"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.static_web_app_location
  name_prefix         = local.name_prefix
  tags                = local.common_tags
}
