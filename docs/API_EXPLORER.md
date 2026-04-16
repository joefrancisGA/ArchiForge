# API explorer (Scalar + OpenAPI)

## 1. Objective

Give developers and operators an interactive, browser-based way to discover and try ArchLucid HTTP endpoints without leaving the documented OpenAPI surface.

## 2. Assumptions

- Non-production environments may enable the explorer; production keeps it off unless explicitly configured.
- Swashbuckle remains the source of truth for `/swagger/v1/swagger.json` (schema IDs, tags, examples, auth metadata).
- Microsoft `MapOpenApi()` continues to expose the alternate OpenAPI document for tooling that prefers that pipeline.

## 3. Constraints

- **Security:** `DeveloperExperience:EnableApiExplorer` must stay `false` in production unless a deliberate exception is made; a warning is logged when enabled outside Development.
- **Network:** Any staging enablement should sit behind private networking or authenticated ingress, not a public internet default.
- **Metering:** Explorer routes do not match `/v1/…` versioned API paths and are not counted as tenant API usage.

## 4. Architecture Overview

```mermaid
flowchart LR
  Browser[Browser]
  ScalarUI[Scalar static UI]
  SwaggerJson["/swagger/v1/swagger.json"]
  OpenApiJson["Microsoft OpenAPI route"]
  Browser --> ScalarUI
  ScalarUI --> SwaggerJson
  OpenApiJson --> Tooling[Codegen / contract tests]
```

## 5. Component Breakdown

| Piece | Role |
| --- | --- |
| `Swashbuckle.AspNetCore` | Generates OpenAPI JSON and serves it via `UseSwagger()`. |
| `Scalar.AspNetCore` | Renders the Scalar UI and points it at the Swashbuckle document pattern. |
| `Microsoft.AspNetCore.OpenApi` | Parallel OpenAPI document for Microsoft-first consumers. |
| `DeveloperExperienceOptions` | Configuration gate for opt-in outside Development. |

## 6. Data Flow

1. Operator opens `/scalar/v1` (or `/scalar/` and picks document `v1`).
2. Scalar loads configuration that references `/swagger/v1/swagger.json`.
3. Browser fetches that JSON from the same host; Scalar renders operations and “try it” UI.

## 7. Security Model

- Default production: explorer endpoints not registered (`EnableApiExplorer: false` and not Development).
- Opt-in: set `DeveloperExperience:EnableApiExplorer` to `true` only with network and identity controls aligned to your threat model.
- Static replay recipe HTML (`DocsController`) links to Scalar instead of legacy Swagger UI.

## 8. Operational Considerations

- **URLs:** Scalar UI: `/scalar/v1` (typical). Swashbuckle JSON: `/swagger/v1/swagger.json`. Microsoft OpenAPI: `/openapi/v1.json` (when mapped).
- **Upgrades:** Keep `Scalar.AspNetCore` aligned with the repo central package versions (`Directory.Packages.props`).
- **Failure mode:** If Scalar fails to load the JSON, check reverse-proxy paths and that `UseSwagger()` runs in the same pipeline configuration as Scalar.
