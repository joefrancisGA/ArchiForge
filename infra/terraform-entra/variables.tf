variable "enable_entra_api_app" {
  type        = bool
  description = "When true, register an Entra ID application with Admin / Operator / Reader app roles matching ArchLucid.Api. Keep false until you are ready to assign roles in the customer tenant."
  default     = false
}

variable "api_application_display_name" {
  type        = string
  description = "Display name of the API registration."
  default     = "ArchLucid API"
}

variable "api_identifier_uri" {
  type        = string
  description = "Application ID URI (must be unique in the tenant), used as JWT audience. Example: api://archlucid-api-contoso"
  default     = "api://archlucid-api"
}

variable "expose_roles_in_tokens" {
  type        = bool
  description = "Emit optional id/access token claims for app roles (recommended)."
  default     = true
}
