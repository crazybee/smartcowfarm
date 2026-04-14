resource "azurerm_iothub" "main" {
  name                = "iot-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  sku {
    name     = var.sku_name
    capacity = var.sku_capacity
  }

  tags = var.tags
}

# Dedicated consumer group for the Azure Functions EventHub trigger.
resource "azurerm_iothub_consumer_group" "functions" {
  name                   = "functions-consumer"
  iothub_name            = azurerm_iothub.main.name
  eventhub_endpoint_name = "events"
  resource_group_name    = var.resource_group_name
}

# Least-privilege SAS policy: service connect + registry read only.
resource "azurerm_iothub_shared_access_policy" "functions_read" {
  name                = "functions-read"
  resource_group_name = var.resource_group_name
  iothub_name         = azurerm_iothub.main.name

  registry_read   = true
  service_connect = true
}
