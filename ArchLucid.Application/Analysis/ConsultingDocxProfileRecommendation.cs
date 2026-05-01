namespace ArchLucid.Application.Analysis;

/// <summary>
///     The result produced by <see cref="IConsultingDocxTemplateRecommendationService.Recommend" />
///     describing the chosen Docx template profile and the reasoning behind the choice.
/// </summary>
public sealed class ConsultingDocxProfileRecommendation
{
    /// <summary>
    ///     Machine-readable name of the recommended profile (e.g. <c>executive</c>, <c>regulated</c>).
    ///     Matches a <see cref="ConsultingDocxTemplateProfileInfo.ProfileName" /> from the catalog.
    /// </summary>
    public string RecommendedProfileName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Human-readable display name of the recommended profile (e.g. <c>Executive Brief</c>).
    /// </summary>
    public string RecommendedProfileDisplayName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Plain-language explanation of why this profile was selected over the alternatives.
    /// </summary>
    public string Reason
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Up to three alternative profile names the caller may choose instead.
    ///     Ordered by display order from the catalog.
    /// </summary>
    public List<string> AlternativeProfiles
    {
        get;
        set;
    } = [];
}
