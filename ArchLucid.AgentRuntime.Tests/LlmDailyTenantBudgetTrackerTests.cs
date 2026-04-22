using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

public sealed class LlmDailyTenantBudgetTrackerTests
{
    [Fact]
    public void EnsureWithinBudgetBeforeCall_when_under_limit_does_not_throw()
    {
        LlmDailyTenantBudgetOptions opts = new()
        {
            Enabled = true,
            MaxTotalTokensPerTenantPerUtcDay = 10_000,
            AssumedMaxTotalTokensPerRequest = 512,
        };

        Mock<IOptionsMonitor<LlmDailyTenantBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);

        LlmDailyTenantBudgetTracker tracker = new(monitor.Object);
        Guid tenant = Guid.NewGuid();

        tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");
        tracker.RecordUsageAndMaybeWarn(
            tenant,
            "azure-openai",
            CreateScopeProvider(tenant),
            null,
            100,
            100);
        tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");
    }

    [Fact]
    public void EnsureWithinBudgetBeforeCall_when_would_exceed_throws()
    {
        LlmDailyTenantBudgetOptions opts = new()
        {
            Enabled = true,
            MaxTotalTokensPerTenantPerUtcDay = 500,
            AssumedMaxTotalTokensPerRequest = 256,
        };

        Mock<IOptionsMonitor<LlmDailyTenantBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);

        LlmDailyTenantBudgetTracker tracker = new(monitor.Object);
        Guid tenant = Guid.NewGuid();

        tracker.RecordUsageAndMaybeWarn(
            tenant,
            "azure-openai",
            CreateScopeProvider(tenant),
            null,
            400,
            0);

        Action act = () => tracker.EnsureWithinBudgetBeforeCall(tenant, "azure-openai");

        act.Should().Throw<LlmTokenQuotaExceededException>();
    }

    [Fact]
    public void RecordUsageAndMaybeWarn_warns_at_most_once_per_utc_day_when_threshold_crossed()
    {
        LlmDailyTenantBudgetOptions opts = new()
        {
            Enabled = true,
            MaxTotalTokensPerTenantPerUtcDay = 1000,
            WarnFraction = 0.8m,
        };

        Mock<IOptionsMonitor<LlmDailyTenantBudgetOptions>> monitor = new();
        monitor.Setup(m => m.CurrentValue).Returns(opts);

        LlmDailyTenantBudgetTracker tracker = new(monitor.Object);
        Guid tenant = Guid.NewGuid();
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        tracker.RecordUsageAndMaybeWarn(
            tenant,
            "azure-openai",
            CreateScopeProvider(tenant),
            audit.Object,
            700,
            0);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        tracker.RecordUsageAndMaybeWarn(
            tenant,
            "azure-openai",
            CreateScopeProvider(tenant),
            audit.Object,
            150,
            0);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.LlmTenantDailyBudgetApproaching),
                It.IsAny<CancellationToken>()),
            Times.Once);

        tracker.RecordUsageAndMaybeWarn(
            tenant,
            "azure-openai",
            CreateScopeProvider(tenant),
            audit.Object,
            10,
            0);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.LlmTenantDailyBudgetApproaching),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IScopeContextProvider CreateScopeProvider(Guid tenantId)
    {
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = tenantId,
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });

        return scope.Object;
    }
}
