# ─── Dev environment ──────────────────────────────────────────────────────────
# Apply: terraform apply -var-file=environments/dev.tfvars
# Init:  terraform init -backend-config=environments/dev.backend.hcl

project_name = "cowfarm"
environment  = "dev"
location     = "australiaeast"
static_web_app_location = "eastasia"
owner        = "SmartCowFarm Team"

# SQL – use a managed password from your CI secret store; never commit here.
sql_admin_login    = "cowfarmsqladmin"
sql_admin_password = "REPLACE_WITH_SECRET"   # ← inject via CI variable group

database_sku         = "Basic"
database_max_size_gb = 2

iothub_sku_name     = "S1"
iothub_sku_capacity = 1

signalr_sku_name = "Free_F1"
signalr_capacity = 1

acr_sku = "Basic"

# EP1 = Elastic Premium (required for Linux container deployments).
# Switch to Y1 (Consumption) only if doing zip-deploy instead of Docker.
functions_sku_name = "EP1"
image_tag          = "latest"
auto_create_database = true

geofence_coordinates = "[[151.2,-33.9],[151.3,-33.9],[151.3,-34.0],[151.2,-34.0],[151.2,-33.9]]"
