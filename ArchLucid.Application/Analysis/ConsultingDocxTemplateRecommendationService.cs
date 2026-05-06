namespace ArchLucid.Application.Analysis;
/// <summary>
///     Selects the most appropriate consulting Docx template profile based on delivery
///     context signals present in a <see cref = "ConsultingDocxProfileRecommendationRequest"/>.
/// </summary>
/// <remarks>
///     Profile resolution follows a priority order: regulated &gt; executive &gt; external delivery &gt;
///     technical depth &gt; audience keyword inference &gt; safe default. The default is
///     <see cref = "ConsultingDocxProfiles.Client"/> when no strong signal is detected.
/// </remarks>
public sealed class ConsultingDocxTemplateRecommendationService(IConsultingDocxTemplateProfileResolver profileResolver) : IConsultingDocxTemplateRecommendationService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(profileResolver);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.Analysis.IConsultingDocxTemplateProfileResolver profileResolver)
    {
        ArgumentNullException.ThrowIfNull(profileResolver);
        return (byte)0;
    }

    /// <inheritdoc/>
    public ConsultingDocxProfileRecommendation Recommend(ConsultingDocxProfileRecommendationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ConsultingDocxTemplateProfileCatalog catalog = profileResolver.GetCatalog();
        if (catalog.Profiles.Count == 0)
            throw new InvalidOperationException("No consulting Docx template profiles are registered. " + "Ensure the profile resolver returns at least one profile.");
        HashSet<string> available = catalog.Profiles.Select(p => p.ProfileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string profile;
        string reason;
        if (request.RegulatedEnvironment)
        {
            profile = Prefer(ConsultingDocxProfiles.Regulated, available);
            reason = "A regulated review was indicated, so a control-heavy report with stronger governance and appendix coverage is the best fit.";
        }
        else if (request.ExecutiveFriendly)
        {
            profile = Prefer(ConsultingDocxProfiles.Executive, available);
            reason = "An executive-friendly output was requested, so a shorter stakeholder-oriented brief is the best fit.";
        }
        else if (request.ExternalDelivery)
        {
            profile = Prefer(ConsultingDocxProfiles.Client, available);
            reason = "External delivery was indicated, so the balanced client-facing report is the best fit.";
        }
        else if (request.NeedDetailedEvidence || request.NeedExecutionTraces || request.NeedDeterminismOrCompareAppendices)
        {
            profile = Prefer(ConsultingDocxProfiles.Internal, available);
            reason = "Detailed evidence, traceability, or replay/comparison depth was requested, so the internal technical review profile is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) && request.Audience.Contains("executive", StringComparison.OrdinalIgnoreCase))
        {
            profile = Prefer(ConsultingDocxProfiles.Executive, available);
            reason = "The audience indicates executives or sponsors, so the executive brief is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) && (request.Audience.Contains("audit", StringComparison.OrdinalIgnoreCase) || request.Audience.Contains("compliance", StringComparison.OrdinalIgnoreCase) || request.Audience.Contains("regulator", StringComparison.OrdinalIgnoreCase)))
        {
            profile = Prefer(ConsultingDocxProfiles.Regulated, available);
            reason = "The audience indicates compliance, audit, or regulated review, so the regulated profile is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) && (request.Audience.Contains("client", StringComparison.OrdinalIgnoreCase) || request.Audience.Contains("external", StringComparison.OrdinalIgnoreCase)))
        {
            profile = Prefer(ConsultingDocxProfiles.Client, available);
            reason = "The audience indicates an external or client-facing reader, so the client delivery profile is the best fit.";
        }
        else
        {
            profile = Prefer(ConsultingDocxProfiles.Client, available);
            reason = "No strong specialized signal was provided, so the balanced client delivery profile is the safest default.";
        }

        ConsultingDocxTemplateProfileInfo selected = catalog.Profiles.First(x => string.Equals(x.ProfileName, profile, StringComparison.OrdinalIgnoreCase));
        List<string> alternatives = catalog.Profiles.Where(x => !string.Equals(x.ProfileName, profile, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).Select(x => x.ProfileName).Take(3).ToList();
        return new ConsultingDocxProfileRecommendation
        {
            RecommendedProfileName = selected.ProfileName,
            RecommendedProfileDisplayName = selected.ProfileDisplayName,
            Reason = reason,
            AlternativeProfiles = alternatives
        };
    }

    /// <summary>
    ///     Returns <paramref name = "preferred"/> if it exists in <paramref name = "available"/>;
    ///     otherwise falls back to the first available profile name.
    /// </summary>
    private static string Prefer(string preferred, HashSet<string> available)
    {
        return available.Contains(preferred) ? preferred : available.First();
    }
}