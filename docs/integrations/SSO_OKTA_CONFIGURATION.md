> **Scope:** Okta OIDC/JWT SSO configuration for ArchLucid API; covers Okta application setup, ArchLucid `ArchLucidAuth` config, token verification, and common troubleshooting. Audience: enterprise IT / identity administrators using Okta as their primary IdP. **Entra ID is the primary supported IdP** — this guide is for organizations that use Okta instead. Does **not** modify ArchLucid auth code; references existing auth paths for contributor context.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# SSO configuration — Okta

**Last reviewed:** 2026-04-26

## 1. Overview

ArchLucid authenticates API requests via **JWT bearer tokens** validated by the ASP.NET Core `JwtBearer` middleware. When `ArchLucidAuth:Mode` is set to `JwtBearer`, the API downloads OIDC metadata from the configured `Authority`, validates the token signature, audience, issuer, and lifetime, then maps the `roles` claim to internal authorization policies.

> **Primary IdP:** Microsoft Entra ID is the primary supported identity provider. ArchLucid's multi-tenant Entra support, SCIM provisioning, and trial authentication features are built and tested against Entra. This guide covers **Okta** for organizations whose workforce identity is managed there. The JWT bearer pipeline is standards-based (OIDC Discovery + RS256/RS384/RS512), so any compliant IdP works, but Okta-specific steps are called out below.

### What ArchLucid expects from the IdP

| Requirement | Detail |
|-------------|--------|
| **Protocol** | OIDC / OAuth 2.0 with JWT access tokens |
| **Discovery** | `/.well-known/openid-configuration` at the Authority URL |
| **Signing** | RSA (RS256 recommended) — keys published via JWKS (`jwks_uri`) |
| **Audience (`aud`)** | Must match `ArchLucidAuth:Audience` (e.g. `api://archlucid`) |
| **Issuer (`iss`)** | Must match the OIDC Discovery `issuer` field at the Authority URL |
| **Roles claim** | `roles` array (or mapped via `ArchLucidRoleClaimsTransformation`) containing one of: `Admin`, `Operator`, `Reader`, `Auditor` |
| **Name claim** | `preferred_username`, `name`, or configured via `ArchLucidAuth:NameClaimType` |

**Contributor context:** JWT bearer configuration lives in `ArchLucid.Api/Auth/Services/AuthServiceCollectionExtensions.cs` (`ConfigureJwtBearer`). Role-to-permission mapping is in `ArchLucid.Api/Auth/Services/ArchLucidRoleClaimsTransformation.cs`. Options model: `ArchLucid.Api/Auth/Models/ArchLucidAuthOptions.cs`. Authorization policies: `ArchLucid.Core/Authorization/ArchLucidPolicies.cs` and `ArchLucid.Core/Authorization/ArchLucidRoles.cs`.

---

## 2. Okta-side configuration

### 2.1 Create an API authorization server (or use the default)

1. Sign in to the **Okta Admin Console**.
2. Navigate to **Security → API**.
3. Use the **default** authorization server (`https://<your-okta-domain>/oauth2/default`) or create a **custom** one:
   - **Name:** `ArchLucid API`
   - **Audience:** `api://archlucid` (must match the value you configure in ArchLucid)
   - **Description:** ArchLucid architecture workflow API

Record the **Issuer URI** shown on the authorization server settings page (e.g. `https://dev-123456.okta.com/oauth2/default`).

### 2.2 Create an OIDC application

1. Navigate to **Applications → Create App Integration**.
2. Select **OIDC - OpenID Connect**.
3. Choose the appropriate application type:
   - **Web Application** — if the ArchLucid UI initiates the login (authorization code + PKCE).
   - **Service (Machine-to-Machine)** — if a CI/CD pipeline or CLI calls the API with client credentials.
4. Configure the application:
   - **App integration name:** `ArchLucid`
   - **Grant types:** Authorization Code (+ PKCE for SPAs/public clients), or Client Credentials for M2M.
   - **Sign-in redirect URIs:** `https://<your-archlucid-ui-host>/api/auth/callback/okta` (adjust for your deployment).
   - **Sign-out redirect URIs:** `https://<your-archlucid-ui-host>` (optional).
   - **Controlled access:** Assign to the appropriate groups/people.

Record the **Client ID** and **Client Secret** (for confidential clients).

### 2.3 Add a custom `roles` claim

ArchLucid reads roles from the `roles` claim on the JWT access token. Okta does not include a `roles` claim by default — you must add one.

1. Navigate to **Security → API → \<your authorization server\> → Claims**.
2. Click **Add Claim**:
   - **Name:** `roles`
   - **Include in token type:** **Access Token** (always)
   - **Value type:** Expression
   - **Value:** an Okta Expression Language expression that maps groups to ArchLucid role strings.

**Example expression** (maps group membership to role strings):

```
isMemberOfGroupName("ArchLucid-Admins") ? "Admin" :
isMemberOfGroupName("ArchLucid-Operators") ? "Operator" :
isMemberOfGroupName("ArchLucid-Auditors") ? "Auditor" : "Reader"
```

> If you need **multiple roles per user**, use a Groups claim mapped to an array (see Okta docs on [customizing tokens with a Groups claim](https://developer.okta.com/docs/guides/customize-tokens-groups-claim/)). You can alternatively use a claim of type **Groups** with a filter, and configure the claim name as `roles`.

3. Verify the claim appears in the **Token Preview** tab with the expected value.

### 2.4 Create Okta groups

Create groups that correspond to ArchLucid roles:

| Okta group | ArchLucid role | Authorization policy |
|------------|---------------|---------------------|
| `ArchLucid-Admins` | `Admin` | `AdminAuthority` — host administration, policy-pack lifecycle |
| `ArchLucid-Operators` | `Operator` | `ExecuteAuthority` — create runs, replays, governance actions |
| `ArchLucid-Readers` | `Reader` | `ReadAuthority` — read-only access to runs, manifests, governance queries |
| `ArchLucid-Auditors` | `Auditor` | `RequireAuditor` — audit CSV/JSON export, compliance-oriented access |

Assign users to the appropriate groups.

---

## 3. ArchLucid-side configuration

Set the following values in `appsettings.json`, `appsettings.Production.json`, environment variables, or Azure Key Vault:

```json
{
  "ArchLucidAuth": {
    "Mode": "JwtBearer",
    "Authority": "https://dev-123456.okta.com/oauth2/default",
    "Audience": "api://archlucid",
    "MultiTenantEntra": false,
    "NameClaimType": "preferred_username"
  }
}
```

| Key | Value | Notes |
|-----|-------|-------|
| `ArchLucidAuth:Mode` | `JwtBearer` | Enables JWT bearer validation (not `ApiKey` or `DevelopmentBypass`) |
| `ArchLucidAuth:Authority` | `https://<okta-domain>/oauth2/<auth-server-id>` | The Okta authorization server issuer URI. The API appends `/.well-known/openid-configuration` to download signing keys and metadata. |
| `ArchLucidAuth:Audience` | `api://archlucid` | Must match the **Audience** configured on the Okta authorization server |
| `ArchLucidAuth:MultiTenantEntra` | `false` | **Must be `false`** — multi-tenant Entra issuer validation rejects non-Entra issuers (see `EntraMultiTenantJwtBearerConfigurator.cs`) |
| `ArchLucidAuth:NameClaimType` | `preferred_username` | Claim type used as the display name; Okta tokens typically use `sub` or `preferred_username` |

### Environment variable form

```
ArchLucidAuth__Mode=JwtBearer
ArchLucidAuth__Authority=https://dev-123456.okta.com/oauth2/default
ArchLucidAuth__Audience=api://archlucid
ArchLucidAuth__MultiTenantEntra=false
ArchLucidAuth__NameClaimType=preferred_username
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

### 4.1 Obtain a token from Okta

**Authorization code flow** (interactive — use for testing):

```bash
# Open in browser to initiate OIDC login:
# https://dev-123456.okta.com/oauth2/default/v1/authorize?\
#   response_type=code&client_id=<CLIENT_ID>&redirect_uri=<REDIRECT_URI>\
#   &scope=openid profile&state=test123

# Exchange the authorization code for tokens:
curl -s -X POST \
  "https://dev-123456.okta.com/oauth2/default/v1/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "code=<AUTH_CODE>" \
  -d "redirect_uri=<REDIRECT_URI>" \
  -d "client_id=<CLIENT_ID>" \
  -d "client_secret=<CLIENT_SECRET>"
```

**Client credentials flow** (M2M / CI):

```bash
curl -s -X POST \
  "https://dev-123456.okta.com/oauth2/default/v1/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=<CLIENT_ID>" \
  -d "client_secret=<CLIENT_SECRET>" \
  -d "scope=archlucid"
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
- `name` or `preferred_username` matching the Okta user
- `roles` containing the expected ArchLucid role (`Admin`, `Operator`, `Reader`, or `Auditor`)

---

## 5. Troubleshooting

### 5.1 `401 Unauthorized` — audience mismatch

**Symptom:** API returns 401; logs show `IDX10214: Audience validation failed`.

**Cause:** The `aud` claim in the Okta-issued token does not match `ArchLucidAuth:Audience`.

**Fix:**
1. In Okta Admin Console → **Security → API → \<authorization server\>**, check the **Audience** field.
2. Ensure `ArchLucidAuth:Audience` in ArchLucid configuration matches exactly (case-sensitive).
3. If you changed the audience, request a new token — existing tokens carry the old `aud`.

### 5.2 `401 Unauthorized` — issuer mismatch or OIDC discovery failure

**Symptom:** API returns 401; logs show `IDX10205: Issuer validation failed` or `Unable to obtain configuration from .../.well-known/openid-configuration`.

**Cause:** `ArchLucidAuth:Authority` does not match the Okta authorization server's issuer, or the API host cannot reach Okta's OIDC metadata endpoint (DNS, firewall, proxy).

**Fix:**
1. Confirm the Authority URL matches the Okta issuer exactly. Common mistake: omitting `/oauth2/default` or using a custom authorization server id when you meant the default (or vice versa).
2. Test connectivity from the API host: `curl -s https://dev-123456.okta.com/oauth2/default/.well-known/openid-configuration | jq .issuer`.
3. If behind a corporate proxy, configure `HTTP_PROXY` / `HTTPS_PROXY` environment variables for the API process.

### 5.3 `403 Forbidden` — missing or incorrect `roles` claim

**Symptom:** Authentication succeeds (no 401), but protected endpoints return 403.

**Cause:** The JWT does not contain a `roles` claim, or the claim value does not match ArchLucid's expected role strings (`Admin`, `Operator`, `Reader`, `Auditor`).

**Fix:**
1. Decode the access token at [jwt.io](https://jwt.io) or with `jq -R 'split(".") | .[1] | @base64d | fromjson'` and inspect the `roles` claim.
2. If `roles` is missing, verify the custom claim is configured on the Okta authorization server (§2.3) and is included in **access tokens** (not just ID tokens).
3. If the value is present but wrong (e.g. `admin` lowercase), update the Okta claim expression to emit the exact case-sensitive values: `Admin`, `Operator`, `Reader`, `Auditor`.
4. Confirm the user is assigned to the correct Okta group and the group is linked to the OIDC application.

### 5.4 `401 Unauthorized` — clock skew

**Symptom:** Tokens that appear valid in jwt.io are rejected; logs show `IDX10222: Lifetime validation failed. The token is expired` or `IDX10223: ... token is not yet valid`.

**Cause:** The system clock on the ArchLucid API host is out of sync with Okta's token issuance server. The default `ClockSkew` in the JWT bearer pipeline is **5 minutes** (Microsoft default) or **2 minutes** for local-key configurations.

**Fix:**
1. Verify the API host clock: `date -u` (Linux) or `[DateTime]::UtcNow` (PowerShell).
2. Sync with NTP: `sudo ntpdate pool.ntp.org` or ensure the Windows Time service is running.
3. If the skew is persistent and greater than 5 minutes, investigate infrastructure clock drift.

### 5.5 `401 Unauthorized` — `MultiTenantEntra` left enabled

**Symptom:** All Okta tokens rejected with `Issuer is not a valid Azure AD v2.0 issuer`.

**Cause:** `ArchLucidAuth:MultiTenantEntra` is `true`. The `EntraMultiTenantJwtBearerConfigurator` installs a custom issuer validator that **only** accepts `https://login.microsoftonline.com/{tid}/v2.0` issuers.

**Fix:** Set `ArchLucidAuth:MultiTenantEntra` to `false` when using Okta.

### 5.6 Token does not contain expected scopes or claims

**Symptom:** `GET /api/auth/me` returns a principal with no roles or unexpected name.

**Fix:**
1. In Okta, use the **Token Preview** tab on the authorization server to inspect what claims are emitted for a given user/application/grant type.
2. Ensure the `roles` claim is configured for **Access Token** (not ID Token only).
3. If using client credentials, ensure the claim expression handles the M2M case (client credentials tokens have no user context; group membership expressions may not resolve). Consider using a static claim or a different claim mapping for M2M clients.

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

- **[SCIM provisioning (Okta supported)](SCIM_PROVISIONING.md)** — automate user/group sync from Okta to ArchLucid.
- **[appsettings.Entra.sample.json](../../ArchLucid.Api/appsettings.Entra.sample.json)** — reference config for the primary Entra ID integration.
- **[docs/security/TRIAL_AUTH.md](../security/TRIAL_AUTH.md)** — trial-tier authentication (External ID + local identity).
- **[README.md § API authentication](../../README.md#api-authentication-archlucidauth)** — summary of all auth modes.
