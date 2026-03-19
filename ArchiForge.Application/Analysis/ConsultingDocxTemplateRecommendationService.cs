using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchiForge.Application.Analysis;

public sealed class ConsultingDocxTemplateRecommendationService(IConsultingDocxTemplateProfileResolver profileResolver)
    : IConsultingDocxTemplateRecommendationService
{
    public ConsultingDocxProfileRecommendation Recommend(
        ConsultingDocxProfileRecommendationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var catalog = profileResolver.GetCatalog();
        var available = catalog.Profiles
            .Select(p => p.ProfileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string profile;
        string reason;

        if (request.RegulatedEnvironment)
        {
            profile = available.Contains("regulated") ? "regulated" : Fallback(available);
            reason = "A regulated review was indicated, so a control-heavy report with stronger governance and appendix coverage is the best fit.";
        }
        else if (request.ExecutiveFriendly)
        {
            profile = available.Contains("executive") ? "executive" : Fallback(available);
            reason = "An executive-friendly output was requested, so a shorter stakeholder-oriented brief is the best fit.";
        }
        else if (request.ExternalDelivery)
        {
            profile = available.Contains("client") ? "client" : Fallback(available);
            reason = "External delivery was indicated, so the balanced client-facing report is the best fit.";
        }
        else if (request.NeedDetailedEvidence || request.NeedExecutionTraces || request.NeedDeterminismOrCompareAppendices)
        {
            profile = available.Contains("internal") ? "internal" : Fallback(available);
            reason = "Detailed evidence, traceability, or replay/comparison depth was requested, so the internal technical review profile is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) &&
                 request.Audience.Contains("executive", StringComparison.OrdinalIgnoreCase))
        {
            profile = available.Contains("executive") ? "executive" : Fallback(available);
            reason = "The audience indicates executives or sponsors, so the executive brief is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) &&
                 (request.Audience.Contains("audit", StringComparison.OrdinalIgnoreCase) ||
                  request.Audience.Contains("compliance", StringComparison.OrdinalIgnoreCase) ||
                  request.Audience.Contains("regulator", StringComparison.OrdinalIgnoreCase)))
        {
            profile = available.Contains("regulated") ? "regulated" : Fallback(available);
            reason = "The audience indicates compliance, audit, or regulated review, so the regulated profile is the best fit.";
        }
        else if (!string.IsNullOrWhiteSpace(request.Audience) &&
                 (request.Audience.Contains("client", StringComparison.OrdinalIgnoreCase) ||
                  request.Audience.Contains("external", StringComparison.OrdinalIgnoreCase)))
        {
            profile = available.Contains("client") ? "client" : Fallback(available);
            reason = "The audience indicates an external or client-facing reader, so the client delivery profile is the best fit.";
        }
        else
        {
            profile = available.Contains("client") ? "client" : Fallback(available);
            reason = "No strong specialized signal was provided, so the balanced client delivery profile is the safest default.";
        }

        var selected = catalog.Profiles.First(x =>
            string.Equals(x.ProfileName, profile, StringComparison.OrdinalIgnoreCase));

        var alternatives = catalog.Profiles
            .Where(x => !string.Equals(x.ProfileName, profile, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.ProfileName)
            .Take(3)
            .ToList();

        return new ConsultingDocxProfileRecommendation
        {
            RecommendedProfileName = selected.ProfileName,
            RecommendedProfileDisplayName = selected.ProfileDisplayName,
            Reason = reason,
            AlternativeProfiles = alternatives
        };
    }

    private static string Fallback(HashSet<string> available)
    {
        if (available.Contains("client")) return "client";
        return available.FirstOrDefault() ?? "client";
    }
}

