# Improvement 11 (2026-04-24) — Azure Monitor Workbook for the golden-cohort real-LLM gate.
#
# Stop-and-ask boundaries from docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md (Prompt 11):
#   - This module does NOT provision the dedicated Azure OpenAI deployment itself
#     (owner-only operational task per Q15).
#   - This module does NOT inject the Azure OpenAI secret (owner-only via the protected
#     GitHub Environment).
# It provisions ONLY the Workbook against an existing App Insights / Log Analytics workspace.

variable "enable_workbook" {
  type        = bool
  description = "When true, provision the cost-and-latency Workbook. Safe default false (CI / laptops)."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Existing resource group that holds the Application Insights resource the Workbook reads from."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region for the Workbook resource (typically the same as Application Insights)."
  default     = "eastus"
}

variable "workbook_display_name" {
  type        = string
  description = "Display name of the Workbook in the Azure Monitor portal."
  default     = "ArchLucid — Golden cohort real-LLM cost & latency"
}

variable "workbook_name_guid" {
  type        = string
  description = "GUID used as the resource name; keep stable to allow in-place updates."
  default     = "1c2d4f6a-9b3e-4f7a-8c5b-d8e7c0f1a234"
}

variable "application_insights_resource_id" {
  type        = string
  description = "Full resource ID of the Application Insights resource the Workbook queries (Microsoft.Insights/components)."
  default     = ""
}

variable "azure_openai_resource_id" {
  type        = string
  description = "Full ARM ID of the dedicated Microsoft.CognitiveServices/accounts resource hosting the cohort deployment. Used by the cost tile to scope ActualCost queries."
  default     = ""
}

variable "monthly_budget_usd" {
  type        = number
  description = "Monthly USD cap shown on the dashboard. Must match tests/golden-cohort/budget.config.json.monthlyTokenBudgetUsd."
  default     = 50
  validation {
    condition     = var.monthly_budget_usd > 0
    error_message = "monthly_budget_usd must be greater than zero."
  }
}

variable "warn_threshold_percent" {
  type        = number
  description = "Warn threshold (% of cap) — value displayed on the kill-switch tile. Q15-conditional rule pins this to 80."
  default     = 80
  validation {
    condition     = var.warn_threshold_percent == 80
    error_message = "warn_threshold_percent must stay at 80 (Q15-conditional rule, PENDING_QUESTIONS Q15)."
  }
}

variable "kill_switch_threshold_percent" {
  type        = number
  description = "Kill threshold (% of cap) — value displayed on the kill-switch tile. Q15-conditional rule pins this to 95."
  default     = 95
  validation {
    condition     = var.kill_switch_threshold_percent == 95
    error_message = "kill_switch_threshold_percent must stay at 95 (Q15-conditional rule, PENDING_QUESTIONS Q15)."
  }
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to the Workbook resource."
}
