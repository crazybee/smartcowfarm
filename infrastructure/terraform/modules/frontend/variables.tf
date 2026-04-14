variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "name_prefix"         { type = string }
variable "tags"                { type = map(string) }

variable "sku_tier" {
  description = "Static Web App pricing tier: Free | Standard."
  type        = string
  default     = "Free"
}

variable "sku_size" {
  description = "Static Web App SKU size (matches sku_tier)."
  type        = string
  default     = "Free"
}
