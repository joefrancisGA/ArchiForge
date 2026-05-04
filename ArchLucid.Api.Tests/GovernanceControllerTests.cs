using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Governance Controller.
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

        payload.Should().NotBeNull();
        payload!.Run.Should().NotBeNull();
        payload.Run!.RunId.Should().NotBeNullOrWhiteSpace(
            "CreateRun response must include Run.RunId so downstream governance calls validate.");

        return payload.Run.RunId;
    }

    /// <summary>
    ///     Governance payloads echo authority run identifiers; formatting may differ from create-run responses while still
    ///     referring to the same run GUID.
    /// </summary>
    private static bool SameArchitectureRunId(string left, string right)
    {
        if (Guid.TryParse(left, out Guid leftGuid) && Guid.TryParse(right, out Guid rightGuid))
            return leftGuid == rightGuid;

        return string.Equals(left, right, StringComparison.Ordinal);
    }

    private static void AssertSameArchitectureRunId(string expectedFromCreateFlow, string actualFromGovernancePayload)
    {
        SameArchitectureRunId(expectedFromCreateFlow, actualFromGovernancePayload).Should().BeTrue(
            $"Expected governance payloads to reference run '{expectedFromCreateFlow}', but saw '{actualFromGovernancePayload}'.");
    }

    // â”€â”€ Submit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task SubmitApprovalRequest_ValidRun_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-SUBMIT-01");

        HttpResponseMessage response =
            await PostGovernanceApprovalRequestAsync(
                runId,
                requestComment: "Ready for test",
                testActorName: GovernanceSubmitterName,
                testActorId: GovernanceSubmitterId);

        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        GovernanceApprovalResponseDto? payload =
            JsonSerializer.Deserialize<GovernanceApprovalResponseDto>(body, JsonOptions);
        payload.Should().NotBeNull();
        payload!.ApprovalRequestId.Should().NotBeNullOrWhiteSpace();
        payload.Status.Should().Be("Submitted");
        payload.RequestedBy.Should().Be(GovernanceSubmitterName);
        AssertSameArchitectureRunId(runId, payload.RunId);
    }

    [SkippableFact]
    public async Task SubmitApprovalRequest_UnknownRun_Returns404()
    {
        // Canonical run identifiers are GUIDs (see RunDetailQueryService.TryParseRunGuid). Use a random GUID so we hit
        // the repository miss path, not only the malformed-id path that returns null before lookup.
        string unknownRunId = Guid.NewGuid().ToString("N");

        HttpResponseMessage response = await PostGovernanceApprovalRequestAsync(unknownRunId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task SubmitApprovalRequest_MissingBody_Returns400()
    {
        HttpResponseMessage response = await Client.PostAsync("/v1/governance/approval-requests", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // â”€â”€ Approve â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task Approve_ValidRequest_ReturnsOkWithApprovedStatus()
    {
        string runId = await CreateRunAsync("REQ-GOV-APPROVE-01");

        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        HttpResponseMessage approveResponse = await PostJsonAsTestActorAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve",
            GovernanceReviewDecisionJsonContent("reviewer1", "Approved"),
            "reviewer1",
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceApprovalResponseDto? result =
            await approveResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        result!.Status.Should().Be("Approved");
        result.ReviewedBy.Should().Be("reviewer1");
    }

    // â”€â”€ Reject â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task Reject_ValidRequest_ReturnsOkWithRejectedStatus()
    {
        string runId = await CreateRunAsync("REQ-GOV-REJECT-01");

        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        HttpResponseMessage rejectResponse = await PostJsonAsTestActorAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/reject",
            GovernanceReviewDecisionJsonContent("reviewer2", "Not ready"),
            "reviewer2",
            "cccccccc-cccc-cccc-cccc-cccccccccccc");

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceApprovalResponseDto? result =
            await rejectResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        result!.Status.Should().Be("Rejected");
    }

    [SkippableFact]
    public async Task Reject_SecondReject_ReturnsConflict()
    {
        string runId = await CreateRunAsync("REQ-GOV-REJ2-01");
        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        string url = $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/reject";
        HttpResponseMessage first = await PostJsonAsTestActorAsync(
            url,
            GovernanceReviewDecisionJsonContent("reviewer-rej2", "no"),
            "reviewer-rej2",
            "99999999-9999-9999-9999-999999999999");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseMessage second = await PostJsonAsTestActorAsync(
            url,
            GovernanceReviewDecisionJsonContent("reviewer-rej2", "no"),
            "reviewer-rej2",
            "99999999-9999-9999-9999-999999999999");
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // â”€â”€ Promote â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task Promote_ToProd_WithoutApproval_Returns400()
    {
        string runId = await CreateRunAsync("REQ-GOV-PROD-01");

        HttpResponseMessage response = await PostGovernancePromotionAsync(
            runId,
            promotedBy: "alice",
            sourceEnvironment: "test",
            targetEnvironment: "prod");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Promote_ToProd_WithApprovedRequest_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-PROD-02");

        // Same reviewer identity shape as Approve_ValidRequest_ReturnsOkWithApprovedStatus (stable vs DevelopmentBypass defaults on CI).
        const string prodReviewerName = "reviewer1";
        const string prodReviewerOid = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        HttpResponseMessage submitResponse =
            await PostGovernanceApprovalRequestAsync(
                runId,
                sourceEnvironment: "test",
                targetEnvironment: "prod",
                testActorName: GovernanceSubmitterName,
                testActorId: GovernanceSubmitterId);

        string submitBody = await submitResponse.Content.ReadAsStringAsync();

        submitResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Submit failed {(int)submitResponse.StatusCode}: {submitBody}");

        GovernanceApprovalResponseDto? submitted =
            JsonSerializer.Deserialize<GovernanceApprovalResponseDto>(submitBody, JsonOptions);

        submitted.Should().NotBeNull();
        submitted!.RunId.Should().NotBeNullOrWhiteSpace();
        submitted.ApprovalRequestId.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage approveResponse = await PostJsonAsTestActorAsync(
            $"/v1/governance/approval-requests/{submitted.ApprovalRequestId}/approve",
            GovernanceReviewDecisionJsonContent(prodReviewerName, "Approved for prod"),
            prodReviewerName,
            prodReviewerOid);

        string approveBody = await approveResponse.Content.ReadAsStringAsync();

        approveResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Approve failed {(int)approveResponse.StatusCode}: {approveBody}");

        HttpResponseMessage response = await PostGovernancePromotionAsync(
            submitted.RunId,
            promotedBy: prodReviewerName,
            sourceEnvironment: "test",
            targetEnvironment: "prod",
            approvalRequestId: submitted.ApprovalRequestId,
            testActorName: prodReviewerName,
            testActorId: prodReviewerOid);

        string promoteBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, promoteBody);

        GovernancePromotionResponseDto? result =
            JsonSerializer.Deserialize<GovernancePromotionResponseDto>(promoteBody, JsonOptions);
        result.Should().NotBeNull();
        result.TargetEnvironment.Should().Be("prod");
        result.PromotedBy.Should().Be(prodReviewerName);
    }

    // â”€â”€ Activate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task Activate_ValidRun_ReturnsOkAndIsActive()
    {
        string runId = await CreateRunAsync("REQ-GOV-ACTIVATE-01");

        HttpResponseMessage response =
            await PostGovernanceActivationAsync(
                runId,
                manifestVersion: "v1",
                environment: "dev",
                testActorName: GovernanceSubmitterName,
                testActorId: GovernanceSubmitterId);

        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        GovernanceActivationResponseDto? result =
            JsonSerializer.Deserialize<GovernanceActivationResponseDto>(body, JsonOptions);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        AssertSameArchitectureRunId(runId, result.RunId);
        result.Environment.Should().Be("dev");
    }

    // â”€â”€ List by run â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [SkippableFact]
    public async Task GetApprovalRequests_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-01");

        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/approval-requests");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceApprovalResponseDto[]? items =
            await listResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => SameArchitectureRunId(x.RunId, runId));
    }

    [SkippableFact]
    public async Task GetPromotions_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-02");

        // FluentValidation requires PromotedBy on the body; Promote persists promotedBy from GetActor() (default "Developer").
        HttpResponseMessage promoteResponse =
            await PostGovernancePromotionAsync(runId, promotedBy: "Developer");

        string promoteBody = await promoteResponse.Content.ReadAsStringAsync();

        promoteResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Promote POST failed with {(int)promoteResponse.StatusCode}: {promoteBody}");

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/promotions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernancePromotionResponseDto[]? items =
            await listResponse.Content.ReadFromJsonAsync<GovernancePromotionResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => SameArchitectureRunId(x.RunId, runId));
    }

    [SkippableFact]
    public async Task GetActivations_ByRunId_ReturnsRows()
    {
        string runId = await CreateRunAsync("REQ-GOV-LIST-03");

        HttpResponseMessage activateResponse =
            await PostGovernanceActivationAsync(
                runId,
                manifestVersion: "v1",
                environment: "test",
                testActorName: GovernanceSubmitterName,
                testActorId: GovernanceSubmitterId);

        string activateBody = await activateResponse.Content.ReadAsStringAsync();

        activateResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Activate POST failed with {(int)activateResponse.StatusCode}: {activateBody}");

        HttpResponseMessage listResponse = await Client.GetAsync($"/v1/governance/runs/{runId}/activations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        GovernanceActivationResponseDto[]? items =
            await listResponse.Content.ReadFromJsonAsync<GovernanceActivationResponseDto[]>(JsonOptions);
        items.Should().NotBeNullOrEmpty();
        items.Should().Contain(x => SameArchitectureRunId(x.RunId, runId));
    }

    [SkippableFact]
    public async Task GetApprovalRequestRationale_AfterSubmit_ReturnsOk()
    {
        string runId = await CreateRunAsync("REQ-GOV-RATIONALE-01");
        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        HttpResponseMessage rationaleResponse = await Client.GetAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/rationale");

        rationaleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GovernanceRationaleResponseDto? payload =
            await rationaleResponse.Content.ReadFromJsonAsync<GovernanceRationaleResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.ApprovalRequestId.Should().Be(submitted.ApprovalRequestId);
        payload.Bullets.Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task Approve_SecondApprove_ReturnsConflict()
    {
        string runId = await CreateRunAsync("REQ-GOV-APR2-01");
        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(
            runId,
            testActorName: GovernanceSubmitterName,
            testActorId: GovernanceSubmitterId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);
        HttpResponseMessage first = await PostJsonAsTestActorAsync(
            $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve",
            GovernanceReviewDecisionJsonContent("reviewer-dup", "ok"),
            "reviewer-dup",
            "dddddddd-dddd-dddd-dddd-dddddddddddd");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseMessage second = await PostJsonAsTestActorAsync(
            $"/v1/governance/approval-requests/{submitted.ApprovalRequestId}/approve",
            GovernanceReviewDecisionJsonContent("reviewer-dup", "ok"),
            "reviewer-dup",
            "dddddddd-dddd-dddd-dddd-dddddddddddd");
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Approve_ParallelDuplicateApproves_FromSameReviewer_HasConsistentOutcome()
    {
        string runId = await CreateRunAsync("REQ-GOV-PAR-01");
        // Default DevelopmentBypass submitter (same pattern as GovernanceApprovalConcurrencyIntegrationTests).
        HttpResponseMessage submitResponse = await PostGovernanceApprovalRequestAsync(runId);
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        submitted.Should().NotBeNull();
        submitted!.ApprovalRequestId.Should().NotBeNullOrWhiteSpace();

        string url = $"/v1/governance/approval-requests/{submitted.ApprovalRequestId}/approve";
        Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 5)
            .Select(_ =>
                PostJsonAsTestActorAsync(url, GovernanceReviewDecisionJsonContent("reviewer-par", "parallel"),
                    "reviewer-par",
                    "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        try
        {
            int okCount = responses.Count(static r => r.StatusCode == HttpStatusCode.OK);
            int badCount = responses.Count(static r => r.StatusCode == HttpStatusCode.BadRequest);
            int conflictCount = responses.Count(static r => r.StatusCode == HttpStatusCode.Conflict);
            int rateLimitedCount = responses.Count(static r => r.StatusCode == HttpStatusCode.TooManyRequests);
            int serverErrorCount = responses.Count(static r => r.StatusCode == HttpStatusCode.InternalServerError);

            okCount.Should().BeGreaterThanOrEqualTo(1);
            (okCount + badCount + conflictCount + rateLimitedCount + serverErrorCount).Should().Be(5);
        }
        finally
        {

            foreach (HttpResponseMessage response in responses)
                response.Dispose();
        }
    }
}

public sealed class GovernanceRationaleResponseDto
{
    public int SchemaVersion
    {
        get;
        set;
    }

    public string ApprovalRequestId
    {
        get;
        set;
    } = string.Empty;

    public string Summary
    {
        get;
        set;
    } = string.Empty;

    public List<string> Bullets
    {
        get;
        set;
    } = [];
}

public sealed class GovernanceApprovalResponseDto
{
    public string ApprovalRequestId
    {
        get;
        set;
    } = string.Empty;

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string ManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string Status
    {
        get;
        set;
    } = string.Empty;

    public string RequestedBy
    {
        get;
        set;
    } = string.Empty;

    public string? ReviewedBy
    {
        get;
        set;
    }

    public string? ReviewComment
    {
        get;
        set;
    }
}

public sealed class GovernancePromotionResponseDto
{
    public string PromotionRecordId
    {
        get;
        set;
    } = string.Empty;

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string ManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string TargetEnvironment
    {
        get;
        set;
    } = string.Empty;

    public string PromotedBy
    {
        get;
        set;
    } = string.Empty;

    public string? ApprovalRequestId
    {
        get;
        set;
    }
}

public sealed class GovernanceActivationResponseDto
{
    public string ActivationId
    {
        get;
        set;
    } = string.Empty;

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string ManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string Environment
    {
        get;
        set;
    } = string.Empty;

    public bool IsActive
    {
        get;
        set;
    }
}
