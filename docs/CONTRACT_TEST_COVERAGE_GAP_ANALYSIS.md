> **Scope:** Contract test coverage — gap analysis - full detail, tables, and links in the sections below.

# Contract test coverage — gap analysis

## Objective

Describe how ArchLucid guards **HTTP/OpenAPI contracts** today, list **known gaps**, and give a repeatable pattern for closing the highest-risk gaps without duplicating Schemathesis or full snapshot churn.

## Assumptions

- **Microsoft OpenAPI** document at `/openapi/v1.json` is the canonical contract for stable diffing (`OpenApiContractSnapshotTests`).
- **NSwag-generated client** (`ArchLucid.Api.Client`) tracks the same surface; drift is caught when CI regenerates or snapshot tests fail.
- **Repository “contract” tests** (`ArchLucid.Persistence.Tests/Contracts/*`) validate storage adapters against in-memory and SQL implementations — orthogonal to HTTP.

## Constraints

- Contract tests must stay **fast** and **hermetic** for the default `dotnet test` filter (`Suite=Core`, no SQL).
- Full response-body golden files per endpoint **do not** scale; prefer **OpenAPI snapshot** + **targeted invariants** + **property tests** on DTOs.

## Architecture overview

| Layer | What is tested | Primary assets |
|-------|----------------|----------------|
| OpenAPI shape | Entire document vs committed JSON | `OpenApiContractSnapshotTests`, `Contracts/openapi-v1.contract.snapshot.json` |
| OpenAPI invariants | High-signal fields independent of full snapshot noise | `OpenApiContractInvariantsTests` |
| DTO JSON | Round-trip serialization | `JsonRoundTripPropertyTests`, `KeyContractsJsonRoundTripTests` |
| Persistence contracts | Repository behavior | `*RepositoryContractTests`, Dapper / in-memory fixtures |
| External fuzz | Example-based API exploration | CI Schemathesis job (see `docs/API_FUZZ_TESTING.md`) |

## Component breakdown

### Covered well

- **Global OpenAPI drift** — any path/schema change updates the snapshot or fails CI.
- **Core contract DTOs** — `ArchitectureRun`, `ArchitectureRequest`, `GovernanceApprovalRequest`, `DecisionNode` (see `ArchLucid.Contracts.Tests`).

### Gaps (prioritized)

1. **Per-status ProblemDetails** — not every error path is asserted to return RFC 9457-shaped bodies; partial coverage exists via guard tests (`ApiControllerProblemDetailsSourceGuardTests`). *Mitigation:* extend guard or add one integration test per **new** controller file template.
2. **Auth / security scheme wiring** — OpenAPI documents schemes, but **runtime** enforcement tests are sparse compared to happy-path tests. *Mitigation:* keep `OpenApiContractInvariantsTests` aligned with required public routes (`/v1/register`, health if exposed later).
3. **Versioning headers** — `ApiVersioningResponseHeadersIntegrationTests` exists; ensure new versioned controllers opt in to the same trait/filter.
4. **Webhook / integration payloads** — JSON schemas live in docs and `IntegrationEventJson`; contract tests are thinner than SQL authority tests. *Mitigation:* add `JsonSerializer` round-trip tests when adding a new `IntegrationEvent` type.

## Data flow

1. Developer changes controller or DTO.
2. `dotnet test` runs `OpenApiContractSnapshotTests` → fails on drift.
3. Engineer reviews diff; if intentional, `ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractSnapshotTests`.
4. `OpenApiContractInvariantsTests` catches accidental **metadata** regression (wrong `openapi` version, missing `/v1/register`) even if snapshot merge conflict was resolved incorrectly.

## Security model

Contract tests do **not** replace auth tests; they ensure **documented surface** matches implementation so clients and security tooling (ZAP, APIM policies) see a truthful contract.

## Operational considerations

- Regenerate OpenAPI snapshot **only** after intentional API changes; never “silence” drift without product/security review.
- Link new contract tests from this doc’s gap list when you close an item (keeps tribal knowledge out of chat).

## Reliability / cost / scalability

- **Reliability:** snapshot + invariants catch accidental breaking changes before release.
- **Cost:** minimal CI time (single WebApplicationFactory host, JSON parse).
- **Scalability:** add **invariant** tests sparingly; avoid N× per-endpoint golden files in the core suite.
