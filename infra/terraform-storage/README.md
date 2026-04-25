# terraform-storage — large artifact blob storage

Creates an Azure Storage account (blob only usage) and private containers aligned with API **`ArtifactLargePayload`**:

| Container | Used for |
|-----------|----------|
| `golden-manifests` | Consolidated golden manifest JSON envelope |
| `artifact-bundles` | Combined artifacts + trace JSON per bundle |
| `artifact-contents` | Per-artifact body when row content is offloaded |

## Wiring

Do **not** commit **`tfplan`** / **`*.tfplan`** into this directory — Trivy IaC scans treat them as **`terraformplan-snapshot`** inputs; a stale plan can resurrect cleared misconfigurations in CI. Plans belong in CI artifacts or a local path ignored by git (see **`.gitignore`**).

1. Apply with `enable_storage_account = true` and a unique `storage_account_name`.
2. Set API **`ArtifactLargePayload:AzureBlobServiceUri`** to **`primary_blob_endpoint`** (include trailing slash optional; the client normalizes the service URI).
3. Grant the API **managed identity** **Storage Blob Data Contributor** on this storage account (or subscription scope if your policy allows).
4. For private access only: set **`public_network_access_enabled = false`**, deploy **`terraform-private`** blob private endpoint using **`storage_account_id`**, and ensure compute (e.g. Container Apps) has VNet integration to resolve `privatelink.blob.core.windows.net`.

## Security

- Containers are **private**; no anonymous blob access.
- **Network default is Deny** via **`azurerm_storage_account_network_rules`** (`default_action = "Deny"`); trusted Azure services can bypass per `bypass = ["AzureServices"]`. Add **`network_rule_subnet_ids`** (service endpoints) and/or **`network_rule_ip_allowlist`** only where you intentionally need public-path access. (Do not add a second `network_rules` block on the storage account — AzureRM allows only one style.)
- **Soft delete** and **versioning** reduce accidental loss.
- Do not expose SMB (port 445); this stack is **HTTPS blob** only.
