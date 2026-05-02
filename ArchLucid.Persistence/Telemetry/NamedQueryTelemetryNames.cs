using ArchLucid.Core.Diagnostics;

namespace ArchLucid.Persistence.Telemetry;

/// <summary>
///     Allowlist-aligned names for <see cref="ArchLucidInstrumentation.RecordNamedQueryLatencyMilliseconds" />
///     (TB-003). Must stay in sync with <c>tests/performance/query-allowlist.json</c>.
/// </summary>
internal static class NamedQueryTelemetryNames
{
    public const string GetRunsByTenantId = "GetRunsByTenantId";

    public const string AppendAuditEvent = "AppendAuditEvent";

    public const string GetFindingsSnapshotById = "GetFindingsSnapshotById";

    public const string GetGoldenManifestById = "GetGoldenManifestById";
}
