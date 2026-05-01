using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.Core.GoldenCorpus;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     End-to-end simulator path vs locked <c>cohort.json</c> expectations. Skips unless
///     <c>ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED=true</c> and SHAs in the cohort file are non-placeholder.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
[Trait("Suite", "GoldenCohort")]
public sealed class GoldenCohortSimulatorDriftTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task Simulator_path_matches_locked_baseline_when_enabled()
    {
        Skip.IfNot(
            string.Equals(
                Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED"),
                "true",
                StringComparison.OrdinalIgnoreCase));

        string cohortPath = Path.Combine(AppContext.BaseDirectory, "golden-cohort", "cohort.json");
        Assert.True(File.Exists(cohortPath),
            $"Missing {cohortPath} (link tests/golden-cohort/cohort.json in project).");

        GoldenCohortDocument document = GoldenCohortDocument.Load(cohortPath);
        bool hasPlaceholder = document.Items.Any(static item =>
            string.Equals(
                item.ExpectedCommittedManifestSha256.Trim(),
                GoldenCohortBaselineConstants.UnlockedManifestSha256Placeholder,
                StringComparison.OrdinalIgnoreCase));

        if (hasPlaceholder)
        {
            Assert.Fail(
                "ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED is true but cohort.json still contains SHA placeholders. "
                + "Clear the repository variable or run `archlucid golden-cohort lock-baseline --write` after owner approval (PENDING_QUESTIONS item 33).");
        }

        int cap = document.Items.Count;
        string? capRaw = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_DRIFT_ITEM_CAP");

        if (int.TryParse(capRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCap) && parsedCap > 0
            && parsedCap < cap)
            cap = parsedCap;

        List<GoldenCohortDriftRow> rows = [];

        for (int i = 0; i < cap; i++)
        {
            GoldenCohortItem item = document.Items[i];

            HttpResponseMessage createResponse = await Client.PostAsync(
                "/v1/architecture/request",
                JsonContent(GoldenCohortArchitectureRequestFactory.FromCohortItem(item)));

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            CreateRunResponseDto? created =
                await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
            created.Should().NotBeNull();

            string runId = created.Run.RunId;

            HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
            executeResponse.EnsureSuccessStatusCode();

            HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
            commitResponse.EnsureSuccessStatusCode();

            string commitJson = await commitResponse.Content.ReadAsStringAsync();
            using JsonDocument commitDoc = JsonDocument.Parse(commitJson);
            JsonElement manifestElement = commitDoc.RootElement.GetProperty("manifest");
            GoldenManifest? manifest = manifestElement.Deserialize<GoldenManifest>(JsonOptions);

            manifest.Should().NotBeNull();

            string actualSha = GoldenManifestFingerprint.ComputeSha256Hex(manifest);
            string expectedSha = item.ExpectedCommittedManifestSha256.Trim();
            bool shaMatches = string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase);

            HttpResponseMessage getRunResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
            getRunResponse.EnsureSuccessStatusCode();

            GetRunResponseDto? runPayload =
                await getRunResponse.Content.ReadFromJsonAsync<GetRunResponseDto>(JsonOptions);
            runPayload.Should().NotBeNull();

            string resultsJson = JsonSerializer.Serialize(runPayload.Results, JsonOptions);
            List<AgentResult>? typedResults = JsonSerializer.Deserialize<List<AgentResult>>(resultsJson, JsonOptions);

            typedResults.Should().NotBeNull();

            SortedSet<string> actualCategories =
                GoldenCohortFindingCategoryAggregator.DistinctCategories(typedResults);
            SortedSet<string> expectedCategories = new(StringComparer.Ordinal);

            foreach (var c in item.ExpectedFindingCategories.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                expectedCategories.Add(c.Trim());
            }

            bool categoryMatches = actualCategories.SetEquals(expectedCategories);

            rows.Add(
                new GoldenCohortDriftRow(
                    item.Id,
                    expectedSha,
                    actualSha,
                    shaMatches,
                    string.Join(", ", expectedCategories),
                    string.Join(", ", actualCategories),
                    categoryMatches));
        }

        string preamble =
            "Simulator path: POST /v1/architecture/request â†’ execute â†’ commit; SHA from `GoldenManifestFingerprint`; "
            + "categories from distinct finding categories across agent results after execute.";

        string markdown = GoldenCohortDriftMarkdown.BuildReport(DateTimeOffset.UtcNow, rows, preamble);

        string? reportRoot = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_DRIFT_REPORT_ROOT");

        if (!string.IsNullOrWhiteSpace(reportRoot))
        {
            Directory.CreateDirectory(reportRoot);
            string latestPath = Path.Combine(reportRoot, "golden-cohort-drift-latest.md");
            await File.WriteAllTextAsync(latestPath, markdown);
        }

        rows.Should().OnlyContain(static r => r.ShaMatches && r.CategoryMatches, markdown);
    }
}
