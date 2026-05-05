using ArchLucid.Api.Health;
using ArchLucid.Core.Integration;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Verifies <see cref="AzureServiceBusNamespaceHealthCheck" /> skips or degrades cleanly when integration event
///     Service Bus wiring is absent or inconsistent (no outbound network calls in these scenarios).
/// </summary>
[Trait("Category", "Unit")]
public sealed class AzureServiceBusNamespaceHealthCheckTests
{
    [Fact]
    public async Task Healthy_when_queue_or_topic_empty()
    {
        IntegrationEventsOptions o = new()
        {
            QueueOrTopicName = "",
            ServiceBusConnectionString = "Endpoint=sb://example/",
            ServiceBusFullyQualifiedNamespace = "example.servicebus.windows.net",
        };

        AzureServiceBusNamespaceHealthCheck sut = new(Options.Create(o));
        HealthCheckResult result =
            await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("not configured");

        ((bool?)result.Data["configured"]).Should().Be(false);
    }

    [Fact]
    public async Task Degraded_when_queue_set_but_no_namespace_or_connection_string()
    {
        IntegrationEventsOptions o = new()
        {
            QueueOrTopicName = "integrations",
            ServiceBusFullyQualifiedNamespace = null,
            ServiceBusConnectionString = null,
        };

        AzureServiceBusNamespaceHealthCheck sut = new(Options.Create(o));
        HealthCheckResult result =
            await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("publishing is disabled");

        ((bool?)result.Data["misconfigured"]).Should().Be(true);
        ((bool?)result.Data["configured"]).Should().Be(false);
    }
}
