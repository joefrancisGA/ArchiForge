namespace ArchLucid.Contracts.ProductLearning.Planning;

/// <summary>
///     Structured improvement theme derived from 58R aggregates/opportunities (snapshot metrics for audit).
/// </summary>
public sealed class ProductLearningImprovementThemeRecord
{
    public Guid ThemeId
    {
        get;
        init;
    }

    public Guid TenantId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ProjectId
    {
        get;
        init;
    }

    /// <summary>Deterministic key for idempotent derivation (scope-unique).</summary>
    public string ThemeKey
    {
        get;
        init;
    } = string.Empty;

    public string? SourceAggregateKey
    {
        get;
        init;
    }

    public string? PatternKey
    {
        get;
        init;
    }

    public string Title
    {
        get;
        init;
    } = string.Empty;

    public string Summary
    {
        get;
        init;
    } = string.Empty;

    public string AffectedArtifactTypeOrWorkflowArea
    {
        get;
        init;
    } = string.Empty;

    public string SeverityBand
    {
        get;
        init;
    } = string.Empty;

    public int EvidenceSignalCount
    {
        get;
        init;
    }

    public int DistinctRunCount
    {
        get;
        init;
    }

    public double? AverageTrustScore
    {
        get;
        init;
    }

    /// <summary>Version token for explainability when derivation rules change (e.g. 59R-v1).</summary>
    public string DerivationRuleVersion
    {
        get;
        init;
    } = string.Empty;

    public string Status
    {
        get;
        init;
    } = ProductLearningImprovementThemeStatusValues.Proposed;

    public DateTime CreatedUtc
    {
        get;
        init;
    }

    public string? CreatedByUserId
    {
        get;
        init;
    }
}
