namespace ArchLucid.Api.Models;

/// <summary>Bounds for manifest summary generation (relationship fan-out, etc.).</summary>
public static class ManifestSummaryLimits
{
    /// <summary>Maximum relationships to include when <c>maxRelationships</c> is supplied on manifest summary endpoints.</summary>
    public const int MaxRelationships = 1000;
}
