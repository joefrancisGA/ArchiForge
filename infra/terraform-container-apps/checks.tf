check "container_apps_required_inputs" {
  assert {
    condition = !var.enable_container_apps || (
      length(trimspace(var.resource_group_name)) > 0 &&
      (!var.create_resource_group || length(trimspace(var.location)) > 0) &&
      length(trimspace(var.api_container_image)) > 0 &&
      length(trimspace(var.ui_container_image)) > 0
    )
    error_message = "With enable_container_apps = true, set resource_group_name, api_container_image, and ui_container_image. If create_resource_group = true, also set location."
  }
}

check "container_apps_replica_bounds" {
  assert {
    condition = !var.enable_container_apps || (
      var.api_min_replicas >= 0 &&
      var.api_max_replicas >= 1 &&
      var.api_min_replicas <= var.api_max_replicas &&
      var.ui_min_replicas >= 0 &&
      var.ui_max_replicas >= 1 &&
      var.ui_min_replicas <= var.ui_max_replicas &&
      var.worker_min_replicas >= 0 &&
      var.worker_max_replicas >= 1 &&
      var.worker_min_replicas <= var.worker_max_replicas
    )
    error_message = "Replica counts must be non-negative (min), max at least 1, and min must not exceed max for API, UI, and worker."
  }
}

check "container_apps_scale_targets" {
  assert {
    condition = !var.enable_container_apps || (
      var.api_scale_concurrent_requests >= 1 &&
      var.ui_scale_concurrent_requests >= 1
    )
    error_message = "HTTP scale concurrent_requests must be at least 1 for API and UI."
  }
}

check "container_apps_internal_lb_requires_subnet" {
  assert {
    condition = !var.enable_container_apps || !var.container_apps_internal_load_balancer || (
      length(trimspace(var.container_apps_subnet_id)) > 0
    )
    error_message = "container_apps_internal_load_balancer = true requires container_apps_subnet_id to be set (VNet-integrated environment)."
  }
}

check "container_apps_artifact_blob_for_api" {
  assert {
    condition = !var.enable_container_apps || (
      length(trimspace(var.artifact_blob_service_uri)) > 0 &&
      length(trimspace(var.artifact_storage_account_id)) > 0
    )
    error_message = "With enable_container_apps = true, set artifact_blob_service_uri and artifact_storage_account_id (from terraform-storage outputs) so the API can offload large payloads to Azure Blob with managed identity."
  }
}
