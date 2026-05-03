using ArchLucid.Core.Resilience;
using ArchLucid.Host.Core.Resilience;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Host.Core.Health;

/// <summary>
/// Surfaces keyed <see cref="CircuitBreakerGate"/> states for authenticated <c>/health</c> (no readiness/liveness tags).
/// Missing gates (OpenAI not wired) are skipped; never returns <see cref="HealthStatus.Unhealthy"/>.
/// </summary>
public sealed class CircuitBreakerHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    private static readonly string[] GateKeys =
    [
        OpenAiCircuitBreakerKeys.Completion,
        OpenAiCircuitBreakerKeys.CompletionFallback,
        OpenAiCircuitBreakerKeys.Embedding,
    ];

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        List<Dictionary<string, object>> gateRows = [];
        gateRows.AddRange(
            GateKeys.Select(serviceProvider.GetKeyedService<CircuitBreakerGate>)
                .OfType<CircuitBreakerGate>()
                .Select(gate => new Dictionary<string, object>
                {
                    ["name"] = gate.GateName,
                    ["state"] = gate.CurrentState,
                    ["consecutiveFailures"] = gate.ConsecutiveFailureCount,
                    ["failureThreshold"] = gate.CurrentFailureThreshold,
                    ["breakDurationSeconds"] = gate.CurrentDurationOfBreakSeconds,
                    ["lastStateChangeUtc"] = gate.LastStateChangeUtc?.ToString("o") ?? "never",
                }));

        IReadOnlyDictionary<string, object> data =
            new Dictionary<string, object> { ["gates"] = gateRows };

        if (gateRows.Count == 0)

            return Task.FromResult(
                HealthCheckResult.Healthy("OpenAI circuit breakers not registered.", data));

        foreach (string state in gateRows.Select(row => (string)row["state"]))

            if (state is "Open" or "HalfOpen")

                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        "One or more OpenAI circuits are open or probing.",
                        data: data));

        return Task.FromResult(
            HealthCheckResult.Healthy("All OpenAI circuit breakers closed.", data));
    }
}
