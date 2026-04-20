> **Scope:** Key Vault references for secrets (Azure) - full detail, tables, and links in the sections below.

# Key Vault references for secrets (Azure)

Production and shared environments should **not** store SQL connection strings, OpenAI API keys, or long-lived API keys in `appsettings.*.json` committed to git.

## Pattern

1. Create an Azure Key Vault and store each secret (e.g. `archlucid-sql-connection-string`, `archlucid-azure-openai-api-key`).
2. Grant the API’s managed identity **Get** permission on secrets.
3. In **Azure App Service** → **Configuration** → **Application settings**, set each setting to a [Key Vault reference](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references):

   - `ConnectionStrings__ArchLucid` → `@Microsoft.KeyVault(VaultName=...;SecretName=archlucid-sql-connection-string)`
   - `AzureOpenAI__ApiKey` → `@Microsoft.KeyVault(...)`
   - **`ArchLucid__FallbackLlm__ApiKey`** → `@Microsoft.KeyVault(...)` when **`ArchLucid:FallbackLlm:Enabled`** is **`true`** (secondary Azure OpenAI chat deployment; store as its own secret, e.g. `archlucid-fallback-llm-api-key`)
   - `Authentication__ApiKey__AdminKey` / `Authentication__ApiKey__ReadOnlyKey` → `@Microsoft.KeyVault(...)` (when `ArchLucidAuth:Mode` is `ApiKey` and API key auth is enabled)

Double underscores (`__`) map to nested JSON sections in ASP.NET Core configuration.

## Sample file

See `ArchLucid.Api/appsettings.KeyVault.sample.json` for a non-functional template of the same shape (do not commit real vault names if they are sensitive; the file is documentation-only).

## Terraform

Represent the App Service settings as `azurerm_app_service` / `azurerm_linux_web_app` `app_settings` blocks whose values are Key Vault reference strings, and use a `azurerm_key_vault_access_policy` or RBAC for the web app’s system-assigned identity.
