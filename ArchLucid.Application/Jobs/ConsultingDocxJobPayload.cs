namespace ArchLucid.Application.Jobs;

/// <summary>
///     Serializable export parameters for async consulting DOCX jobs (mirrors API <c>ConsultingDocxExportRequest</c>
///     fields).
/// </summary>
public sealed record ConsultingDocxJobPayload
{
    public string RunId
    {
        get;
        init;
    } = string.Empty;

    public string? TemplateProfile
    {
        get;
        init;
    }

    public string? Audience
    {
        get;
        init;
    }

    public bool ExternalDelivery
    {
        get;
        init;
    }

    public bool ExecutiveFriendly
    {
        get;
        init;
    }

    public bool RegulatedEnvironment
    {
        get;
        init;
    }

    public bool NeedDetailedEvidence
    {
        get;
        init;
    }

    public bool NeedExecutionTraces
    {
        get;
        init;
    }

    public bool NeedDeterminismOrCompareAppendices
    {
        get;
        init;
    }

    public bool IncludeEvidence
    {
        get;
        init;
    } = true;

    public bool IncludeExecutionTraces
    {
        get;
        init;
    } = true;

    public bool IncludeManifest
    {
        get;
        init;
    } = true;

    public bool IncludeDiagram
    {
        get;
        init;
    } = true;

    public bool IncludeSummary
    {
        get;
        init;
    } = true;

    public bool IncludeDeterminismCheck
    {
        get;
        init;
    }

    public int DeterminismIterations
    {
        get;
        init;
    } = 3;

    public bool IncludeManifestCompare
    {
        get;
        init;
    }

    public string? CompareManifestVersion
    {
        get;
        init;
    }

    public bool IncludeAgentResultCompare
    {
        get;
        init;
    }

    public string? CompareRunId
    {
        get;
        init;
    }
}
