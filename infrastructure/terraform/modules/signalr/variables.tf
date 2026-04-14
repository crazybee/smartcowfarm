variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "name_prefix"         { type = string }
variable "sku_name" {
  type    = string
  default = "Free_F1"
}

variable "capacity" {
  type    = number
  default = 1
}
variable "tags"                { type = map(string) }

variable "allowed_origins" {
  description = "CORS allowed origins. Replace '*' with the Static Web App URL in production."
  type        = list(string)
  default     = ["*"]
}
