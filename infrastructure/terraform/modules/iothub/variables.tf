variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "name_prefix"         { type = string }
variable "sku_name" {
	type    = string
	default = "S1"
}

variable "sku_capacity" {
	type    = number
	default = 1
}
variable "tags"                { type = map(string) }
