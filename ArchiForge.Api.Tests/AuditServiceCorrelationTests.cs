using System.Diagnostics;

using ArchiForge.Core.Audit;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Audit;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Ensures <see cref="AuditService"/> stamps <see cref="AuditEvent.CorrelationId"/> from the logical <c>correlation.id</c> activity tag (parent chain), not only the innermost span id.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuditServiceCorrelationTests
{
    private static readonly ActivitySource TestSource = new("ArchiForge.Tests.AuditServiceCorrelation");

    static AuditServiceCorrelationTests()
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == TestSource.Name,
            Sample = (ref _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
    }

    [Fact]
    public async Task LogAsync_UsesLogicalCorrelationFromAncestor_WhenCurrentActivityIsChild()
    {
        AuditEvent? captured = null;
        Mock<IAuditRepository> repo = new();
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IHttpContextAccessor> httpAccessor = new();
        httpAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty
                });

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);

        using Activity? parent = TestSource.StartActivity("http-request");
        parent?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, "client-supplied-corr");
        using Activity? child = TestSource.StartActivity("authority-run");

        await sut.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ManifestGenerated,
                ActorUserId = "u1",
                ActorUserName = "tester"
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("client-supplied-corr");
    }

    [Fact]
    public async Task LogAsync_UsesHttpTraceIdentifier_WhenNoActivityTagAndHttpPresent()
    {
        AuditEvent? captured = null;
        Mock<IAuditRepository> repo = new();
        repo
            .Setup(r => r.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-from-kestrel"
        };

        Mock<IHttpContextAccessor> httpAccessor = new();
        httpAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty
                });

        AuditService sut = new(repo.Object, httpAccessor.Object, scopeProvider.Object);

        await sut.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertTriggered,
                ActorUserId = "u1",
                ActorUserName = "tester"
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("trace-from-kestrel");
    }
}
