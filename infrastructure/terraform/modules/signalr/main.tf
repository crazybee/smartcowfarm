resource "azurerm_signalr_service" "main" {
  name                = "sigr-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  sku {
    name     = var.sku_name
    capacity = var.capacity
  }

  # Serverless mode is required for Azure Functions SignalR bindings.
  service_mode = "Serverless"

  cors {
    allowed_origins = var.allowed_origins
  }

  live_trace {
    enabled                   = true
    messaging_logs_enabled    = true
    connectivity_logs_enabled = true
  }

  tags = var.tags
}
