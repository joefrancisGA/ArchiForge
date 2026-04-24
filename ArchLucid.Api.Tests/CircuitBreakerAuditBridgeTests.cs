using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Resilience;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>Unit tests for <see cref="CircuitBreakerAuditBridge" /> (async audit scheduling).</summary>
public sealed class CircuitBreakerAuditBridgeTests
{
    [Fact]
    public async Task CreateCallback_StateTransition_EmitsAuditEvent()
    {
        TaskCompletionSource<AuditEvent> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => tcs.TrySetResult(e))
            .Returns(Task.CompletedTask);

        await using ServiceProvider root = BuildRootProvider(audit.Object);
        IServiceScopeFactory scopeFactory = root.GetRequiredService<IServiceScopeFactory>();

        Mock<IScopeContextProvider> scopeProvider = new();
        Guid tenantId = Guid.NewGuid();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        Mock<ILogger<CircuitBreakerAuditBridge>> logger = new();
        Mock<IAuditRetryQueue> auditRetry = new();
        auditRetry.Setup(q => q.TryEnqueue(It.IsAny<AuditEvent>())).Returns(true);

        CircuitBreakerAuditBridge sut = new(scopeFactory, scopeProvider.Object, auditRetry.Object, logger.Object);

        Action<CircuitBreakerAuditEntry> cb = sut.CreateCallback();
        DateTimeOffset occurred = DateTimeOffset.UtcNow;
        cb.Invoke(new CircuitBreakerAuditEntry("gate-x", "StateTransition", "Closed", "Open", null, occurred));

        AuditEvent captured = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        captured.EventType.Should().Be(AuditEventTypes.CircuitBreakerStateTransition);
        captured.ActorUserId.Should().Be("system");
        captured.ActorUserName.Should().Be("CircuitBreakerGate");
        captured.TenantId.Should().Be(tenantId);

        using JsonDocument doc = JsonDocument.Parse(captured.DataJson);
        doc.RootElement.GetProperty("gate").GetString().Should().Be("gate-x");
        doc.RootElement.GetProperty("fromState").GetString().Should().Be("Closed");
        doc.RootElement.GetProperty("toState").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task CreateCallback_Rejection_SetsCorrectEventType()
    {
        TaskCompletionSource<AuditEvent> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => tcs.TrySetResult(e))
            .Returns(Task.CompletedTask);

        await using ServiceProvider root = BuildRootProvider(audit.Object);
        IServiceScopeFactory scopeFactory = root.GetRequiredService<IServiceScopeFactory>();

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        CircuitBreakerAuditBridge sut = new(
            scopeFactory,
            scopeProvider.Object,
            Mock.Of<IAuditRetryQueue>(),
            Mock.Of<ILogger<CircuitBreakerAuditBridge>>());

        Action<CircuitBreakerAuditEntry> cb = sut.CreateCallback();
        cb.Invoke(new CircuitBreakerAuditEntry("g", "Rejection", "Open", "Open", null, DateTimeOffset.UtcNow));

        AuditEvent captured = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        captured.EventType.Should().Be(AuditEventTypes.CircuitBreakerRejection);
    }

    [Fact]
    public async Task CreateCallback_AuditFailure_DoesNotThrow_and_logs_warning()
    {
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk full"));

        await using ServiceProvider root = BuildRootProvider(audit.Object);
        IServiceScopeFactory scopeFactory = root.GetRequiredService<IServiceScopeFactory>();

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        Mock<ILogger<CircuitBreakerAuditBridge>> logger = new();
        // Moq returns default(bool)=false for IsEnabled unless configured; the bridge guards LogWarning with IsEnabled.
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        Mock<IAuditRetryQueue> auditRetry = new();
        auditRetry.Setup(q => q.TryEnqueue(It.IsAny<AuditEvent>())).Returns(false);

        CircuitBreakerAuditBridge sut = new(scopeFactory, scopeProvider.Object, auditRetry.Object, logger.Object);

        Action<CircuitBreakerAuditEntry> cb = sut.CreateCallback();
        Action act = () => cb.Invoke(
            new CircuitBreakerAuditEntry("g", "StateTransition", "A", "B", null, DateTimeOffset.UtcNow));

        act.Should().NotThrow();
        await Task.Delay(500);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static ServiceProvider BuildRootProvider(IAuditService auditService)
    {
        ServiceCollection services = [];
        services.AddSingleton<IAuditService>(auditService);

        return services.BuildServiceProvider();
    }
}
