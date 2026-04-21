using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GoldenManifests;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.GoldenManifests;

/// <summary>
/// Branch matrix for <see cref="GoldenManifestPhase1RelationalRead.HydrateAsync"/> JSON sections and relational
/// decision / provenance slices (targets remaining conditional paths alongside existing direct SQL suites).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestPhase1RelationalReadBranchMatrixDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static GoldenManifest EmptyTemplate(
        Guid manifestId,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        DateTime createdUtc) =>
        new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            FindingsSnapshotId = findingsId,
            DecisionTraceId = traceId,
            CreatedUtc = createdUtc,
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task HydrateAsync_branch_matrix_json_and_relational_slices(int branch)
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedFullChainAsync(
            connection,
            TenantId,
            WorkspaceId,
            ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            "proj-gm-branch-mx-" + branch,
            CancellationToken.None);

        GoldenManifest template = EmptyTemplate(manifestId, runId, contextId, graphId, findingsId, traceId, createdUtc);

        string metadataJson = JsonEntitySerializer.Serialize(template.Metadata);
        string requirementsJson = JsonEntitySerializer.Serialize(template.Requirements);
        string topologyJson = JsonEntitySerializer.Serialize(template.Topology);
        string securityJson = JsonEntitySerializer.Serialize(template.Security);
        string complianceJson = JsonEntitySerializer.Serialize(template.Compliance);
        string costJson = JsonEntitySerializer.Serialize(template.Cost);
        string constraintsJson = JsonEntitySerializer.Serialize(template.Constraints);
        string unresolvedJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues);
        string assumptionsJson = JsonEntitySerializer.Serialize(new List<string>());
        string warningsJson = JsonEntitySerializer.Serialize(new List<string>());
        string provenanceJson = JsonEntitySerializer.Serialize(new ManifestProvenance());
        string decisionsJson = JsonEntitySerializer.Serialize(new List<ResolvedArchitectureDecision>());

        switch (branch)
        {
            case 0:
                metadataJson = """{"name":"branch-0"}""";
                break;
            case 1:
                requirementsJson = JsonEntitySerializer.Serialize(new RequirementsCoverageSection());
                break;
            case 2:
                topologyJson = JsonEntitySerializer.Serialize(new TopologySection());
                break;
            case 3:
                securityJson = JsonEntitySerializer.Serialize(new SecuritySection());
                break;
            case 4:
                complianceJson = JsonEntitySerializer.Serialize(new ComplianceSection());
                break;
            case 5:
                costJson = JsonEntitySerializer.Serialize(new CostSection());
                break;
            case 6:
                constraintsJson = JsonEntitySerializer.Serialize(new ConstraintSection());
                break;
            case 7:
                unresolvedJson = JsonEntitySerializer.Serialize(new UnresolvedIssuesSection());
                break;
            case 8:
                assumptionsJson = """["a1","a2"]""";
                break;
            case 9:
                warningsJson = """["w1"]""";
                break;
            case 10:
                provenanceJson = """{"sourceFindingIds":["f1"],"sourceGraphNodeIds":[],"appliedRuleIds":[]}""";
                break;
            case 11:
                decisionsJson =
                    """[{"decisionId":"d-json","category":"c","title":"t","selectedOption":"o","rationale":"r","supportingFindingIds":[],"relatedNodeIds":[]}]""";
                break;
        }

        if (branch is 12 or 13)
        {
            decisionsJson = "[]";
        }

        const string insertManifest = """
            INSERT INTO dbo.GoldenManifests
            (
                TenantId, WorkspaceId, ProjectId,
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            )
            VALUES
            (
                @TenantId, @WorkspaceId, @ProjectId,
                @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @ComplianceJson, @CostJson,
                @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson,
                @WarningsJson, @ProvenanceJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertManifest,
                new
                {
                    TenantId,
                    WorkspaceId,
                    ProjectId,
                    ManifestId = manifestId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    FindingsSnapshotId = findingsId,
                    DecisionTraceId = traceId,
                    CreatedUtc = createdUtc,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "1",
                    RuleSetHash = "rh",
                    MetadataJson = metadataJson,
                    RequirementsJson = requirementsJson,
                    TopologyJson = topologyJson,
                    SecurityJson = securityJson,
                    ComplianceJson = complianceJson,
                    CostJson = costJson,
                    ConstraintsJson = constraintsJson,
                    UnresolvedIssuesJson = unresolvedJson,
                    DecisionsJson = decisionsJson,
                    AssumptionsJson = assumptionsJson,
                    WarningsJson = warningsJson,
                    ProvenanceJson = provenanceJson,
                },
                cancellationToken: CancellationToken.None));

        if (branch == 12)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.GoldenManifestDecisions
                    (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                    VALUES (@M, 0, N'd1', N'cat', N'title', N'opt', N'rat', NULL);
                    INSERT INTO dbo.GoldenManifestDecisionEvidenceLinks (ManifestId, DecisionId, SortOrder, FindingId)
                    VALUES (@M, N'd1', 0, N'fid');
                    """,
                    new
                    {
                        M = manifestId
                    },
                    cancellationToken: CancellationToken.None));
        }

        if (branch == 13)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.GoldenManifestDecisions
                    (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                    VALUES (@M, 0, N'd2', N'cat', N'title', N'opt', N'rat', NULL);
                    INSERT INTO dbo.GoldenManifestDecisionNodeLinks (ManifestId, DecisionId, SortOrder, NodeId)
                    VALUES (@M, N'd2', 0, N'nid');
                    """,
                    new
                    {
                        M = manifestId
                    },
                    cancellationToken: CancellationToken.None));
        }

        if (branch == 14)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.GoldenManifestProvenanceAppliedRules (ManifestId, SortOrder, RuleId)
                    VALUES (@M, 0, N'rule-a');
                    """,
                    new
                    {
                        M = manifestId
                    },
                    cancellationToken: CancellationToken.None));
        }

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(
                """
                SELECT
                    TenantId, WorkspaceId, ProjectId,
                    ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                    CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                    MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                    ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                    WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId;
                """,
                new
                {
                    ManifestId = manifestId
                },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        GoldenManifest hydrated = await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        switch (branch)
        {
            case 0:
                hydrated.Metadata.Name.Should().Be("branch-0");
                return;
            case 1:
                hydrated.Requirements.Should().NotBeNull();
                return;
            case 2:
                hydrated.Topology.Should().NotBeNull();
                return;
            case 3:
                hydrated.Security.Should().NotBeNull();
                return;
            case 4:
                hydrated.Compliance.Should().NotBeNull();
                return;
            case 5:
                hydrated.Cost.Should().NotBeNull();
                return;
            case 6:
                hydrated.Constraints.Should().NotBeNull();
                return;
            case 7:
                hydrated.UnresolvedIssues.Should().NotBeNull();
                return;
            case 8:
                hydrated.Assumptions.Should().Equal("a1", "a2");
                return;
            case 9:
                hydrated.Warnings.Should().Equal("w1");
                return;
            case 10:
                hydrated.Provenance.SourceFindingIds.Should().Equal("f1");
                return;
            case 11:
                hydrated.Decisions.Should().ContainSingle(d => d.DecisionId == "d-json");
                return;
            case 12:
                {
                    ResolvedArchitectureDecision d = hydrated.Decisions.Should().ContainSingle(x => x.DecisionId == "d1").Subject;
                    d.SupportingFindingIds.Should().Equal("fid");
                    d.RelatedNodeIds.Should().BeEmpty();
                    return;
                }

            case 13:
                {
                    ResolvedArchitectureDecision d = hydrated.Decisions.Should().ContainSingle(x => x.DecisionId == "d2").Subject;
                    d.RelatedNodeIds.Should().Equal("nid");
                    d.SupportingFindingIds.Should().BeEmpty();
                    return;
                }
            case 14:
                hydrated.Provenance.AppliedRuleIds.Should().Equal("rule-a");
                return;
        }
    }
}
