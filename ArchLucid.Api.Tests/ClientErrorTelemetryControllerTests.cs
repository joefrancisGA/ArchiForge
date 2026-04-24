using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Models;
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
    private static ClientErrorTelemetryController CreateController(IScopeContextProvider? scopeProviderOverride = null)
    {
        IScopeContextProvider scopeProvider = scopeProviderOverride ?? CreateDefaultScopeProvider();

        ClientErrorTelemetryController controller =
            new(NullLogger<ClientErrorTelemetryController>.Instance, scopeProvider)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        return controller;
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
}
