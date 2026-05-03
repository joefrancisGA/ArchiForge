using System.Diagnostics;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

using Xunit.Abstractions;


namespace ArchLucid.Api.Tests.Performance;

/// <summary>
///     In-process performance gate for the pilot path: create â†’ seed fake results â†’ commit â†’ get manifest, using the
///     same simulator + in-memory storage profile as the fast integration suite (not production SQL latency).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Slow")]
public sealed class CorePilotFlowPerformanceTests(ArchLucidApiFactory factory, ITestOutputHelper output)
    : IntegrationTestBase(factory)
{
    private const int TotalCapMs = 10_000;
    private const int ManifestP95MaxMs = 500;
    private const int ManifestSamples = 10;

    [SkippableFact]
    public async Task CorePilotFlow_CompletesWithinTarget()
    {
        Stopwatch total = Stopwatch.StartNew();
        StepTiming tCreate = new("POST /v1/architecture/request");
        StepTiming tSeed = new("POST .../seed-fake-results");
        StepTiming tCommit = new("POST .../commit");
        StepTiming tGet = new("GET /v1/architecture/manifest/{version}");

        tCreate.Sw.Start();
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest($"REQ-PERF-E2E-{Guid.NewGuid():N}")));
        tCreate.Sw.Stop();
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        tSeed.Sw.Start();
        HttpResponseMessage seedResponse =
            await Client.PostAsync($"/v1/internal/architecture/runs/{runId}/seed-fake-results", null);
        tSeed.Sw.Stop();
        seedResponse.EnsureSuccessStatusCode();

        tCommit.Sw.Start();
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        tCommit.Sw.Stop();
        commitResponse.EnsureSuccessStatusCode();
        CommitRunResponseDto? commit =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string manifestVersion = commit!.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().NotBeNullOrWhiteSpace();

        tGet.Sw.Start();
        HttpResponseMessage manifestResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
        tGet.Sw.Stop();
        manifestResponse.EnsureSuccessStatusCode();
        total.Stop();
        output.WriteLine($"[perf] {tCreate}");
        output.WriteLine($"[perf] {tSeed}");
        output.WriteLine($"[perf] {tCommit}");
        output.WriteLine($"[perf] {tGet}");
        output.WriteLine($"[perf] total={total.ElapsedMilliseconds}ms (cap {TotalCapMs}ms, simulator+in-memory)");

        total.ElapsedMilliseconds.Should()
            .BeLessThan(TotalCapMs, "E2E pilot path should stay under the in-process regression cap.");
    }

    [SkippableFact]
    public async Task ManifestRetrieval_CompletesWithin500ms()
    {
        (string manifestVersion, _, _) = await CreateCommittedRunAsync();
        List<long> ms = new(ManifestSamples);
        for (int i = 0; i < ManifestSamples; i++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            HttpResponseMessage r = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}");
            sw.Stop();
            r.EnsureSuccessStatusCode();
            ms.Add(sw.ElapsedMilliseconds);
        }

        ms.Sort();
        // Nearest-rank p95: 1-based rank ceil(0.95 * n) â†’ 0-based index.
        int rank1Based = (int)Math.Ceiling(0.95 * ms.Count);
        int p95Index = Math.Max(0, Math.Min(ms.Count - 1, rank1Based - 1));
        long p95 = ms[p95Index];
        output.WriteLine(
            $"[perf] manifest GET x{ManifestSamples} ms=[{string.Join(", ", ms)}] p95={p95}ms (max {ManifestP95MaxMs}ms)");

        p95.Should()
            .BeLessThan(
                ManifestP95MaxMs,
                "repeated GET manifest in simulator/in-memory should stay well under the p95 cap.");
    }

    private async Task<(string ManifestVersion, string RunId, CommitRunResponseDto Commit)> CreateCommittedRunAsync()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest($"REQ-PERF-MANIFEST-{Guid.NewGuid():N}")));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage seedResponse =
            await Client.PostAsync($"/v1/internal/architecture/runs/{runId}/seed-fake-results", null);
        seedResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();
        CommitRunResponseDto? commit =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string manifestVersion = commit!.Manifest.Metadata.ManifestVersion;
        manifestVersion.Should().NotBeNullOrWhiteSpace();
        return (manifestVersion, runId, commit);
    }

    private readonly struct StepTiming
    {
        public StepTiming(string name)
        {
            Name = name;
            Sw = new Stopwatch();
        }

        private string Name
        {
            get;
        }

        public Stopwatch Sw
        {
            get;
        }

        public override string ToString() => $"{Name}={Sw.ElapsedMilliseconds}ms";
    }
}
