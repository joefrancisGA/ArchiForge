using ArchLucid.Core;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

public sealed class LlmMonthlyTenantDollarBudgetTrackerTests
{
    [Fact]
    public void EnsureWithinBudgetBeforeCall_when_under_hard_cutoff_does_not_throw()
    {
        LlmMonthlyTenantDollarBudgetOptions opts = new()
        {
            Enabled = true,
            IncludedUsdPerUtcMonth = 50m,
            HardCutoffUsdPerUtcMonth = 75m,
            WarnFraction = 0.75m,
            AssumedMaxPromptTokensPerRequest = 1,
            AssumedMaxCompletionTokensPerRequest = 1
        };

        Mock<IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);
        Mock<ILlmCostEstimator> cost = new();
        cost.Setup(e => e.EstimateUsd(It.IsAny<int>(), It.IsAny<int>())).Returns(5m);

        LlmMonthlyTenantDollarBudgetTracker tracker = new(monitor.Object, cost.Object);
        Guid tenant = Guid.NewGuid();

        tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");
        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", CreateScopeProvider(tenant), null, 100, 100);
        tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");
    }

    [Fact]
    public void EnsureWithinBudgetBeforeCall_when_would_exceed_hard_cutoff_throws()
    {
        LlmMonthlyTenantDollarBudgetOptions opts = new()
        {
            Enabled = true,
            IncludedUsdPerUtcMonth = 50m,
            HardCutoffUsdPerUtcMonth = 75m,
            AssumedMaxPromptTokensPerRequest = 1,
            AssumedMaxCompletionTokensPerRequest = 1
        };

        Mock<IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);
        Mock<ILlmCostEstimator> cost = new();
        cost.Setup(e => e.EstimateUsd(It.IsAny<int>(), It.IsAny<int>())).Returns(25m);

        LlmMonthlyTenantDollarBudgetTracker tracker = new(monitor.Object, cost.Object);
        Guid tenant = Guid.NewGuid();

        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", CreateScopeProvider(tenant), null, 10, 10);
        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", CreateScopeProvider(tenant), null, 10, 10);
        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", CreateScopeProvider(tenant), null, 10, 10);

        Action act = () => tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");

        act.Should().Throw<LlmTokenQuotaExceededException>();
    }

    [Fact]
    public void RecordUsageAndMaybeWarn_warns_at_most_once_per_utc_month_when_threshold_crossed()
    {
        LlmMonthlyTenantDollarBudgetOptions opts = new()
        {
            Enabled = true,
            IncludedUsdPerUtcMonth = 50m,
            HardCutoffUsdPerUtcMonth = 75m,
            WarnFraction = 0.75m
        };

        Mock<IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);
        Mock<ILlmCostEstimator> cost = new();
        cost.Setup(e => e.EstimateUsd(It.IsAny<int>(), It.IsAny<int>())).Returns(12m);

        LlmMonthlyTenantDollarBudgetTracker tracker = new(monitor.Object, cost.Object);
        Guid tenant = Guid.NewGuid();
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        IScopeContextProvider scopeProvider = CreateScopeProvider(tenant);

        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", scopeProvider, audit.Object, 10, 10);
        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", scopeProvider, audit.Object, 10, 10);
        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", scopeProvider, audit.Object, 10, 10);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", scopeProvider, audit.Object, 10, 10);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.LlmTenantMonthlyDollarBudgetApproaching),
                It.IsAny<CancellationToken>()),
            Times.Once);

        tracker.RecordUsageAndMaybeWarn(tenant, "azure-openai", scopeProvider, audit.Object, 1, 1);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.LlmTenantMonthlyDollarBudgetApproaching),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void EnsureWithinBudgetBeforeCall_skips_for_simulator_provider()
    {
        LlmMonthlyTenantDollarBudgetOptions opts = new()
        {
            Enabled = true,
            IncludedUsdPerUtcMonth = 1m,
            HardCutoffUsdPerUtcMonth = 1m
        };

        Mock<IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);
        Mock<ILlmCostEstimator> cost = new();
        cost.Setup(e => e.EstimateUsd(It.IsAny<int>(), It.IsAny<int>())).Returns(100m);

        LlmMonthlyTenantDollarBudgetTracker tracker = new(monitor.Object, cost.Object);
        Guid tenant = Guid.NewGuid();

        tracker.EnsureWithinBudgetBeforeCall(tenant, "simulator");
    }

    private static IScopeContextProvider CreateScopeProvider(Guid tenantId)
    {
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        return scope.Object;
    }
}
