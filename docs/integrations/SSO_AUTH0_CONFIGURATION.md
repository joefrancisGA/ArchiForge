> **Scope:** Auth0 OIDC/JWT SSO configuration for ArchLucid API; covers Auth0 tenant/application setup, custom claims via Actions, ArchLucid `ArchLucidAuth` config, token verification, and common troubleshooting. Audience: enterprise IT / identity administrators using Auth0 as their primary IdP. **Entra ID is the primary supported IdP** — this guide is for organizations that use Auth0 instead. Does **not** modify ArchLucid auth code; references existing auth paths for contributor context.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# SSO configuration — Auth0

**Last reviewed:** 2026-04-26

## 1. Overview

ArchLucid authenticates API requests via **JWT bearer tokens** validated by the ASP.NET Core `JwtBearer` middleware. When `ArchLucidAuth:Mode` is set to `JwtBearer`, the API downloads OIDC metadata from the configured `Authority`, validates the token signature, audience, issuer, and lifetime, then maps the `roles` claim to internal authorization policies.

> **Primary IdP:** Microsoft Entra ID is the primary supported identity provider. ArchLucid's multi-tenant Entra support, SCIM provisioning, and trial authentication features are built and tested against Entra. This guide covers **Auth0** for organizations whose workforce identity is managed there. The JWT bearer pipeline is standards-based (OIDC Discovery + RS256/RS384/RS512), so any compliant IdP works, but Auth0-specific steps are called out below.

### What ArchLucid expects from the IdP

| Requirement | Detail |
|-------------|--------|
| **Protocol** | OIDC / OAuth 2.0 with JWT access tokens |
| **Discovery** | `/.well-known/openid-configuration` at the Authority URL |
| **Signing** | RSA (RS256 recommended) — keys published via JWKS (`jwks_uri`) |
| **Audience (`aud`)** | Must match `ArchLucidAuth:Audience` (e.g. `api://archlucid`) |
| **Issuer (`iss`)** | Must match the OIDC Discovery `issuer` field at the Authority URL |
| **Roles claim** | `roles` array containing one of: `Admin`, `Operator`, `Reader`, `Auditor` |
| **Name claim** | `preferred_username`, `name`, `email`, or configured via `ArchLucidAuth:NameClaimType` |

**Contributor context:** JWT bearer configuration lives in `ArchLucid.Api/Auth/Services/AuthServiceCollectionExtensions.cs` (`ConfigureJwtBearer`). Role-to-permission mapping is in `ArchLucid.Api/Auth/Services/ArchLucidRoleClaimsTransformation.cs`. Options model: `ArchLucid.Api/Auth/Models/ArchLucidAuthOptions.cs`. Authorization policies: `ArchLucid.Core/Authorization/ArchLucidPolicies.cs` and `ArchLucid.Core/Authorization/ArchLucidRoles.cs`.

---

## 2. Auth0-side configuration

### 2.1 Register an API

1. Sign in to the **Auth0 Dashboard**.
2. Navigate to **Applications → APIs → Create API**.
3. Configure:
   - **Name:** `ArchLucid API`
   - **Identifier (Audience):** `api://archlucid` (this value becomes the `aud` claim; must match `ArchLucidAuth:Audience`)
   - **Signing Algorithm:** RS256

Record the **API Audience** value — you will use it in ArchLucid configuration.

### 2.2 Create an application

1. Navigate to **Applications → Applications → Create Application**.
2. Choose the appropriate type:
   - **Regular Web Application** — if the ArchLucid UI initiates the login (authorization code + PKCE).
   - **Machine to Machine** — if a CI/CD pipeline or CLI calls the API with client credentials.
3. Configure the application:
   - **Name:** `ArchLucid`
   - **Allowed Callback URLs:** `https://<your-archlucid-ui-host>/api/auth/callback/auth0`
   - **Allowed Logout URLs:** `https://<your-archlucid-ui-host>`
   - **Allowed Web Origins:** `https://<your-archlucid-ui-host>`
4. On the **APIs** tab, authorize the application for the `ArchLucid API` audience.

Record the **Domain**, **Client ID**, and **Client Secret** (for confidential clients).

### 2.3 Add a custom `roles` claim via Auth0 Actions

Auth0 does not include a `roles` claim in access tokens by default. Use an **Auth0 Action** (post-login trigger) to inject it.

1. Navigate to **Actions → Library → Build Custom**.
2. Create a new Action:
   - **Name:** `Add ArchLucid roles`
   - **Trigger:** Login / Post Login
3. Add the following code:

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'roles';

  const assignedRoles = event.authorization?.roles || [];

  if (assignedRoles.length > 0) {
    api.accessToken.setCustomClaim(namespace, assignedRoles);
  }
};
```

4. **Deploy** the Action.
5. Navigate to **Actions → Flows → Login** and drag the new Action into the flow.

> **Important:** Auth0 namespaced claims (e.g. `https://archlucid.net/roles`) are common in Auth0 tutorials, but ArchLucid reads the claim named exactly `roles`. The Action above sets a top-level `roles` claim. If your Auth0 tenant requires namespaced claims, you must also set `ArchLucidAuth:NameClaimType` or adjust the `RoleClaimType` — contact ArchLucid support for guidance.

### 2.4 Create Auth0 roles

1. Navigate to **User Management → Roles**.
2. Create roles that match ArchLucid's expected values:

| Auth0 role name | ArchLucid role | Authorization policy |
|-----------------|---------------|---------------------|
| `Admin` | `Admin` | `AdminAuthority` — host administration, policy-pack lifecycle |
| `Operator` | `Operator` | `ExecuteAuthority` — create runs, replays, governance actions |
| `Reader` | `Reader` | `ReadAuthority` — read-only access to runs, manifests, governance queries |
| `Auditor` | `Auditor` | `RequireAuditor` — audit CSV/JSON export, compliance-oriented access |

3. Assign users to the appropriate roles via **User Management → Users → \<user\> → Roles**.

---

## 3. ArchLucid-side configuration

Set the following values in `appsettings.json`, `appsettings.Production.json`, environment variables, or Azure Key Vault:

```json
{
  "ArchLucidAuth": {
    "Mode": "JwtBearer",
    "Authority": "https://your-tenant.auth0.com/",
    "Audience": "api://archlucid",
    "MultiTenantEntra": false,
    "NameClaimType": "name"
  }
}
```

| Key | Value | Notes |
|-----|-------|-------|
| `ArchLucidAuth:Mode` | `JwtBearer` | Enables JWT bearer validation (not `ApiKey` or `DevelopmentBypass`) |
| `ArchLucidAuth:Authority` | `https://<your-tenant>.auth0.com/` | The Auth0 tenant domain **with trailing slash**. The API appends `.well-known/openid-configuration` to download signing keys and metadata. |
| `ArchLucidAuth:Audience` | `api://archlucid` | Must match the **Identifier** configured on the Auth0 API |
| `ArchLucidAuth:MultiTenantEntra` | `false` | **Must be `false`** — multi-tenant Entra issuer validation rejects non-Entra issuers (see `EntraMultiTenantJwtBearerConfigurator.cs`) |
| `ArchLucidAuth:NameClaimType` | `name` | Auth0 tokens include `name` by default; use `email` if your organization prefers email as the display identity |

> **Auth0 Authority trailing slash:** Auth0's OIDC Discovery issuer is `https://<tenant>.auth0.com/` (with trailing slash). The Authority value must match the `iss` claim exactly. Omitting the trailing slash causes issuer validation to fail.

### Environment variable form

```
ArchLucidAuth__Mode=JwtBearer
ArchLucidAuth__Authority=https://your-tenant.auth0.com/
ArchLucidAuth__Audience=api://archlucid
ArchLucidAuth__MultiTenantEntra=false
ArchLucidAuth__NameClaimType=name
```

### Disable API key authentication

When using JWT bearer, disable the API key scheme to avoid confusion:

```json
{
  "Authentication": {
    "ApiKey": {
      "Enabled": false,
      "DevelopmentBypassAll": false
    }
  }
}
```

---

## 4. Verification

### 4.1 Obtain a token from Auth0

**Authorization code flow** (interactive — use for testing):

```bash
# Open in browser to initiate OIDC login:
# https://your-tenant.auth0.com/authorize?\
#   response_type=code&client_id=<CLIENT_ID>&redirect_uri=<REDIRECT_URI>\
#   &audience=api://archlucid&scope=openid profile&state=test123

# Exchange the authorization code for tokens:
curl -s -X POST \
  "https://your-tenant.auth0.com/oauth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "grant_type": "authorization_code",
    "client_id": "<CLIENT_ID>",
    "client_secret": "<CLIENT_SECRET>",
    "code": "<AUTH_CODE>",
    "redirect_uri": "<REDIRECT_URI>"
  }'
```

**Client credentials flow** (M2M / CI):

```bash
curl -s -X POST \
  "https://your-tenant.auth0.com/oauth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "grant_type": "client_credentials",
    "client_id": "<CLIENT_ID>",
    "client_secret": "<CLIENT_SECRET>",
    "audience": "api://archlucid"
  }'
```

### 4.2 Call the ArchLucid health endpoint

```bash
# Readiness check (authenticated)
curl -s -w "\nHTTP %{http_code}\n" \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  "https://<archlucid-host>/health/ready"
```

Expected: HTTP 200 with `Healthy` or a JSON health report.

### 4.3 Inspect the authenticated principal

```bash
curl -s \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  "https://<archlucid-host>/api/auth/me" | jq .
```

Verify the response includes:
- `name` or `email` matching the Auth0 user
- `roles` containing the expected ArchLucid role (`Admin`, `Operator`, `Reader`, or `Auditor`)

---

## 5. Troubleshooting

### 5.1 `401 Unauthorized` — audience mismatch

**Symptom:** API returns 401; logs show `IDX10214: Audience validation failed`.

**Cause:** The `aud` claim in the Auth0-issued token does not match `ArchLucidAuth:Audience`.

**Fix:**
1. In Auth0 Dashboard → **Applications → APIs → ArchLucid API**, check the **Identifier** field.
2. Ensure `ArchLucidAuth:Audience` in ArchLucid configuration matches exactly (case-sensitive).
3. When requesting tokens, always include `audience=api://archlucid` in the authorization or token request. Auth0 issues **opaque tokens** (not JWTs) when no audience is specified — ArchLucid cannot validate opaque tokens.

### 5.2 `401 Unauthorized` — issuer mismatch (trailing slash)

**Symptom:** API returns 401; logs show `IDX10205: Issuer validation failed`.

**Cause:** Auth0 issues tokens with `iss` = `https://<tenant>.auth0.com/` (trailing slash). If `ArchLucidAuth:Authority` omits the trailing slash, the JWT middleware downloads metadata successfully but the issuer claim comparison fails.

**Fix:** Ensure `ArchLucidAuth:Authority` includes the trailing slash: `https://your-tenant.auth0.com/`.

### 5.3 `403 Forbidden` — missing or incorrect `roles` claim

**Symptom:** Authentication succeeds (no 401), but protected endpoints return 403.

**Cause:** The JWT does not contain a `roles` claim, or the claim value does not match ArchLucid's expected role strings (`Admin`, `Operator`, `Reader`, `Auditor`).

**Fix:**
1. Decode the access token at [jwt.io](https://jwt.io) or with `jq -R 'split(".") | .[1] | @base64d | fromjson'` and inspect the `roles` claim.
2. If `roles` is missing, verify the Auth0 Action (§2.3) is deployed and placed in the Login flow.
3. If the claim is namespaced (e.g. `https://archlucid.net/roles` instead of `roles`), update the Action to use a non-namespaced claim name, or contact ArchLucid support about `RoleClaimType` configuration.
4. Confirm the user has an Auth0 role assigned (**User Management → Users → \<user\> → Roles**) and the role name matches exactly (case-sensitive): `Admin`, `Operator`, `Reader`, `Auditor`.
5. For **M2M (client credentials)** tokens: Auth0 does not run the Login flow for client credentials grants. You must use a separate **Machine to Machine** Action trigger (Credentials Exchange) or assign roles to the M2M application directly via the Auth0 Management API.

### 5.4 `401 Unauthorized` — clock skew

**Symptom:** Tokens that appear valid in jwt.io are rejected; logs show `IDX10222: Lifetime validation failed. The token is expired` or `IDX10223: ... token is not yet valid`.

**Cause:** The system clock on the ArchLucid API host is out of sync with Auth0's token issuance server. The default `ClockSkew` in the JWT bearer pipeline is **5 minutes** (Microsoft default) or **2 minutes** for local-key configurations.

**Fix:**
1. Verify the API host clock: `date -u` (Linux) or `[DateTime]::UtcNow` (PowerShell).
2. Sync with NTP: `sudo ntpdate pool.ntp.org` or ensure the Windows Time service is running.
3. If the skew is persistent and greater than 5 minutes, investigate infrastructure clock drift.

### 5.5 `401 Unauthorized` — `MultiTenantEntra` left enabled

**Symptom:** All Auth0 tokens rejected with `Issuer is not a valid Azure AD v2.0 issuer`.

**Cause:** `ArchLucidAuth:MultiTenantEntra` is `true`. The `EntraMultiTenantJwtBearerConfigurator` installs a custom issuer validator that **only** accepts `https://login.microsoftonline.com/{tid}/v2.0` issuers.

**Fix:** Set `ArchLucidAuth:MultiTenantEntra` to `false` when using Auth0.

### 5.6 Auth0 returns opaque tokens instead of JWTs

**Symptom:** The token from Auth0 is a short opaque string (not a three-part base64 JWT). ArchLucid rejects it immediately.

**Cause:** Auth0 returns opaque tokens when the token request does not include an `audience` parameter.

**Fix:** Always include `audience=api://archlucid` (matching your API Identifier) in the `/authorize` and `/oauth/token` requests. Auth0 only issues JWT access tokens when an audience is specified.

---

## 6. Role and policy reference

| ArchLucid role | Authorization policy | Permissions granted |
|---------------|---------------------|---------------------|
| `Admin` | `AdminAuthority` | `commit:run`, `seed:results`, `export:consulting-docx`, `replay:comparisons`, `replay:diagnostics`, `metrics:read` |
| `Operator` | `ExecuteAuthority` | `commit:run`, `seed:results`, `export:consulting-docx`, `replay:comparisons`, `replay:diagnostics` |
| `Reader` | `ReadAuthority` | `metrics:read` |
| `Auditor` | `RequireAuditor` | `metrics:read` (plus audit export surfaces) |

**Source:** `ArchLucid.Api/Auth/Services/ArchLucidRoleClaimsTransformation.cs`, `ArchLucid.Core/Authorization/ArchLucidRoles.cs`, `ArchLucid.Core/Authorization/ArchLucidPolicies.cs`.

## Related

- **[SCIM provisioning](SCIM_PROVISIONING.md)** — automate user/group sync from Auth0 to ArchLucid (Auth0 supports outbound SCIM via enterprise connections).
- **[appsettings.Entra.sample.json](../../ArchLucid.Api/appsettings.Entra.sample.json)** — reference config for the primary Entra ID integration.
- **[docs/security/TRIAL_AUTH.md](../security/TRIAL_AUTH.md)** — trial-tier authentication (External ID + local identity).
- **[README.md § API authentication](../../README.md#api-authentication-archlucidauth)** — summary of all auth modes.
