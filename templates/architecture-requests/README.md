> **Scope:** Architecture request JSON presets for `POST /v1/architecture/request` — inventory, difficulty, usage; includes Greenfield and migration scenarios.

# Architecture request templates

Starter **`ArchitectureRequest`** JSON payloads (same shape as `POST /v1/architecture/request`) for demos and first runs. **Fictional** Contoso-style names only — replace with your system before production.

| File | Scenario | Difficulty |
|------|----------|------------|
| [web-app-with-database.json](web-app-with-database.json) | Retail-style three-tier stack on Azure (App Service + **Azure SQL** + CDN + Entra ID) | beginner |
| [event-driven-microservices.json](event-driven-microservices.json) | Telemetry / microservices ingestion on Azure (events, workers, analytics plane) | intermediate |
| [greenfield-web-app.json](greenfield-web-app.json) | Three-tier web app on Azure (App Service + SQL + CDN) | beginner |
| [cloud-migration-lift-shift.json](cloud-migration-lift-shift.json) | Lift-and-shift monolith to Container Apps + managed SQL | intermediate |
| [cloud-migration-lift-and-shift.json](cloud-migration-lift-and-shift.json) | Alternate lift-and-shift preset (claims-style batch stack; same API shape) | intermediate |
| [microservices-decomposition.json](microservices-decomposition.json) | Decompose a monolith into bounded contexts with messaging | intermediate |
| [data-platform-modernization.json](data-platform-modernization.json) | Lakehouse-style data platform modernization with lineage and PII guardrails | intermediate |
| [zero-trust-network-review.json](zero-trust-network-review.json) | Zero-trust network posture review (hub/spoke, private PaaS, inspection) | advanced |
| [event-driven-data-pipeline.json](event-driven-data-pipeline.json) | Streaming data pipeline with Event Hubs, Functions, and Delta Lake | intermediate |
| [api-gateway-bff.json](api-gateway-bff.json) | API gateway + Backend-for-Frontend for multi-channel consumers | intermediate |
| [zero-trust-internal-app.json](zero-trust-internal-app.json) | Zero-trust internal HR portal with Conditional Access and CMK encryption | advanced |
| [multi-region-ha.json](multi-region-ha.json) | Multi-region active-passive with automatic failover (RTO < 5 min) | advanced |

**Usage:** copy a file, set `requestId`, adjust `systemName` / `description`, then POST via CLI or operator UI wizard. Wizard wiring to pick these files is a follow-on; today they are copy-paste starters.
