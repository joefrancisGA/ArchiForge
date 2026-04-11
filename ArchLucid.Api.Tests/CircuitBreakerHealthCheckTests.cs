using ArchLucid.Core.Resilience;
using ArchLucid.Host.Core.Health;
using ArchLucid.Host.Core.Resilience;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class CircuitBreakerHealthCheckTests
{
    [Fact]
    public async Task AllClosed_Returns_Healthy()
    {
        ServiceCollection services = new();
        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        services.AddKeyedSingleton(
            OpenAiCircuitBreakerKeys.Completion,
            new CircuitBreakerGate(OpenAiCircuitBreakerKeys.Completion, options));
        services.AddKeyedSingleton(
            OpenAiCircuitBreakerKeys.Embedding,
            new CircuitBreakerGate(OpenAiCircuitBreakerKeys.Embedding, options));

        await using ServiceProvider provider = services.BuildServiceProvider();
        CircuitBreakerHealthCheck check = new(provider);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("gates");
    }

    [Fact]
    public async Task OneOpen_Returns_Degraded()
    {
        ServiceCollection services = new();
        CircuitBreakerOptions closedOpts = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        CircuitBreakerOptions openOpts = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate openGate = new(OpenAiCircuitBreakerKeys.Completion, openOpts);
        openGate.RecordFailure();

        services.AddKeyedSingleton(OpenAiCircuitBreakerKeys.Completion, openGate);
        services.AddKeyedSingleton(
            OpenAiCircuitBreakerKeys.Embedding,
            new CircuitBreakerGate(OpenAiCircuitBreakerKeys.Embedding, closedOpts));

        await using ServiceProvider provider = services.BuildServiceProvider();
        CircuitBreakerHealthCheck check = new(provider);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task NullGates_Returns_Healthy()
    {
        ServiceCollection services = new();

        await using ServiceProvider provider = services.BuildServiceProvider();
        CircuitBreakerHealthCheck check = new(provider);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("gates");
        object gatesObj = result.Data["gates"];
        List<Dictionary<string, object>>? gateList = gatesObj as List<Dictionary<string, object>>;
        gateList.Should().NotBeNull();
        gateList!.Should().BeEmpty();
    }
}
