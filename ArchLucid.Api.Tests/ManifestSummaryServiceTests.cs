using ArchLucid.Application.Summaries;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="ManifestSummaryService" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ManifestSummaryServiceTests
{
    /// <summary>
    ///     Enough services and relationships to exceed <see cref="ManifestSummaryOptions.MaxRelationships" /> in the
    ///     capped-output test.
    /// </summary>
    private const int RelationshipStressPairCount = 5;

    private readonly ManifestSummaryService _sut = new();

    [SkippableFact]
    public void GenerateMarkdown_NullManifest_Throws()
    {
        Action act = () => _sut.GenerateMarkdown(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [SkippableFact]
    public void GenerateMarkdown_EmptyManifest_ContainsSystemNameAndOverview()
    {
        GoldenManifest manifest = CreateMinimalManifest("TestSystem");

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("# Architecture Summary: TestSystem");
        markdown.Should().Contain("## Overview");
        markdown.Should().Contain("**System Name:** TestSystem");
    }

    [SkippableFact]
    public void GenerateMarkdown_WithServices_ContainsServiceSection()
    {
        GoldenManifest manifest = CreateMinimalManifest("Svc");
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "svc-1",
            ServiceName = "OrderApi",
            ServiceType = ServiceType.Api,
            RuntimePlatform = RuntimePlatform.AppService,
            Purpose = "Handles orders"
        });

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Services");
        markdown.Should().Contain("### OrderApi");
        markdown.Should().Contain("**Service Type:** Api");
        markdown.Should().Contain("**Purpose:** Handles orders");
    }

    [SkippableFact]
    public void GenerateMarkdown_WithDatastores_ContainsDatastoreSection()
    {
        GoldenManifest manifest = CreateMinimalManifest("Ds");
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "ds-1",
            DatastoreName = "OrdersDb",
            DatastoreType = DatastoreType.Sql,
            RuntimePlatform = RuntimePlatform.SqlServer,
            PrivateEndpointRequired = true,
            EncryptionAtRestRequired = true
        });

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Datastores");
        markdown.Should().Contain("### OrdersDb");
        markdown.Should().Contain("**Private Endpoint Required:** Yes");
        markdown.Should().Contain("**Encryption At Rest Required:** Yes");
    }

    [SkippableFact]
    public void GenerateMarkdown_WithRelationships_ContainsRelationshipSection()
    {
        GoldenManifest manifest = CreateMinimalManifest("Rel");
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "api",
            ServiceName = "WebApi",
            ServiceType = ServiceType.Api,
            RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "db",
            DatastoreName = "MainDb",
            DatastoreType = DatastoreType.Sql,
            RuntimePlatform = RuntimePlatform.SqlServer
        });
        manifest.Relationships.Add(new ManifestRelationship
        {
            SourceId = "api",
            TargetId = "db",
            RelationshipType = RelationshipType.WritesTo,
            Description = "Persists order data"
        });

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Relationships");
        markdown.Should().Contain("WebApi");
        markdown.Should().Contain("MainDb");
    }

    [SkippableFact]
    public void GenerateMarkdown_WithComplianceTags_ContainsComplianceSection()
    {
        GoldenManifest manifest = CreateMinimalManifest("Gov");
        manifest.Governance.ComplianceTags.AddRange(["SOC2", "ISO27001"]);

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Compliance Tags");
        markdown.Should().Contain("- ISO27001");
        markdown.Should().Contain("- SOC2");
    }

    [SkippableFact]
    public void GenerateMarkdown_WithRequiredControls_ContainsControlsSection()
    {
        GoldenManifest manifest = CreateMinimalManifest("Controls");
        manifest.Governance.RequiredControls.Add("ManagedIdentity");

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Required Controls");
        markdown.Should().Contain("- ManagedIdentity");
    }

    [SkippableFact]
    public void GenerateMarkdown_IncludeRelationshipsFalse_OmitsRelationships()
    {
        GoldenManifest manifest = CreateMinimalManifest("NoRel");
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "a",
            ServiceName = "A",
            ServiceType = ServiceType.Api,
            RuntimePlatform = RuntimePlatform.AppService
        });
        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "b",
            DatastoreName = "B",
            DatastoreType = DatastoreType.Sql,
            RuntimePlatform = RuntimePlatform.SqlServer
        });
        manifest.Relationships.Add(new ManifestRelationship
        {
            SourceId = "a", TargetId = "b", RelationshipType = RelationshipType.Calls
        });

        ManifestSummaryOptions options = new() { IncludeRelationships = false };

        string markdown = _sut.GenerateMarkdown(manifest, options);

        markdown.Should().NotContain("## Relationships");
    }

    [SkippableFact]
    public void GenerateMarkdown_IncludeComplianceTagsFalse_OmitsComplianceTags()
    {
        GoldenManifest manifest = CreateMinimalManifest("NoCT");
        manifest.Governance.ComplianceTags.Add("SOC2");

        ManifestSummaryOptions options = new() { IncludeComplianceTags = false };

        string markdown = _sut.GenerateMarkdown(manifest, options);

        markdown.Should().NotContain("## Compliance Tags");
    }

    [SkippableFact]
    public void GenerateMarkdown_MaxRelationships_LimitsOutput()
    {
        GoldenManifest manifest = CreateMinimalManifest("Limited");

        for (int i = 0; i < RelationshipStressPairCount; i++)
        {
            manifest.Services.Add(new ManifestService
            {
                ServiceId = $"svc{i}",
                ServiceName = $"Svc{i}",
                ServiceType = ServiceType.Api,
                RuntimePlatform = RuntimePlatform.AppService
            });
        }

        manifest.Datastores.Add(new ManifestDatastore
        {
            DatastoreId = "ds0",
            DatastoreName = "Ds0",
            DatastoreType = DatastoreType.Sql,
            RuntimePlatform = RuntimePlatform.SqlServer
        });

        for (int i = 0; i < RelationshipStressPairCount; i++)
        {
            manifest.Relationships.Add(new ManifestRelationship
            {
                SourceId = $"svc{i}", TargetId = "ds0", RelationshipType = RelationshipType.Calls
            });
        }

        ManifestSummaryOptions options = new() { MaxRelationships = 2 };

        string markdown = _sut.GenerateMarkdown(manifest, options);

        int count = markdown.Split("->").Length - 1;
        count.Should().Be(2);
    }

    [SkippableFact]
    public void GenerateMarkdown_DefaultOptions_IncludesAllSections()
    {
        GoldenManifest manifest = CreateMinimalManifest("Full");
        manifest.Services.Add(new ManifestService
        {
            ServiceId = "s1",
            ServiceName = "Alpha",
            ServiceType = ServiceType.Api,
            RuntimePlatform = RuntimePlatform.AppService,
            RequiredControls = ["TLS"],
            Tags = ["core"]
        });
        manifest.Governance.RequiredControls.Add("WAF");
        manifest.Governance.ComplianceTags.Add("PCI");

        string markdown = _sut.GenerateMarkdown(manifest);

        markdown.Should().Contain("## Required Controls");
        markdown.Should().Contain("## Compliance Tags");
        markdown.Should().Contain("**Required Controls:** TLS");
        markdown.Should().Contain("**Tags:** core");
    }

    private static GoldenManifest CreateMinimalManifest(string systemName)
    {
        return new GoldenManifest
        {
            RunId = Guid.NewGuid().ToString("N"),
            SystemName = systemName,
            Metadata = new ManifestMetadata { ManifestVersion = "v1" }
        };
    }
}
