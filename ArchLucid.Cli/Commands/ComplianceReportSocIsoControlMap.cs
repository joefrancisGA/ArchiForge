namespace ArchLucid.Cli.Commands;

/// <summary>
///     Informal SOC 2 TSC (Security / Availability) themes aligned with ISO/IEC 27001:2022 Annex A control references.
///     Mapping is narrative only — not a certification or formal Statement of Applicability.
/// </summary>
internal static class ComplianceReportSocIsoControlMap
{
    internal static IReadOnlyList<SocIsoMappingRow> Rows =>
    [
        new(
            "Security — logical access (CC6)",
            "A.5.15, A.5.16, A.5.17, A.5.18",
            "Authentication mode, RBAC, API keys, `AuthSafetyGuard`; validate-config checks for JWT authority when bearer auth is enabled",
            "Governance approvals, SCIM/admin token lifecycle, alert routing mutations, policy pack operations (see audit matrix)"),
        new(
            "Security — data protection (CC6)",
            "A.8.2, A.8.3, A.8.11, A.8.12",
            "Storage provider, SQL connection presence, encryption-at-rest delegated to Azure SQL / managed disk posture (see Terraform)",
            "Artifact/export/download events, run export, replay export — scope-bound durable rows"),
        new(
            "Security — system operations & monitoring (CC7)",
            "A.8.15, A.8.16, A.8.17",
            "Health endpoints configuration, OpenTelemetry / diagnostics toggles from appsettings",
            "Alert lifecycle, circuit-breaker audit, advisory scan events, SLA breach signals"),
        new(
            "Security — change management (CC8)",
            "A.8.9, A.8.25, A.8.32",
            "Agent execution mode, schema validation toggles, CI security tooling (repo docs — not read from live host)",
            "Manifest promote / environment activate, governance dry-run and pre-commit simulations"),
        new(
            "Availability (A1)",
            "A.8.6, A.8.14",
            "Rate limiting configuration keys, regional defaults in archlucid.json architecture section when present",
            "Run lifecycle (`RunStarted`, `RunCompleted`, `Run.Failed`), data-archival host failures if emitted"),
    ];

    internal sealed record SocIsoMappingRow(
        string Soc2Theme,
        string Iso27001AnnexAReferences,
        string ConfigurationEvidenceHint,
        string AuditLogEvidenceHint);
}
