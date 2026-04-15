# JSON naming for public HTTP contracts

## API controllers

`ArchLucid.Api` configures MVC JSON serialization with **camelCase** property names and **camelCase** dictionary keys (`AddJsonOptions` in `Startup/MvcExtensions.cs`). Public JSON responses and request bodies from controllers should use **PascalCase in C#** on DTOs and rely on this policy for wire format.

## Problem Details

Error responses include:

- `type`, `title`, `status`, `detail`, `instance` (RFC 9457 Problem Details; obsoletes RFC 7807).
- `extensions.errorCode` — stable machine code (see `ArchLucid.Api.ProblemDetails.ProblemErrorCodes`).
- Additional `extensions` as documented per error (e.g. `retryAfterUtc`, `driftDetected`).

## OpenAPI / clients

Generated clients should assume **camelCase** JSON unless a specific DTO opts out with `JsonPropertyName` for an exception.

## Persisted aggregate `schemaVersion`

Authority **`GoldenManifest`** and **`GraphSnapshot`** include **`schemaVersion`** (integer, default **1**) for forward-compatible evolution. **Additive** optional fields do not require a bump; **removing or renaming** fields should bump **`schemaVersion`** and be documented in **ADR 0013** and release notes.
