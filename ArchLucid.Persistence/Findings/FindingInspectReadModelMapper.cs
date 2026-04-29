using ArchLucid.Contracts.Findings;

namespace ArchLucid.Persistence.Findings;

internal static class FindingInspectReadModelMapper
{
    public static FindingHumanReviewStatus ParseHumanReview(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out FindingHumanReviewStatus st))
            return st;

        return FindingHumanReviewStatus.NotRequired;
    }
}
