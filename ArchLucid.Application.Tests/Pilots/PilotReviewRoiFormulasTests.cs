using ArchLucid.Application.Pilots;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Pilots;

public sealed class PilotReviewRoiFormulasTests
{
    [Fact]
    public void Annual_savings_is_half_of_status_quo_at_fifty_percent_hour_reduction()
    {
        decimal q = 12;
        decimal h = 40;
        decimal c = 150m;
        decimal status = PilotReviewRoiFormulas.AnnualReviewCostStatusQuo(q, h, c);
        decimal savings = PilotReviewRoiFormulas.AnnualReviewSavings(q, h, c);

        status.Should().Be(12m * 4m * 40m * 150m);
        savings.Should().BeApproximately(status * 0.5m, 0.01m);
    }
}
