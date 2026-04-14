# ─── General ──────────────────────────────────────────────────────────────────

variable "project_name" {
  description = "Short project name used in resource naming (lowercase, no spaces)."
  type        = string
  default     = "cowfarm"
}

variable "environment" {
  description = "Deployment environment: dev | staging | prod."
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "australiaeast"
}

variable "static_web_app_location" {
  description = "Azure region for Static Web App. Supported regions include westus2, centralus, eastus2, westeurope, eastasia."
  type        = string
  default     = "eastasia"
}

variable "owner" {
  description = "Owner tag applied to every resource."
  type        = string
  default     = "SmartCowFarm Team"
}

# ─── SQL Database ─────────────────────────────────────────────────────────────

variable "sql_admin_login" {
  description = "SQL Server administrator login name."
  type        = string
  sensitive   = true

  validation {
    condition     = lower(var.sql_admin_login) != "admin" && lower(var.sql_admin_login) != "administrator" && lower(var.sql_admin_login) != "sa"
    error_message = "sql_admin_login must not be admin, administrator, or sa. Use a custom login such as sqladminuser or cowfarmsqladmin."
  }
}

variable "sql_admin_password" {
  description = "SQL Server administrator password (min 8 chars, complexity required)."
  type        = string
  sensitive   = true
}

variable "database_sku" {
  description = "Azure SQL Database pricing tier (e.g. Basic, S1, GP_S_Gen5_1)."
  type        = string
  default     = "S1"
}

variable "database_max_size_gb" {
  description = "Maximum database size in GB."
  type        = number
  default     = 5
}

# ─── IoT Hub ──────────────────────────────────────────────────────────────────

variable "iothub_sku_name" {
  description = "IoT Hub SKU: F1 (free), B1–B3 (basic), S1–S3 (standard)."
  type        = string
  default     = "S1"
}

variable "iothub_sku_capacity" {
  description = "Number of IoT Hub units."
  type        = number
  default     = 1
}

# ─── SignalR ──────────────────────────────────────────────────────────────────

variable "signalr_sku_name" {
  description = "SignalR SKU: Free_F1 | Standard_S1 | Premium_P1."
  type        = string
  default     = "Free_F1"
}

variable "signalr_capacity" {
  description = "SignalR unit count (must be 1 for Free_F1)."
  type        = number
  default     = 1
}

# ─── Azure Container Registry ─────────────────────────────────────────────────

variable "acr_sku" {
  description = "ACR SKU: Basic | Standard | Premium."
  type        = string
  default     = "Basic"
}

# ─── Azure Functions ──────────────────────────────────────────────────────────

variable "functions_sku_name" {
  description = "Functions hosting plan SKU. EP1/EP2/EP3 = Elastic Premium; Y1 = Consumption (no containers)."
  type        = string
  default     = "EP1"
}

variable "image_tag" {
  description = "Docker image tag to deploy to the Functions app."
  type        = string
  default     = "latest"
}

# ─── Application ──────────────────────────────────────────────────────────────

variable "geofence_coordinates" {
  description = "JSON polygon coordinates for the farm geofence alert rule."
  type        = string
  default     = "[[0,0],[0,1],[1,1],[1,0],[0,0]]"
}

variable "auto_create_database" {
  description = "Run EF Core EnsureCreated() on startup. true for dev, false for prod (use migrations instead)."
  type        = bool
  default     = false
}
