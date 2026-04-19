variable "resource_group_name" {
  type        = string
  description = "Resource group that will own the Service Bus namespace."
}

variable "location" {
  type        = string
  description = "Azure region for the namespace (e.g. eastus)."
}

variable "namespace_name" {
  type        = string
  description = "Globally unique Service Bus namespace name."
  default     = "archlucid-events"
}

variable "topic_name" {
  type        = string
  description = "Topic used for ArchLucid integration JSON events."
  default     = "archlucid-integration-events"
}

variable "sku" {
  type        = string
  description = "Service Bus SKU (Basic | Standard | Premium). Standard supports topics."
  default     = "Standard"
}

variable "zone_redundant" {
  type        = bool
  description = "When true, enable zone redundancy (Standard/Premium; requires region support)."
  default     = false
}

variable "max_delivery_count" {
  type        = number
  description = "Max deliveries before Service Bus dead-letters the message on the subscription."
  default     = 10
}

variable "worker_subscription_name" {
  type        = string
  description = "Subscription consumed by ArchLucid workers (load leveling across replicas)."
  default     = "archlucid-worker"
}

variable "external_subscription_name" {
  type        = string
  description = "Subscription for external/automation consumers (fan-out)."
  default     = "archlucid-external"
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to created resources."
  default     = {}
}

variable "api_managed_identity_principal_id" {
  type        = string
  description = "Optional Entra principal id for the API managed identity (Azure Service Bus Data Sender)."
  default     = ""
}

variable "worker_managed_identity_principal_id" {
  type        = string
  description = "Optional Entra principal id for the worker managed identity (Sender + Receiver)."
  default     = ""
}

variable "enable_private_endpoint" {
  type        = bool
  description = "When true, create a private endpoint for the namespace (recommended with terraform-private)."
  default     = false
}

variable "private_endpoints_subnet_id" {
  type        = string
  description = "Subnet id for private endpoint NICs (delegation not required)."
  default     = ""
}

variable "private_dns_zone_ids" {
  type        = list(string)
  description = "Private DNS zone resource ids for privatelink.servicebus.windows.net (from terraform-private)."
  default     = []
}

variable "enable_logic_app_governance_approval_subscription" {
  type        = bool
  description = "When true, create a dedicated topic subscription for Logic Apps (Standard) that only receives com.archlucid.governance.approval.submitted (application property event_type)."
  default     = false
}

variable "logic_app_governance_approval_subscription_name" {
  type        = string
  description = "Subscription name on the integration topic for governance approval routing (Logic App trigger)."
  default     = "archlucid-logicapp-governance-approval"
}

variable "governance_logic_app_managed_identity_principal_id" {
  type        = string
  description = "Optional Entra principal id for the governance Logic App (Standard) system-assigned or user-assigned identity — Azure Service Bus Data Receiver on the namespace when non-empty and the governance subscription is enabled."
  default     = ""
}
