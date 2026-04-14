# Storage account name: 3–24 chars, lowercase alphanumeric only, globally unique.
# Pattern: st<short_prefix><suffix>  e.g. stcowfarmd-abc123 → stcowfarmdabc123
resource "azurerm_storage_account" "main" {
  name                     = "st${replace(var.short_prefix, "-", "")}${var.suffix}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = var.tags
}

resource "azurerm_service_plan" "main" {
  name                = "asp-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name
  tags                = var.tags
}

resource "azurerm_linux_function_app" "main" {
  name                = "func-${var.name_prefix}-${var.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key
  service_plan_id            = azurerm_service_plan.main.id

  # System-assigned identity is required for Key Vault references.
  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    # Application Insights
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = var.app_insights_instrumentation_key

    # Container registry credentials (also set in site_config for pull auth)
    "DOCKER_REGISTRY_SERVER_URL"      = "https://${var.acr_login_server}"
    "DOCKER_REGISTRY_SERVER_USERNAME" = var.acr_admin_username
    "DOCKER_REGISTRY_SERVER_PASSWORD" = var.acr_admin_password

    # Disable persistent storage for container deployments.
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"

    # Secrets resolved at runtime from Key Vault (no plaintext stored here).
    "SqlConnectionString"          = "@Microsoft.KeyVault(SecretUri=${var.sql_secret_id})"
    "IoTHubConnectionString"       = "@Microsoft.KeyVault(SecretUri=${var.iothub_secret_id})"
    "AzureSignalRConnectionString" = "@Microsoft.KeyVault(SecretUri=${var.signalr_secret_id})"

    # Application config
    "GeofenceCoordinates" = var.geofence_coordinates
    "AutoCreateDatabase"  = tostring(var.auto_create_database)

    # Required for isolated worker model
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
  }

  site_config {
    application_stack {
      docker {
        registry_url      = "https://${var.acr_login_server}"
        image_name        = "smartcowfarm-functions"
        image_tag         = var.image_tag
        registry_username = var.acr_admin_username
        registry_password = var.acr_admin_password
      }
    }

    cors {
      # Tighten to the Static Web App URL once known.
      allowed_origins     = ["*"]
      support_credentials = false
    }
  }

  tags = var.tags
}
