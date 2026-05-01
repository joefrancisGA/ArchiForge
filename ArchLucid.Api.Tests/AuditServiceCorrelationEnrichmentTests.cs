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
///     Covers <see cref="AuditService.LogAsync" /> correlation enrichment (activity tag, HTTP trace id, explicit
///     override).
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
            Sample = (ref _) => ActivitySamplingResult.AllData
        };

        ActivitySource.AddActivityListener(listener);
    }

    [SkippableFact]
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
            .Returns(new ScopeContext
            {
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "u1"),
                    new Claim(ClaimTypes.Name, "User One")
                ]))
        };

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = "Test"
        };

        using (Activity? activity = TestSource.StartActivity("op"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "test-corr-123");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("test-corr-123");
    }

    [SkippableFact]
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
            .Returns(new ScopeContext
            {
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });

        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "http-trace-456",
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "u2"),
                    new Claim(ClaimTypes.Name, "User Two")
                ]))
        };

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = "Test"
        };

        await sut.LogAsync(auditEvent, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("http-trace-456");
    }

    [SkippableFact]
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
            .Returns(new ScopeContext
            {
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "u3"),
                    new Claim(ClaimTypes.Name, "User Three")
                ]))
        };

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = "Test",
            CorrelationId = "explicit-789"
        };

        using (Activity? activity = TestSource.StartActivity("op"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "would-win-if-empty");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("explicit-789");
    }

    [SkippableFact]
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
            ActorUserName = "CircuitBreakerGate"
        };

        using (Activity? activity = TestSource.StartActivity("breaker"))
        {
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "breaker-corr-99");

            await sut.LogAsync(auditEvent, CancellationToken.None);
        }

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("breaker-corr-99");
    }

    [SkippableFact]
    public async Task LogAsync_fills_Tenant_scope_from_ambient_scope_when_ids_are_empty()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        Mock<IHttpContextAccessor> httpAccessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "u-scope"),
                    new Claim(ClaimTypes.Name, "Scope User")
                ]))
        };

        httpAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = "Test"
        };

        await sut.LogAsync(auditEvent, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(scope.TenantId);
        captured.WorkspaceId.Should().Be(scope.WorkspaceId);
        captured.ProjectId.Should().Be(scope.ProjectId);
    }

    [SkippableFact]
    public async Task LogAsync_preserves_explicit_tenant_scope_when_already_non_empty()
    {
        Mock<IAuditRepository> repo = new();
        AuditEvent? captured = null;
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Guid explicitTenant = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Guid explicitWs = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        Guid explicitProj = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(new ScopeContext
            {
                TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
            });

        Mock<IHttpContextAccessor> httpAccessor = new();
        httpAccessor.Setup(a => a.HttpContext).Returns(
            new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "u-keep"),
                        new Claim(ClaimTypes.Name, "Keep User")
                    ]))
            });

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);
        AuditEvent auditEvent = new()
        {
            EventType = "Test",
            TenantId = explicitTenant,
            WorkspaceId = explicitWs,
            ProjectId = explicitProj
        };

        await sut.LogAsync(auditEvent, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(explicitTenant);
        captured.WorkspaceId.Should().Be(explicitWs);
        captured.ProjectId.Should().Be(explicitProj);
    }
}
