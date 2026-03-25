using ArchiForge.Application.Diffs;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ManifestDiffServiceTests
{
    [Fact]
    public void Compare_ShouldDetectAddedServiceAndControl()
    {
        GoldenManifest left = new()
        {
            RunId = "RUN-001",
            SystemName = "EnterpriseRag",
            Services =
            [
                new ManifestService
                {
                    ServiceId = "svc-api",
                    ServiceName = "rag-api",
                    ServiceType = ServiceType.Api,
                    RuntimePlatform = RuntimePlatform.AppService
                }
            ],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance
            {
                RequiredControls = ["Managed Identity"]
            },
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v1"
            }
        };

        GoldenManifest right = new()
        {
            RunId = "RUN-001",
            SystemName = "EnterpriseRag",
            Services =
            [
                new ManifestService
                {
                    ServiceId = "svc-api",
                    ServiceName = "rag-api",
                    ServiceType = ServiceType.Api,
                    RuntimePlatform = RuntimePlatform.AppService
                },
                new ManifestService
                {
                    ServiceId = "svc-search",
                    ServiceName = "rag-search",
                    ServiceType = ServiceType.SearchService,
                    RuntimePlatform = RuntimePlatform.AzureAiSearch
                }
            ],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance
            {
                RequiredControls = ["Managed Identity", "Private Endpoints"]
            },
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v2"
            }
        };

        ManifestDiffService service = new();

        ManifestDiffResult diff = service.Compare(left, right);

        diff.AddedServices.Should().Contain("rag-search");
        diff.RemovedServices.Should().BeEmpty();
        diff.AddedRequiredControls.Should().Contain("Private Endpoints");
        diff.RemovedRequiredControls.Should().BeEmpty();
    }

    [Fact]
    public void Compare_ShouldDetectRemovedDatastore()
    {
        GoldenManifest left = new()
        {
            RunId = "RUN-001",
            SystemName = "EnterpriseRag",
            Services = [],
            Datastores =
            [
                new ManifestDatastore
                {
                    DatastoreId = "ds-meta",
                    DatastoreName = "rag-metadata",
                    DatastoreType = DatastoreType.Sql,
                    RuntimePlatform = RuntimePlatform.SqlServer
                }
            ],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v1"
            }
        };

        GoldenManifest right = new()
        {
            RunId = "RUN-001",
            SystemName = "EnterpriseRag",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v2"
            }
        };

        ManifestDiffService service = new();

        ManifestDiffResult diff = service.Compare(left, right);

        diff.RemovedDatastores.Should().Contain("rag-metadata");
        diff.AddedDatastores.Should().BeEmpty();
    }
}
