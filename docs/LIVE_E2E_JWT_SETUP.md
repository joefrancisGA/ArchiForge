> **Scope:** Live E2E — JwtBearer with a local RSA public key (CI / lab) - full detail, tables, and links in the sections below.

# Live E2E — JwtBearer with a local RSA public key (CI / lab)

## Objective

Run Playwright **`live-api-*.spec.ts`** against **`ArchLucidAuth:Mode=JwtBearer`** when Entra metadata is unavailable, by validating JWTs with **`ArchLucidAuth:JwtSigningPublicKeyPemPath`** plus **`JwtLocalIssuer`** / **`JwtLocalAudience`**.

## Assumptions

- **Non-production only:** configuration validation rejects **`JwtSigningPublicKeyPemPath`** in Production (use Entra **`Authority`** + metadata there).
- **Claim shape:** tokens use short JWT claim names **`roles`** (array or repeated) and **`name`** aligned with **`LIVE_JWT_ACTOR_NAME`** (default **`JwtE2eAdmin`**). The API sets **`JwtBearerOptions.MapInboundClaims = false`** for this path so **`roles`** matches **`[Authorize]`** role checks.
- **Next.js BFF:** browser calls that go through **`archlucid-ui`’s API proxy** may not send **`Authorization`**; set **`ARCHLUCID_PROXY_BEARER_TOKEN`** to the same value as **`LIVE_JWT_TOKEN`** so the server attaches **`Authorization: Bearer`** upstream.
- **RSC / server `fetch`:** Run detail and other Server Components call the API **directly** (same origin as the proxy target, not via `/api/proxy`). **`getServerUpstreamAuthHeaders`** in **`archlucid-ui`** applies **`ARCHLUCID_PROXY_BEARER_TOKEN`** there too so JWT CI matches Playwright’s direct API auth.

## Constraints

- Issuer and audience on the API **must** match the mint script (`scripts/ci/mint_ci_jwt.py`).
- CI job **`ui-e2e-live-jwt`** is merge-blocking when enabled in **`.github/workflows/ci.yml`**; failures indicate JWT + UI proxy + RSC auth drift.

## Architecture overview

**Nodes:** OpenSSL (RSA keypair), Python (**PyJWT** + **cryptography**), SQL catalog, **`ArchLucid.Api`**, Playwright, optional Next **`webServer`**.

**Edges:** Private key → mint script → **`LIVE_JWT_TOKEN`**; public PEM → API env; Playwright → direct API or Next proxy with **`ARCHLUCID_PROXY_BEARER_TOKEN`**.

## CI constants (subset job)

| Setting | Value |
|---------|--------|
| SQL database | **`ArchLucidLiveE2eJwt`** |
| **`ArchLucidAuth:JwtLocalIssuer`** | `https://ci.archlucid.local` |
| **`ArchLucidAuth:JwtLocalAudience`** | `api://archlucid-live-e2e-jwt` |
| Nightly (full suite) | DB **`ArchLucidLiveE2eNightlyJwt`**, issuer `https://nightly.archlucid.local`, audience `api://archlucid-live-e2e-nightly-jwt` |

## Local quick test

1. Generate keys and mint a token (see **`scripts/ci/mint_ci_jwt.py --help`**).
2. Point the API at the **public** PEM and set issuer/audience to match mint args.
3. Export **`LIVE_JWT_TOKEN`** and **`ARCHLUCID_PROXY_BEARER_TOKEN`** (same string) before **`npx playwright test`** (default live config).

## Related links

- [LIVE_E2E_AUTH_PARITY.md](LIVE_E2E_AUTH_PARITY.md)
- [LIVE_E2E_AUTH_ASSUMPTIONS.md](LIVE_E2E_AUTH_ASSUMPTIONS.md)
- [LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)
