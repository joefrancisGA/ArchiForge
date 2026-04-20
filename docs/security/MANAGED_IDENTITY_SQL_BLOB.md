> **Scope:** Managed identity for Azure SQL and Blob (ArchLucid) - full detail, tables, and links in the sections below.

# Managed identity for Azure SQL and Blob (ArchLucid)

## 1. Objective

Give operators a **repeatable pattern** for connecting the API to **Azure SQL** and **Azure Blob** using **Microsoft Entra ID authentication** (managed identity on Azure App Service / Container Apps / AKS workload identity), instead of storing SQL or storage keys in configuration.

## 2. Assumptions

- The API runs on Azure with a **user-assigned or system-assigned managed identity**.
- SQL is **Azure SQL**; storage is **Azure Storage** with blob containers used by ArchLucid features that target blob.
- Network path uses **private endpoints** where required by policy (see `infra/terraform-private/`).

## 3. Constraints

- **Least privilege:** grant only `db_datareader` / `db_datawriter` / custom roles as needed, not `db_owner`, unless a one-time migration role is isolated.
- **No public SMB:** tenant data does not flow over SMB port 445 exposed to the internet; use HTTPS + private endpoints for Azure services.
- **Rotation:** Entra tokens are short-lived; the SQL client stack acquires tokens automatically when using the correct connection string pattern.

## 4. Architecture overview

**Nodes:** API (MI), Entra ID, Azure SQL server, storage account, optional private endpoints.

**Edges:** API obtains token as MI → SQL / blob SDK presents token → Azure validates and authorizes.

## 5. Component breakdown

| Interface | Implementation |
|-----------|------------------|
| Identity | Managed identity (Entra) on compute |
| SQL auth | `Active Directory Default` / `Active Directory Managed Identity` in connection string or `SqlConnection` credential |
| Blob auth | `DefaultAzureCredential` or managed-identity–specific credential in the blob client options |
| Network | Private endpoints + DNS (`privatelink.database.windows.net`, `privatelink.blob.core.windows.net`) |

## 6. Data flow

1. Host assigns MI to the API workload.
2. SQL firewall / Entra admin allows the MI as a login/user in the target database.
3. Connection string uses **no SQL password**; driver uses MI for token acquisition.
4. Blob clients use the same MI for `Storage Blob Data Contributor` (or narrower custom role) on the account or container scope.

## 7. Security model

- **Deny by default:** SQL firewall locked to VNet / private endpoint; storage public access disabled when policy requires it.
- **Separation:** use distinct MIs for production vs non-production subscriptions when possible.
- **Audit:** enable Azure Activity Log / Defender for Cloud alerts on unusual authentication failures.

## 8. Operational considerations

- **Local dev:** keep `Trusted_Connection` / dev storage emulator; do not use production MI from laptops.
- **Troubleshooting:** verify Entra user mapping exists in SQL (`CREATE USER [mi-name] FROM EXTERNAL PROVIDER`), DNS resolves private endpoint FQDNs, and MI has correct RBAC on storage.
- **Cost:** MI has no direct meter; private endpoints and SQL compute/storage dominate cost.

## 9. Example connection string (Azure SQL, MI)

Use the pattern recommended for your driver version, for example:

`Server=tcp:your-server.database.windows.net,1433;Database=ArchLucid;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False`

Adjust to `Active Directory Managed Identity` with `User Id` / client id when using a **user-assigned** identity, per Microsoft documentation for `Microsoft.Data.SqlClient`.

## 10. RLS alignment

When **`SqlServer:RowLevelSecurity:ApplySessionContext`** is enabled, the API sets `SESSION_CONTEXT` after open. Managed identity affects **who** opens the connection; RLS still depends on **session keys** set by the app for tenant isolation (see `docs/security/MULTI_TENANT_RLS.md`).
