using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Application.Templates;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Templates;

public sealed class ArchitectureRequestTemplatesTests
{
    private static readonly JsonSerializerOptions JsonRoundTripOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static TheoryData<Func<string?, ArchitectureRequest>> TemplateFactories =>
    [
        ArchitectureRequestTemplates.MicroservicesWebPlatform,
        ArchitectureRequestTemplates.MonolithMigrationAssessment,
        ArchitectureRequestTemplates.EventDrivenProcessingPipeline,
        ArchitectureRequestTemplates.CloudNativeMigration,
        ArchitectureRequestTemplates.RegulatedHealthcareSystem,
        ArchitectureRequestTemplates.RetailBankingAndPaymentsPlatform,
        ArchitectureRequestTemplates.SmartManufacturingOtItReference
    ];

    [SkippableFact]
    public void Summaries_has_seven_unique_template_ids_aligned_with_catalog()
    {
        ArchitectureRequestTemplates.Summaries.Should().HaveCount(7);

        int distinctIds = ArchitectureRequestTemplates.Summaries.Select(s => s.TemplateId).Distinct().Count();
        distinctIds.Should().Be(7);

        HashSet<string> summaryIds = ArchitectureRequestTemplates.Summaries.Select(s => s.TemplateId).ToHashSet();
        summaryIds.Should().Contain("microservices-web-platform");
        summaryIds.Should().Contain("monolith-migration-assessment");
        summaryIds.Should().Contain("event-driven-processing-pipeline");
        summaryIds.Should().Contain("cloud-native-migration-azure");
        summaryIds.Should().Contain("regulated-healthcare-hipaa");
        summaryIds.Should().Contain("financial-services-pci-sox");
        summaryIds.Should().Contain("manufacturing-ot-it-convergence");
    }

    [Theory]
    [MemberData(nameof(TemplateFactories))]
    public void Each_factory_produces_valid_request_shape(Func<string?, ArchitectureRequest> factory)
    {
        ArchitectureRequest request = factory(null);

        request.Should().NotBeNull();
        request.RequestId.Should().NotBeNullOrWhiteSpace();
        request.RequestId.Length.Should().BeLessOrEqualTo(64);
        request.Description.Should().NotBeNullOrWhiteSpace();
        request.Description.Length.Should().BeInRange(10, 4000);
        request.SystemName.Should().NotBeNullOrWhiteSpace();
        request.SystemName.Length.Should().BeLessOrEqualTo(200);
        request.Environment.Should().NotBeNullOrWhiteSpace();
        request.Environment.Length.Should().BeLessOrEqualTo(50);
        request.CloudProvider.Should().Be(Contracts.Common.CloudProvider.Azure);

        request.Constraints.Should().NotBeNull();
        request.RequiredCapabilities.Should().NotBeNull();
        request.Assumptions.Should().NotBeNull();
        request.InlineRequirements.Should().NotBeNull();
        request.Documents.Should().NotBeNull();
        request.PolicyReferences.Should().NotBeNull();
        request.TopologyHints.Should().NotBeNull();
        request.SecurityBaselineHints.Should().NotBeNull();
        request.InfrastructureDeclarations.Should().NotBeNull();

        request.Documents.Should().HaveCountGreaterThanOrEqualTo(4,
            "expect ArchLucid.TemplateId plus at least three evidence markdown documents");

        ContextDocumentRequest templateMarker = request.Documents[0];
        templateMarker.Name.Should().Be("ArchLucid.TemplateId");
        templateMarker.ContentType.Should().Be("text/plain");
        templateMarker.Content.Should().NotBeNullOrWhiteSpace();

        int evidenceDocs = request.Documents.Count - 1;
        evidenceDocs.Should().BeGreaterThanOrEqualTo(3);

        foreach (ContextDocumentRequest doc in request.Documents.Skip(1))
        {
            doc.Name.Should().NotBeNullOrWhiteSpace();
            doc.ContentType.Should().NotBeNullOrWhiteSpace();
            doc.Content.Should().NotBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(TemplateFactories))]
    public void Each_factory_round_trips_json_without_loss_of_required_fields(Func<string?, ArchitectureRequest> factory)
    {
        ArchitectureRequest original = factory("req-roundtrip-001");

        string json = JsonSerializer.Serialize(original, JsonRoundTripOptions);
        ArchitectureRequest? restored = JsonSerializer.Deserialize<ArchitectureRequest>(json, JsonRoundTripOptions);

        restored.Should().NotBeNull();
        restored.RequestId.Should().Be(original.RequestId);
        restored.Description.Should().Be(original.Description);
        restored.SystemName.Should().Be(original.SystemName);
        restored.Environment.Should().Be(original.Environment);
        restored.CloudProvider.Should().Be(original.CloudProvider);
        restored.Documents.Should().HaveSameCount(original.Documents);

        for (int i = 0; i < original.Documents.Count; i++)
        {
            restored.Documents[i].Name.Should().Be(original.Documents[i].Name);
            restored.Documents[i].ContentType.Should().Be(original.Documents[i].ContentType);
            restored.Documents[i].Content.Should().Be(original.Documents[i].Content);
        }
    }

    [SkippableFact]
    public void Summary_titles_are_non_empty_and_match_template_intent()
    {
        foreach (ArchitectureRequestTemplateSummary s in ArchitectureRequestTemplates.Summaries)
        {
            s.TemplateId.Should().NotBeNullOrWhiteSpace();
            s.Title.Should().NotBeNullOrWhiteSpace();
            s.ShortDescription.Should().NotBeNullOrWhiteSpace();
        }
    }
}
