using ArchiForge.Api.ProblemDetails;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Unit tests for operator-facing <c>supportHint</c> attachment (56R hardening).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ProblemSupportHintsTests
{
    [Fact]
    public void AttachForProblemType_WhenProblemIsNull_throws()
    {
        Action act = () => ProblemSupportHints.AttachForProblemType(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AttachForProblemType_WhenTypeIsEmpty_does_not_add_supportHint()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new() { Type = "" };

        ProblemSupportHints.AttachForProblemType(problem);

        problem.Extensions.Should().NotContainKey("supportHint");
    }

    [Fact]
    public void AttachForProblemType_WhenTypeIsUnknown_does_not_add_supportHint()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new() { Type = "https://example.invalid/problems/unknown" };

        ProblemSupportHints.AttachForProblemType(problem);

        problem.Extensions.Should().NotContainKey("supportHint");
    }

    [Fact]
    public void AttachForProblemType_WhenRunNotFound_adds_scope_hint()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new() { Type = ProblemTypes.RunNotFound };

        ProblemSupportHints.AttachForProblemType(problem);

        problem.Extensions.Should().ContainKey("supportHint");
        string hint = problem.Extensions["supportHint"].Should().BeOfType<string>().Subject;
        hint.ToLowerInvariant().Should().Contain("scope");
    }

    [Fact]
    public void AttachForProblemType_WhenConflict_adds_idempotency_hint()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new() { Type = ProblemTypes.Conflict };

        ProblemSupportHints.AttachForProblemType(problem);

        problem.Extensions.Should().ContainKey("supportHint");
        string hint = problem.Extensions["supportHint"].Should().BeOfType<string>().Subject;
        hint.ToLowerInvariant().Should().Contain("idempotency");
    }

    [Fact]
    public void AttachForProblemType_WhenDatabaseTimeout_adds_health_ready_hint()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new() { Type = ProblemTypes.DatabaseTimeout };

        ProblemSupportHints.AttachForProblemType(problem);

        problem.Extensions.Should().ContainKey("supportHint");
        string hint = problem.Extensions["supportHint"].Should().BeOfType<string>().Subject;
        hint.ToLowerInvariant().Should().Contain("health/ready");
    }
}
