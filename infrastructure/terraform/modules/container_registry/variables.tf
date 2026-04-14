variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "short_prefix"        { type = string }
variable "suffix"              { type = string }
variable "sku" {
	type    = string
	default = "Basic"
}
variable "tags"                { type = map(string) }
