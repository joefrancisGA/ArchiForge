using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Executes the <c>archlucid trial smoke</c> happy path against an HTTP API. Pure HTTP — no docker, no SQL,
///     no NSwag client coupling — so it can run against staging in Stripe TEST mode and be unit-tested with a
///     <see cref="HttpMessageHandler" /> mock.
/// </summary>
public sealed class TrialSmokeRunner(HttpClient http)
{
    /// <summary>Canonical correlation header emitted by <c>CorrelationIdMiddleware</c> on every API response.</summary>
    private const string CorrelationHeaderName = "X-Correlation-ID";

    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));

    public async Task<TrialSmokeReport> RunAsync(TrialSmokeCommandOptions options, CancellationToken ct = default)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        List<TrialSmokeStepResult> steps = [];

        (TrialSmokeStepResult registerStep, TrialSmokeRegisterResponse? registerResponse, string? correlationId) =
            await RegisterAsync(options, ct);
        steps.Add(registerStep);

        if (!registerStep.Passed || registerResponse is null)
            return new TrialSmokeReport { Steps = steps, AllPassed = false, RegistrationCorrelationId = correlationId };

        (TrialSmokeStepResult statusStep, TrialSmokeTrialStatusResponse? statusResponse) =
            await TrialStatusAsync(registerResponse, ct);
        steps.Add(statusStep);

        if (!statusStep.Passed || statusResponse is null)
            return new TrialSmokeReport
            {
                Steps = steps,
                AllPassed = false,
                TenantId = registerResponse.TenantId,
                RegistrationCorrelationId = correlationId
            };

        if (options.SkipPilotRunDeltas || string.IsNullOrWhiteSpace(statusResponse.TrialWelcomeRunId))
            return new TrialSmokeReport
            {
                Steps = steps,
                AllPassed = steps.All(s => s.Passed),
                TenantId = registerResponse.TenantId,
                TrialWelcomeRunId = statusResponse.TrialWelcomeRunId,
                RegistrationCorrelationId = correlationId
            };

        TrialSmokeStepResult deltasStep =
            await PilotRunDeltasAsync(registerResponse, statusResponse.TrialWelcomeRunId!, ct);
        steps.Add(deltasStep);

        return new TrialSmokeReport
        {
            Steps = steps,
            AllPassed = steps.All(s => s.Passed),
            TenantId = registerResponse.TenantId,
            TrialWelcomeRunId = statusResponse.TrialWelcomeRunId,
            RegistrationCorrelationId = correlationId
        };
    }

    private async Task<(TrialSmokeStepResult Step, TrialSmokeRegisterResponse? Body, string? CorrelationId)>
        RegisterAsync(TrialSmokeCommandOptions options, CancellationToken ct)
    {
        const string name = "register";
        const string hint = "Look for TrialSignupAttempted / TrialSignupFailed in dbo.AuditEvents.";

        TrialSmokeRegisterRequest payload = new()
        {
            OrganizationName = options.OrganizationName,
            AdminEmail = options.AdminEmail,
            AdminDisplayName = options.AdminDisplayName,
            BaselineReviewCycleHours = options.BaselineReviewCycleHours,
            BaselineReviewCycleSource = options.BaselineReviewCycleSource
        };

        try
        {
            using HttpResponseMessage res = await _http.PostAsJsonAsync("/v1/register", payload, JsonCamel, ct);
            string? correlationId = ReadCorrelationId(res);

            if (res.StatusCode != HttpStatusCode.Created)
            {
                string body = await res.Content.ReadAsStringAsync(ct);

                return (
                    new TrialSmokeStepResult
                    {
                        Name = name,
                        Passed = false,
                        Detail = $"POST /v1/register returned {(int)res.StatusCode}. Body: {Truncate(body, 240)}",
                        FailureHint = hint
                    }, null, correlationId);
            }

            TrialSmokeRegisterResponse? body200 =
                await res.Content.ReadFromJsonAsync<TrialSmokeRegisterResponse>(JsonCamel, ct);

            if (body200 is null || string.IsNullOrWhiteSpace(body200.TenantId))
                return (
                    new TrialSmokeStepResult
                    {
                        Name = name,
                        Passed = false,
                        Detail = "POST /v1/register returned 201 but the response body did not contain a tenantId.",
                        FailureHint = hint
                    }, null, correlationId);

            return (
                new TrialSmokeStepResult
                {
                    Name = name, Passed = true, Detail = $"POST /v1/register → 201 (tenantId={body200.TenantId})."
                }, body200, correlationId);
        }
        catch (Exception ex)
        {
            return (
                new TrialSmokeStepResult
                {
                    Name = name,
                    Passed = false,
                    Detail = $"POST /v1/register threw: {ex.GetType().Name}: {ex.Message}",
                    FailureHint = hint
                }, null, null);
        }
    }

    private static string? ReadCorrelationId(HttpResponseMessage res)
    {
        return res.Headers.TryGetValues(CorrelationHeaderName, out IEnumerable<string>? values)
            ? (from v in values where !string.IsNullOrWhiteSpace(v) select v.Trim()).FirstOrDefault()
            : null;
    }

    private async Task<(TrialSmokeStepResult Step, TrialSmokeTrialStatusResponse? Body)> TrialStatusAsync(
        TrialSmokeRegisterResponse register,
        CancellationToken ct)
    {
        const string name = "trial-status";
        const string hint = "Look for TrialProvisioned in dbo.AuditEvents and confirm the tenant row in dbo.Tenants.";

        try
        {
            using HttpRequestMessage req = new(HttpMethod.Get, "/v1/tenant/trial-status");
            ApplyRegistrationScope(req, register);

            using HttpResponseMessage res = await _http.SendAsync(req, ct);

            if (res.StatusCode != HttpStatusCode.OK)
            {
                string body = await res.Content.ReadAsStringAsync(ct);

                return (
                    new TrialSmokeStepResult
                    {
                        Name = name,
                        Passed = false,
                        Detail =
                            $"GET /v1/tenant/trial-status returned {(int)res.StatusCode}. Body: {Truncate(body, 240)}",
                        FailureHint = hint
                    }, null);
            }

            TrialSmokeTrialStatusResponse? body200 =
                await res.Content.ReadFromJsonAsync<TrialSmokeTrialStatusResponse>(JsonCamel, ct);

            if (body200 is null)
                return (
                    new TrialSmokeStepResult
                    {
                        Name = name,
                        Passed = false,
                        Detail = "GET /v1/tenant/trial-status returned 200 with an empty/invalid JSON body.",
                        FailureHint = hint
                    }, null);

            return (
                new TrialSmokeStepResult
                {
                    Name = name,
                    Passed = true,
                    Detail =
                        $"GET /v1/tenant/trial-status → 200 (status={body200.Status}, welcomeRunId={body200.TrialWelcomeRunId ?? "<none>"})."
                }, body200);
        }
        catch (Exception ex)
        {
            return (
                new TrialSmokeStepResult
                {
                    Name = name,
                    Passed = false,
                    Detail = $"GET /v1/tenant/trial-status threw: {ex.GetType().Name}: {ex.Message}",
                    FailureHint = hint
                }, null);
        }
    }

    private async Task<TrialSmokeStepResult> PilotRunDeltasAsync(
        TrialSmokeRegisterResponse register,
        string trialWelcomeRunId,
        CancellationToken ct)
    {
        const string name = "pilot-run-deltas";
        const string hint =
            "Look for Run.CommitCompleted in dbo.AuditEvents.";

        try
        {
            string path = $"/v1/pilots/runs/{Uri.EscapeDataString(trialWelcomeRunId)}/pilot-run-deltas";
            using HttpRequestMessage req = new(HttpMethod.Get, path);
            ApplyRegistrationScope(req, register);

            using HttpResponseMessage res = await _http.SendAsync(req, ct);

            if (res.StatusCode != HttpStatusCode.OK)
            {
                string body = await res.Content.ReadAsStringAsync(ct);

                return new TrialSmokeStepResult
                {
                    Name = name,
                    Passed = false,
                    Detail = $"GET {path} returned {(int)res.StatusCode}. Body: {Truncate(body, 240)}",
                    FailureHint = hint
                };
            }

            TrialSmokePilotRunDeltasShape? body200 =
                await res.Content.ReadFromJsonAsync<TrialSmokePilotRunDeltasShape>(JsonCamel, ct);
            string seconds = body200?.TimeToCommittedManifestTotalSeconds is { } s
                ? s.ToString("0.##", CultureInfo.InvariantCulture)
                : "<null>";

            return new TrialSmokeStepResult
            {
                Name = name,
                Passed = true,
                Detail = $"GET {path} → 200 (timeToCommittedManifestTotalSeconds={seconds})."
            };
        }
        catch (Exception ex)
        {
            return new TrialSmokeStepResult
            {
                Name = name,
                Passed = false,
                Detail = $"GET pilot-run-deltas threw: {ex.GetType().Name}: {ex.Message}",
                FailureHint = hint
            };
        }
    }

    private static void ApplyRegistrationScope(HttpRequestMessage req, TrialSmokeRegisterResponse register)
    {
        if (!string.IsNullOrWhiteSpace(register.TenantId))
            req.Headers.TryAddWithoutValidation("X-Tenant-Id", register.TenantId);

        if (!string.IsNullOrWhiteSpace(register.DefaultWorkspaceId))
            req.Headers.TryAddWithoutValidation("X-Workspace-Id", register.DefaultWorkspaceId);

        if (!string.IsNullOrWhiteSpace(register.DefaultProjectId))
            req.Headers.TryAddWithoutValidation("X-Project-Id", register.DefaultProjectId);
    }

    private static string Truncate(string s, int max)
    {
        return string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
    }
}
