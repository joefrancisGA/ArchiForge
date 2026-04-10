# NOTE: Resource addresses in this module may still use the historical `archiforge` token to avoid Terraform state disruption.
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

data "azuread_client_config" "current" {}

resource "random_uuid" "role_admin" {}

resource "random_uuid" "role_operator" {}

resource "random_uuid" "role_reader" {}

resource "random_uuid" "oauth_scope_access_as_user" {}

locals {
  entra_enabled = var.enable_entra_api_app
}

resource "azuread_application" "api" {
  count = local.entra_enabled ? 1 : 0

  display_name     = var.api_application_display_name
  sign_in_audience = "AzureADMyOrg"

  identifier_uris = [var.api_identifier_uri]

  owners = [data.azuread_client_config.current.object_id]

  api {
    requested_access_token_version = 2

    oauth2_permission_scope {
      admin_consent_description  = "Allow the application to access ArchLucid on behalf of the signed-in user."
      admin_consent_display_name = "Access ArchLucid API"
      enabled                    = true
      id                         = random_uuid.oauth_scope_access_as_user.result
      type                       = "User"
      user_consent_description   = "Allow this client to call ArchLucid when you are signed in."
      user_consent_display_name  = "Access ArchLucid API"
      value                      = "access_as_user"
    }
  }

  app_role {
    allowed_member_types = ["User", "Application"]
    description          = "Full access to ArchLucid API operations."
    display_name         = "Admin"
    enabled              = true
    id                   = random_uuid.role_admin.result
    value                = "Admin"
  }

  app_role {
    allowed_member_types = ["User", "Application"]
    description          = "Operate runs, replay, and exports without full admin."
    display_name         = "Operator"
    enabled              = true
    id                   = random_uuid.role_operator.result
    value                = "Operator"
  }

  app_role {
    allowed_member_types = ["User", "Application"]
    description          = "Read metrics and health-oriented surfaces."
    display_name         = "Reader"
    enabled              = true
    id                   = random_uuid.role_reader.result
    value                = "Reader"
  }

  dynamic "optional_claims" {
    for_each = var.expose_roles_in_tokens ? [1] : []
    content {
      access_token {
        name = "roles"
      }
      id_token {
        name = "roles"
      }
    }
  }
}

resource "azuread_service_principal" "api" {
  count = local.entra_enabled ? 1 : 0

  client_id = azuread_application.api[0].client_id
  owners    = [data.azuread_client_config.current.object_id]
}
