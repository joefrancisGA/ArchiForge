using System.Diagnostics;
using System.Security.Claims;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Covers <see cref="AuditService.LogAsync"/> correlation enrichment (activity tag, HTTP trace id, explicit override).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AuditServiceCorrelationEnrichmentTests
{
    private static readonly ActivitySource TestSource = new("ArchLucid.Tests.AuditServiceCorrelation");

    static AuditServiceCorrelationEnrichmentTests()
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == TestSource.Name,
            Sample = (ref _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);
    }

    [Fact]
    public async Task LogAsync_SetsCorrelationId_FromActivityTag_WhenAvailable()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IHttpContextAccessor> httpAccessor = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "u1"),
                new Claim(ClaimTypes.Name, "User One"),
            ]));

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new() { EventType = "Test" };

        using (Activity? activity = TestSource.StartActivity("op"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "test-corr-123");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("test-corr-123");
    }

    [Fact]
    public async Task LogAsync_SetsCorrelationId_FromHttpTraceIdentifier_WhenNoActivity()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IHttpContextAccessor> httpAccessor = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "http-trace-456",
        };

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "u2"),
                new Claim(ClaimTypes.Name, "User Two"),
            ]));

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new() { EventType = "Test" };

        await sut.LogAsync(auditEvent, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("http-trace-456");
    }

    [Fact]
    public async Task LogAsync_PreservesExplicitCorrelationId_WhenAlreadySet()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IHttpContextAccessor> httpAccessor = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "u3"),
                new Claim(ClaimTypes.Name, "User Three"),
            ]));

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new() { EventType = "Test", CorrelationId = "explicit-789" };

        using (Activity? activity = TestSource.StartActivity("op"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "would-win-if-empty");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("explicit-789");
    }

    [Fact]
    public async Task LogAsync_CircuitBreakerPath_SetsCorrelationId_FromActivity()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IHttpContextAccessor> httpAccessor = new();
        Mock<IScopeContextProvider> scopeProvider = new();

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = AuditEventTypes.CircuitBreakerStateTransition,
            ActorUserId = "system",
            ActorUserName = "CircuitBreakerGate",
        };

        using (Activity? activity = TestSource.StartActivity("breaker"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "breaker-corr-99");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("breaker-corr-99");
    }
}
