namespace ArchiForge.Application.Analysis;

/// <summary>
/// Context signals used by <see cref="IConsultingDocxTemplateRecommendationService"/>
/// to infer the most appropriate consulting Docx template profile.
/// </summary>
/// <remarks>
/// Signals are evaluated in priority order: <see cref="RegulatedEnvironment"/> &gt;
/// <see cref="ExecutiveFriendly"/> &gt; <see cref="ExternalDelivery"/> &gt; technical
/// depth flags &gt; <see cref="Audience"/> keyword inference &gt; safe default.
/// </remarks>
public sealed class ConsultingDocxProfileRecommendationRequest
{
    /// <summary>
    /// Free-text description of the intended audience (e.g. <c>executive</c>,
    /// <c>client</c>, <c>compliance</c>). Used for keyword inference when no
    /// explicit flag is set.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// <see langword="true"/> when the document will be delivered externally
    /// to a client rather than consumed internally.
    /// </summary>
    public bool ExternalDelivery { get; set; }

    /// <summary>
    /// <see langword="true"/> when the primary reader is an executive or sponsor
    /// who expects a concise brief without deep technical depth.
    /// </summary>
    public bool ExecutiveFriendly { get; set; }

    /// <summary>
    /// <see langword="true"/> when the report must satisfy a regulated or compliance
    /// review that requires control tables and governance appendices.
    /// </summary>
    public bool RegulatedEnvironment { get; set; }

    /// <summary>
    /// <see langword="true"/> when the report should include a detailed evidence appendix.
    /// Promotes the <c>internal</c> profile when no higher-priority signal is present.
    /// </summary>
    public bool NeedDetailedEvidence { get; set; }

    /// <summary>
    /// <see langword="true"/> when per-agent execution traces should be included in the
    /// report. Promotes the <c>internal</c> profile when no higher-priority signal is present.
    /// </summary>
    public bool NeedExecutionTraces { get; set; }

    /// <summary>
    /// <see langword="true"/> when determinism check results or run-comparison appendices
    /// are required. Promotes the <c>internal</c> profile when no higher-priority signal is present.
    /// </summary>
    public bool NeedDeterminismOrCompareAppendices { get; set; }
}
