> **Scope:** ADR 0006: URL-path API versioning (/v1) - full detail, tables, and links in the sections below.

# ADR 0006: URL-path API versioning (`/v1`)

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

Clients need a stable contract while allowing future breaking changes.

## Decision

Major version in the URL path: **`/v1/...`**, with Asp.Versioning reporting supported/deprecated headers where configured.

## Consequences

- **Positive:** Obvious routing at edge (APIM, Front Door); easy Bruno/OpenAPI alignment.
- **Negative:** Longer paths; v2 will duplicate surface until old versions sunset.

## v2 introduction and deprecation (operational policy — 2026-04-15)

- **Breaking change** (requires `/v2` or a negotiated version): removing or changing the JSON type of a required response field, changing HTTP method or path for the same logical operation, or changing semantics of a field in a way that breaks existing clients.
- **Non-breaking:** adding optional response fields, adding new endpoints, adding enum values only when clients are documented to ignore unknown values, or tightening validation in ways that reject previously invalid payloads only.
- **Deprecation:** minimum **6 calendar months** from first documented deprecation (release notes + `Sunset` HTTP response header per [RFC 8594](https://www.rfc-editor.org/rfc/rfc8594.html) when implemented) until removal of the old route.
- **Notification:** document deprecations in release notes and (when implemented) **`Sunset`** / **`Deprecation`** response headers; optional future integration event type may be added to **`docs/INTEGRATION_EVENT_CATALOG.md`** for subscribers.
- **JSON payloads:** integration events and public JSON contracts carry integer **`schemaVersion`** (see `schemas/integration-events/*.schema.json`); consumers must treat unknown `schemaVersion` as “requires code update” while still accepting older versions until sunset.

## Links

- `docs/API_CONTRACTS.md`
- `schemas/integration-events/README.md`
