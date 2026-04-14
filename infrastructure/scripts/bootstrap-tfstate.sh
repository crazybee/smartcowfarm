#!/usr/bin/env bash
# bootstrap-tfstate.sh
# Creates the Azure Storage Account that holds Terraform remote state.
# Run once per subscription before the first terraform init.
#
# Usage: bash bootstrap-tfstate.sh [dev|prod]

set -euo pipefail

ENV=${1:-dev}
LOCATION="australiaeast"
RG_NAME="rg-tfstate-cowfarm"
SA_NAME="stterraformstatecowfarm"   # change if name is taken
CONTAINER_NAME="tfstate"

echo "==> Creating resource group: $RG_NAME"
az group create --name "$RG_NAME" --location "$LOCATION" --output none

echo "==> Creating storage account: $SA_NAME"
az storage account create \
  --name "$SA_NAME" \
  --resource-group "$RG_NAME" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --allow-blob-public-access false \
  --output none

echo "==> Enabling versioning (protects state files)"
az storage account blob-service-properties update \
  --account-name "$SA_NAME" \
  --resource-group "$RG_NAME" \
  --enable-versioning true \
  --output none

echo "==> Creating blob container: $CONTAINER_NAME"
az storage container create \
  --name "$CONTAINER_NAME" \
  --account-name "$SA_NAME" \
  --auth-mode login \
  --output none

echo ""
echo "✅  Terraform state backend is ready."
echo "    Run the following to initialise Terraform for $ENV:"
echo ""
echo "    cd infrastructure/terraform"
echo "    terraform init -backend-config=environments/$ENV.backend.hcl"
