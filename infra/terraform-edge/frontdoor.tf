resource "azurerm_cdn_frontdoor_profile" "main" {
  count = local.fd_enabled ? 1 : 0

  name                = var.front_door_profile_name
  resource_group_name = local.fd_resource_group_name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = var.tags
}

resource "azurerm_cdn_frontdoor_firewall_policy" "main" {
  count = local.fd_enabled ? 1 : 0

  name                = "${var.front_door_profile_name}waf"
  resource_group_name = local.fd_resource_group_name
  sku_name            = "Standard_AzureFrontDoor"
  enabled             = true
  mode                = "Prevention"

  managed_rule {
    type    = "Microsoft_DefaultRuleSet"
    version = "2.1"
    action  = "Block"
  }

  managed_rule {
    type    = "Microsoft_BotManagerRuleSet"
    version = "1.0"
    action  = "Block"
  }
}

resource "azurerm_cdn_frontdoor_endpoint" "main" {
  count = local.fd_enabled ? 1 : 0

  name                     = "${var.front_door_profile_name}-ep"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main[0].id

  tags = var.tags
}

resource "azurerm_cdn_frontdoor_origin_group" "main" {
  count = local.fd_enabled ? 1 : 0

  name                     = "${var.front_door_profile_name}-og"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main[0].id

  load_balancing {
    additional_latency_in_milliseconds = 0
    sample_size                        = 4
    successful_samples_required        = 3
  }

  health_probe {
    interval_in_seconds = 100
    path                = var.front_door_health_probe_path
    protocol            = "Https"
    request_type        = "HEAD"
  }
}

resource "azurerm_cdn_frontdoor_origin" "main" {
  count = local.fd_enabled ? 1 : 0

  name                          = "${var.front_door_profile_name}-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.main[0].id

  enabled                        = true
  host_name                      = var.backend_hostname
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = local.origin_header
  priority                       = 1
  weight                         = 1000
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_origin" "secondary" {
  count = local.secondary_origin_enabled ? 1 : 0

  name                          = "${var.front_door_profile_name}-origin-secondary"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.main[0].id

  enabled                        = true
  host_name                      = trimspace(var.secondary_backend_hostname)
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = local.secondary_origin_header
  priority                       = 2
  weight                         = 500
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_route" "main" {
  count = local.fd_enabled ? 1 : 0

  name                          = "${var.front_door_profile_name}-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.main[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.main[0].id
  cdn_frontdoor_origin_ids = local.secondary_origin_enabled ? [
    azurerm_cdn_frontdoor_origin.main[0].id,
    azurerm_cdn_frontdoor_origin.secondary[0].id,
  ] : [azurerm_cdn_frontdoor_origin.main[0].id]

  patterns_to_match   = var.route_patterns
  supported_protocols = ["Http", "Https"]

  forwarding_protocol    = "MatchRequest"
  https_redirect_enabled = true
  link_to_default_domain = true
}

resource "azurerm_cdn_frontdoor_security_policy" "main" {
  count = local.fd_enabled ? 1 : 0

  name                     = "${var.front_door_profile_name}-sec"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main[0].id

  security_policies {
    firewall {
      cdn_frontdoor_firewall_policy_id = azurerm_cdn_frontdoor_firewall_policy.main[0].id

      association {
        patterns_to_match = var.route_patterns

        domain {
          cdn_frontdoor_domain_id = azurerm_cdn_frontdoor_endpoint.main[0].id
        }
      }
    }
  }
}
