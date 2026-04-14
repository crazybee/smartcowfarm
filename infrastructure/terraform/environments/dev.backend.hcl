# Backend config for the dev Terraform state file.
# Usage: terraform init -backend-config=environments/dev.backend.hcl
#
# The Storage Account must exist before running terraform init.
# Create it once with the bootstrap script: infrastructure/scripts/bootstrap-tfstate.sh

resource_group_name  = "rg-tfstate"
storage_account_name = "tfstateparcelrouting123"
container_name       = "tfstate"
key                  = "smartcowfarm-dev.tfstate"
