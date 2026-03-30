namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Queries.ManifestSummaryDto"/> (manifest summary endpoint).
/// </summary>
public class ManifestSummaryResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.ManifestId"/>
    public Guid ManifestId { get; set; }
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.RunId"/>
    public Guid RunId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.CreatedUtc"/>
    public DateTime CreatedUtc { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.ManifestHash"/>
    public string ManifestHash { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.RuleSetId"/>
    public string RuleSetId { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.RuleSetVersion"/>
    public string RuleSetVersion { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.DecisionCount"/>
    public int DecisionCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.WarningCount"/>
    public int WarningCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.UnresolvedIssueCount"/>
    public int UnresolvedIssueCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.ManifestSummaryDto.Status"/>
    public string Status { get; set; } = null!;

    /// <summary>True when <see cref="WarningCount"/> is greater than zero.</summary>
    public bool HasWarnings { get; set; }

    /// <summary>True when <see cref="UnresolvedIssueCount"/> is greater than zero.</summary>
    public bool HasUnresolvedIssues { get; set; }

    /// <summary>
    /// Single-line summary for operator shells (deterministic composition from counts and <see cref="Status"/>).
    /// </summary>
    public string OperatorSummary { get; set; } = "";
}
