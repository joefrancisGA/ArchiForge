using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Governance Controller.
/// </summary>

[Trait("Category", "Integration")]
public sealed class GovernanceControllerTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    private async Task<string> CreateRunAsync(string requestId)
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));
        response.EnsureSuccessStatusCode();
        CreateRunResponseDto? payload = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        return payload!.Run.RunId;
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitApprovalRequest_ValidRun_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-SUBMIT-01");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            RequestComment = "Ready for test"
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceApprovalResponseDto? payload = await response.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.ApprovalRequestId.Should().NotBeNullOrWhiteSpace();
        payload.Status.Should().Be("Submitted");
        payload.RunId.Should().Be(runId);
    }

    [Fact]
    public async Task SubmitApprovalRequest_UnknownRun_Returns404()
    {
        var body = new
        {
            RunId = "run-does-not-exist",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitApprovalRequest_MissingBody_Returns400()
    {
        HttpResponseMessage response = await Client.PostAsync("/v1/governance/approval-requests", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_ValidRequest_ReturnsOkWithApprovedStatus()
    {
        string runId = await CreateRunAsync("REQ-GOV-APPROVE-01");

        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        var approveBody = new { ReviewedBy = "reviewer1", ReviewComment = "Approved" };
        HttpResponseMessage approveResponse = await Client.PostAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve",
            JsonContent(approveBody));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceApprovalResponseDto? result = await approveResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        result!.Status.Should().Be("Approved");
        result.ReviewedBy.Should().Be("reviewer1");
    }

    // ── Reject ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reject_ValidRequest_ReturnsOkWithRejectedStatus()
    {
        string runId = await CreateRunAsync("REQ-GOV-REJECT-01");

        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        var rejectBody = new { ReviewedBy = "reviewer2", ReviewComment = "Not ready" };
        HttpResponseMessage rejectResponse = await Client.PostAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/reject",
            JsonContent(rejectBody));

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceApprovalResponseDto? result = await rejectResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        result!.Status.Should().Be("Rejected");
    }

    // ── Promote ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Promote_ToProd_WithoutApproval_Returns400()
    {
        string runId = await CreateRunAsync("REQ-GOV-PROD-01");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "test",
            TargetEnvironment = "prod",
            PromotedBy = "alice"
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/governance/promotions", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Promote_ToProd_WithApprovedRequest_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-PROD-02");

        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "test",
            TargetEnvironment = "prod"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        var approveBody = new { ReviewedBy = "approver", ReviewComment = "Approved for prod" };
        HttpResponseMessage approveResponse = await Client.PostAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve",
            JsonContent(approveBody));
        approveResponse.EnsureSuccessStatusCode();

        var promoteBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "test",
            TargetEnvironment = "prod",
            PromotedBy = "alice",
            submitted.ApprovalRequestId
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/governance/promotions", JsonContent(promoteBody));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernancePromotionResponseDto? result = await response.Content.ReadFromJsonAsync<GovernancePromotionResponseDto>(JsonOptions);
        result.Should().NotBeNull();
        result.TargetEnvironment.Should().Be("prod");
        result.PromotedBy.Should().Be("alice");
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_ValidRun_ReturnsOkAndIsActive()
    {
        string runId = await CreateRunAsync("REQ-GOV-ACTIVATE-01");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            Environment = "dev"
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/governance/activations", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceActivationResponseDto? result = await response.Content.ReadFromJsonAsync<GovernanceActivationResponseDto>(JsonOptions);
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        result.RunId.Should().Be(runId);
        result.Environment.Should().Be("dev");
    }

    // ── List by run ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetApprovalRequests_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-01");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(body));
        submitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/approval-requests");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceApprovalResponseDto[]? items = await listResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => x.RunId == runId);
    }

    [Fact]
    public async Task GetPromotions_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-02");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            PromotedBy = "alice"
        };
        HttpResponseMessage promoteResponse = await Client.PostAsync("/v1/governance/promotions", JsonContent(body));
        promoteResponse.EnsureSuccessStatusCode();

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/promotions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernancePromotionResponseDto[]? items = await listResponse.Content.ReadFromJsonAsync<GovernancePromotionResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => x.RunId == runId);
    }

    [Fact]
    public async Task GetActivations_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-03");

        var body = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            Environment = "test"
        };
        HttpResponseMessage activateResponse = await Client.PostAsync("/v1/governance/activations", JsonContent(body));
        activateResponse.EnsureSuccessStatusCode();

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/activations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceActivationResponseDto[]? items = await listResponse.Content.ReadFromJsonAsync<GovernanceActivationResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => x.RunId == runId);
    }

    [Fact]
    public async Task GetApprovalRequestRationale_AfterSubmit_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-RATIONALE-01");
        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        HttpResponseMessage rationaleResponse = await Client.GetAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/rationale");

        rationaleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceRationaleResponseDto? payload = await rationaleResponse.Content.ReadFromJsonAsync<GovernanceRationaleResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.ApprovalRequestId.Should().Be(submitted.ApprovalRequestId);
        payload.Bullets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Approve_SecondApprove_ReturnsBadRequest()
    {
        string runId = await CreateRunAsync("REQ-GOV-APR2-01");
        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        var approveBody = new { ReviewedBy = "reviewer-dup", ReviewComment = "ok" };
        HttpResponseMessage first = await Client.PostAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve",
            JsonContent(approveBody));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseMessage second = await Client.PostAsync(
            $"/v1/governance/approval-requests/{submitted.ApprovalRequestId}/approve",
            JsonContent(approveBody));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approve_ParallelDuplicateApproves_FromSameReviewer_HasConsistentOutcome()
    {
        string runId = await CreateRunAsync("REQ-GOV-PAR-01");
        var submitBody = new
        {
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        };
        HttpResponseMessage submitResponse = await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted = await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        string url = $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve";
        var approveBody = new { ReviewedBy = "reviewer-par", ReviewComment = "parallel" };
        Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 5)
            .Select(_ => Client.PostAsync(url, JsonContent(approveBody)))
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(tasks);
        int okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        int badCount = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);
        okCount.Should().BeGreaterThanOrEqualTo(1);
        (okCount + badCount).Should().Be(5);
    }
}

public sealed class GovernanceRationaleResponseDto
{
    public int SchemaVersion { get; set; }

    public string ApprovalRequestId { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public List<string> Bullets { get; set; } = [];
}

public sealed class GovernanceApprovalResponseDto
{
    public string ApprovalRequestId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string? ReviewComment { get; set; }
}

public sealed class GovernancePromotionResponseDto
{
    public string PromotionRecordId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string PromotedBy { get; set; } = string.Empty;
    public string? ApprovalRequestId { get; set; }
}

public sealed class GovernanceActivationResponseDto
{
    public string ActivationId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
