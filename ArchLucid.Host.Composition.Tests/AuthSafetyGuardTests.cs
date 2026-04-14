using ArchLucid.Host.Core.Startup;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>Startup guard: DevelopmentBypass must not run in production (ASPNETCORE_ENVIRONMENT or ARCHLUCID_ENVIRONMENT).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AuthSafetyGuardTests
{
    [Fact]
    public void GuardDevelopmentBypassInProduction_production_environment_and_dev_bypass_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "DevelopmentBypass" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Production);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DevelopmentBypass*JwtBearer*ApiKey*");
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_development_environment_and_dev_bypass_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "DevelopmentBypass" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_archlucid_environment_production_and_dev_bypass_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["ARCHLUCID_ENVIRONMENT"] = "Production",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_production_jwt_bearer_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "JwtBearer" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Production);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_production_api_key_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "ApiKey" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Production);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().NotThrow();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "ArchLucid.Host.Composition.Tests";

        public string ContentRootPath { get; set; } = "/";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
