variable "enable_api_management" {
  type        = bool
  description = "When true, deploy Azure API Management (Consumption). Set false for laptop-only work or stacks that omit APIM."
  default     = false
}

variable "create_resource_group" {
  type        = bool
  description = "When true and enable_api_management is true, create the resource group. When false, the resource group must already exist."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Resource group that will hold APIM (created or existing)."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region for new resources (required when create_resource_group is true). Ignored when using an existing resource group unless create_resource_group is true."
  default     = ""
}

variable "apim_name" {
  type        = string
  description = "Globally unique name for the API Management instance (alphanumeric, max 50). Example: archiforge-dev-apim."
  default     = ""
}

variable "apim_publisher_name" {
  type        = string
  description = "Publisher name shown in APIM developer portal metadata."
  default     = "ArchiForge"
}

variable "apim_publisher_email" {
  type        = string
  description = "Publisher contact email for APIM."
  default     = "admin@example.com"
}

variable "archiforge_api_backend_url" {
  type        = string
  description = "HTTPS origin URL for ArchiForge.Api as reached from APIM (trailing slash stripped), e.g. https://archiforge-api.azurewebsites.net"
  default     = ""
}

variable "apim_api_path_suffix" {
  type        = string
  description = "URL path suffix on the APIM gateway for this API (no leading slash), e.g. v1 or archiforge."
  default     = "v1"
}

variable "apim_openapi_spec_url" {
  type        = string
  description = "Optional HTTPS URL to import OpenAPI (openapi-link). When empty, a minimal bootstrap spec from the module is used; replace with your deployed API swagger URL, e.g. https://api.example.com/swagger/v1/swagger.json"
  default     = ""
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to created resources."
  default     = {}
}
