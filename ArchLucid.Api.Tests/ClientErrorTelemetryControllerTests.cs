using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Models;
using ArchLucid.Application.Telemetry;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ClientErrorTelemetryControllerTests
{
    private static ClientErrorTelemetryController CreateController(
        IScopeContextProvider? scopeProviderOverride = null,
        IFirstTenantFunnelEmitter? funnelEmitterOverride = null)
    {
        IScopeContextProvider scopeProvider = scopeProviderOverride ?? CreateDefaultScopeProvider();
        IFirstTenantFunnelEmitter emitter = funnelEmitterOverride ?? new NullFirstTenantFunnelEmitter();

        ClientErrorTelemetryController controller =
            new(NullLogger<ClientErrorTelemetryController>.Instance, scopeProvider, emitter)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        return controller;
    }

    /// <summary>
    ///     Improvement 12 — captures the calls the controller makes to <see cref="IFirstTenantFunnelEmitter"/>
    ///     so the tests can assert that the controller never sends a tenant id in the request body
    ///     (it must always be inferred from the scope) and never extends the body shape beyond the event name.
    /// </summary>
    private sealed class CapturingFirstTenantFunnelEmitter : IFirstTenantFunnelEmitter
    {
        public List<(string EventName, Guid TenantId)> Calls { get; } = [];

        public Task EmitAsync(string eventName, Guid tenantId, CancellationToken ct = default)
        {
            Calls.Add((eventName, tenantId));
            return Task.CompletedTask;
        }
    }

    private sealed class NullFirstTenantFunnelEmitter : IFirstTenantFunnelEmitter
    {
        public Task EmitAsync(string eventName, Guid tenantId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private static IScopeContextProvider CreateDefaultScopeProvider()
    {
        ScopeContext ctx = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            WorkspaceId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
            ProjectId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000")
        };
        Mock<IScopeContextProvider> mock = new();
        mock.Setup(s => s.GetCurrentScope()).Returns(ctx);

        return mock.Object;
    }

    [Fact]
    public void PostClientError_valid_report_returns_204()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostClientError(
            new ClientErrorReport
            {
                Message = "Test client error",
                Stack = "at x",
                Pathname = "/runs",
                UserAgent = "Vitest",
                TimestampUtc = "2026-04-16T00:00:00Z"
            });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void PostClientError_null_body_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostClientError(null);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public void PostClientError_empty_message_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostClientError(new ClientErrorReport { Message = "   " });

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public void PostClientError_context_too_many_entries_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();
        Dictionary<string, string> ctx = new();

        // One more than ClientErrorTelemetryIngestLimits.MaxContextEntries to assert the over-limit branch.
        for (int i = 0; i < ClientErrorTelemetryIngestLimits.MaxContextEntries + 2; i++)
        {
            ctx[$"k{i}"] = "v";
        }

        IActionResult result = controller.PostClientError(
            new ClientErrorReport { Message = "overflow", Context = ctx });

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public void PostSponsorBannerFirstCommitBadge_valid_bucket_returns_204()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostSponsorBannerFirstCommitBadge(
            new SponsorBannerFirstCommitBadgeRequest { DaysSinceFirstCommitBucket = "4-7" });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void PostSponsorBannerFirstCommitBadge_invalid_bucket_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostSponsorBannerFirstCommitBadge(
            new SponsorBannerFirstCommitBadgeRequest { DaysSinceFirstCommitBucket = "nope" });

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public void PostSponsorBannerFirstCommitBadge_null_body_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = controller.PostSponsorBannerFirstCommitBadge(null);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    /// <summary>
    ///     Improvement 12 — happy path. Controller infers tenantId from scope and forwards exactly the
    ///     event name to the emitter; the request body never carries a tenant id.
    /// </summary>
    [Fact]
    public async Task PostFirstTenantFunnelEvent_valid_event_returns_204_and_emits_with_scoped_tenant()
    {
        Guid scopeTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CapturingFirstTenantFunnelEmitter emitter = new();
        ClientErrorTelemetryController controller = CreateController(funnelEmitterOverride: emitter);

        IActionResult result = await controller.PostFirstTenantFunnelEvent(
            new FirstTenantFunnelEventRequest { Event = FirstTenantFunnelEventNames.Signup },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        emitter.Calls.Should().ContainSingle();
        emitter.Calls[0].EventName.Should().Be(FirstTenantFunnelEventNames.Signup);
        emitter.Calls[0].TenantId.Should().Be(scopeTenantId,
            "the controller must infer tenantId from request scope, not from the body");
    }

    /// <summary>
    ///     Improvement 12 — the request DTO has no tenant id field, so even if a malicious client wrapped
    ///     a tenant id into JSON, it would be ignored. Smoke-test that an unknown tenant in scope still
    ///     produces a 204; emitter receives the scoped tenant id only.
    /// </summary>
    [Fact]
    public async Task PostFirstTenantFunnelEvent_unauthenticated_scope_uses_empty_tenant_and_still_returns_204()
    {
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext
        {
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty
        });
        CapturingFirstTenantFunnelEmitter emitter = new();
        ClientErrorTelemetryController controller = CreateController(scope.Object, emitter);

        IActionResult result = await controller.PostFirstTenantFunnelEvent(
            new FirstTenantFunnelEventRequest { Event = FirstTenantFunnelEventNames.TourOptIn },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        emitter.Calls.Should().ContainSingle();
        emitter.Calls[0].TenantId.Should().Be(Guid.Empty,
            "signup-time funnel events fire before auth; emitter must tolerate Guid.Empty");
    }

    [Fact]
    public async Task PostFirstTenantFunnelEvent_unknown_event_name_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = await controller.PostFirstTenantFunnelEvent(
            new FirstTenantFunnelEventRequest { Event = "definitely-not-a-funnel-event" },
            CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public async Task PostFirstTenantFunnelEvent_null_body_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = await controller.PostFirstTenantFunnelEvent(null, CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public async Task PostFirstTenantFunnelEvent_missing_event_returns_400()
    {
        ClientErrorTelemetryController controller = CreateController();

        IActionResult result = await controller.PostFirstTenantFunnelEvent(
            new FirstTenantFunnelEventRequest { Event = "   " },
            CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>();
    }
}
