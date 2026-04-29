# Trial → paid identity handoff

## Objective

After a self-service trial tenant is marked **converted** (`POST /v1/tenant/convert`), corporate Entra JWTs need a stable mapping from the directory id (`tid` claim) to the existing ArchLucid tenant row. Without that mapping, operators relied on manual scope headers or ad-hoc SQL.

This runbook describes the **two-step** product flow and how to retire **local email/password** trial users safely.

## Assumptions

- Trial data (runs, manifests, findings) stays under the **same** `dbo.Tenants` row; conversion does not clone tenants.
- Commercial sign-in uses **workforce Entra** (multi-tenant app) with `tid` matching the customer’s Microsoft Entra tenant.
- Optional **local trial identity** (`dbo.IdentityUsers`) may exist for the same admin email used at signup.

## Operator flow

### 1. Convert the trial (billing / stub)

`POST /v1/tenant/convert` (Admin) — existing behavior. Sets `TrialStatus = Converted`, optional tier bump.

### 2. Bind the corporate directory

`POST /v1/tenant/link-entra` (Admin)

Request JSON:

| Field | Required | Description |
|-------|----------|-------------|
| `entraTenantId` | Yes | Guid from the customer’s Entra directory (`tid` in access tokens). |
| `localEmail` | No | Must be paired with `entraOid`. Matches `dbo.IdentityUsers.Email` after normalization. |
| `entraOid` | No | Entra object id (`oid`) for the user that replaces the local trial principal. |

**Directory only (External ID / SSO trials):**

```http
POST /v1/tenant/link-entra
Content-Type: application/json

{ "entraTenantId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" }
```

**Directory + local identity row:**

```json
{
  "entraTenantId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "localEmail": "admin@customer.com",
  "entraOid": "00000000-1111-2222-3333-444444444444"
}
```

Idempotent: repeating the same `entraTenantId` for the same tenant succeeds. The API will **not** overwrite `EntraTenantId` if it already points at a **different** directory (conflict).  
Local link is idempotent for the same `oid`; a different `oid` for the same email returns **409**.

### 3. Observe handoff status

`GET /v1/tenant/trial-status` (Read)

When `trialStatus` is `Converted` and `entraTenantId` is still null on the row, `identityHandoffPending` is **true**. After a successful `link-entra`, it becomes **false**.

## Retiring LocalIdentity mode

1. Run **link-entra** with `localEmail` + `entraOid` for each trial admin that used password signup (or confirm they already sign in with Entra only).
2. Verify `GET /v1/tenant/trial-status` shows `identityHandoffPending: false`.
3. Remove `LocalIdentity` from `Auth:Trial:Modes` in the environment where local login must be disabled (see [`docs/security/TRIAL_AUTH.md`](../security/TRIAL_AUTH.md)).
4. **Do not** delete `dbo.IdentityUsers` rows for audit/DSAR unless policy requires; `LinkedEntraOid` / `LinkedUtc` document the handoff.

## Security

- Only **AdminAuthority** may call `link-entra`. Directory binding is a tenancy-critical operation.
- Entra directory ids are **unique** per `dbo.Tenants.EntraTenantId` (filtered unique index) where configured — rebinding is blocked when another tenant already owns that `tid`.
- Audit: `TenantEntraDirectoryBound`, `TrialLocalIdentityLinkedToEntra` (when local link succeeds).

## Scalability / reliability

- Two small SQL updates; no bulk migrations. Retries are safe when idempotent.
- If `link-entra` succeeds for the directory but local link fails (rare race), bind can be retried; directory update is idempotent.

## Cost

Negligible: two indexed lookups and conditional updates per customer.

## Related code

- [`ArchLucid.Api/Controllers/Tenancy/TenantTrialController.cs`](../ArchLucid.Api/Controllers/Tenancy/TenantTrialController.cs)
- [`ArchLucid.Persistence/Tenancy/DapperTenantRepository.cs`](../ArchLucid.Persistence/Tenancy/DapperTenantRepository.cs) — `UpdateEntraTenantIdAsync`
- [`ArchLucid.Persistence/Identity/SqlTrialIdentityUserRepository.cs`](../ArchLucid.Persistence/Identity/SqlTrialIdentityUserRepository.cs) — `TryLinkLocalIdentityToEntraAsync`
- Migration **131** — `dbo.IdentityUsers.LinkedEntraOid`, `LinkedUtc`
