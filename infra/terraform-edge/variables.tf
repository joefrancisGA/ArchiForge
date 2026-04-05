variable "enable_front_door_waf" {
  type        = bool
  description = "When true, deploy Azure Front Door (Standard) with a WAF policy in Prevention mode. Keep false until Azure resources and backend hostnames exist."
  default     = false
}

variable "create_resource_group" {
  type        = bool
  description = "When true and enable_front_door_waf is true, create the resource group."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Resource group for Front Door profile and WAF policy."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region for the resource group (when create_resource_group is true)."
  default     = ""
}

variable "front_door_profile_name" {
  type        = string
  description = "Globally unique Front Door profile name (alphanumeric/dash)."
  default     = ""
}

variable "backend_hostname" {
  type        = string
  description = "Public hostname of the origin (e.g. myapim.azure-api.net or api.contoso.com). No https:// prefix."
  default     = ""
}

variable "origin_host_header" {
  type        = string
  description = "Optional Host header sent to origin; defaults to backend_hostname when empty."
  default     = ""
}

variable "route_patterns" {
  type        = list(string)
  description = "Path patterns served by this route (default all paths)."
  default     = ["/*"]
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "front_door_health_probe_path" {
  type        = string
  description = "Origin health probe path. Use /health/ready when the origin is the ArchiForge API (ASP.NET). Use / when the origin is the Next.js UI only (no readiness route)."
  default     = "/health/ready"
}

variable "secondary_backend_hostname" {
  type        = string
  description = "Optional passive standby origin (paired region API or APIM). Empty = single origin. Front Door sends traffic to priority 1 first; priority 2 used when primary is unhealthy."
  default     = ""
}

variable "secondary_origin_host_header" {
  type        = string
  description = "Optional Host header for secondary origin; defaults to secondary_backend_hostname when empty."
  default     = ""
}
