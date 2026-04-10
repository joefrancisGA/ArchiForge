variable "enable_openai_consumption_budget" {
  type        = bool
  description = "When true, create an azurerm_consumption_budget_resource_group for Azure OpenAI / Cognitive Services spend in openai_consumption_budget_resource_group_id."
  default     = false
}

variable "openai_consumption_budget_resource_group_id" {
  type        = string
  description = "Full ARM id of the resource group that contains the Azure OpenAI (Cognitive Services) account(s): /subscriptions/{sub}/resourceGroups/{name}."
  default     = ""
}

variable "openai_consumption_budget_account_resource_ids" {
  type        = list(string)
  description = "Optional full ARM ids of specific Microsoft.CognitiveServices/accounts resources to scope the budget. When empty, the budget filters all Cognitive Services accounts in the resource group."
  default     = []
}

variable "openai_consumption_budget_name" {
  type        = string
  description = "Budget name (unique within the resource group scope in Cost Management)."
  default     = "archlucid-openai-monthly"

  validation {
    condition     = length(var.openai_consumption_budget_name) >= 1 && length(var.openai_consumption_budget_name) <= 63
    error_message = "openai_consumption_budget_name must be 1-63 characters."
  }
}

variable "openai_consumption_budget_amount" {
  type        = number
  description = "Monthly budget amount in the subscription billing currency (e.g. USD)."
  default     = 300

  validation {
    condition     = var.openai_consumption_budget_amount > 0
    error_message = "openai_consumption_budget_amount must be positive."
  }
}

variable "openai_consumption_budget_time_period_start" {
  type        = string
  description = "Budget period start (RFC3339, first day of a month UTC). Azure requires month boundaries."
  default     = "2026-01-01T00:00:00Z"
}

variable "openai_consumption_budget_contact_emails" {
  type        = list(string)
  description = "Email addresses for budget alerts. When empty, contact_roles is used instead."
  default     = []
}

variable "openai_consumption_budget_contact_roles" {
  type        = list(string)
  description = "RBAC roles to notify when openai_consumption_budget_contact_emails is empty."
  default     = ["Owner"]
}
