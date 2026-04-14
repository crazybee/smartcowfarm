variable "resource_group_name"  { type = string }
variable "location"             { type = string }
variable "name_prefix"          { type = string }
variable "suffix"               { type = string }

variable "sql_admin_login" {
  type      = string
  sensitive = true
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

variable "database_sku" {
  type    = string
  default = "S1"
}

variable "database_max_size_gb" {
  type    = number
  default = 5
}

variable "tags" { type = map(string) }
