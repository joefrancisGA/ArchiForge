using System;
using System.Linq;

namespace ArchiForge.Application.Analysis;

public sealed class ConsultingDocxExportProfileSelector(
    IConsultingDocxTemplateProfileResolver profileResolver,
    IConsultingDocxTemplateRecommendationService recommendationService)
    : IConsultingDocxExportProfileSelector
{
    public ResolvedConsultingDocxExportProfile Resolve(
        string? templateProfile,
        ConsultingDocxProfileRecommendationRequest recommendationRequest)
    {
        if (!string.IsNullOrWhiteSpace(templateProfile))
        {
            var catalog = profileResolver.GetCatalog();

            var summary = catalog.Profiles.FirstOrDefault(x =>
                string.Equals(x.ProfileName, templateProfile, StringComparison.OrdinalIgnoreCase));

            return new ResolvedConsultingDocxExportProfile
            {
                SelectedProfileName = summary?.ProfileName ?? templateProfile.Trim(),
                SelectedProfileDisplayName = summary?.ProfileDisplayName ?? templateProfile.Trim(),
                WasAutoSelected = false,
                ResolutionReason = "Template profile was explicitly specified by the caller."
            };
        }

        var recommendation = recommendationService.Recommend(recommendationRequest);

        return new ResolvedConsultingDocxExportProfile
        {
            SelectedProfileName = recommendation.RecommendedProfileName,
            SelectedProfileDisplayName = recommendation.RecommendedProfileDisplayName,
            WasAutoSelected = true,
            ResolutionReason = recommendation.Reason
        };
    }
}

