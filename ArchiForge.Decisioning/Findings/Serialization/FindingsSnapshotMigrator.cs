using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Serialization;

/// <summary>
/// Normalizes legacy findings (pre-envelope) to the current schema so stored snapshots remain readable.
/// </summary>
public static class FindingsSnapshotMigrator
{
    public static void Apply(FindingsSnapshot snapshot)
    {
        foreach (var finding in snapshot.Findings)
            MigrateFinding(finding);

        if (snapshot.SchemaVersion < FindingsSchema.CurrentSnapshotVersion)
            snapshot.SchemaVersion = FindingsSchema.CurrentSnapshotVersion;
    }

    private static void MigrateFinding(Finding f)
    {
        if (f.FindingSchemaVersion >= FindingsSchema.CurrentFindingVersion)
            return;

        if (string.IsNullOrWhiteSpace(f.Category))
            f.Category = InferCategory(f.FindingType);

        if (string.IsNullOrWhiteSpace(f.PayloadType))
            f.PayloadType = InferPayloadTypeName(f.FindingType);

        f.FindingSchemaVersion = FindingsSchema.CurrentFindingVersion;
    }

    private static string InferCategory(string findingType)
    {
        if (string.Equals(findingType, "RequirementFinding", StringComparison.OrdinalIgnoreCase))
            return "Requirement";
        if (string.Equals(findingType, "TopologyGap", StringComparison.OrdinalIgnoreCase))
            return "Topology";
        if (string.Equals(findingType, "SecurityControlFinding", StringComparison.OrdinalIgnoreCase))
            return "Security";
        if (string.Equals(findingType, "CostConstraintFinding", StringComparison.OrdinalIgnoreCase))
            return "Cost";
        return "General";
    }

    private static string? InferPayloadTypeName(string findingType)
    {
        if (string.Equals(findingType, "RequirementFinding", StringComparison.OrdinalIgnoreCase))
            return nameof(ArchiForge.Decisioning.Findings.Payloads.RequirementFindingPayload);
        if (string.Equals(findingType, "TopologyGap", StringComparison.OrdinalIgnoreCase))
            return nameof(ArchiForge.Decisioning.Findings.Payloads.TopologyGapFindingPayload);
        if (string.Equals(findingType, "SecurityControlFinding", StringComparison.OrdinalIgnoreCase))
            return nameof(ArchiForge.Decisioning.Findings.Payloads.SecurityControlFindingPayload);
        if (string.Equals(findingType, "CostConstraintFinding", StringComparison.OrdinalIgnoreCase))
            return nameof(ArchiForge.Decisioning.Findings.Payloads.CostConstraintFindingPayload);
        return null;
    }
}
