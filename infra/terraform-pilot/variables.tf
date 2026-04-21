variable "deployment_profile" {
  type        = string
  description = "Documentation-only: pilot uses cost-aware posture; production references full SaaS stack defaults."
  default     = "pilot"

  validation {
    condition     = contains(["pilot", "production"], var.deployment_profile)
    error_message = "deployment_profile must be \"pilot\" or \"production\"."
  }
}

variable "multi_root_apply_opt_in" {
  type        = bool
  description = "Set true in tfvars when intentionally using separate state per infra/terraform-* root; default path validates this profile only."
  default     = false
}

variable "pilot_monthly_budget_usd" {
  type        = number
  description = "Soft cap for FinOps review (not enforced by Terraform alone; set Azure budgets in portal or consumption_budget stacks)."
  default     = 500
}

variable "sql_sku_hint" {
  type        = string
  description = "Human-readable Azure SQL target for pilot (actual deployment uses terraform-sql-failover variables)."
  default     = "Basic or S0 single-region"
}

variable "container_apps_max_replicas" {
  type        = number
  description = "Suggested maxReplicas for API/Worker during pilot."
  default     = 3
}

variable "app_insights_sampling_percent" {
  type        = number
  description = "Target sampling percentage for Application Insights in pilot (coordinate with terraform-monitoring)."
  default     = 25
}
