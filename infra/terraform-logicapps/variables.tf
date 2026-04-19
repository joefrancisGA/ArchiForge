variable "enable_logic_apps" {
  type        = bool
  description = "When true, deploy a Logic App (Standard) host plus backing storage and WS1 plan. Keep false until VNet + Service Bus subscriptions are designed."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Existing resource group for Logic App Standard resources."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region (must match the resource group when enable_logic_apps is true)."
  default     = ""
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to created resources."
}

variable "storage_account_name" {
  type        = string
  description = "Globally unique storage account name (lowercase alphanumeric, max 24) for the Logic App file share backend."
  default     = ""
}

variable "app_service_plan_name" {
  type        = string
  description = "App Service plan name hosting Logic App Standard (WS1)."
  default     = "asp-archlucid-logic"
}

variable "logic_app_name" {
  type        = string
  description = "Logic App (Standard) site name."
  default     = "archlucid-logic-edge"
}

variable "storage_share_name" {
  type        = string
  description = "Azure Files share name used by the Logic App runtime (workflow definitions are deployed separately)."
  default     = "workflow-content"
}
