using ArchLucid.Core.Audit;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Archival;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="DataArchivalHostIteration" /> audit emission on coordinator failure.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DataArchivalHostIterationTests
{
    [SkippableFact]
    public async Task RunOnceAsync_when_disabled_does_not_resolve_coordinator()
    {
        Mock<IServiceScopeFactory> scopeFactory = new();
        DataArchivalOptions options = new() { Enabled = false };
        DataArchivalHostHealthState health = new();

        bool ok = await DataArchivalHostIteration.RunOnceAsync(
            scopeFactory.Object,
            options,
            NullLogger.Instance,
            health,
            CancellationToken.None);

        ok.Should().BeTrue();
        scopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    [SkippableFact]
    public async Task RunOnceAsync_when_coordinator_throws_logs_audit_event()
    {
        Mock<IDataArchivalCoordinator> coordinator = new();
        coordinator
            .Setup(c => c.RunOnceAsync(It.IsAny<DataArchivalOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("archival failed"));

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IServiceProvider> spCoordinator = new();
        spCoordinator.Setup(sp => sp.GetService(typeof(IDataArchivalCoordinator))).Returns(coordinator.Object);

        Mock<IServiceProvider> spAudit = new();
        spAudit.Setup(sp => sp.GetService(typeof(IAuditService))).Returns(audit.Object);

        Mock<IServiceScope> scopeCoordinator = new();
        scopeCoordinator.Setup(s => s.ServiceProvider).Returns(spCoordinator.Object);

        Mock<IServiceScope> scopeAudit = new();
        scopeAudit.Setup(s => s.ServiceProvider).Returns(spAudit.Object);

        Queue<IServiceScope> scopes = new();
        scopes.Enqueue(scopeCoordinator.Object);
        scopes.Enqueue(scopeAudit.Object);

        Mock<IServiceScopeFactory> scopeFactory = new();
        scopeFactory.Setup(f => f.CreateScope()).Returns(() => scopes.Dequeue());

        DataArchivalOptions options = new() { Enabled = true };
        DataArchivalHostHealthState health = new();

        bool ok = await DataArchivalHostIteration.RunOnceAsync(
            scopeFactory.Object,
            options,
            NullLogger.Instance,
            health,
            CancellationToken.None);

        ok.Should().BeFalse();

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.DataArchivalHostLoopFailed),
                It.IsAny<CancellationToken>()),
            Times.Once);

        HealthStatus status = health.Evaluate(true).Status;
        status.Should().Be(HealthStatus.Degraded);
    }

    [SkippableFact]
    public async Task RunOnceAsync_when_coordinator_succeeds_marks_health_healthy()
    {
        Mock<IDataArchivalCoordinator> coordinator = new();
        coordinator
            .Setup(c => c.RunOnceAsync(It.IsAny<DataArchivalOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IServiceProvider> spCoordinator = new();
        spCoordinator.Setup(sp => sp.GetService(typeof(IDataArchivalCoordinator))).Returns(coordinator.Object);

        Mock<IServiceScope> scopeCoordinator = new();
        scopeCoordinator.Setup(s => s.ServiceProvider).Returns(spCoordinator.Object);

        Mock<IServiceScopeFactory> scopeFactory = new();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeCoordinator.Object);

        DataArchivalOptions options = new() { Enabled = true };
        DataArchivalHostHealthState health = new();

        bool ok = await DataArchivalHostIteration.RunOnceAsync(
            scopeFactory.Object,
            options,
            NullLogger.Instance,
            health,
            CancellationToken.None);

        ok.Should().BeTrue();
        HealthStatus status = health.Evaluate(true).Status;
        status.Should().Be(HealthStatus.Healthy);
    }
}
