namespace ArchLucid.Application.Analysis;

/// <summary>
///     Default implementation of <see cref="IConsultingDocxExportProfileSelector" />.
/// </summary>
/// <remarks>
///     When an explicit profile name is given it is passed through directly after a
///     catalog look-up to enrich the display name. When no profile is given,
///     <see cref="IConsultingDocxTemplateRecommendationService.Recommend" /> is called
///     to infer the best profile from context signals.
/// </remarks>
public sealed class ConsultingDocxExportProfileSelector(
    IConsultingDocxTemplateProfileResolver profileResolver,
    IConsultingDocxTemplateRecommendationService recommendationService)
    : IConsultingDocxExportProfileSelector
{
    /// <inheritdoc />
    public ResolvedConsultingDocxExportProfile Resolve(
        string? templateProfile,
        ConsultingDocxProfileRecommendationRequest recommendationRequest)
    {
        if (!string.IsNullOrWhiteSpace(templateProfile))
        {
            ConsultingDocxTemplateProfileCatalog catalog = profileResolver.GetCatalog();

            ConsultingDocxTemplateProfileInfo? summary = catalog.Profiles.FirstOrDefault(x =>
                string.Equals(x.ProfileName, templateProfile, StringComparison.OrdinalIgnoreCase));

            return new ResolvedConsultingDocxExportProfile
            {
                SelectedProfileName = summary?.ProfileName ?? templateProfile.Trim(),
                SelectedProfileDisplayName = summary?.ProfileDisplayName ?? templateProfile.Trim(),
                WasAutoSelected = false,
                ResolutionReason = "Template profile was explicitly specified by the caller."
            };
        }

        ConsultingDocxProfileRecommendation recommendation = recommendationService.Recommend(recommendationRequest);

        return new ResolvedConsultingDocxExportProfile
        {
            SelectedProfileName = recommendation.RecommendedProfileName,
            SelectedProfileDisplayName = recommendation.RecommendedProfileDisplayName,
            WasAutoSelected = true,
            ResolutionReason = recommendation.Reason
        };
    }
}
