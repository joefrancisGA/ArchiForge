> **Scope:** API versioning (ArchLucid) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# API versioning (ArchLucid)

## Objective

Document how **Asp.Versioning** is wired for `ArchLucid.Api`, how clients should call versioned routes, and how to introduce a future **v2** without breaking v1.

## Assumptions

- Primary contract remains **OpenAPI document** `openapi/v1.json` (see CI contract snapshot tests).
- Breaking changes require a new **major** API version (URL segment or explicit header).

## Constraints

- Anonymous infrastructure endpoints (`/health/*`, `/version`) and static docs remain **version-neutral**.
- Auth debug (`/api/auth/me`) and HTML docs (`DocsController`) are **version-neutral** by design.

## Current configuration

Registration lives in **`ArchLucid.Api/Startup/MvcExtensions.cs`**:

- **Default version:** `1.0` with `AssumeDefaultVersionWhenUnspecified = true`.
- **Reporting:** `ReportApiVersions = true` (response headers advertise supported versions).
- **URL substitution:** `SubstituteApiVersionInUrl = true` — routes use `v{version:apiVersion}` (e.g. **`/v1/architecture/...`**).

Controllers declare **`[ApiVersion("1.0")]`** or **`[ApiVersionNeutral]`**. A regression test **`ApiControllerApiVersionMetadataTests`** fails the build if a new controller omits both.

## Adding v2 (future)

1. Add **`[ApiVersion("2.0")]`** on new or duplicated controller types (or use **`[ApiVersions("1.0", "2.0")]`** on shared types during transition).
2. Register a second OpenAPI document in **`AddOpenApi`** / Swashbuckle configuration (group by `GroupNameFormat` from the API explorer).
3. Update **`Contracts/openapi-v1.contract.snapshot.json`** only for v1; add **`openapi-v2.contract.snapshot.json`** when v2 ships.
4. Document deprecation: **`Sunset`** / **`Deprecation`** headers or release notes — align with **`docs/API_CONTRACTS.md`**.

## Related

- **`docs/OPENAPI_CONTRACT_DRIFT.md`** — CI drift checks.
- **`ArchLucid.Api.Client`** — regenerate when OpenAPI changes.
