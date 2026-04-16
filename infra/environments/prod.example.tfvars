# Example knobs for PRODUCTION (private endpoints, edge, HA SQL listener — coordinate change windows).
# azure_subscription_id = "00000000-0000-0000-0000-000000000000"
# location              = "eastus2"

# terraform-private
# enable_private_data_plane = true

# terraform-storage (before Container Apps when large-payload offload is on)
# enable_storage_account = true

# terraform-container-apps
# enable_container_apps = true

# terraform-sql-failover (after servers + databases exist)
# enable_sql_failover_group = true

# terraform-edge
# enable_front_door_waf = true

# terraform-monitoring
# enable_monitoring_stack = true
