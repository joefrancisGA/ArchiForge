using ArchLucid.ContextIngestion.Models;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

using static ArchLucid.Persistence.Tests.Support.PersistenceIntegrationTestScope;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="SqlFindingsSnapshotRepository" /> against SQL Server + DbUp (relational findings + FindingsJson
///     dual-write).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlFindingsSnapshotRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task Save_then_GetById_round_trips_relational_rows_and_payload_sidecar()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        SqlFindingsSnapshotRepository repository = new(factory, Empty);

        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            Findings =
            [
                new Finding
                {
                    FindingId = "f1",
                    FindingType = "RequirementFinding",
                    Category = "Requirement",
                    EngineType = "TestEngine",
                    Severity = FindingSeverity.Warning,
                    Title = "T1",
                    Rationale = "Because",
                    RelatedNodeIds = ["n1", "n2"],
                    RecommendedActions = ["fix it"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "R1", RequirementText = "text", IsMandatory = false
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["g1"],
                        RulesApplied = ["rule-a"],
                        DecisionsTaken = ["decided"],
                        AlternativePathsConsidered = ["alt"],
                        Notes = ["note1"]
                    }
                }
            ]
        };

        FindingsSnapshotMigrator.Apply(snapshot);
        await repository.SaveAsync(snapshot, CancellationToken.None);

        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.SchemaVersion.Should().Be(FindingsSchema.CurrentSnapshotVersion);
        loaded.Findings.Should().ContainSingle();
        Finding f = loaded.Findings[0];
        f.FindingId.Should().Be("f1");
        f.RelatedNodeIds.Should().Equal("n1", "n2");
        f.RecommendedActions.Should().Equal("fix it");
        f.Properties["k"].Should().Be("v");
        f.Payload.Should().BeOfType<RequirementFindingPayload>();
        ((RequirementFindingPayload)f.Payload!).RequirementName.Should().Be("R1");
        f.Trace.GraphNodeIdsExamined.Should().Equal("g1");
        f.Trace.RulesApplied.Should().Equal("rule-a");
        f.Trace.DecisionsTaken.Should().Equal("decided");
        f.Trace.AlternativePathsConsidered.Should().Equal("alt");
        f.Trace.Notes.Should().Equal("note1");
    }

    [Fact]
    public async Task GetById_when_no_FindingRecords_falls_back_to_FindingsJson()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot original = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow,
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "legacy",
                    FindingType = "RequirementFinding",
                    Category = "Requirement",
                    EngineType = "JsonFallbackEngine",
                    Severity = FindingSeverity.Warning,
                    Title = "Legacy title",
                    Rationale = "Legacy rationale",
                    RelatedNodeIds = ["rn1", "rn2"],
                    RecommendedActions = ["act-a", "act-b"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["propKey"] = "propVal" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "ReqN", RequirementText = "Req body", IsMandatory = true
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["gx1"],
                        RulesApplied = ["rule-json"],
                        DecisionsTaken = ["dec-json"],
                        AlternativePathsConsidered = ["alt-json"],
                        Notes = ["trace-note"]
                    }
                }
            ]
        };

        FindingsSnapshotMigrator.Apply(original);
        string findingsJson = JsonEntitySerializer.Serialize(original);

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    original.CreatedUtc,
                    SchemaVersion = 1,
                    FindingsJson = findingsJson
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().ContainSingle(f => f.FindingId == "legacy");
        Finding lf = loaded.Findings[0];
        lf.Title.Should().Be("Legacy title");
        lf.Rationale.Should().Be("Legacy rationale");
        lf.RelatedNodeIds.Should().Equal("rn1", "rn2");
        lf.RecommendedActions.Should().Equal("act-a", "act-b");
        lf.Properties["propKey"].Should().Be("propVal");
        lf.Payload.Should().BeOfType<RequirementFindingPayload>();
        RequirementFindingPayload reqPayload = (RequirementFindingPayload)lf.Payload!;
        reqPayload.RequirementName.Should().Be("ReqN");
        reqPayload.RequirementText.Should().Be("Req body");
        reqPayload.IsMandatory.Should().BeTrue();
        lf.Trace.GraphNodeIdsExamined.Should().Equal("gx1");
        lf.Trace.RulesApplied.Should().Equal("rule-json");
        lf.Trace.DecisionsTaken.Should().Equal("dec-json");
        lf.Trace.AlternativePathsConsidered.Should().Equal("alt-json");
        lf.Trace.Notes.Should().Equal("trace-note");
    }

    [Fact]
    public async Task GetById_json_fallback_preserves_multi_finding_ordering()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 7, 15, 14, 30, 0, DateTimeKind.Utc);
        FindingsSnapshot original = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = createdUtc,
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "f-first",
                    FindingType = "RequirementFinding",
                    Category = "CatFirst",
                    EngineType = "EngineAlpha",
                    Severity = FindingSeverity.Warning,
                    Title = "Title First",
                    Rationale = "Rationale first",
                    RelatedNodeIds = ["rn-first-a", "rn-first-b"],
                    RecommendedActions = ["action-first-1"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["pf"] = "vf" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "ReqFirst", RequirementText = "Text first", IsMandatory = true
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["g1a", "g1b"],
                        RulesApplied = ["r1a"],
                        DecisionsTaken = ["d1a", "d1b"],
                        AlternativePathsConsidered = ["ap1"],
                        Notes = ["n1a", "n1b", "n1c"]
                    }
                },
                new Finding
                {
                    FindingId = "f-middle",
                    FindingType = "ComplianceFinding",
                    Category = "CatMiddle",
                    EngineType = "EngineBeta",
                    Severity = FindingSeverity.Error,
                    Title = "Title Middle",
                    Rationale = "Rationale middle",
                    RelatedNodeIds = ["rn-mid-x", "rn-mid-y"],
                    RecommendedActions = ["action-mid-1", "action-mid-2"],
                    Properties =
                        new Dictionary<string, string>(StringComparer.Ordinal) { ["pm1"] = "vm1", ["pm2"] = "vm2" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "ReqMiddle", RequirementText = "Text middle", IsMandatory = false
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["g2a"],
                        RulesApplied = ["r2a", "r2b"],
                        DecisionsTaken = ["d2a"],
                        AlternativePathsConsidered = ["ap2a", "ap2b"],
                        Notes = ["n2a"]
                    }
                },
                new Finding
                {
                    FindingId = "f-last",
                    FindingType = "SecurityFinding",
                    Category = "CatLast",
                    EngineType = "EngineGamma",
                    Severity = FindingSeverity.Info,
                    Title = "Title Last",
                    Rationale = "Rationale last",
                    RelatedNodeIds = ["rn-last-1", "rn-last-2"],
                    RecommendedActions = ["action-last"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["pl"] = "vl" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "ReqLast", RequirementText = "Text last", IsMandatory = true
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["g3a", "g3b", "g3c"],
                        RulesApplied = ["r3a"],
                        DecisionsTaken = ["d3a"],
                        AlternativePathsConsidered = ["ap3"],
                        Notes = ["n3a", "n3b"]
                    }
                }
            ]
        };

        FindingsSnapshotMigrator.Apply(original);
        string findingsJson = JsonEntitySerializer.Serialize(original);

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = 1,
                    FindingsJson = findingsJson
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().HaveCount(3);
        loaded.Findings[0].FindingId.Should().Be("f-first");
        loaded.Findings[1].FindingId.Should().Be("f-middle");
        loaded.Findings[2].FindingId.Should().Be("f-last");

        Finding a = loaded.Findings[0];
        a.Title.Should().Be("Title First");
        a.Category.Should().Be("CatFirst");
        a.Severity.Should().Be(FindingSeverity.Warning);
        a.EngineType.Should().Be("EngineAlpha");
        a.RelatedNodeIds.Should().Equal("rn-first-a", "rn-first-b");
        a.RecommendedActions.Should().Equal("action-first-1");
        a.Payload.Should().BeOfType<RequirementFindingPayload>();
        ((RequirementFindingPayload)a.Payload!).RequirementName.Should().Be("ReqFirst");
        a.Trace.GraphNodeIdsExamined.Should().Equal("g1a", "g1b");
        a.Trace.RulesApplied.Should().Equal("r1a");
        a.Trace.DecisionsTaken.Should().Equal("d1a", "d1b");
        a.Trace.AlternativePathsConsidered.Should().Equal("ap1");
        a.Trace.Notes.Should().Equal("n1a", "n1b", "n1c");

        Finding b = loaded.Findings[1];
        b.Title.Should().Be("Title Middle");
        b.Category.Should().Be("CatMiddle");
        b.Severity.Should().Be(FindingSeverity.Error);
        b.EngineType.Should().Be("EngineBeta");
        b.RelatedNodeIds.Should().Equal("rn-mid-x", "rn-mid-y");
        b.RecommendedActions.Should().Equal("action-mid-1", "action-mid-2");
        b.Payload.Should().BeOfType<RequirementFindingPayload>();
        ((RequirementFindingPayload)b.Payload!).RequirementName.Should().Be("ReqMiddle");
        b.Trace.GraphNodeIdsExamined.Should().Equal("g2a");
        b.Trace.RulesApplied.Should().Equal("r2a", "r2b");
        b.Trace.DecisionsTaken.Should().Equal("d2a");
        b.Trace.AlternativePathsConsidered.Should().Equal("ap2a", "ap2b");
        b.Trace.Notes.Should().Equal("n2a");

        Finding c = loaded.Findings[2];
        c.Title.Should().Be("Title Last");
        c.Category.Should().Be("CatLast");
        c.Severity.Should().Be(FindingSeverity.Info);
        c.EngineType.Should().Be("EngineGamma");
        c.RelatedNodeIds.Should().Equal("rn-last-1", "rn-last-2");
        c.RecommendedActions.Should().Equal("action-last");
        c.Payload.Should().BeOfType<RequirementFindingPayload>();
        ((RequirementFindingPayload)c.Payload!).RequirementName.Should().Be("ReqLast");
        c.Trace.GraphNodeIdsExamined.Should().Equal("g3a", "g3b", "g3c");
        c.Trace.Notes.Should().Equal("n3a", "n3b");
    }

    [Fact]
    public async Task GetById_json_fallback_deserializes_mixed_payload_types()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        FindingsSnapshot original = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = createdUtc,
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "f-req",
                    FindingType = "RequirementFinding",
                    Category = "Requirement",
                    EngineType = "TestEngine",
                    Severity = FindingSeverity.Warning,
                    Title = "Requirement title",
                    Rationale = "Rationale req",
                    RelatedNodeIds = ["n-req"],
                    RecommendedActions = ["fix-req"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal),
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "NamedRequirement",
                        RequirementText = "Requirement body text",
                        IsMandatory = true
                    },
                    Trace = new ExplainabilityTrace()
                },
                new Finding
                {
                    FindingId = "f-comp",
                    FindingType = "ComplianceFinding",
                    Category = "Compliance",
                    EngineType = "compliance",
                    Severity = FindingSeverity.Error,
                    Title = "Compliance title",
                    Rationale = "Rationale comp",
                    RelatedNodeIds = ["n-comp"],
                    RecommendedActions = ["fix-comp"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal),
                    PayloadType = nameof(ComplianceFindingPayload),
                    Payload = new ComplianceFindingPayload
                    {
                        RulePackId = "pack-1",
                        RulePackVersion = "2026.01",
                        RuleId = "rule-comp-1",
                        ControlId = "AC-2",
                        ControlName = "Account management",
                        AppliesToCategory = "Identity",
                        AffectedResources = ["sub-a", "sub-b"]
                    },
                    Trace = new ExplainabilityTrace()
                },
                new Finding
                {
                    FindingId = "f-cost",
                    FindingType = "CostConstraintFinding",
                    Category = "Cost",
                    EngineType = "cost-constraint",
                    Severity = FindingSeverity.Info,
                    Title = "Cost title",
                    Rationale = "Rationale cost",
                    RelatedNodeIds = ["n-cost"],
                    RecommendedActions = ["fix-cost"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal),
                    PayloadType = nameof(CostConstraintFindingPayload),
                    Payload = new CostConstraintFindingPayload
                    {
                        BudgetName = "MonthlyCap", MaxMonthlyCost = 12_500.50m, CostRisk = "high"
                    },
                    Trace = new ExplainabilityTrace()
                }
            ]
        };

        FindingsSnapshotMigrator.Apply(original);
        string findingsJson = JsonEntitySerializer.Serialize(original);

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = 1,
                    FindingsJson = findingsJson
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().HaveCount(3);
        loaded.Findings[0].FindingId.Should().Be("f-req");
        loaded.Findings[1].FindingId.Should().Be("f-comp");
        loaded.Findings[2].FindingId.Should().Be("f-cost");

        Finding first = loaded.Findings[0];
        first.Payload.Should().BeOfType<RequirementFindingPayload>();
        RequirementFindingPayload req = (RequirementFindingPayload)first.Payload!;
        req.RequirementName.Should().Be("NamedRequirement");

        Finding second = loaded.Findings[1];
        second.Payload.Should().BeOfType<ComplianceFindingPayload>();
        ComplianceFindingPayload comp = (ComplianceFindingPayload)second.Payload!;
        comp.ControlId.Should().Be("AC-2");
        comp.RuleId.Should().Be("rule-comp-1");

        Finding third = loaded.Findings[2];
        third.Payload.Should().BeOfType<CostConstraintFindingPayload>();
        CostConstraintFindingPayload cost = (CostConstraintFindingPayload)third.Payload!;
        cost.BudgetName.Should().Be("MonthlyCap");
        cost.CostRisk.Should().Be("high");
        cost.MaxMonthlyCost.Should().Be(12_500.50m);
    }

    [Fact]
    public async Task GetById_when_no_FindingRecords_and_FindingsJson_is_null_returns_empty_findings()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 8, 2, 9, 0, 0, DateTimeKind.Utc);
        const int schemaVersion = 1;

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = schemaVersion,
                    FindingsJson = (string?)null
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().BeEmpty();
        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.RunId.Should().Be(runId);
        loaded.ContextSnapshotId.Should().Be(contextId);
        loaded.GraphSnapshotId.Should().Be(graphId);
        loaded.CreatedUtc.Should().Be(createdUtc);
        loaded.SchemaVersion.Should().Be(schemaVersion);
    }

    [Fact]
    public async Task GetById_when_no_FindingRecords_and_FindingsJson_is_empty_string_returns_empty_findings()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
        const int schemaVersion = 1;

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = schemaVersion,
                    FindingsJson = ""
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().BeEmpty();
        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.RunId.Should().Be(runId);
        loaded.ContextSnapshotId.Should().Be(contextId);
        loaded.GraphSnapshotId.Should().Be(graphId);
        loaded.CreatedUtc.Should().Be(createdUtc);
        loaded.SchemaVersion.Should().Be(schemaVersion);
    }

    [Fact]
    public async Task GetById_when_no_FindingRecords_and_FindingsJson_has_empty_array_returns_empty_findings()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        DateTime createdUtc = new(2026, 11, 6, 15, 0, 0, DateTimeKind.Utc);
        const int schemaVersion = 1;
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = createdUtc,
            SchemaVersion = schemaVersion,
            Findings = []
        };

        FindingsSnapshotMigrator.Apply(snapshot);
        string findingsJson = JsonEntitySerializer.Serialize(snapshot);

        const string insertHeader = """
                                    INSERT INTO dbo.FindingsSnapshots
                                    (
                                        FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                                        SchemaVersion, FindingsJson
                                    )
                                    VALUES
                                    (
                                        @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                                        @SchemaVersion, @FindingsJson
                                    );
                                    """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = schemaVersion,
                    FindingsJson = findingsJson
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().BeEmpty();
        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.RunId.Should().Be(runId);
        loaded.ContextSnapshotId.Should().Be(contextId);
        loaded.GraphSnapshotId.Should().Be(graphId);
        loaded.CreatedUtc.Should().Be(createdUtc);
        // Relational header may store a legacy version; JSON hydrate path runs FindingsSnapshotMigrator (same as production reads).
        loaded.SchemaVersion.Should().Be(FindingsSchema.CurrentSnapshotVersion);
    }

    [Fact]
    public async Task SaveAsync_with_explicit_transaction_commits_relational_rows()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        SqlFindingsSnapshotRepository repository = new(factory, Empty);
        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow,
            Findings = []
        };

        FindingsSnapshotMigrator.Apply(snapshot);
        await using SqlTransaction tx = connection.BeginTransaction();
        await repository.SaveAsync(snapshot, CancellationToken.None, connection, tx);
        tx.Commit();

        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Findings.Should().BeEmpty();
    }

    private static async Task SeedAuthorityParentsAsync(
        SqlConnection connection,
        Guid runId,
        Guid contextSnapshotId,
        Guid graphSnapshotId,
        CancellationToken ct)
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        const string insertRun = """
                                 INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
                                 VALUES (@RunId, @ProjectId, @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRun,
                new
                {
                    RunId = runId,
                    ProjectId = "proj-seed",
                    CreatedUtc = DateTime.UtcNow,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ScopeProjectId = scopeProjectId
                },
                cancellationToken: ct));

        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyStringList = JsonEntitySerializer.Serialize(new List<string>());

        const string insertContext = """
                                     INSERT INTO dbo.ContextSnapshots
                                     (
                                         SnapshotId, RunId, ProjectId, CreatedUtc,
                                         CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                                     )
                                     VALUES
                                     (
                                         @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                                         @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                                     );
                                     """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertContext,
                new
                {
                    SnapshotId = contextSnapshotId,
                    RunId = runId,
                    ProjectId = "proj-seed",
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = emptyCanonical,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyStringList,
                    ErrorsJson = emptyStringList,
                    SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>())
                },
                cancellationToken: ct));

        string emptyNodes = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string emptyEdges = JsonEntitySerializer.Serialize(new List<GraphEdge>());
        string emptyGraphWarnings = JsonEntitySerializer.Serialize(new List<string>());

        const string insertGraph = """
                                   INSERT INTO dbo.GraphSnapshots
                                   (
                                       GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                                       NodesJson, EdgesJson, WarningsJson
                                   )
                                   VALUES
                                   (
                                       @GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc,
                                       @NodesJson, @EdgesJson, @WarningsJson
                                   );
                                   """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertGraph,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                    ContextSnapshotId = contextSnapshotId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = emptyNodes,
                    EdgesJson = emptyEdges,
                    WarningsJson = emptyGraphWarnings
                },
                cancellationToken: ct));
    }
}
