namespace ArchLucid.Application.Analysis;

/// <summary>
///     Selects the most appropriate consulting Docx template profile based on contextual
///     signals such as audience, delivery mode, and required appendices.
/// </summary>
public interface IConsultingDocxTemplateRecommendationService
{
    /// <summary>
    ///     Returns a profile recommendation for the given <paramref name="request" />.
    /// </summary>
    /// <param name="request">
    ///     The context signals used to infer the best template profile.
    /// </param>
    /// <returns>
    ///     A <see cref="ConsultingDocxProfileRecommendation" /> that names the recommended
    ///     profile and explains the selection rationale.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="request" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no template profiles are registered in the profile resolver catalog.
    /// </exception>
    ConsultingDocxProfileRecommendation Recommend(
        ConsultingDocxProfileRecommendationRequest request);
}
