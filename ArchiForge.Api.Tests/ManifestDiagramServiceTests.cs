using ArchiForge.Application.Diagrams;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Unit tests for <see cref="ManifestDiagramService"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ManifestDiagramServiceTests
{
    private readonly ManifestDiagramService _sut = new();

    [Fact]
    public void GenerateMermaid_NullManifest_Throws()
    {
        Action act = () => _sut.GenerateMermaid(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateMermaid_EmptyManifest_ReturnsFlowchartHeader()
    {
        GoldenManifest manifest = CreateMinimalManifest();

        string mermaid = _sut.GenerateMermaid(manifest);

        mermaid.Should().StartWith("flowchart LR");
    }

    [Fact]
    public void GenerateMermaid_LayoutTb_ReturnsTopBottomHeader()
    {
        GoldenManifest manifest = CreateMinimalManifest();

        string mermaid = _sut.GenerateMermaid(manifest, new ManifestDiagramOptions { Layout = "TB" });

        mermaid.Should().StartWith("flowchart TB");
    }

    [Fact]
    public void GenerateMermaid_WithService_ContainsServiceNode()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "order-api",
            ServiceName = "OrderApi",
            ServiceType = ServiceType.Api,
            RuntimePlatform = RuntimePlatform.AppService
        });

        string mermaid = _sut.GenerateMermaid(manifest);

        mermaid.Should().Contain("OrderApi");
        mermaid.Should().Contain("AppService");
    }

    [Fact]
    public void GenerateMermaid_WithDatastore_ContainsCylinderNode()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "main-db",
            DatastoreName = "MainDb",
            DatastoreType = DatastoreType.Sql,
            RuntimePlatform = RuntimePlatform.SqlServer
        });

        string mermaid = _sut.GenerateMermaid(manifest);

        // Mermaid cylinder syntax: nodeId[("label")]
        mermaid.Should().Contain("MainDb");
        mermaid.Should().Contain("[(\"");
    }

    [Fact]
    public void GenerateMermaid_WithRelationship_ContainsEdge()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "api", ServiceName = "Api",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "db", DatastoreName = "Db",
            DatastoreType = DatastoreType.Sql, RuntimePlatform = RuntimePlatform.SqlServer
        });
        manifest.Relationships.Add(new ManifestRelationship
        {
            SourceId = "api", TargetId = "db", RelationshipType = RelationshipType.WritesTo
        });

        string mermaid = _sut.GenerateMermaid(manifest);

        mermaid.Should().Contain("-->|WritesTo|");
    }

    [Fact]
    public void GenerateMermaid_RelationshipLabelsNone_OmitsEdgeLabel()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "a", ServiceName = "A",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "b", DatastoreName = "B",
            DatastoreType = DatastoreType.Sql, RuntimePlatform = RuntimePlatform.SqlServer
        });
        manifest.Relationships.Add(new ManifestRelationship
        {
            SourceId = "a", TargetId = "b", RelationshipType = RelationshipType.Calls
        });

        ManifestDiagramOptions options = new() { RelationshipLabels = "none" };

        string mermaid = _sut.GenerateMermaid(manifest, options);

        mermaid.Should().Contain(" --> ");
        mermaid.Should().NotContain("-->|");
    }

    [Fact]
    public void GenerateMermaid_IncludeRuntimePlatformFalse_OmitsPlatformLabel()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "svc", ServiceName = "MySvc",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.ContainerApps
        });

        ManifestDiagramOptions options = new() { IncludeRuntimePlatform = false };

        string mermaid = _sut.GenerateMermaid(manifest, options);

        mermaid.Should().Contain("MySvc");
        mermaid.Should().NotContain("ContainerApps");
    }

    [Fact]
    public void GenerateMermaid_GroupByRuntimePlatform_ContainsSubgraph()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "s1", ServiceName = "Svc1",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "s2", ServiceName = "Svc2",
            ServiceType = ServiceType.Worker, RuntimePlatform = RuntimePlatform.Functions
        });

        ManifestDiagramOptions options = new() { GroupBy = "runtimeplatform" };

        string mermaid = _sut.GenerateMermaid(manifest, options);

        mermaid.Should().Contain("subgraph");
        mermaid.Should().Contain("end");
    }

    [Fact]
    public void GenerateMermaid_GroupByServiceType_ContainsSubgraph()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "s1", ServiceName = "Svc1",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.AppService
        });

        ManifestDiagramOptions options = new() { GroupBy = "servicetype" };

        string mermaid = _sut.GenerateMermaid(manifest, options);

        mermaid.Should().Contain("subgraph");
    }

    [Fact]
    public void GenerateMermaid_DuplicateServiceIds_ProducesUniqueNodeIds()
    {
        GoldenManifest manifest = CreateMinimalManifest();
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "svc", ServiceName = "Alpha",
            ServiceType = ServiceType.Api, RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "svc", ServiceName = "Beta",
            ServiceType = ServiceType.Worker, RuntimePlatform = RuntimePlatform.Functions
        });

        string mermaid = _sut.GenerateMermaid(manifest);

        mermaid.Should().Contain("Alpha");
        mermaid.Should().Contain("Beta");
    }

    private static GoldenManifest CreateMinimalManifest()
    {
        return new GoldenManifest
        {
            RunId = Guid.NewGuid().ToString("N"),
            SystemName = "Test"
        };
    }
}
