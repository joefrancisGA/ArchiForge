> **Scope:** Microsoft Entra ID automatic provisioning toward ArchLucid’s SCIM 2.0 endpoints — configuration guidance only.

# SCIM provisioning with Entra ID (recipe)

**Audience:** Tenant administrators configuring **Enterprise applications → Provisioning**.

**Companion threat model:** [docs/security/SCIM_THREAT_MODEL.md](../security/SCIM_THREAT_MODEL.md)

**Evidence URL shape (hosted API):**

- Tenant SCIM root: **`https://<api-host>/scim/v2`**
- Discovery: **`GET /scim/v2/ServiceProviderConfig`** (`application/scim+json`) — [`ScimDiscoveryController`](../../ArchLucid.Api/Controllers/Scim/ScimDiscoveryController.cs)
- Users / Groups: [`ScimUsersController`](../../ArchLucid.Api/Controllers/Scim/ScimUsersController.cs), [`ScimGroupsController`](../../ArchLucid.Api/Controllers/Scim/ScimGroupsController.cs)

## 1. Issue a provisioning token (operator UI / admin API)

Provisioning uses **`IScimTokenIssuer`** → opaque **`archlucid_scim.<public-key>.<base64url-secret>`** bearer verified by **`ScimBearerTokenAuthenticator`** (see **`ArchLucid.Application`** / **`ArchLucid.Api.Auth.Scim`**). Store only in Entra **Secret token**; rotate via the tenant token lifecycle already documented beside SCIM controllers.

## 2. Entra enterprise application checklist

| Step | Notes |
|------|--------|
| **Provisioning mode** | Automatic |
| **Tenant URL** | `https://<api-host>/scim/v2` (no trailing resource path — Entra appends **`/Users`**, **`/Groups`**) |
| **Secret token** | Full plaintext SCIM bearer from step 1 |
| **Mappings** | Map Entra **`userPrincipalName`** / **`mail`** → ArchLucid user fields expected by **`ScimUsersController`** payloads; validate one test user before broad assignment |
| **Filters / scoping** | Optional Entra assignment + scope rules — SCIM filtering support is surfaced in **`ScimDiscoveryController`** (`filter.supported = true`). Parser regressions live in **`ScimFilterParserTests`** |

## 3. Verification sequence

1. **Anonymous discovery** → **401** (see **`ScimBearerSecurityIntegrationTests`**).
2. **Authenticated discovery** → **200** + `application/scim+json` (**`ScimDiscoveryAuthenticatedIntegrationTests`**).
3. **Single user PATCH/PUT** smoke from Entra provisioning logs → correlate with **`ScimUsersController`** audits.

## 4. Constraints

| Constraint | Handling |
|-----------|----------|
| **No SOC2 attestation** | This document proves **interoperability wiring**, not third-party SOC2 for ArchLucid. |
| **IdP quirks** | Entra emits SCIM filters and attribute casing that differ slightly from textbook RFC examples — **`ScimFilterParser`** favors Entra-realistic literals (see **`Parse_Entra_*`** tests). |

## 5. Troubleshooting

- **401 Unauthorized** → clock skew negligible; check bearer prefix **`archlucid_scim.`**, revocation, copy/paste of secret segments.
- **403 / empty writes** → policy **`ScimWrite`** requires successful SCIM scheme authentication (**not** Entra SSO JWT).
