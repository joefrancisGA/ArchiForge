using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;

using FluentAssertions;

namespace ArchiForge.DecisionEngine.Tests;

/// <summary>Broader merge scenarios (formerly in Api.Tests). Uses passthrough schema validation.</summary>
public sealed class DecisionEngineServiceScenarioTests
{
    private readonly DecisionEngineService _service = new(new PassthroughSchemaValidationService());

    [Fact]
    public void MergeResults_ValidTopologyAndCompliance_CreatesManifest()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult topology = new()
        {
            ResultId = "RES-TOPO-001",
            TaskId = "TASK-TOPO-001",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add API and search services"],
            EvidenceRefs = ["request", "catalog"],
            Confidence = 0.92,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-001",
                SourceAgent = AgentType.Topology,
                AddedServices =
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
                AddedDatastores =
                [
                    new ManifestDatastore
                    {
                        DatastoreId = "ds-meta",
                        DatastoreName = "rag-metadata",
                        DatastoreType = DatastoreType.Sql,
                        RuntimePlatform = RuntimePlatform.SqlServer
                    }
                ]
            }
        };

        AgentResult compliance = new()
        {
            ResultId = "RES-COMP-001",
            TaskId = "TASK-COMP-001",
            RunId = "RUN-001",
            AgentType = AgentType.Compliance,
            Claims = ["Managed identity required"],
            EvidenceRefs = ["policy-pack"],
            Confidence = 0.97,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-COMP-001",
                SourceAgent = AgentType.Compliance,
                RequiredControls =
                [
                    "Managed Identity",
                    "Private Endpoints",
                    "Key Vault"
                ]
            }
        };

        DecisionMergeResult result = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [topology, compliance],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeTrue();
        result.Manifest.RunId.Should().Be("RUN-001");
        result.Manifest.SystemName.Should().Be("EnterpriseRag");
        result.Manifest.Services.Should().HaveCount(2);
        result.Manifest.Datastores.Should().HaveCount(1);
        result.Manifest.Governance.RequiredControls.Should().Contain("Managed Identity");
        result.Manifest.Governance.RequiredControls.Should().Contain("Private Endpoints");
    }

    [Fact]
    public void MergeResults_MalformedResult_FailsMerge()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult malformed = new()
        {
            ResultId = "",
            TaskId = "",
            RunId = "",
            AgentType = AgentType.Topology,
            Claims = [],
            EvidenceRefs = [],
            Confidence = 1.2
        };

        DecisionMergeResult result = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [malformed],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void MergeResults_RequiredControls_PropagateToServices()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult topology = new()
        {
            ResultId = "RES-TOPO-002",
            TaskId = "TASK-TOPO-002",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add API service"],
            EvidenceRefs = ["request"],
            Confidence = 0.90,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-002",
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-api",
                        ServiceName = "rag-api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService
                    }
                ]
            }
        };

        AgentResult compliance = new()
        {
            ResultId = "RES-COMP-002",
            TaskId = "TASK-COMP-002",
            RunId = "RUN-001",
            AgentType = AgentType.Compliance,
            Claims = ["Managed identity required"],
            EvidenceRefs = ["policy-pack"],
            Confidence = 0.95,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-COMP-002",
                SourceAgent = AgentType.Compliance,
                RequiredControls = ["Managed Identity", "Key Vault"]
            }
        };

        DecisionMergeResult result = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [topology, compliance],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeTrue();
        result.Manifest.Services.Should().ContainSingle();

        ManifestService service = result.Manifest.Services.Single();
        service.RequiredControls.Should().Contain(c =>
            c.Equals("Managed Identity", StringComparison.OrdinalIgnoreCase));
        service.RequiredControls.Should().Contain(c =>
            c.Equals("Key Vault", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MergeResults_PrivateEndpointControl_SetsDatastoreFlag()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult topology = new()
        {
            ResultId = "RES-TOPO-003",
            TaskId = "TASK-TOPO-003",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add SQL datastore"],
            EvidenceRefs = ["request"],
            Confidence = 0.88,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-003",
                SourceAgent = AgentType.Topology,
                AddedDatastores =
                [
                    new ManifestDatastore
                    {
                        DatastoreId = "ds-meta",
                        DatastoreName = "rag-metadata",
                        DatastoreType = DatastoreType.Sql,
                        RuntimePlatform = RuntimePlatform.SqlServer,
                        PrivateEndpointRequired = false
                    }
                ]
            }
        };

        AgentResult compliance = new()
        {
            ResultId = "RES-COMP-003",
            TaskId = "TASK-COMP-003",
            RunId = "RUN-001",
            AgentType = AgentType.Compliance,
            Claims = ["Private endpoints required"],
            EvidenceRefs = ["policy-pack"],
            Confidence = 0.96,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-COMP-003",
                SourceAgent = AgentType.Compliance,
                RequiredControls = ["Private Endpoints"]
            }
        };

        DecisionMergeResult result = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [topology, compliance],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeTrue();
        result.Manifest.Datastores.Should().ContainSingle();
        result.Manifest.Datastores.Single().PrivateEndpointRequired.Should().BeTrue();
    }

    [Fact]
    public void MergeResults_DuplicateServices_AreMergedNotDuplicated()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult topologyA = new()
        {
            ResultId = "RES-TOPO-A",
            TaskId = "TASK-TOPO-A",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add rag-api"],
            EvidenceRefs = ["request"],
            Confidence = 0.90,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-A",
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-api",
                        ServiceName = "rag-api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService,
                        Tags = ["api"]
                    }
                ]
            }
        };

        AgentResult topologyB = new()
        {
            ResultId = "RES-TOPO-B",
            TaskId = "TASK-TOPO-B",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add rag-api tag and purpose"],
            EvidenceRefs = ["request"],
            Confidence = 0.89,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-B",
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-api-2",
                        ServiceName = "rag-api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService,
                        Purpose = "Primary API",
                        Tags = ["entrypoint"]
                    }
                ]
            }
        };

        DecisionMergeResult result = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [topologyA, topologyB],
            evaluations: [],
            decisionNodes: []);

        result.Success.Should().BeTrue();
        result.Manifest.Services.Should().ContainSingle();

        ManifestService service = result.Manifest.Services.Single();
        service.ServiceName.Should().Be("rag-api");
        service.Purpose.Should().Be("Primary API");
        service.Tags.Should().Contain("api");
        service.Tags.Should().Contain("entrypoint");
    }

    [Fact]
    public void MergeResults_ManifestVersionIncrementsAcrossCalls()
    {
        ArchitectureRequest request = CreateRequest();

        AgentResult topology = new()
        {
            ResultId = "RES-TOPO-004",
            TaskId = "TASK-TOPO-004",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Claims = ["Add API service"],
            EvidenceRefs = ["request"],
            Confidence = 0.90,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-TOPO-004",
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-api",
                        ServiceName = "rag-api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService
                    }
                ]
            }
        };

        DecisionMergeResult v1 = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: [topology],
            evaluations: [],
            decisionNodes: []);

        DecisionMergeResult v2 = _service.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v2",
            results: [topology],
            evaluations: [],
            decisionNodes: [],
            parentManifestVersion: "v1");

        v1.Success.Should().BeTrue();
        v2.Success.Should().BeTrue();
        v1.Manifest.Metadata.ManifestVersion.Should().Be("v1");
        v2.Manifest.Metadata.ManifestVersion.Should().Be("v2");
        v2.Manifest.Metadata.ParentManifestVersion.Should().Be("v1");
    }

    private static ArchitectureRequest CreateRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "EnterpriseRag",
            Description = "Design a secure Azure RAG system for internal enterprise documents.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints =
            [
                "Private endpoints required",
                "Use managed identity"
            ],
            RequiredCapabilities =
            [
                "Azure AI Search",
                "SQL",
                "Managed Identity",
                "Private Networking"
            ]
        };
    }
}
