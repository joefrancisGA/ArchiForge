using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureExportAuditTests : IntegrationTestBase
{
    public ArchitectureExportAuditTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ConsultingDocxExport_PersistsExportHistory()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-EXPORT-AUDIT-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var request = new
        {
            templateProfile = (string?)null,
            audience = "Executives and sponsors",
            externalDelivery = true,
            executiveFriendly = true,
            regulatedEnvironment = false,
            needDetailedEvidence = false,
            needExecutionTraces = false,
            needDeterminismOrCompareAppendices = false,
            includeEvidence = true,
            includeExecutionTraces = true,
            includeManifest = true,
            includeDiagram = true,
            includeSummary = true,
            includeDeterminismCheck = false,
            determinismIterations = 3,
            includeManifestCompare = false,
            compareManifestVersion = (string?)null,
            includeAgentResultCompare = false,
            compareRunId = (string?)null
        };

        var exportResponse = await Client.PostAsJsonAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export/docx/consulting",
            request);

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/exports");

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await historyResponse.Content.ReadFromJsonAsync<RunExportHistoryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Exports.Should().NotBeEmpty();

        var export = payload.Exports.First();
        export.RunId.Should().Be(runId);
        export.ExportType.Should().Be("analysis-report-consulting-docx");
        export.Format.Should().Be("docx");
        export.TemplateProfile.Should().NotBeNullOrWhiteSpace();
        export.WasAutoSelected.Should().BeTrue();
    }
}

