variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "name_prefix"         { type = string }
variable "short_prefix"        { type = string }
variable "suffix"              { type = string }
variable "tags"                { type = map(string) }

variable "sku_name" {
  description = "Service Plan SKU. EP1/EP2/EP3 = Elastic Premium (required for container deployments)."
  type        = string
  default     = "EP1"
}

variable "app_insights_connection_string" {
  type      = string
  sensitive = true
}

variable "app_insights_instrumentation_key" {
  type      = string
  sensitive = true
}

variable "acr_login_server"  { type = string }
variable "acr_admin_username" {
  type      = string
  sensitive = true
}
variable "acr_admin_password" {
  type      = string
  sensitive = true
}

variable "image_tag" {
  type    = string
  default = "latest"
}

variable "sql_secret_id"     { type = string }
variable "iothub_secret_id"  { type = string }
variable "signalr_secret_id" { type = string }

variable "geofence_coordinates" {
  type    = string
  default = "[[0,0],[0,1],[1,1],[1,0],[0,0]]"
}

variable "auto_create_database" {
  description = "Run EF Core EnsureCreated() on startup. Set true for dev, false for prod (use migrations instead)."
  type        = bool
  default     = false
}
