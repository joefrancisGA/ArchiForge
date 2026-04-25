# Improvement 12 (2026-04-24) — Azure Monitor Workbook for the first-tenant onboarding
# telemetry funnel. Sibling to infra/modules/golden-cohort-cost-dashboard.
#
# This module provisions ONLY the Workbook (and reads from an existing App Insights resource).
# It does NOT flip the per-tenant emission feature flag — that stays owner-only per
# pending question 40 / docs/security/PRIVACY_NOTE.md §3.A.

variable "enable_workbook" {
  type        = bool
  description = "When true, provision the funnel Workbook. Safe default false (CI / laptops)."
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
  default     = "ArchLucid — First-tenant onboarding funnel"
}

variable "workbook_name_guid" {
  type        = string
  description = "GUID used as the resource name; keep stable to allow in-place updates."
  default     = "9f3a2c4e-7d1b-4a8f-9e2c-5b6d8e0f1a23"
}

variable "application_insights_resource_id" {
  type        = string
  description = "Full resource ID of the Application Insights resource the Workbook queries (Microsoft.Insights/components)."
  default     = ""
}

variable "thirty_minute_target_percent" {
  type        = number
  description = "Target conversion percentage from signup → first_finding_viewed within 30 minutes. Owner Q40 baseline; surfaces on the success-rate tile."
  default     = 60
  validation {
    condition     = var.thirty_minute_target_percent >= 0 && var.thirty_minute_target_percent <= 100
    error_message = "thirty_minute_target_percent must be between 0 and 100."
  }
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to the Workbook resource."
}
