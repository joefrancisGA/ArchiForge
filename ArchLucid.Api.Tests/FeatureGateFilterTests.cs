using ArchLucid.Api.Attributes;
using ArchLucid.Api.Filters;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit-tests for <see cref="FeatureGateFilter" />: confirms the filter forwards the action when the gate is open
///     (<c>Demo:Enabled=true</c>) and short-circuits with <c>404 Not Found</c> Problem Details when it is not. The 404
///     (rather than 403) is intentional so production-like deployments cannot leak the existence of demo surfaces.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class FeatureGateFilterTests
{
    [SkippableFact]
    public async Task DemoEnabled_open_invokes_next_and_does_not_set_result()
    {
        FeatureGateFilter sut = new(FeatureGateKey.DemoEnabled, Options.Create(new DemoOptions { Enabled = true }));

        ActionExecutingContext executing = BuildExecutingContext("/v1/demo/explain");
        bool delegateInvoked = false;

        await sut.OnActionExecutionAsync(executing, () =>
        {
            delegateInvoked = true;
            return Task.FromResult(BuildExecutedContext(executing));
        });

        delegateInvoked.Should().BeTrue();
        executing.Result.Should().BeNull();
    }

    [SkippableFact]
    public async Task DemoEnabled_closed_short_circuits_with_404_problem_details()
    {
        FeatureGateFilter sut = new(FeatureGateKey.DemoEnabled, Options.Create(new DemoOptions { Enabled = false }));

        ActionExecutingContext executing = BuildExecutingContext("/v1/demo/explain");
        bool delegateInvoked = false;

        await sut.OnActionExecutionAsync(executing, () =>
        {
            delegateInvoked = true;
            return Task.FromResult(BuildExecutedContext(executing));
        });

        delegateInvoked.Should().BeFalse("the filter must short-circuit before the action runs");
        ObjectResult result = executing.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Instance.Should().Be("/v1/demo/explain");
    }

    [SkippableFact]
    public async Task Unmapped_gate_key_closes_by_default()
    {
        // An undefined key (cast forces the discard arm). The filter must err on the side of *closed* so an
        // accidentally added attribute cannot silently expose a route on production.
        const FeatureGateKey unmapped = (FeatureGateKey)int.MaxValue;
        FeatureGateFilter sut = new(unmapped, Options.Create(new DemoOptions { Enabled = true }));

        ActionExecutingContext executing = BuildExecutingContext("/v1/anything");

        await sut.OnActionExecutionAsync(executing, () => Task.FromResult(BuildExecutedContext(executing)));

        ObjectResult result = executing.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    private static ActionExecutingContext BuildExecutingContext(string requestPath)
    {
        DefaultHttpContext httpContext = new() { Request = { Path = requestPath } };

        ActionContext actionContext = new(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutedContext BuildExecutedContext(ActionExecutingContext executing)
    {
        return new ActionExecutedContext(executing, [], new object());
    }
}
