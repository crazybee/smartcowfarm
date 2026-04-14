terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.2"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Populated via -backend-config flags or environment variables in CI/CD.
  # Run terraform init with:
  #   -backend-config="resource_group_name=rg-tfstate-cowfarm"
  #   -backend-config="storage_account_name=<globally-unique-name>"
  #   -backend-config="container_name=tfstate"
  #   -backend-config="key=smartcowfarm-<env>.tfstate"
  #
  # CI variables to set: ARM_CLIENT_ID, ARM_CLIENT_SECRET,
  #                       ARM_SUBSCRIPTION_ID, ARM_TENANT_ID
  backend "azurerm" {}
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}
