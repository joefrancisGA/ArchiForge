> **Scope:** For operators configuring Microsoft Entra ID enterprise application provisioning into ArchLucid via SCIM 2.0; not general IdP architecture, non-Microsoft IdPs, or a substitute for the SCIM threat model and admin API docs.

# SCIM 2.0 — Microsoft Entra ID provisioning recipe

## Objective

Configure **automated inbound provisioning** from **Microsoft Entra ID** (Enterprise Application → provisioning) to ArchLucid using **SCIM 2.0** ([RFC 7644](https://www.rfc-editor.org/rfc/rfc7644.html)), **without** granting Entra administrative access to ArchLucid operator APIs beyond the SCIM surface.

This document is written for operators who understand Entra Enterprise Applications but may be new to SCIM.

## What ArchLucid implements (protocol surface)

ArchLucid exposes SCIM **under `/scim/v2`** with **`application/scim+json`** responses.

| Capability | HTTP | Notes |
|------------|------|--------|
| **Service provider configuration** | `GET /scim/v2/ServiceProviderConfig` | Declares **PATCH** support, **filter** support (`maxResults` capped), **`bulk` unsupported**, and **`authenticationSchemes`** describing bearer-token authentication metadata per RFC 7644 §5. |
| **Schemas** | `GET /scim/v2/Schemas` | Lightweight schema discovery — User + Group identifiers. |
| **Resource types** | `GET /scim/v2/ResourceTypes` | Lists **`User`** (`endpoint`: `/Users`) and **`Group`** (`endpoint`: `/Groups`) relative to the SCIM root. |
| **Users** | `GET`, `POST` `/scim/v2/Users` | List supports **`filter`** (RFC 7644 §3.4.2.2). Create parses core fields ArchLucid persists today (see mapping table). |
| **User instance** | `GET`, `PUT`, `PATCH`, `DELETE` `/scim/v2/Users/{id}` | **`DELETE`** performs ArchLucid **soft deprovision** (directory deactivate aligned with audit semantics — callers should treat this as IdP-driven offboarding). |

**Authentication:** SCIM endpoints use the **`ScimBearer`** ASP.NET Core authentication scheme — **`Authorization: Bearer <ArchLucid-issued SCIM secret>`**. This is **not** an OAuth authorization-code flow on the SCIM URL itself; operators obtain secrets via ArchLucid’s **admin SCIM token API** (see below).

For engineering threat framing and abuse cases, see [`docs/security/SCIM_THREAT_MODEL.md`](../security/SCIM_THREAT_MODEL.md).

## Prerequisites

1. **Microsoft Entra ID tenant** with permission to create Enterprise Applications and configure provisioning.
2. **ArchLucid deployment URL** reachable from Entra’s provisioning service (HTTPS, valid TLS chain).
3. **ArchLucid tenant administrator** able to call **`POST /v1/admin/scim/tokens`** (Admin-authority JWT session — normal ArchLucid operator authentication, **not** Entra hitting admin APIs during provisioning).
4. Agreement on **enterprise SCIM seat limits** (`EnterpriseSeatsLimit`) where applicable — provisioning **active** users increments metered seats when limits are configured.

## SCIM base URL

Entra’s “Tenant URL” / SCIM endpoint should be:

```text
https://<your-archlucid-host>/scim/v2
```

Examples:

```text
https://api.customer.example/scim/v2
```

Entra constructs resource URLs such as **`…/scim/v2/Users`** and **`…/scim/v2/Users/{id}`** automatically when the base ends at **`/scim/v2`**.

## Authentication configuration

### 1) Mint a SCIM bearer token (ArchLucid admin)

Call (authenticated as ArchLucid **Admin** policy — same mechanism as other admin endpoints):

```http
POST /v1/admin/scim/tokens
```

Successful response JSON includes **`plaintextToken`** exactly once — store it in your secrets manager.

Revocation:

```http
DELETE /v1/admin/scim/tokens/{tokenId}
```

### 2) Configure Entra provisioning credentials

In the Enterprise Application → **Provisioning** → **Admin Credentials**:

- **Secret token**: paste **`plaintextToken`** from ArchLucid (no `Bearer ` prefix in Entra UI — Entra adds the scheme).
- **Tenant URL**: `https://<host>/scim/v2`

### 3) Validation-only smoke (`curl`)

Replace placeholders:

```bash
curl -sS -H "Authorization: Bearer $ARCHLUCID_SCIM_TOKEN" \
  -H "Accept: application/scim+json" \
  "https://<host>/scim/v2/ServiceProviderConfig"
```

Expect **`200`** and JSON containing **`"patch":{"supported":true}`**, **`"filter":{"supported":true,...}`**, and a non-empty **`authenticationSchemes`** array typed **`oauthbearertoken`** (metadata — actual auth remains the issued bearer secret).

## Attribute mapping — Entra → ArchLucid

ArchLucid’s inbound parser (`ScimUserResourceParser`) currently persists:

| Entra / SCIM attribute | ArchLucid persistence | Notes |
|------------------------|----------------------|--------|
| **`userName`** | **`UserName`** (required) | Primary immutable SCIM identifier for ArchLucid rows today — usually maps from Entra **userPrincipalName** or **mail**. |
| **`externalId`** | **`ExternalId`** | Defaults to **`userName`** when omitted — commonly maps from Entra **objectId** if you emit it as SCIM **`externalId`**. |
| **`displayName`** | **`DisplayName`** | Optional — maps directly when Entra sends root **`displayName`**. ArchLucid **does not** yet synthesize **`displayName`** from **`name.givenName` / `name.familyName`** during POST — prefer mapping **`displayName`** in Entra or issuing PATCH updates after create. |
| **`active`** | **`Active`** | Defaults **`true`** when omitted — maps from Entra account enabled semantics. |
| **`name.*`**, **`emails[]`** | *(ignored today for persistence)* | Safe to send for Entra compatibility — ignored beyond JSON parsing tolerance. |

**Filtering:** ArchLucid translates common Entra probes:

| Example Entra-style filter | Backing column |
|---------------------------|----------------|
| `userName eq "alice@contoso.com"` | **`UserName`** |
| `emails[type eq "work"].value eq "alice@contoso.com"` | **`UserName`** (primary email aligns with **`userName`** in typical mappings) |
| `externalId eq "{objectGuid}"` | **`ExternalId`** |

Unsupported filters return **`400`** with **`scimType":"invalidFilter"`** — prefer narrowing mappings or simplifying probes before contacting support.

## Step-by-step — Entra Enterprise Application

High-level sequence (exact Entra UI labels change slightly between portals):

1. **Entra admin center** → **Enterprise applications** → **New application** → **Create your own application**.
2. Choose **Integrate any other application you don’t find in the gallery** (non-gallery SCIM app).
3. Open **Provisioning** → set mode **Automatic**.
4. Paste **Tenant URL** (`https://<host>/scim/v2`) and **Secret token** (ArchLucid plaintext SCIM bearer).
5. **Test connection** — Entra should receive **`200`** from **`GET ServiceProviderConfig`** with bearer auth.
6. Edit **Mappings**:

   - Enable **Provision Microsoft Entra users**.
   - Map **`userPrincipalName` → `userName`** (common pattern).
   - Map **`objectId` → `externalId`** when you require stable immutable correlation independent of UPN retries.
   - Map **`displayName` → `displayName`** if pages rely on friendly naming.

7. Assign users/groups → save → **Start provisioning**.
8. Rotate tokens periodically via **`POST /v1/admin/scim/tokens`** + Entra credential update — revoke stale tokens via **`DELETE /v1/admin/scim/tokens/{id}`**.

## Automated regression coverage (CI)

These **`ArchLucid.Api.Tests`** fixtures simulate Entra payloads against **`JwtLocalSigningWebAppFactory`** (**InMemory** storage — **no Entra tenant required**):

| Class | Scenario |
|-------|-----------|
| `ScimUsersPostEntraProvisioningIntegrationTests` | POST User with **`name`** + **`emails[type=work]`** |
| `ScimUsersGetFilterEntraProvisioningIntegrationTests` | GET **`filter=userName eq "…"`** and **`filter=emails[type eq "work"].value eq "…"`** |
| `ScimUsersPatchEntraProvisioningIntegrationTests` | PATCH **`displayName`** + **`active`** |
| `ScimUsersDeleteProvisioningIntegrationTests` | DELETE → subsequent GET **`404`** SCIM error |
| `ScimServiceProviderConfigCapabilitiesIntegrationTests` | Discovery metadata (**PATCH**, **FILTER**, **authenticationSchemes**) |

Parser literals live in **`ArchLucid.Application.Tests/Scim/ScimFilterParserTests.cs`** (`Parse_Entra_*` cases).

## Troubleshooting — common HTTP outcomes

| HTTP | Typical meaning | Mitigation |
|------|-----------------|------------|
| **401 Unauthorized** | Missing/invalid bearer or revoked SCIM token | Re-mint token; confirm Entra stores secret without stray whitespace; verify clock skew negligible (tokens validated server-side via persistence lookup). |
| **403 Forbidden** (`mutability`) | Enterprise SCIM seat limit reached while activating users | Raise **`EnterpriseSeatsLimit`**, deactivate dormant SCIM users, or temporarily pause provisioning assignments. |
| **400 Bad Request** (`invalidFilter`) | Filter grammar unsupported or attribute unknown to SQL translator | Switch Entra probe to **`userName`** / **`externalId`** patterns validated above; simplify compound valued-attribute selectors beyond **`emails[type eq "work"].value`**. |
| **409 Conflict** (`uniqueness`) | Duplicate **`externalId`** for tenant | Align **`externalId`** mapping (usually stable **`objectId`**). |
| **404 Not Found** (`notFound`) | Unknown **`{id}`** after delete or typo | Expected post-delete — Entra should unlink stale references via sync cycles. |

## Procurement cross-links

- Evidence row: **[Procurement pack index — SCIM recipe](../go-to-market/PROCUREMENT_PACK_INDEX.md)**  
- Questionnaire pre-fill pointers: **[SIG Core pre-fill](../security/SIG_CORE_2026.md)**

## Evolution notes

Future enhancements (not commitments in this doc): richer **`displayName`** derivation from **`name`**, multi-valued **`emails`** persistence distinct from **`userName`**, broader **`filter`** grammar coverage, and optional **`bulk`** batch endpoints — gated behind RFC interoperability testing.
