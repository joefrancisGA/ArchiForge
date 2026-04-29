using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Category", "Unit")]
public sealed class GovernanceSegregationRulesTests
{
    [Fact]
    public void SameActor_JwtMatchingKeys_returns_true_even_when_displays_differ()
    {
        const string canon = $"{ActorContext.JwtActorKeyPrefix}tid-guid:object-guid-user";
        GovernanceApprovalRequest req = new()
        {
            RequestedBy = "portal-name@fabrikam.net",
            RequestedByActorKey = canon,
        };

        bool same = GovernanceSegregationRules.IsSameActorForReview(
            req,
            reviewedByDisplay: "deployment-sp-name",
            reviewedByActorKey: canon);

        same.Should().BeTrue();
    }

    [Fact]
    public void SameActor_DisplaysMatching_when_no_jwt_requested_key_uses_requested_by_comparison()
    {
        GovernanceApprovalRequest req = new()
        {
            RequestedBy = "alice",
            RequestedByActorKey = null,
        };

        bool same = GovernanceSegregationRules.IsSameActorForReview(req, reviewedByDisplay: "Alice", reviewedByActorKey: "anything");

        same.Should().BeTrue();
    }

    [Fact]
    public void SameActor_DisplaysDiffer_but_jwt_both_sid_and_equal_returns_true()
    {
        const string k = $"{ActorContext.JwtActorKeyPrefix}s-t-id:o-id";
        GovernanceApprovalRequest req = new()
        {
            RequestedBy = "wrong-for-old-path",
            RequestedByActorKey = k,
        };

        bool same = GovernanceSegregationRules.IsSameActorForReview(req, reviewedByDisplay: "other-display", reviewedByActorKey: k);

        same.Should().BeTrue();
    }
}
