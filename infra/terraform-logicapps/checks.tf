check "logic_apps_inputs" {
  assert {
    condition = !var.enable_logic_apps || (
      trimspace(var.resource_group_name) != "" &&
      trimspace(var.location) != "" &&
      trimspace(var.storage_account_name) != ""
    )

    error_message = "When enable_logic_apps is true, resource_group_name, location, and storage_account_name must be non-empty."
  }
}
