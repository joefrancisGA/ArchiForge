using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureExportRecordDiffTests : IntegrationTestBase
{
    public ArchitectureExportRecordDiffTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareExportRecords_ReturnsDifferencesBetweenTwoExports()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-EXPORT-DIFF-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var executiveRequest = new
        {
            templateProfile = "executive",
            audience = "Executives",
            externalDelivery = true,
            executiveFriendly = true,
            regulatedEnvironment = false,
            needDetailedEvidence = false,
            needExecutionTraces = false,
            needDeterminismOrCompareAppendices = false,
            includeEvidence = true,
            includeExecutionTraces = false,
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

        var internalRequest = new
        {
            templateProfile = "internal",
            audience = "Internal architects",
            externalDelivery = false,
            executiveFriendly = false,
            regulatedEnvironment = false,
            needDetailedEvidence = true,
            needExecutionTraces = true,
            needDeterminismOrCompareAppendices = true,
            includeEvidence = true,
            includeExecutionTraces = true,
            includeManifest = true,
            includeDiagram = true,
            includeSummary = true,
            includeDeterminismCheck = true,
            determinismIterations = 3,
            includeManifestCompare = false,
            compareManifestVersion = (string?)null,
            includeAgentResultCompare = false,
            compareRunId = (string?)null
        };

        await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export/docx/consulting",
            JsonContent(executiveRequest));

        await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export/docx/consulting",
            JsonContent(internalRequest));

        var historyResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/exports");
        historyResponse.EnsureSuccessStatusCode();

        var history = await historyResponse.Content.ReadFromJsonAsync<RunExportHistoryResponse>(JsonOptions);
        history.Should().NotBeNull();
        history!.Exports.Should().HaveCountGreaterThanOrEqualTo(2);

        var leftId = history.Exports[0].ExportRecordId;
        var rightId = history.Exports[1].ExportRecordId;

        var compareResponse = await Client.GetAsync(
            $"/v1/architecture/run/exports/compare?leftExportRecordId={leftId}&rightExportRecordId={rightId}");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await compareResponse.Content.ReadFromJsonAsync<ExportRecordDiffResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Diff.ChangedTopLevelFields.Should().NotBeEmpty();
    }
}

