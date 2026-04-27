using System.Text.Json;

using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Audit;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class BaselineMutationAuditServiceArchitectureDurableEchoTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task RunCreated_emits_Run_Created_with_scope_and_payload()
    {
        Guid runGuid = Guid.NewGuid();
        string runId = runGuid.ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        await sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunCreated,
            "actor-1",
            runId,
            "RequestId=req-42; Environment=dev; SystemName=AlphaSys",
            CancellationToken.None);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.Created
                    && e.RunId == runGuid
                    && e.TenantId == TestScope.TenantId
                    && e.WorkspaceId == TestScope.WorkspaceId
                    && e.ProjectId == TestScope.ProjectId
                    && JsonHasString(e.DataJson, "requestId", "req-42")
                    && JsonHasString(e.DataJson, "systemName", "AlphaSys")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunExecuteSucceeded_emits_Run_ExecuteSucceeded_with_resultCount()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        await sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded,
            "actor-ex",
            runId,
            "ResultCount=3",
            CancellationToken.None);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.ExecuteSucceeded
                    && JsonHasInt(e.DataJson, "resultCount", 3)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunCompleted_without_commitPath_uses_legacy_three_field_DataJson()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        await sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunCompleted,
            "actor-c",
            runId,
            "ManifestVersion=v9; SystemName=CoSys; WarningCount=2",
            CancellationToken.None);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.CommitCompleted
                    && JsonHasString(e.DataJson, "runId", runId)
                    && JsonHasString(e.DataJson, "manifestVersion", "v9")
                    && JsonHasString(e.DataJson, "systemName", "CoSys")
                    && !JsonHasProperty(e.DataJson, "warningCount")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunCompleted_with_commitPath_includes_authority_fields_in_DataJson()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        await sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunCompleted,
            "actor-a",
            runId,
            "ManifestVersion=vA; SystemName=AuthSys; WarningCount=1; CommitPath=authority",
            CancellationToken.None);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.CommitCompleted
                    && JsonHasString(e.DataJson, "commitPath", "authority")
                    && JsonHasInt(e.DataJson, "warningCount", 1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunFailed_emits_Run_Failed()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        await sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunFailed,
            "actor-f",
            runId,
            "Coordination failed: boom",
            CancellationToken.None);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.Failed
                    && JsonHasString(e.DataJson, "reason", "Coordination failed: boom")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordAsync_when_LogAsync_throws_completes_without_propagating()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);
        Mock<IAuditService> auditService = new();
        auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit down"));

        BaselineMutationAuditService sut = new(
            NullLogger<BaselineMutationAuditService>.Instance,
            auditService.Object,
            scopeProvider.Object);

        Func<Task> act = () => sut.RecordAsync(
            AuditEventTypes.Baseline.Architecture.RunStarted,
            "actor-s",
            runId,
            null,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static bool JsonHasString(string? dataJson, string property, string expected)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            return false;

        using JsonDocument doc = JsonDocument.Parse(dataJson);

        return doc.RootElement.TryGetProperty(property, out JsonElement p)
               && p.ValueKind == JsonValueKind.String
               && string.Equals(p.GetString(), expected, StringComparison.Ordinal);
    }

    private static bool JsonHasInt(string? dataJson, string property, int expected)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            return false;

        using JsonDocument doc = JsonDocument.Parse(dataJson);

        return doc.RootElement.TryGetProperty(property, out JsonElement p)
               && p.ValueKind == JsonValueKind.Number
               && p.TryGetInt32(out int n)
               && n == expected;
    }

    private static bool JsonHasProperty(string? dataJson, string property)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            return false;

        using JsonDocument doc = JsonDocument.Parse(dataJson);

        return doc.RootElement.TryGetProperty(property, out _);
    }
}
