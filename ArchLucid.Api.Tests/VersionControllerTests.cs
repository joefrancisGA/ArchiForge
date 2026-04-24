using System.Text.Json;

using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class VersionControllerTests
{
    [Fact]
    public void Get_returns_ok_with_expected_fields()
    {
        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Staging");

        VersionController controller = new(env.Object);

        IActionResult result = controller.Get();

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        BuildInfoResponse response = ok.Value.Should().BeOfType<BuildInfoResponse>().Subject;

        response.Application.Should().Be("ArchLucid.Api");
        response.Environment.Should().Be("Staging");
        response.InformationalVersion.Should().NotBeNullOrWhiteSpace();
        response.AssemblyVersion.Should().NotBeNullOrWhiteSpace();
        response.RuntimeFramework.Should().Contain(".NET");
    }

    [Fact]
    public void Get_response_serializes_to_expected_json_shape()
    {
        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");

        VersionController controller = new(env.Object);

        OkObjectResult ok = (OkObjectResult)controller.Get();
        string json = JsonSerializer.Serialize(ok.Value,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false });

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("application", out _).Should().BeTrue();
        root.TryGetProperty("informationalVersion", out _).Should().BeTrue();
        root.TryGetProperty("assemblyVersion", out _).Should().BeTrue();
        root.TryGetProperty("runtimeFramework", out _).Should().BeTrue();
        root.TryGetProperty("environment", out _).Should().BeTrue();
        root.TryGetProperty("commitSha", out _).Should().BeTrue();
        root.TryGetProperty("fileVersion", out _).Should().BeTrue();
    }
}
