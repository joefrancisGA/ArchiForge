namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Shared literals for golden-cohort baseline state.</summary>
public static class GoldenCohortBaselineConstants
{
    /// <summary>Placeholder SHA-256 in <c>cohort.json</c> before an owner-approved lock-baseline run.</summary>
    public const string UnlockedManifestSha256Placeholder =
        "0000000000000000000000000000000000000000000000000000000000000000";
}
