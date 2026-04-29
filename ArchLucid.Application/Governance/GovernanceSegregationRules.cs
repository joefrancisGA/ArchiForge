using ArchLucid.Application.Common;
using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
/// Determines whether a governance review is a self-approval for segregation-of-duties purposes:
/// matches Entra JWT canonical keys when present, otherwise compares display names (legacy / API key).
/// </summary>
public static class GovernanceSegregationRules
{
    /// <summary>
    /// Returns <see langword="true"/> when the reviewer is the same logical actor as the submitter.
    /// When both sides carry <see cref="ActorContext.JwtActorKeyPrefix"/> keys, compares those; otherwise
    /// compares <see cref="GovernanceApprovalRequest.RequestedBy"/> to <paramref name="reviewedByDisplay"/>.
    /// </summary>
    public static bool IsSameActorForReview(
        GovernanceApprovalRequest request,
        string reviewedByDisplay,
        string reviewedByActorKey)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedByDisplay);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedByActorKey);

        bool requestJwt = LooksLikeJwtCanonicalActorKey(request.RequestedByActorKey);
        bool reviewerJwt = LooksLikeJwtCanonicalActorKey(reviewedByActorKey);

        if (requestJwt && reviewerJwt)
            return string.Equals(request.RequestedByActorKey, reviewedByActorKey, StringComparison.OrdinalIgnoreCase);


        return string.Equals(request.RequestedBy, reviewedByDisplay, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool LooksLikeJwtCanonicalActorKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.StartsWith(ActorContext.JwtActorKeyPrefix, StringComparison.Ordinal);
}
