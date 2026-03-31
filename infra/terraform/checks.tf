check "api_management_required_inputs" {
  assert {
    condition = !var.enable_api_management || (
      length(trimspace(var.resource_group_name)) > 0 &&
      length(trimspace(var.apim_name)) > 0 &&
      length(trimspace(var.archiforge_api_backend_url)) > 0 &&
      (!var.create_resource_group || length(trimspace(var.location)) > 0)
    )
    error_message = "With enable_api_management = true, set resource_group_name, apim_name, archiforge_api_backend_url. If create_resource_group = true, also set location."
  }
}
