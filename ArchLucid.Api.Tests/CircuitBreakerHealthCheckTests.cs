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
        ServiceCollection services = [];
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
        object gatesObj = result.Data["gates"];
        List<Dictionary<string, object>>? gateList = gatesObj as List<Dictionary<string, object>>;
        gateList.Should().NotBeNull();
        gateList.Should().HaveCount(2);

        foreach (Dictionary<string, object> row in gateList)
        {
            row.Should().ContainKeys(
                "name",
                "state",
                "consecutiveFailures",
                "failureThreshold",
                "breakDurationSeconds",
                "lastStateChangeUtc");
            row["state"].Should().Be("Closed");
            row["consecutiveFailures"].Should().Be(0);
            row["failureThreshold"].Should().Be(5);
            row["breakDurationSeconds"].Should().Be(60);
            row["lastStateChangeUtc"].Should().Be("never");
        }
    }

    [Fact]
    public async Task OneOpen_Returns_Degraded()
    {
        ServiceCollection services = [];
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
        object gatesObj = result.Data["gates"];
        List<Dictionary<string, object>>? gateList = gatesObj as List<Dictionary<string, object>>;
        gateList.Should().NotBeNull();
        Dictionary<string, object>? completionRow =
            gateList.FirstOrDefault(r => (string)r["name"] == OpenAiCircuitBreakerKeys.Completion);
        completionRow.Should().NotBeNull();
        completionRow["state"].Should().Be("Open");
        completionRow["consecutiveFailures"].Should().Be(1);
        completionRow["failureThreshold"].Should().Be(1);
        completionRow["breakDurationSeconds"].Should().Be(60);
        completionRow["lastStateChangeUtc"].Should().BeOfType<string>().Which.Should().NotBe("never");
    }

    [Fact]
    public async Task NullGates_Returns_Healthy()
    {
        ServiceCollection services = [];

        await using ServiceProvider provider = services.BuildServiceProvider();
        CircuitBreakerHealthCheck check = new(provider);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("gates");
        object gatesObj = result.Data["gates"];
        List<Dictionary<string, object>>? gateList = gatesObj as List<Dictionary<string, object>>;
        gateList.Should().NotBeNull();
        gateList.Should().BeEmpty();
    }
}
