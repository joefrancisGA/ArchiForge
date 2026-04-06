namespace ArchiForge.Application.Analysis;

/// <summary>
/// Describes the metadata-level differences between two <see cref="ArchiForge.Contracts.Metadata.ArchitectureRun"/> records
/// as part of an end-to-end replay comparison.
/// </summary>
public sealed class RunMetadataDiffResult
{
    /// <summary>Names of top-level run fields whose values differ between the two runs.</summary>
    public List<string> ChangedFields { get; set; } = [];

    /// <summary><c>true</c> when the two runs reference different <c>RequestId</c> values.</summary>
    public bool RequestIdsDiffer { get; set; }

    /// <summary><c>true</c> when the two runs have different <c>CurrentManifestVersion</c> values.</summary>
    public bool ManifestVersionsDiffer { get; set; }

    /// <summary><c>true</c> when the two runs are in different <c>Status</c> states.</summary>
    public bool StatusDiffers { get; set; }

    /// <summary><c>true</c> when one run has a <c>CompletedUtc</c> and the other does not, or their values differ.</summary>
    public bool CompletionStateDiffers { get; set; }
}

