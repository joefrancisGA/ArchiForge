using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Notifications.Email;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TrialScheduledLifecycleEmailScannerTests
{
    [SkippableFact]
    public async Task PublishDueAsync_logic_app_owner_skips_without_querying_tenants()
    {
        Mock<ITenantRepository> tenants = new();
        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Mock<IIntegrationEventPublisher> publisher = new();
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationOpts = new();
        integrationOpts.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions());
        Mock<IOptionsMonitor<TrialLifecycleEmailRoutingOptions>> routing = new();
        routing.Setup(m => m.CurrentValue).Returns(
            new TrialLifecycleEmailRoutingOptions { Owner = TrialLifecycleEmailRoutingOptions.OwnerModes.LogicApp, });

        TrialScheduledLifecycleEmailScanner sut = new(
            tenants.Object,
            outbox.Object,
            publisher.Object,
            integrationOpts.Object,
            routing.Object,
            Mock.Of<ILogger<TrialScheduledLifecycleEmailScanner>>());

        await sut.PublishDueAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        tenants.Verify(repository => repository.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
