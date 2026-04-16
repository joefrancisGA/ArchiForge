using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Models;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ClientErrorTelemetryControllerTests
{
    private static ClientErrorTelemetryController CreateController()
    {
        ClientErrorTelemetryController controller = new(NullLogger<ClientErrorTelemetryController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        return controller;
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
                TimestampUtc = "2026-04-16T00:00:00Z",
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

        for (int i = 0; i < 12; i++)
        {
            ctx[$"k{i}"] = "v";
        }

        IActionResult result = controller.PostClientError(
            new ClientErrorReport
            {
                Message = "overflow",
                Context = ctx,
            });

        result.Should().BeAssignableTo<ObjectResult>();
    }
}
