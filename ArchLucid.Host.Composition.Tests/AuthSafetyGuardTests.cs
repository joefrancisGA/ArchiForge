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
    public void GuardDevelopmentBypassInProduction_archlucid_environment_lowercase_production_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["ARCHLUCID_ENVIRONMENT"] = "production",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_archlucid_environment_staging_prod_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["ARCHLUCID_ENVIRONMENT"] = "staging-prod",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_environment_name_prod_throws_even_when_not_IsProduction()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "DevelopmentBypass" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment("Prod");

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_environment_name_non_production_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "DevelopmentBypass" })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment("non-production");

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_archlucid_environment_staging_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["ARCHLUCID_ENVIRONMENT"] = "Staging",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().NotThrow();
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

    [Fact]
    public void GuardAllDevelopmentBypasses_production_and_development_bypass_all_true_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "JwtBearer",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Production);

        Action act = () => AuthSafetyGuard.GuardAllDevelopmentBypasses(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Authentication:ApiKey:DevelopmentBypassAll*");
    }

    [Fact]
    public void GuardAllDevelopmentBypasses_development_environment_and_development_bypass_all_true_does_not_throw()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "JwtBearer",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardAllDevelopmentBypasses(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void GuardAllDevelopmentBypasses_archlucid_environment_production_and_development_bypass_all_throws()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "ApiKey",
                ["ARCHLUCID_ENVIRONMENT"] = "Production",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Development);

        Action act = () => AuthSafetyGuard.GuardAllDevelopmentBypasses(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Authentication:ApiKey:DevelopmentBypassAll*");
    }

    [Fact]
    public void GuardDevelopmentBypassInProduction_delegates_to_guard_all_and_blocks_bypass_all_in_production()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucidAuth:Mode"] = "ApiKey",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
            })
            .Build();
        IHostEnvironment environment = new StubHostEnvironment(Environments.Production);

        Action act = () => AuthSafetyGuard.GuardDevelopmentBypassInProduction(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Authentication:ApiKey:DevelopmentBypassAll*");
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
