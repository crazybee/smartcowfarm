# ─── Production environment ───────────────────────────────────────────────────
# Apply: terraform apply -var-file=environments/prod.tfvars
# Init:  terraform init -backend-config=environments/prod.backend.hcl

project_name = "cowfarm"
environment  = "prod"
location     = "australiaeast"
static_web_app_location = "eastasia"
owner        = "SmartCowFarm Team"

sql_admin_login    = "cowfarmsqladmin"
sql_admin_password = "REPLACE_WITH_SECRET"   # ← inject via CI variable group

database_sku         = "S2"
database_max_size_gb = 50

iothub_sku_name     = "S1"
iothub_sku_capacity = 1

signalr_sku_name = "Standard_S1"
signalr_capacity = 1

acr_sku = "Standard"

functions_sku_name = "EP2"
image_tag          = "REPLACE_WITH_BUILD_ID"  # ← injected by CD pipeline

geofence_coordinates = "[[151.2,-33.9],[151.3,-33.9],[151.3,-34.0],[151.2,-34.0],[151.2,-33.9]]"
