# ACR names: 5–50 chars, alphanumeric only, globally unique.
# Pattern: acr<short_prefix><suffix>  e.g. acrcowfarmd-abc123 → acrcowfarmdabc123
resource "azurerm_container_registry" "main" {
  name                = "acr${replace(var.short_prefix, "-", "")}${var.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.sku
  admin_enabled       = true
  tags                = var.tags
}
