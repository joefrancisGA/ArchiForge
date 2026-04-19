using System.Text.Json;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests.Filters;

[Trait("Suite", "Core")]
public sealed class TrialLimitFilterTests
{
    [Fact]
    public void Trial_limit_problem_json_contains_required_extensions()
    {
        DefaultHttpContext http = new();
        http.TraceIdentifier = "test-correlation";
        TrialLimitExceededException ex = new(TrialLimitReason.Expired, daysRemaining: 0);

        ObjectResult result = TrialLimitProblemResponse.CreateResult(ex, "/v1/runs", http);

        result.StatusCode.Should().Be(402);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.TrialExpired);

        string json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        root.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("traceCompleteness").GetProperty("totalFindings").GetInt32().Should().Be(0);
        root.GetProperty("traceCompleteness").GetProperty("overallCompletenessRatio").GetDouble().Should().Be(0);
        root.GetProperty("trialReason").GetString().Should().Be("Expired");
        root.GetProperty("daysRemaining").GetInt32().Should().Be(0);
    }
}
