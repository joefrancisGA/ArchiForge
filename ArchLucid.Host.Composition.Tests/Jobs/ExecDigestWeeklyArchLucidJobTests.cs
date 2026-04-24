using ArchLucid.Application.ExecDigest;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExecDigestWeeklyArchLucidJobTests
{
    [Fact]
    public void Name_is_canonical_exec_digest_weekly_slug()
    {
        ExecDigestWeeklyArchLucidJob job = new(Mock.Of<IServiceProvider>(),
            NullLogger<ExecDigestWeeklyArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.ExecDigestWeekly);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_no_enabled_tenants()
    {
        await using ServiceProvider provider = BuildProviderWithNoEnabledTenants();
        ExecDigestWeeklyArchLucidJob job = new(provider, NullLogger<ExecDigestWeeklyArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    private static ServiceProvider BuildProviderWithNoEnabledTenants()
    {
        Mock<ITenantExecDigestPreferencesRepository> digestPrefs = new();
        digestPrefs
            .Setup(r => r.ListEmailEnabledTenantIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ServiceCollection services = [];
        services.AddSingleton(digestPrefs.Object);
        services.AddSingleton(Mock.Of<ITenantRepository>());
        services.AddSingleton(Mock.Of<IExecDigestComposer>());
        services.AddSingleton(Mock.Of<IExecDigestEmailDispatcher>());
        services.AddSingleton(Mock.Of<ITenantTrialEmailContactLookup>());
        services.AddSingleton(Mock.Of<IExecDigestUnsubscribeTokenFactory>());
        services.AddSingleton(Mock.Of<IOptionsMonitor<EmailNotificationOptions>>());
        services.AddScoped<ExecDigestWeeklyDeliveryScanner>();
        services.AddSingleton<ILogger<ExecDigestWeeklyDeliveryScanner>>(
            NullLogger<ExecDigestWeeklyDeliveryScanner>.Instance);

        return services.BuildServiceProvider();
    }
}
