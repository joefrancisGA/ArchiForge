using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TrialSmokeRunnerTests
{
    private const string BaseUrl = "https://staging.archlucid.test";
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string WorkspaceId = "22222222-2222-2222-2222-222222222222";
    private const string ProjectId = "33333333-3333-3333-3333-333333333333";
    private const string WelcomeRunId = "44444444-4444-4444-4444-444444444444";

    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task RunAsync_HappyPath_PassesAllThreeStepsAndPropagatesIds()
    {
        StubHandler handler = new();
        handler.OnRequest = async req =>
        {
            string path = req.RequestUri!.AbsolutePath;

            if (req.Method == HttpMethod.Post && path == "/v1/register")
            {
                JsonDocument doc = await ReadJsonAsync(req);
                doc.RootElement.GetProperty("organizationName").GetString().Should().Be("Acme");
                doc.RootElement.GetProperty("baselineReviewCycleHours").GetDecimal().Should().Be(16m);

                return JsonResponse(HttpStatusCode.Created,
                    new { tenantId = TenantId, defaultWorkspaceId = WorkspaceId, defaultProjectId = ProjectId });
            }

            if (req.Method == HttpMethod.Get && path == "/v1/tenant/trial-status")
            {
                req.Headers.GetValues("X-Tenant-Id").Should().ContainSingle().Which.Should().Be(TenantId);
                req.Headers.GetValues("X-Workspace-Id").Should().ContainSingle().Which.Should().Be(WorkspaceId);
                req.Headers.GetValues("X-Project-Id").Should().ContainSingle().Which.Should().Be(ProjectId);

                return JsonResponse(HttpStatusCode.OK, new
                {
                    status = "Active",
                    trialWelcomeRunId = WelcomeRunId,
                    baselineReviewCycleHours = 16m,
                    firstCommitUtc = "2026-04-15T12:00:00Z"
                });
            }

            if (req.Method == HttpMethod.Get && path == $"/v1/pilots/runs/{WelcomeRunId}/pilot-run-deltas")
            {
                return JsonResponse(HttpStatusCode.OK, new { timeToCommittedManifestTotalSeconds = 14_400.0 });
            }

            return JsonResponse(HttpStatusCode.NotFound, new { });
        };

        TrialSmokeReport report = await RunAsync(handler, new TrialSmokeCommandOptions
        {
            OrganizationName = "Acme",
            AdminEmail = "ops@example.com",
            AdminDisplayName = "Ops",
            BaselineReviewCycleHours = 16m
        });

        report.AllPassed.Should().BeTrue();
        report.Steps.Should().HaveCount(3);
        report.Steps.Select(s => s.Name).Should().ContainInOrder("register", "trial-status", "pilot-run-deltas");
        report.Steps.Should().OnlyContain(s => s.Passed);
        report.TenantId.Should().Be(TenantId);
        report.TrialWelcomeRunId.Should().Be(WelcomeRunId);
    }

    [Fact]
    public async Task RunAsync_RegisterFails_StopsAfterFirstStepWithFailureHint()
    {
        StubHandler handler = new()
        {
            OnRequest = req =>
                Task.FromResult(JsonResponse(HttpStatusCode.Conflict, new { error = "duplicate_slug" }))
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeFalse();
        report.Steps.Should().HaveCount(1);
        report.Steps[0].Name.Should().Be("register");
        report.Steps[0].Passed.Should().BeFalse();
        report.Steps[0].FailureHint.Should().Contain("TrialSignupAttempted");
    }

    [Fact]
    public async Task RunAsync_TrialStatusFails_StopsAfterSecondStepAndKeepsTenantId()
    {
        StubHandler handler = new();
        handler.OnRequest = req =>
        {
            string path = req.RequestUri!.AbsolutePath;

            if (req.Method == HttpMethod.Post && path == "/v1/register")
                return Task.FromResult(JsonResponse(HttpStatusCode.Created,
                    new { tenantId = TenantId, defaultWorkspaceId = WorkspaceId, defaultProjectId = ProjectId }));

            return Task.FromResult(JsonResponse(HttpStatusCode.InternalServerError, new { error = "boom" }));
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeFalse();
        report.Steps.Should().HaveCount(2);
        report.Steps[1].Name.Should().Be("trial-status");
        report.Steps[1].Passed.Should().BeFalse();
        report.TenantId.Should().Be(TenantId);
        report.TrialWelcomeRunId.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_SkipPilotRunDeltasFlag_StopsAfterTrialStatus()
    {
        StubHandler handler = new();
        handler.OnRequest = req =>
        {
            string path = req.RequestUri!.AbsolutePath;

            if (req.Method == HttpMethod.Post && path == "/v1/register")
                return Task.FromResult(JsonResponse(HttpStatusCode.Created,
                    new { tenantId = TenantId, defaultWorkspaceId = WorkspaceId, defaultProjectId = ProjectId }));

            if (req.Method == HttpMethod.Get && path == "/v1/tenant/trial-status")
                return Task.FromResult(JsonResponse(HttpStatusCode.OK,
                    new { status = "Active", trialWelcomeRunId = WelcomeRunId }));

            throw new InvalidOperationException($"Unexpected request to {path} after --skip-pilot-run-deltas.");
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions
            {
                OrganizationName = "Acme", AdminEmail = "ops@example.com", SkipPilotRunDeltas = true
            });

        report.AllPassed.Should().BeTrue();
        report.Steps.Should().HaveCount(2);
        report.TrialWelcomeRunId.Should().Be(WelcomeRunId);
    }

    [Fact]
    public async Task RunAsync_TrialStatusWithoutWelcomeRun_SkipsPilotRunDeltasGracefully()
    {
        StubHandler handler = new();
        handler.OnRequest = req =>
        {
            string path = req.RequestUri!.AbsolutePath;

            if (req.Method == HttpMethod.Post && path == "/v1/register")
                return Task.FromResult(JsonResponse(HttpStatusCode.Created,
                    new { tenantId = TenantId, defaultWorkspaceId = WorkspaceId, defaultProjectId = ProjectId }));

            if (req.Method == HttpMethod.Get && path == "/v1/tenant/trial-status")
                return Task.FromResult(JsonResponse(HttpStatusCode.OK,
                    new { status = "Active", trialWelcomeRunId = (string?)null }));

            throw new InvalidOperationException($"Unexpected request to {path} when no welcome run is set.");
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeTrue();
        report.Steps.Should().HaveCount(2);
        report.TrialWelcomeRunId.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_HappyPath_PropagatesXCorrelationIdFromRegisterResponse()
    {
        const string CorrelationId = "abcd-1234-correlation";
        StubHandler handler = new();
        handler.OnRequest = req =>
        {
            string path = req.RequestUri!.AbsolutePath;

            if (req.Method == HttpMethod.Post && path == "/v1/register")
            {
                HttpResponseMessage created = JsonResponse(HttpStatusCode.Created,
                    new { tenantId = TenantId, defaultWorkspaceId = WorkspaceId, defaultProjectId = ProjectId });
                created.Headers.Add("X-Correlation-ID", CorrelationId);
                return Task.FromResult(created);
            }

            if (req.Method == HttpMethod.Get && path == "/v1/tenant/trial-status")
                return Task.FromResult(JsonResponse(HttpStatusCode.OK,
                    new { status = "Active", trialWelcomeRunId = (string?)null }));

            throw new InvalidOperationException($"Unexpected request to {path}.");
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeTrue();
        report.RegistrationCorrelationId.Should().Be(CorrelationId);
    }

    [Fact]
    public async Task RunAsync_RegisterFailsWithCorrelationHeader_StillReportsCorrelationId()
    {
        const string CorrelationId = "fail-trace-9999";
        StubHandler handler = new()
        {
            OnRequest = req =>
            {
                HttpResponseMessage conflict = JsonResponse(HttpStatusCode.Conflict, new { error = "duplicate_slug" });
                conflict.Headers.Add("X-Correlation-ID", CorrelationId);
                return Task.FromResult(conflict);
            }
        };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeFalse();
        report.RegistrationCorrelationId.Should().Be(CorrelationId);
    }

    [Fact]
    public async Task RunAsync_RegisterThrows_RecordsFailureWithoutCrashing()
    {
        StubHandler handler = new() { OnRequest = _ => throw new HttpRequestException("connection refused") };

        TrialSmokeReport report = await RunAsync(handler,
            new TrialSmokeCommandOptions { OrganizationName = "Acme", AdminEmail = "ops@example.com" });

        report.AllPassed.Should().BeFalse();
        report.Steps.Should().HaveCount(1);
        report.Steps[0].Name.Should().Be("register");
        report.Steps[0].Detail.Should().Contain("connection refused");
    }

    private static async Task<TrialSmokeReport> RunAsync(StubHandler handler, TrialSmokeCommandOptions options)
    {
        using HttpClient http = new(handler) { BaseAddress = new Uri(BaseUrl + "/") };
        TrialSmokeRunner runner = new(http);

        return await runner.RunAsync(options);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) =>
        new(status) { Content = JsonContent.Create(body, options: JsonCamel) };

    private static async Task<JsonDocument> ReadJsonAsync(HttpRequestMessage req)
    {
        string raw = await req.Content!.ReadAsStringAsync();

        return JsonDocument.Parse(raw);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> OnRequest
        {
            get;
            set;
        } =
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            OnRequest(request);
    }
}
