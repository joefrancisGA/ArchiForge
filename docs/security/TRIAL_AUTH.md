> **Scope:** Trial-tier authentication (External ID + local identity) - full detail, tables, and links in the sections below.

# Trial-tier authentication (External ID + local identity)

**Last reviewed:** 2026-04-17

## 1. Objective

Document the **trust boundary**, **configuration switches**, and **operational trade-offs** for ArchLucid **self-service trial** sign-in when the buyer has **no workforce Entra tenant** federated yet.

## 2. Assumptions

- Trial buyers may authenticate with **Microsoft personal accounts (MSA)**, **Google**, or **email/password** hosted in **Entra External ID (CIAM)** (`Auth:Trial:Modes` contains `MsaExternalId`).
- Some pilots need a **fallback** without any Microsoft consumer directory: **local email/password** backed by SQL (`Auth:Trial:Modes` contains `LocalIdentity`, migration **077**, table **`dbo.IdentityUsers`**).
- Production hosts are **misconfigured by default** if External ID mode is enabled without a tenant id (fail-fast rule).

## 3. Constraints

- Passwords for local trial identity must align with **NIST SP 800-63B** style guidance used in product code: **length bounds only** (no composition rules in validator).
- **Have I Been Pwned** range API is **optional** (`Auth:Trial:LocalIdentity:PwnedPasswordRangeCheckEnabled`) and uses **k-anonymity** (SHA-1 prefix only on the wire).
- **Email verification** is mandatory before **`TrialProvisioned`** when a **`dbo.IdentityUsers`** row exists for the registering admin email (`TrialTenantBootstrapService` + `TrialBootstrapEmailVerificationPolicy`).

## 4. Architecture overview

**Nodes:** Browser → ArchLucid.Api → (A) **Entra External ID** OIDC/OAuth endpoints, or (B) **`/v1/auth/trial/local/*`** → SQL **`dbo.IdentityUsers`** → minted **local RSA JWT** → existing **JwtBearer** validation path.

**Edges:** Tokens carry **`roles`** / **`tenant_id`** / scope claims consistent with workforce JWTs so **`ArchLucidPolicies`** stay unified.

**Flows:** Registration (`POST /v1/register`) remains anonymous; bootstrap runs only after provisioning and **email policy** allows it.

## 5. Component breakdown

| Surface | Responsibility |
|--------|----------------|
| **`Auth:Trial:Modes`** | Declares enabled trial auth lanes (`MsaExternalId`, `LocalIdentity`). |
| **`Auth:Trial:ExternalIdTenantId`** | Directory id for External ID; **required in Production** when `MsaExternalId` is enabled. |
| **`TrialExternalIdJwtBearerSupport`** | Relaxes issuer validation for consumer CIAM issuers when mapped to the JwtBearer pipeline. |
| **`TrialLocalIdentityAuthController`** | Register / verify-email / token for SQL-backed users (404 when `LocalIdentity` disabled). |
| **`TrialTenantBootstrapService`** | Gates demo seed + `TrialProvisioned` audit on **`ITrialBootstrapEmailVerificationPolicy`**. |

## 6. Data flow

1. **External ID:** User completes CIAM user flow → JWT to browser/app → ArchLucid.Api validates via JwtBearer → scope from claims + registration (B1) resolves tenant on first use.
2. **Local identity:** User registers → hashed password + email token stored → **`verify-email`** sets **`EmailVerifiedUtc`** → org registration may bootstrap trial.

## 7. Security model

- **Trust boundary:** External IdP (Microsoft/Google) vs ArchLucid-issued JWTs for local trial mode.
- **Local passwords:** **PBKDF2** via ASP.NET Core **`PasswordHasher`** (per-user salt embedded in stored hash format); **lockout** after repeated failures (`MaxFailedAccessAttemptsBeforeLockout`, default **5**).
- **Pwned range cache:** In-process **`IMemoryCache`** per SHA-1 prefix for **`PwnedPasswordRangeClient.RangeResponseCacheDuration` (24 hours)** to reduce egress and timing noise.
- **Least privilege:** Default trial JWT role for integration tests mirrors **Reader → `ReadAuthority`**; **ExecuteAuthority** requires **Operator** or **Admin** (same as Entra app roles).

## 8. Operational considerations

- **Cost:** HIBP calls are skipped when disabled; cache reduces duplicate range downloads.
- **Reliability:** External ID availability follows Microsoft’s SLA; local identity depends on SQL + email delivery for verification links.
- **Scalability:** Range cache is per **API replica** (memory); move to distributed cache only if prefix fan-out becomes hot.

## Related

- **`docs/security/SYSTEM_THREAT_MODEL.md`**
- **`docs/adr/0015-trial-tier-authentication-model.md`**
- **`docs/security/TRIAL_LIMITS.md`**
