namespace ArchLucid.Contracts.Trust;

/// <summary>
/// Admin request body to record publication of a third-party security assessment summary (durable audit + trust UI signal).
/// </summary>
public sealed class SecurityAssessmentPublicationRequest
{
    /// <summary>Programmatic label, e.g. <c>2026-Q2</c>.</summary>
    public string AssessmentCode { get; set; } = string.Empty;

    /// <summary>Repository-relative path or canonical URL to the redacted summary customers may open.</summary>
    public string SummaryReference { get; set; } = string.Empty;

    /// <summary>Optional assessor legal name as published on the cover letter.</summary>
    public string? AssessorDisplayName { get; set; }
}
