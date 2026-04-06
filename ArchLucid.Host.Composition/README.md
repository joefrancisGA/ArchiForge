# ArchiForge.Host.Composition

**Dependency-injection registration** for ArchiForge (storage, agents, scheduling, health checks, hosted services). Extracted from `ArchiForge.Host.Core` so the host assembly stays focused on HTTP/worker pipeline, middleware, Serilog, and OpenTelemetry wiring.

## Consumers

- `ArchiForge.Api` — calls `AddArchiForgeApplicationServices` after auth/OpenTelemetry registration.
- `ArchiForge.Worker` — same for the worker role.

## Relationship

- **`ArchiForge.Host.Core`** — no reference to this project (avoids cycles). Holds `ObservabilityExtensions`, persistence startup, health checks, middleware, configuration validation.
- **`ArchiForge.Host.Composition`** — references `Host.Core` and all domain projects required by registrations.

## Extension entry point

- `ServiceCollectionExtensions.AddArchiForgeApplicationServices(...)` in namespace `ArchiForge.Host.Composition`.
- `ArchiForgeStorageServiceCollectionExtensions.AddArchiForgeStorage(...)` in the same namespace.
