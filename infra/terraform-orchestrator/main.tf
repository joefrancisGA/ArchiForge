/**
 * Intentionally minimal Terraform root.
 *
 * ArchLucid Azure stacks stay as separate Terraform roots (each with its own state file).
 * Compose them in dependency order using scripts/provision-landing-zone.ps1 (or .sh), not a monolithic apply here.
 * This directory exists so CI can run terraform init/validate/fmt on a stable entry point.
 */

terraform {
  required_version = ">= 1.5.0"
}
