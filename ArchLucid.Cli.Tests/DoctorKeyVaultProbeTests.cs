using ArchLucid.Cli.Diagnostics;

using Azure;
using Azure.Identity;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Cli")]
public sealed class DoctorKeyVaultProbeTests
{
    [Fact]
    public void ResolvePlan_WhenConfigurationNull_throwsArgumentNullException()
    {
        FluentActions.Invoking(() => DoctorKeyVaultProbe.ResolvePlan(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void ResolvePlan_WhenUriMissingAndProviderNotKeyVault_skipsWithMessage()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:Secrets:Provider"] = "EnvironmentVariable",
            })
            .Build();

        DoctorKeyVaultProbePlan plan = DoctorKeyVaultProbe.ResolvePlan(configuration);

        plan.DecisionKind.Should().Be(DoctorKeyVaultProbePlan.Decision.Skip);
        plan.Message.Should().Contain("Skipped");
    }

    [Fact]
    public void ResolvePlan_WhenProviderIsKeyVaultButUriEmpty_isBadConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:Secrets:Provider"] = "KeyVault",
                ["ArchLucid:Secrets:KeyVaultUri"] = "   ",
            })
            .Build();

        DoctorKeyVaultProbePlan plan = DoctorKeyVaultProbe.ResolvePlan(configuration);

        plan.DecisionKind.Should().Be(DoctorKeyVaultProbePlan.Decision.BadConfiguration);
        plan.Message.Should().Contain("Misconfigured");
    }

    [Fact]
    public void ResolvePlan_WhenUriIsNotHttps_isBadConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:Secrets:KeyVaultUri"] = "http://vault.azure.net/",
            })
            .Build();

        DoctorKeyVaultProbePlan plan = DoctorKeyVaultProbe.ResolvePlan(configuration);

        plan.DecisionKind.Should().Be(DoctorKeyVaultProbePlan.Decision.BadConfiguration);
        plan.Message.Should().Contain("https");
    }

    [Fact]
    public void ResolvePlan_WhenHttpsUriPresent_isReady()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:Secrets:KeyVaultUri"] = "https://example.vault.azure.net/",
            })
            .Build();

        DoctorKeyVaultProbePlan plan = DoctorKeyVaultProbe.ResolvePlan(configuration);

        plan.DecisionKind.Should().Be(DoctorKeyVaultProbePlan.Decision.Ready);
        plan.VaultUri.Should().NotBeNull();
        plan.VaultUri!.Host.Should().Be("example.vault.azure.net");
    }

    [Fact]
    public void DescribeFailure_When403AndList_mentionsListPermission()
    {
        string message = DoctorKeyVaultProbe.DescribeFailure(new RequestFailedException(403, "Forbidden", errorCode: null,
            innerException: null), probeWasGetSecret: false);

        message.Should().Contain("Permission Denied");
        message.Should().Contain("list");
    }

    [Fact]
    public void DescribeFailure_When403AndGet_mentionsGetPermission()
    {
        string message = DoctorKeyVaultProbe.DescribeFailure(new RequestFailedException(403, "Forbidden", errorCode: null,
            innerException: null), probeWasGetSecret: true);

        message.Should().Contain("Permission Denied");
        message.Should().Contain("get");
    }

    [Fact]
    public void DescribeFailure_When401_saysAuthenticationFailed()
    {
        string message = DoctorKeyVaultProbe.DescribeFailure(new RequestFailedException(401, "Unauthorized", errorCode: null,
            innerException: null), probeWasGetSecret: false);

        message.Should().Contain("Authentication Failed");
    }

    [Fact]
    public void DescribeFailure_WhenCredentialUnavailable_mapsMessage()
    {
        string message = DoctorKeyVaultProbe.DescribeFailure(new CredentialUnavailableException("none"), probeWasGetSecret: false);

        message.Should().Contain("Authentication Failed");
        message.Should().Contain("Managed Identity");
    }

    [Fact]
    public void DescribeFailure_WhenCanceled_saysTimeout()
    {
        string message =
            DoctorKeyVaultProbe.DescribeFailure(new OperationCanceledException(), probeWasGetSecret: false);

        message.Should().Contain("Timed out");
    }

    [Fact]
    public void DescribeFailure_WhenAggregateSingleInner_unwrapsRequestFailed()
    {
        RequestFailedException inner = new(403, "Forbidden", errorCode: null, innerException: null);
        string message = DoctorKeyVaultProbe.DescribeFailure(new AggregateException(inner), probeWasGetSecret: false);

        message.Should().Contain("Permission Denied");
    }
}
