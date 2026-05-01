namespace ArchLucid.Application.Analysis;

/// <summary>
///     Resolves the Docx template profile to use for a consulting export.
/// </summary>
/// <remarks>
///     When an explicit <c>templateProfile</c> is supplied the selector validates it against
///     the registered catalog and returns it directly. When no profile is supplied the selector
///     delegates to <see cref="IConsultingDocxTemplateRecommendationService" /> to infer the
///     best match from contextual signals.
/// </remarks>
public interface IConsultingDocxExportProfileSelector
{
    /// <summary>
    ///     Resolves the profile to use for a consulting Docx export.
    /// </summary>
    /// <param name="templateProfile">
    ///     An explicit profile name requested by the caller, or <see langword="null" /> /
    ///     empty to trigger automatic recommendation.
    /// </param>
    /// <param name="recommendationRequest">
    ///     Context signals used when automatic recommendation is required.
    /// </param>
    /// <returns>
    ///     A <see cref="ResolvedConsultingDocxExportProfile" /> containing the selected profile
    ///     name, display name, and the resolution rationale.
    /// </returns>
    ResolvedConsultingDocxExportProfile Resolve(
        string? templateProfile,
        ConsultingDocxProfileRecommendationRequest recommendationRequest);
}
