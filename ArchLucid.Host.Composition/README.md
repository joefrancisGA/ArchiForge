# ArchLucid.Host.Composition

**Dependency-injection registration** for ArchLucid (storage, agents, scheduling, health checks, hosted services). Extracted from `ArchLucid.Host.Core` so the host assembly stays focused on HTTP/worker pipeline, middleware, Serilog, and OpenTelemetry wiring.

## Consumers

- `ArchLucid.Api` — calls `AddArchLucidApplicationServices` after auth/OpenTelemetry registration.
- `ArchLucid.Worker` — same for the worker role.

## Relationship

- **`ArchLucid.Host.Core`** — no reference to this project (avoids cycles). Holds `ObservabilityExtensions`, persistence startup, health checks, middleware, configuration validation.
- **`ArchLucid.Host.Composition`** — references `Host.Core` and all domain projects required by registrations.

## Extension entry point

- `ServiceCollectionExtensions.AddArchLucidApplicationServices(...)` in namespace `ArchLucid.Host.Composition`.
- `ArchLucidStorageServiceCollectionExtensions.AddArchLucidStorage(...)` in the same namespace.
