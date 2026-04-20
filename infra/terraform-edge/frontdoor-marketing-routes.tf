# Path-based routing: public marketing UI (Container Apps Next.js) vs API (APIM or API hostname).
# When var.marketing_backend_hostname is empty, the module keeps the legacy single-route layout.

resource "azurerm_cdn_frontdoor_origin_group" "marketing_ui" {
  count = local.marketing_edge_enabled ? 1 : 0

  name                     = "${var.front_door_profile_name}-mkt-og"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main[0].id

  load_balancing {
    additional_latency_in_milliseconds = 0
    sample_size                        = 4
    successful_samples_required        = 3
  }

  health_probe {
    interval_in_seconds = 100
    path                = "/"
    protocol            = "Https"
    request_type        = "HEAD"
  }
}

resource "azurerm_cdn_frontdoor_origin" "marketing_ui" {
  count = local.marketing_edge_enabled ? 1 : 0

  name                          = "${var.front_door_profile_name}-mkt-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.marketing_ui[0].id

  enabled                        = true
  host_name                      = trimspace(var.marketing_backend_hostname)
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = trimspace(var.marketing_origin_host_header) != "" ? trimspace(var.marketing_origin_host_header) : trimspace(var.marketing_backend_hostname)
  priority                       = 1
  weight                         = 1000
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_route" "marketing_public" {
  count = local.marketing_edge_enabled ? 1 : 0

  name                          = "${var.front_door_profile_name}-mkt-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.main[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.marketing_ui[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.marketing_ui[0].id]

  patterns_to_match   = var.marketing_site_route_patterns
  supported_protocols = ["Http", "Https"]

  forwarding_protocol    = "MatchRequest"
  https_redirect_enabled = true
  link_to_default_domain = true

  cdn_frontdoor_rule_set_ids = local.fd_pricing_json_redirect_enabled ? [azurerm_cdn_frontdoor_rule_set.pricing_redirects[0].id] : []
}

resource "azurerm_cdn_frontdoor_custom_domain" "marketing" {
  count = local.marketing_edge_enabled && trimspace(var.marketing_custom_domain_hostname) != "" ? 1 : 0

  name                     = "${var.front_door_profile_name}mktcd"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main[0].id
  host_name                = trimspace(var.marketing_custom_domain_hostname)

  tls {
    certificate_type = "ManagedCertificate"
  }
}

resource "azurerm_cdn_frontdoor_custom_domain_association" "marketing" {
  count = local.marketing_edge_enabled && trimspace(var.marketing_custom_domain_hostname) != "" ? 1 : 0

  cdn_frontdoor_custom_domain_id = azurerm_cdn_frontdoor_custom_domain.marketing[0].id

  cdn_frontdoor_route_ids = [
    azurerm_cdn_frontdoor_route.marketing_public[0].id,
    azurerm_cdn_frontdoor_route.main[0].id,
  ]
}
