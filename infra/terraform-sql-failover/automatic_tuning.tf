# Server-level automatic tuning: applies to all databases on each logical server unless a database overrides.
# https://learn.microsoft.com/en-us/azure/azure-sql/database/automatic-tuning-overview
# azurerm has no first-class block for this; Azure/azapi updates the existing `automaticTuning/current` sub-resource.

resource "azapi_update_resource" "primary_sql_server_automatic_tuning" {
  count = local.sql_automatic_tuning_primary_eligible ? 1 : 0

  type        = "Microsoft.Sql/servers/automaticTuning@2021-11-01"
  resource_id = "${var.primary_sql_server_resource_id}/automaticTuning/current"

  body = jsonencode({
    properties = {
      options = {
        forceLastGoodPlan = { desiredState = var.sql_automatic_tuning_force_last_good_plan }
        createIndex       = { desiredState = var.sql_automatic_tuning_create_index }
        dropIndex         = { desiredState = var.sql_automatic_tuning_drop_index }
      }
    }
  })
}

resource "azapi_update_resource" "partner_sql_server_automatic_tuning" {
  count = local.sql_automatic_tuning_partner_eligible ? 1 : 0

  type        = "Microsoft.Sql/servers/automaticTuning@2021-11-01"
  resource_id = "${var.partner_sql_server_resource_id}/automaticTuning/current"

  body = jsonencode({
    properties = {
      options = {
        forceLastGoodPlan = { desiredState = var.sql_automatic_tuning_force_last_good_plan }
        createIndex       = { desiredState = var.sql_automatic_tuning_create_index }
        dropIndex         = { desiredState = var.sql_automatic_tuning_drop_index }
      }
    }
  })
}
