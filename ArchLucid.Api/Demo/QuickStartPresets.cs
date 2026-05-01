namespace ArchLucid.Api.Demo;

/// <summary>Anonymous marketing quick-start presets (architecture request scaffolding).</summary>
public static class QuickStartPresets
{
    /// <summary>Logical scope labels surfaced as constraints alongside well-known GUID scope values.</summary>
    public static readonly string[] LogicalScopePins =
    [
        "scope=quickstart-demo",
        "workspace=demo",
        "project=quickstart"
    ];

    public static readonly Dictionary<string, PresetPayload> Items =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["microservices"] = new PresetPayload(
                "Distributed microservices on Azure Container Apps with centralized identity, "
                + "PostgreSQL backends, Redis cache fronts, asynchronous processing via Azure Service Bus, "
                + "and internal-only networking with private ingress and centralized observability. "
                + "Assume PCI-adjacent cardholder workflows stay behind segmented subnets and hardened APIs.",
                "Quick Start — Microservices",
                [
                    .. LogicalScopePins,
                    "Prefer managed services over bespoke clusters",
                    "Zero-trust service-to-service identity"
                ],
                ["Regional HA", "Autoscaling workloads", "Message-driven choreography"]),

            ["monolith-migration"] = new PresetPayload(
                "Strangler-fig modernization from a modular monolith (.NET Core) toward incrementally carved "
                + "domains on Azure Kubernetes Service while retaining the core OLTP relational database during "
                + "dual-write phases. Bounded contexts for orders, fulfillment, billing, notifications; phased cutover.",
                "Quick Start — Monolith migration",
                [
                    .. LogicalScopePins,
                    "Minimize big-bang data migrations",
                    "Preserve existing SLAs until cutover checkpoints"
                ],
                ["Incremental extraction", "Data synchronization", "Backwards-compatible APIs"]),


            ["event-driven"] = new PresetPayload(
                "Event-driven architecture leveraging Azure Event Hubs ingestion, Functions-based processors, "
                + "Materialized CQRS views in Cosmos DB with denormalized read models for dashboards, saga-style "
                + "coordination across payments and inventory, replayable telemetry, DLQ remediation paths.",
                "Quick Start — Event-driven platform",
                [
                    .. LogicalScopePins,
                    "Idempotent consumers mandatory",
                    "Schema evolution with compatibility gates"
                ],
                ["Replay", "Temporal ordering safeguards", "Compensating actions"])
        };

    public sealed record PresetPayload(
        string ArchitectureDescription,
        string SystemDisplayName,
        IReadOnlyList<string> Constraints,
        IReadOnlyList<string> RequiredCapabilities);

    /// <returns><see langword="false"/> when <paramref name="presetId"/> is null/empty or unknown.</returns>
    public static bool TryGet(string? presetId, out PresetPayload payload)
    {
        payload = null!;

        if (string.IsNullOrWhiteSpace(presetId))
            return false;

        if (!Items.TryGetValue(presetId.Trim(), out PresetPayload? found))
            return false;

        payload = found;

        return true;
    }
}
