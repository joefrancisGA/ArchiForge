using System.IO.Compression;

using ArchLucid.Application.Traceability;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Traceability;

[Trait("Suite", "Core")]
public sealed class TraceabilityBundleBuilderTests
{
    [Fact]
    public async Task BuildAsync_WhenRunMissing_ReturnsNullAndDoesNotQueryAudit()
    {
        Mock<IRunDetailQueryService> runDetail = new();
        runDetail.Setup(s => s.GetRunDetailAsync("missing-run", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Mock<IAuditRepository> audit = new();
        TraceabilityBundleBuilder sut = new(runDetail.Object, audit.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        byte[]? zip = await sut.BuildAsync("missing-run", scope, 10_000_000, CancellationToken.None);

        zip.Should().BeNull();
        audit.Verify(
            a => a.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BuildAsync_WhenRunPresent_ReturnsZipWithExpectedEntries()
    {
        Guid runGuid = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        ArchitectureRunDetail detail = new()
        {
            Run = new ArchitectureRun
            {
                RunId = runGuid.ToString("N"),
                RequestId = "req-1",
                Status = ArchitectureRunStatus.Committed,
            },
        };

        Mock<IRunDetailQueryService> runDetail = new();
        runDetail.Setup(s => s.GetRunDetailAsync(runGuid.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        Mock<IAuditRepository> audit = new();
        audit.Setup(a => a.GetFilteredAsync(
                tenantId,
                workspaceId,
                projectId,
                It.Is<AuditEventFilter>(f => f.RunId == runGuid && f.Take == 1000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        TraceabilityBundleBuilder sut = new(runDetail.Object, audit.Object);
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
        };

        byte[]? zipBytes = await sut.BuildAsync(runGuid.ToString("N"), scope, 10_000_000, CancellationToken.None);

        zipBytes.Should().NotBeNull();
        ReadOnlySpan<byte> head = zipBytes.AsSpan(0, Math.Min(4, zipBytes.Length));
        head[0].Should().Be(0x50);
        head[1].Should().Be(0x4B);

        using MemoryStream ms = new(zipBytes);
        await using ZipArchive zip = new(ms, ZipArchiveMode.Read, leaveOpen: false);
        zip.GetEntry("run-summary.json").Should().NotBeNull();
        zip.GetEntry("audit-events.json").Should().NotBeNull();
        zip.GetEntry("decision-traces.json").Should().NotBeNull();
        zip.GetEntry("README.txt").Should().NotBeNull();
    }

    [Fact]
    public async Task BuildAsync_WhenZipExceedsMax_ThrowsTraceabilityBundleTooLargeException()
    {
        Guid runGuid = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        ArchitectureRunDetail detail = new()
        {
            Run = new ArchitectureRun
            {
                RunId = runGuid.ToString("N"),
                RequestId = "req-2",
                Status = ArchitectureRunStatus.Committed,
            },
        };

        Mock<IRunDetailQueryService> runDetail = new();
        runDetail.Setup(s => s.GetRunDetailAsync(runGuid.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        Mock<IAuditRepository> audit = new();
        audit.Setup(a => a.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AuditEvent>());

        TraceabilityBundleBuilder sut = new(runDetail.Object, audit.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Func<Task> act = async () =>
            _ = await sut.BuildAsync(runGuid.ToString("N"), scope, maxZipBytes: 1, CancellationToken.None);

        await act.Should().ThrowAsync<TraceabilityBundleTooLargeException>();
    }
}
