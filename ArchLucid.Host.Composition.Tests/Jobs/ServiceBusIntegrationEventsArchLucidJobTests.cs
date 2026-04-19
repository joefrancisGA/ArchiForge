using ArchLucid.Core.Integration;
using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ServiceBusIntegrationEventsArchLucidJobTests
{
    [Fact]
    public void Name_is_canonical_service_bus_integration_events_slug()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions { ConsumerEnabled = false });

        ServiceBusIntegrationEventsArchLucidJob job = new(
            Array.Empty<IIntegrationEventHandler>(),
            options.Object,
            NullLogger<ServiceBusIntegrationEventsArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.ServiceBusIntegrationEvents);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_consumer_disabled()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions { ConsumerEnabled = false });

        ServiceBusIntegrationEventsArchLucidJob job = new(
            Array.Empty<IIntegrationEventHandler>(),
            options.Object,
            NullLogger<ServiceBusIntegrationEventsArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_configuration_error_when_topic_missing()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(
            new IntegrationEventsOptions
            {
                ConsumerEnabled = true,
                QueueOrTopicName = " ",
                SubscriptionName = "sub",
                ServiceBusFullyQualifiedNamespace = "ns.servicebus.windows.net"
            });

        ServiceBusIntegrationEventsArchLucidJob job = new(
            Array.Empty<IIntegrationEventHandler>(),
            options.Object,
            NullLogger<ServiceBusIntegrationEventsArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task RunOnceAsync_returns_configuration_error_when_subscription_missing()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(
            new IntegrationEventsOptions
            {
                ConsumerEnabled = true,
                QueueOrTopicName = "topic",
                SubscriptionName = null,
                ServiceBusFullyQualifiedNamespace = "ns.servicebus.windows.net"
            });

        ServiceBusIntegrationEventsArchLucidJob job = new(
            Array.Empty<IIntegrationEventHandler>(),
            options.Object,
            NullLogger<ServiceBusIntegrationEventsArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task RunOnceAsync_returns_configuration_error_when_no_service_bus_endpoint_configured()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(
            new IntegrationEventsOptions
            {
                ConsumerEnabled = true,
                QueueOrTopicName = "topic",
                SubscriptionName = "sub",
                ServiceBusFullyQualifiedNamespace = null,
                ServiceBusConnectionString = null
            });

        ServiceBusIntegrationEventsArchLucidJob job = new(
            Array.Empty<IIntegrationEventHandler>(),
            options.Object,
            NullLogger<ServiceBusIntegrationEventsArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }
}
