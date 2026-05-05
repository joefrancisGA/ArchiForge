using ArchLucid.ContextIngestion.Mapping;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.Application.Tests.MigratedCoordinator;

/// <summary>
/// <see cref="ContextIngestionRequestMapper.FromArchitectureRequest"/> â€” the bridge from coordinator/API request to ingestion pipeline input.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ContextIngestionRequestMapperTests
{
    [SkippableFact]
    public void FromArchitectureRequest_maps_system_name_to_project_id_and_collections()
    {
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            Description = "Build secure APIs with private networking.",
            SystemName = "billing-svc",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["c1"],
            RequiredCapabilities = ["cap"],
            InlineRequirements = ["ir1"],
            Documents =
            [
                new ContextDocumentRequest { Name = "notes.txt", ContentType = "text/plain", Content = "hello" }
            ],
            PolicyReferences = ["p1"],
            TopologyHints = ["t1"],
            SecurityBaselineHints = ["s1"],
            InfrastructureDeclarations =
            [
                new InfrastructureDeclarationRequest { Name = "main.tf", Format = "terraform", Content = "resource \"x\" \"y\" {}" }
            ]
        };

        ContextIngestionRequest mapped = ContextIngestionRequestMapper.FromArchitectureRequest(request);

        mapped.ProjectId.Should().Be("billing-svc");
        mapped.Description.Should().Be(request.Description);
        mapped.InlineRequirements.Should().Equal("ir1");
        mapped.Documents.Should().ContainSingle(d => d.Name == "notes.txt" && d.Content == "hello");
        mapped.PolicyReferences.Should().Equal("p1");
        mapped.TopologyHints.Should().Equal("t1");
        mapped.SecurityBaselineHints.Should().Equal("s1");
        mapped.InfrastructureDeclarations.Should().ContainSingle(d => d.Name == "main.tf" && d.Format == "terraform");
    }

    [SkippableFact]
    public void FromArchitectureRequest_throws_when_request_null()
    {
        Action act = () => ContextIngestionRequestMapper.FromArchitectureRequest(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }
}
