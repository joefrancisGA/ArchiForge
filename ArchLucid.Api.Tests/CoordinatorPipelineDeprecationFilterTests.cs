using ArchLucid.Api.Filters;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Unit tests for <see cref="CoordinatorPipelineDeprecationFilter"/>: confirms the filter forwards the
/// action to the next delegate and registers <c>OnStarting</c> callbacks that emit the standards-track
/// deprecation triplet (RFC 9745 / 8594 / 8288). The filter is unconditional — ADR 0021 Phase 2 is a
/// hard architectural milestone, not a per-deployment toggle.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CoordinatorPipelineDeprecationFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_invokes_next_and_emits_deprecation_triplet_on_response_start()
    {
        CoordinatorPipelineDeprecationFilter sut = new();

        ActionExecutingContext executing = BuildExecutingContext("/v1/architecture/run/abc/commit", out CapturingHttpResponseFeature feature);
        bool delegateInvoked = false;

        await sut.OnActionExecutionAsync(executing, () =>
        {
            delegateInvoked = true;
            return Task.FromResult(BuildExecutedContext(executing));
        });

        delegateInvoked.Should().BeTrue();

        await feature.FireOnStartingAsync();

        executing.HttpContext.Response.Headers["Deprecation"].ToString().Should().Be("true");
        executing.HttpContext.Response.Headers["Sunset"].ToString().Should().Be(CoordinatorPipelineDeprecationFilter.SunsetHttpDate);
        executing.HttpContext.Response.Headers["Link"].ToString().Should().Be(CoordinatorPipelineDeprecationFilter.DeprecationLink);
    }

    [Fact]
    public async Task OnActionExecutionAsync_does_not_duplicate_headers_when_callback_fires_twice()
    {
        // Some hosting pipelines invoke OnStarting once per write; assignment-style header writes
        // (rather than Append) keep us idempotent.
        CoordinatorPipelineDeprecationFilter sut = new();

        ActionExecutingContext executing = BuildExecutingContext("/v1/architecture/run/abc/execute", out CapturingHttpResponseFeature feature);

        await sut.OnActionExecutionAsync(executing, () => Task.FromResult(BuildExecutedContext(executing)));

        await feature.FireOnStartingAsync();
        await feature.FireOnStartingAsync();

        executing.HttpContext.Response.Headers["Deprecation"].Should().ContainSingle().Which.Should().Be("true");
        executing.HttpContext.Response.Headers["Sunset"].Should().ContainSingle().Which.Should().Be(CoordinatorPipelineDeprecationFilter.SunsetHttpDate);
        executing.HttpContext.Response.Headers["Link"].Should().ContainSingle().Which.Should().Be(CoordinatorPipelineDeprecationFilter.DeprecationLink);
    }

    [Fact]
    public void Constants_satisfy_RFC_8594_and_RFC_8288_shape()
    {
        // Sunset MUST be an RFC 1123 / RFC 5322 HTTP-date — round-trip via "r" specifier.
        DateTimeOffset parsedSunset = DateTimeOffset.ParseExact(
            CoordinatorPipelineDeprecationFilter.SunsetHttpDate,
            "r",
            System.Globalization.CultureInfo.InvariantCulture);

        parsedSunset.Should().BeAfter(new DateTimeOffset(2026, 4, 21, 0, 0, 0, TimeSpan.Zero),
            because: "Sunset must be in the future relative to the ADR 0021 Phase 2 ship date so consumers have a real migration window");

        string link = CoordinatorPipelineDeprecationFilter.DeprecationLink;
        link.Should().StartWith("<", because: "RFC 8288 wraps the URI reference in angle brackets");
        link.Should().Contain("rel=\"deprecation\"", because: "the link relation type must be \"deprecation\" per RFC 9745 §4");
        link.Should().Contain("0021-coordinator-pipeline-strangler-plan.md", because: "the link must point at ADR 0021 (the canonical migration target)");
    }

    [Fact]
    public async Task OnActionExecutionAsync_throws_on_null_arguments()
    {
        CoordinatorPipelineDeprecationFilter sut = new();
        ActionExecutingContext context = BuildExecutingContext("/v1/architecture/x", out _);

        Func<Task> withNullContext = async () => await sut.OnActionExecutionAsync(null!, () => Task.FromResult(BuildExecutedContext(context)));
        Func<Task> withNullDelegate = async () => await sut.OnActionExecutionAsync(context, null!);

        await withNullContext.Should().ThrowAsync<ArgumentNullException>();
        await withNullDelegate.Should().ThrowAsync<ArgumentNullException>();
    }

    private static ActionExecutingContext BuildExecutingContext(string requestPath, out CapturingHttpResponseFeature responseFeature)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = requestPath;

        // Replace DefaultHttpContext's response feature with a capturing implementation that lets
        // tests fire the OnStarting callbacks deterministically (the default impl only fires them
        // once a real body write happens — not appropriate for a unit test).
        responseFeature = new CapturingHttpResponseFeature();
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);

        ActionContext actionContext = new(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ActionExecutedContext BuildExecutedContext(ActionExecutingContext executing)
        => new(executing, [], controller: new object());

    /// <summary>
    /// Minimal <see cref="IHttpResponseFeature"/> implementation that captures every OnStarting callback
    /// and exposes <see cref="FireOnStartingAsync"/> so unit tests can assert the headers the production
    /// filter would emit on a real HTTP response. Only the members <see cref="DefaultHttpContext"/> and
    /// the production filter actually touch are implemented; everything else is left as a no-op.
    /// </summary>
    private sealed class CapturingHttpResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));

            _onStarting.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            // Not exercised by the production filter; left intentionally empty.
        }

        public async Task FireOnStartingAsync()
        {
            foreach ((Func<object, Task> callback, object state) in _onStarting)
                await callback(state);
        }
    }
}
