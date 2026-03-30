using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Backfill;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// 53R-5 SQL integration tests for <see cref="SqlCutoverReadinessService"/>:
/// verify that the readiness report correctly distinguishes between headers
/// that have relational children and those still relying on JSON fallback.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class CutoverReadinessSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task AssessAsync_MixedState_ReportsCorrectCounts()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        // Seed one JSON-only ContextSnapshot (no relational children)
        await SeedJsonOnlyContextSnapshotAsync(factory);

        // Seed one fully-relational ContextSnapshot (via repository Save)
        await SeedRelationalContextSnapshotAsync(factory);

        SqlCutoverReadinessService service = new(factory, NullLogger<SqlCutoverReadinessService>.Instance);
        CutoverReadinessReport report = await service.AssessAsync(CancellationToken.None);

        report.Should().NotBeNull();
        report.Slices.Should().NotBeEmpty();

        CutoverSliceReadiness? canonicalSlice = report.Slices
            .FirstOrDefault(s => s.SliceName == "ContextSnapshot.CanonicalObjects");

        canonicalSlice.Should().NotBeNull();
        canonicalSlice.TotalHeaderRows.Should().BeGreaterOrEqualTo(2);
        canonicalSlice.HeadersWithRelationalRows.Should().BeGreaterOrEqualTo(1);

        // At least one header is JSON-only (the one we seeded without relational children)
        // The relational one (via Save) should be counted as having children.
        // Other tests in the suite may have left rows, so we can't assert exact counts,
        // but we can assert the report structure is valid.
        canonicalSlice.HeadersMissingRelationalRows.Should().BeGreaterOrEqualTo(1);
    }

    [SkippableFact]
    public async Task AssessAsync_AllSlicesPresent()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        SqlCutoverReadinessService service = new(factory, NullLogger<SqlCutoverReadinessService>.Instance);
        CutoverReadinessReport report = await service.AssessAsync(CancellationToken.None);

        List<string> sliceNames = report.Slices.Select(s => s.SliceName).ToList();

        sliceNames.Should().Contain("ContextSnapshot.CanonicalObjects");
        sliceNames.Should().Contain("ContextSnapshot.Warnings");
        sliceNames.Should().Contain("ContextSnapshot.Errors");
        sliceNames.Should().Contain("ContextSnapshot.SourceHashes");
        sliceNames.Should().Contain("GraphSnapshot.Nodes");
        sliceNames.Should().Contain("GraphSnapshot.Edges");
        sliceNames.Should().Contain("GraphSnapshot.Warnings");
        sliceNames.Should().Contain("GraphSnapshot.EdgeProperties");
        sliceNames.Should().Contain("FindingsSnapshot.Findings");
        sliceNames.Should().Contain("GoldenManifest.Assumptions");
        sliceNames.Should().Contain("GoldenManifest.Warnings");
        sliceNames.Should().Contain("GoldenManifest.Decisions");
        sliceNames.Should().Contain("GoldenManifest.Provenance");
        sliceNames.Should().Contain("ArtifactBundle.Artifacts");

        sliceNames.Should().HaveCount(14);
    }

    [SkippableFact]
    public async Task AssessAsync_ReportAggregates_AreConsistent()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        SqlCutoverReadinessService service = new(factory, NullLogger<SqlCutoverReadinessService>.Instance);
        CutoverReadinessReport report = await service.AssessAsync(CancellationToken.None);

        foreach (CutoverSliceReadiness slice in report.Slices)
        {
            slice.HeadersWithRelationalRows.Should().BeLessThanOrEqualTo(slice.TotalHeaderRows);
            slice.HeadersMissingRelationalRows.Should().BeGreaterOrEqualTo(0);
            (slice.HeadersWithRelationalRows + slice.HeadersMissingRelationalRows).Should().Be(slice.TotalHeaderRows);

            if (slice.TotalHeaderRows == 0)
                slice.IsReady.Should().BeTrue($"slice {slice.SliceName} with 0 headers should be ready");
        }

        int notReadyCount = report.Slices.Count(s => !s.IsReady);
        report.SlicesNotReady.Should().HaveCount(notReadyCount);
    }

    // ── Seed helpers ───────────────────────────────────────────────

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ScopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static async Task SeedJsonOnlyContextSnapshotAsync(SqlConnectionFactory factory)
    {
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid snapshotId = Guid.NewGuid();

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
            VALUES (@RunId, 'proj-readiness-json', @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
            """,
            new
            {
                RunId = runId,
                CreatedUtc = DateTime.UtcNow,
                TenantId,
                WorkspaceId,
                ScopeProjectId,
            },
            cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.ContextSnapshots
            (SnapshotId, RunId, ProjectId, CreatedUtc, CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson)
            VALUES (@SnapshotId, @RunId, 'proj-readiness-json', @CreatedUtc, @CanonicalObjectsJson, NULL, @WarningsJson, @ErrorsJson, @SourceHashesJson);
            """,
            new
            {
                SnapshotId = snapshotId,
                RunId = runId,
                CreatedUtc = DateTime.UtcNow,
                CanonicalObjectsJson = JsonEntitySerializer.Serialize(new List<CanonicalObject>
                {
                    new()
                    {
                        ObjectId = "readiness-obj",
                        ObjectType = "T",
                        Name = "N",
                        SourceType = "S",
                        SourceId = "sid",
                        Properties = [],
                    },
                }),
                WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
                ErrorsJson = JsonEntitySerializer.Serialize(new List<string>()),
                SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>()),
            },
            cancellationToken: CancellationToken.None));
    }

    private static async Task SeedRelationalContextSnapshotAsync(SqlConnectionFactory factory)
    {
        SqlContextSnapshotRepository repository = new(factory);

        ContextSnapshot snapshot = new()
        {
            SnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ProjectId = "proj-readiness-rel",
            CreatedUtc = DateTime.UtcNow,
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectId = "rel-obj",
                    ObjectType = "Service",
                    Name = "Svc",
                    SourceType = "R",
                    SourceId = "s",
                    Properties = [],
                },
            ],
            Warnings = ["w"],
            Errors = [],
            SourceHashes = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);
    }
}
