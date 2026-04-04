check "monitoring_requires_resource_group" {
  assert {
    condition     = !var.enable_monitoring_stack || length(trimspace(var.resource_group_name)) > 0
    error_message = "enable_monitoring_stack = true requires resource_group_name."
  }
}

check "monitoring_requires_alert_email" {
  assert {
    condition     = !var.enable_monitoring_stack || length(trimspace(var.alert_email_address)) > 0
    error_message = "enable_monitoring_stack = true requires alert_email_address for the action group."
  }
}

check "managed_grafana_requires_resource_group" {
  assert {
    condition     = !var.enable_managed_grafana || length(trimspace(var.resource_group_name)) > 0
    error_message = "enable_managed_grafana = true requires resource_group_name."
  }
}
