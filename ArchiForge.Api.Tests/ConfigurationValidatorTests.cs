using ArchiForge.Api.Startup.Validation;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

public sealed class ConfigurationValidatorTests
{
    [Fact]
    public async Task StartAsync_WhenProductionAndDevelopmentBypass_throws()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchiForge"] = "Server=.;",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        ConfigurationValidator validator = new(
            Mock.Of<ILogger<ConfigurationValidator>>(),
            configuration,
            env.Object);

        Func<Task> act = () => validator.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Configuration validation failed*");
    }

    [Fact]
    public async Task StartAsync_WhenProductionAndJwtBearerWithoutAuthority_throws()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchiForge"] = "Server=.;",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        ConfigurationValidator validator = new(
            Mock.Of<ILogger<ConfigurationValidator>>(),
            configuration,
            env.Object);

        Func<Task> act = () => validator.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_WhenProductionAndApiKeyModeButKeysDisabled_throws()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchiForge"] = "Server=.;",
            ["ArchiForgeAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "false",
            ["Authentication:ApiKey:AdminKey"] = "k",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        ConfigurationValidator validator = new(
            Mock.Of<ILogger<ConfigurationValidator>>(),
            configuration,
            env.Object);

        Func<Task> act = () => validator.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_WhenDevelopmentAndDevelopmentBypass_completes()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchiForge"] = "Server=.;",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        ConfigurationValidator validator = new(
            Mock.Of<ILogger<ConfigurationValidator>>(),
            configuration,
            env.Object);

        await validator.StartAsync(CancellationToken.None);
    }
}
