using ArchLucid.Application.ExecDigest;
using ArchLucid.Application.Governance;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.ExecDigest;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExecDigestComposerTests
{
    [Fact]
    public async Task ComposeAsync_includes_compliance_table_when_service_returns_points()
    {
        Mock<IComplianceDriftTrendService> compliance = new();
        compliance
            .Setup(
                s => s.GetTrendAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ComplianceDriftTrendPoint
                {
                    BucketUtc = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    ChangeCount = 2,
                    ChangesByType = new Dictionary<string, int>(StringComparer.Ordinal) { ["PackUpdated"] = 2 },
                },
            ]);

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(
                s => s.ListRunsByProjectAsync(
                    It.IsAny<ScopeContext>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IRunDetailQueryService> runDetails = new();
        Mock<IPilotRunDeltaComputer> deltas = new();

        ExecDigestComposer composer = new(
            compliance.Object,
            authority.Object,
            runDetails.Object,
            deltas.Object,
            NullLogger<ExecDigestComposer>.Instance);

        Guid tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        DateTime start = new(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = new(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ProjectId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
        };

        ExecDigestComposition result = await composer.ComposeAsync(tenantId, start, end, scope, "https://app.example", CancellationToken.None);

        result.ComplianceDriftMarkdown.Should().NotBeNull();
        result.ComplianceDriftMarkdown!.Should().Contain("| Day (UTC) |");
        result.DashboardUrl.Should().StartWith("https://app.example");
    }
}
