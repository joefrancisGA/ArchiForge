using ArchiForge.ContextIngestion.Mapping;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class ContextIngestionRequestMapperTests
{
    [Fact]
    public void FromArchitectureRequest_MapsSystemNameToProjectId_AndDocuments()
    {
        ArchitectureRequest request = new ArchitectureRequest
        {
            Description = "1234567890 minimum len",
            SystemName = "billing-api",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Documents =
            [
                new ContextDocumentRequest
                {
                    Name = "spec.txt",
                    ContentType = "text/plain",
                    Content = "REQ: Must scale"
                }
            ],
            InlineRequirements = ["ir1"],
            PolicyReferences = ["SOC2"],
            TopologyHints = ["subnet-a"],
            SecurityBaselineHints = ["encrypt at rest"],
            InfrastructureDeclarations =
            [
                new InfrastructureDeclarationRequest
                {
                    Name = "env.json",
                    Format = "json",
                    Content = """{"resources":[]}"""
                }
            ]
        };

        ContextIngestionRequest mapped = ContextIngestionRequestMapper.FromArchitectureRequest(request);

        mapped.ProjectId.Should().Be("billing-api");
        mapped.Description.Should().Be(request.Description);
        mapped.InlineRequirements.Should().Equal("ir1");
        mapped.PolicyReferences.Should().Equal("SOC2");
        mapped.TopologyHints.Should().Equal("subnet-a");
        mapped.SecurityBaselineHints.Should().Equal("encrypt at rest");
        mapped.Documents.Should().HaveCount(1);
        mapped.Documents[0].Name.Should().Be("spec.txt");
        mapped.Documents[0].ContentType.Should().Be("text/plain");
        mapped.Documents[0].Content.Should().Be("REQ: Must scale");
        mapped.Documents[0].DocumentId.Should().NotBeNullOrEmpty();
        mapped.InfrastructureDeclarations.Should().HaveCount(1);
        mapped.InfrastructureDeclarations[0].Name.Should().Be("env.json");
        mapped.InfrastructureDeclarations[0].Format.Should().Be("json");
    }
}
