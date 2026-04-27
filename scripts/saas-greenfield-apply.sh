#!/usr/bin/env bash
# Provisions a disposable resource group and, when both CAE and App Insights secrets are
# available, applies infra/terraform-otel-collector (local state on the GitHub runner).
# Intended for the cd-saas-greenfield workflow; see .github/workflows/cd-saas-greenfield.yml.
set -euo pipefail
ROOT="${GITHUB_WORKSPACE:-$PWD}"
cd "$ROOT"
RUN_ID="${GITHUB_RUN_ID:-local}"
RG_NAME="${ARCHLUCID_GREENFIELD_RG_NAME:-archlucid-greenfield-$RUN_ID}"
REGION="${ARCHLUCID_GREENFIELD_REGION:-eastus2}"
export RG_NAME
az group create --location "$REGION" --name "$RG_NAME" --output none

if [ -n "${AZURE_GREENFIELD_CAE_ID:-}" ] && [ -n "${AZURE_GREENFIELD_APPINSIGHTS_CS:-}" ]
then
  export TF_IN_AUTOMATION=1
  pushd infra/terraform-otel-collector >/dev/null
  terraform init -backend=false -input=false
  terraform apply -auto-approve -input=false \
    -var "resource_group_name=$RG_NAME" \
    -var "location=$REGION" \
    -var "container_apps_environment_id=$AZURE_GREENFIELD_CAE_ID" \
    -var "application_insights_connection_string=$AZURE_GREENFIELD_APPINSIGHTS_CS" \
    -var "enable_otel_deployment=true"
  popd >/dev/null
  if [ -n "${GITHUB_ENV:-}" ]
  then
    echo "GREENFIELD_TERRAFORM_APPLIED=true" >> "$GITHUB_ENV"
  fi
  echo "::notice::OpenTelemetry collector stack applied; local Terraform state is on this runner (destroy stack via az group delete if needed)."
else
  if [ -n "${GITHUB_ENV:-}" ]
  then
    echo "GREENFIELD_TERRAFORM_APPLIED=false" >> "$GITHUB_ENV"
  fi
  echo "::notice::Skipping Terraform apply: set repository secrets AZURE_GREENFIELD_CAE_ID and AZURE_GREENFIELD_APPINSIGHTS_CS to create the collector in this run."
fi
