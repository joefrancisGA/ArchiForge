# Architecture request templates

Starter **`ArchitectureRequest`** JSON payloads (same shape as `POST /v1/architecture/request`) for demos and first runs. **Fictional** Contoso-style names only — replace with your system before production.

| File | Scenario | Difficulty |
|------|----------|------------|
| [greenfield-web-app.json](greenfield-web-app.json) | Three-tier web app on Azure (App Service + SQL + CDN) | beginner |
| [cloud-migration-lift-shift.json](cloud-migration-lift-shift.json) | Lift-and-shift monolith to Container Apps + managed SQL | intermediate |
| [microservices-decomposition.json](microservices-decomposition.json) | Decompose a monolith into bounded contexts with messaging | intermediate |

**Usage:** copy a file, set `requestId`, adjust `systemName` / `description`, then POST via CLI or operator UI wizard. Wizard wiring to pick these files is a follow-on; today they are copy-paste starters.
