using System.Net;

using FluentAssertions;
namespace ArchLucid.Api.Tests;

/// <summary>
///     <c>POST /v1/governance/policy-packs/dry-run</c> validation and shape tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PolicyPackGovernanceDryRunEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task DryRunProposedPolicyPack_MissingBody_Returns400()
    {
        HttpResponseMessage response = await Client.PostAsync("/v1/governance/policy-packs/dry-run", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task DryRunProposedPolicyPack_Both_targets_Returns400()
    {
        Dictionary<string, object?> body = new()
        {
            ["policyPackContentJson"] = "{}",
            ["targetRunId"] = Guid.NewGuid().ToString("N"),
            ["targetManifestId"] = Guid.NewGuid(),
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/governance/policy-packs/dry-run",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task DryRunProposedPolicyPack_Neither_target_Returns400()
    {
        Dictionary<string, object?> body = new() { ["policyPackContentJson"] = "{}" };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/governance/policy-packs/dry-run",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task DryRunProposedPolicyPack_InvalidJson_Returns400()
    {
        Dictionary<string, object?> body = new()
        {
            ["policyPackContentJson"] = "{ not json",
            ["targetRunId"] = Guid.NewGuid().ToString("N"),
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/governance/policy-packs/dry-run",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task DryRunProposedPolicyPack_UnknownRun_Returns404()
    {
        Dictionary<string, object?> body = new()
        {
            ["policyPackContentJson"] = "{}",
            ["targetRunId"] = Guid.NewGuid().ToString("N"),
        };

        HttpResponseMessage response = await Client.PostAsync(
            "/v1/governance/policy-packs/dry-run",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
