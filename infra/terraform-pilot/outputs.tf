output "apply_order_doc" {
  value       = "See docs/REFERENCE_SAAS_STACK_ORDER.md and docs/deployment/PILOT_PROFILE.md"
  description = "Canonical sequencing narrative for Azure roots."
}

output "canonical_profile_root" {
  value       = "infra/terraform-pilot"
  description = "Default operator entry: cost knobs + nested stack order metadata (no resources in this root)."
}

output "multi_root_apply_opt_in" {
  value       = var.multi_root_apply_opt_in
  description = "When true, operators intentionally use separate terraform init/apply per nested stack path; default workflow stays on this profile root only."
}

output "nested_infrastructure_roots" {
  value       = local.nested_infrastructure_roots
  description = "Ordered stacks for the opt-in multi-root path (paths relative to repository root)."
}

output "deployment_profile" {
  value       = var.deployment_profile
  description = "pilot = cost-aware defaults; production = stricter reference posture (documentation-only here)."
}

output "cost_variables" {
  value = {
    pilot_monthly_budget_usd      = var.pilot_monthly_budget_usd
    sql_sku_hint                  = var.sql_sku_hint
    container_apps_max_replicas   = var.container_apps_max_replicas
    app_insights_sampling_percent = var.app_insights_sampling_percent
  }
  description = "Opinionated pilot FinOps knobs — set matching tfvars in downstream roots when applying the opt-in path."
}
