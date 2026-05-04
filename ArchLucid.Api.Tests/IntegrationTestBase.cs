using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Base for API integration tests: provides an <see cref="HttpClient" /> from <see cref="ArchLucidApiFactory" /> and
///     JSON helpers aligned with the API’s serializer settings.
/// </summary>
public class IntegrationTestBase(ArchLucidApiFactory factory) : IClassFixture<ArchLucidApiFactory>
{
    /// <summary>Distinct actor for governance submit vs review in DevelopmentBypass integration tests.</summary>
    protected const string GovernanceSubmitterName = "governance-submitter";

    protected const string GovernanceSubmitterId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    /// <summary>
    ///     DevelopmentBypass authentication does not emit <c>tenant_id</c> claims; scope headers align the client with
    ///     <see cref="ScopeIds" /> defaults so SQL-backed <c>CommercialTenantTierFilter</c> can resolve <c>dbo.Tenants</c>.
    /// </summary>
    protected readonly HttpClient Client = CreateClientWithDefaultScopeHeaders(factory);

    /// <summary>
    ///     Aligned with <see cref="ArchLucid.Api.Startup.MvcExtensions" /> API JSON options (camelCase properties, string
    ///     enums).
    /// </summary>
    protected readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(null) }
    };

    /// <summary>Factory for the hosted API (singleton services, SQL connection string, etc.).</summary>
    protected ArchLucidApiFactory Factory
    {
        get;
    } = factory;

    /// <summary>
    ///     Serializes <paramref name="value" /> with <see cref="JsonOptions" /> and returns <see cref="StringContent" />
    ///     suitable for <c>application/json</c> POST bodies.
    /// </summary>
    protected StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    ///     POST <c>/v1/governance/approval-requests</c> using explicit camelCase JSON keys so binding stays stable across
    ///     CI hosts (anonymous projection payloads have proven flaky versus MVC deserialization).
    /// </summary>
    protected Task<HttpResponseMessage> PostGovernanceApprovalRequestAsync(
        string runId,
        string manifestVersion = "v1",
        string sourceEnvironment = "dev",
        string targetEnvironment = "test",
        string? requestComment = null,
        string? testActorName = null,
        string? testActorId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        Dictionary<string, object?> payload =
            new()
            {
                ["runId"] = runId,
                ["manifestVersion"] = manifestVersion,
                ["sourceEnvironment"] = sourceEnvironment,
                ["targetEnvironment"] = targetEnvironment
            };

        if (requestComment is not null)
            payload["requestComment"] = requestComment;

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        StringContent content = new(json, Encoding.UTF8, "application/json");

        return PostGovernanceMutationAsync(
            "/v1/governance/approval-requests",
            content,
            Guid.NewGuid().ToString("N"),
            testActorName,
            testActorId);
    }

    /// <summary>
    ///     JSON for governance <c>.../approve</c> and <c>.../reject</c> bodies; explicit camelCase keys for stable MVC
    ///     binding across CI hosts.
    /// </summary>
    protected StringContent GovernanceReviewDecisionJsonContent(string reviewedBy, string? reviewComment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        Dictionary<string, object?> payload = new() { ["reviewedBy"] = reviewedBy };

        if (reviewComment is not null)
            payload["reviewComment"] = reviewComment;

        string json = JsonSerializer.Serialize(payload, JsonOptions);

        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    ///     POST <c>/v1/governance/activations</c> using explicit camelCase keys so binding stays stable across CI hosts.
    /// </summary>
    protected Task<HttpResponseMessage> PostGovernanceActivationAsync(
        string runId,
        string manifestVersion = "v1",
        string environment = "dev",
        string? testActorName = null,
        string? testActorId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        Dictionary<string, object?> payload =
            new()
            {
                ["runId"] = runId,
                ["manifestVersion"] = manifestVersion,
                ["environment"] = environment
            };

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        StringContent content = new(json, Encoding.UTF8, "application/json");

        return PostGovernanceMutationAsync(
            "/v1/governance/activations",
            content,
            Guid.NewGuid().ToString("N"),
            testActorName,
            testActorId);
    }

    /// <summary>
    ///     POST <c>/v1/governance/promotions</c> using explicit camelCase keys so binding stays stable across CI hosts.
    /// </summary>
    protected Task<HttpResponseMessage> PostGovernancePromotionAsync(
        string runId,
        string promotedBy,
        string manifestVersion = "v1",
        string sourceEnvironment = "dev",
        string targetEnvironment = "test",
        string? approvalRequestId = null,
        string? notes = null,
        string? testActorName = null,
        string? testActorId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(promotedBy);

        Dictionary<string, object?> payload =
            new()
            {
                ["runId"] = runId,
                ["manifestVersion"] = manifestVersion,
                ["sourceEnvironment"] = sourceEnvironment,
                ["targetEnvironment"] = targetEnvironment,
                ["promotedBy"] = promotedBy
            };

        if (approvalRequestId is not null)
            payload["approvalRequestId"] = approvalRequestId;

        if (notes is not null)
            payload["notes"] = notes;

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        StringContent content = new(json, Encoding.UTF8, "application/json");

        return PostGovernanceMutationAsync(
            "/v1/governance/promotions",
            content,
            Guid.NewGuid().ToString("N"),
            testActorName,
            testActorId);
    }

    /// <summary>
    ///     POST with <c>Idempotency-Key</c> for persisted governance mutations (submit approval, promote, activate).
    ///     Optional test actor headers apply when both <paramref name="testActorName" /> and
    ///     <paramref name="testActorId" /> are non-empty (requires <c>ArchLucidAuth:AllowTestActorHeaders</c>).
    /// </summary>
    protected async Task<HttpResponseMessage> PostGovernanceMutationAsync(
        string relativeUrl,
        HttpContent content,
        string idempotencyKey,
        string? testActorName = null,
        string? testActorId = null)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, relativeUrl) { Content = content };

        _ = request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        if (!string.IsNullOrWhiteSpace(testActorName) && !string.IsNullOrWhiteSpace(testActorId))
            ApplyTestActorHeaders(request, testActorName, testActorId);

        return await Client.SendAsync(request);
    }

    /// <summary>JSON body with a unique idempotency key and default DevelopmentBypass actor.</summary>
    protected Task<HttpResponseMessage> PostGovernanceMutationAsync(string relativeUrl, object body)
    {
        return PostGovernanceMutationAsync(relativeUrl, body, null, null);
    }

    /// <summary>JSON body, idempotency key, and optional DevelopmentBypass test actor override.</summary>
    protected Task<HttpResponseMessage> PostGovernanceMutationAsync(
        string relativeUrl,
        object body,
        string? testActorName,
        string? testActorId)
    {
        return PostGovernanceMutationAsync(
            relativeUrl,
            JsonContent(body),
            Guid.NewGuid().ToString("N"),
            testActorName,
            testActorId);
    }

    /// <summary>POST JSON as a specific test actor (no idempotency): approve/reject and similar actions.</summary>
    protected Task<HttpResponseMessage> PostJsonAsTestActorAsync(
        string relativeUrl,
        object body,
        string testActorName,
        string testActorId)
    {
        ArgumentNullException.ThrowIfNull(body);

        return PostJsonAsTestActorAsync(relativeUrl, JsonContent(body), testActorName, testActorId);
    }

    /// <inheritdoc cref="PostJsonAsTestActorAsync(string,object,string,string)" />
    protected async Task<HttpResponseMessage> PostJsonAsTestActorAsync(
        string relativeUrl,
        HttpContent content,
        string testActorName,
        string testActorId)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        using HttpRequestMessage request = new(HttpMethod.Post, relativeUrl) { Content = content };

        ApplyTestActorHeaders(request, testActorName, testActorId);

        return await Client.SendAsync(request);
    }

    private static void ApplyTestActorHeaders(HttpRequestMessage request, string testActorName, string testActorId)
    {
        if (string.IsNullOrWhiteSpace(testActorName) || string.IsNullOrWhiteSpace(testActorId))
            throw new ArgumentException("Test actor name and id are required.");

        _ = request.Headers.TryAddWithoutValidation(ArchLucidAuthOptions.TestActorNameHeader, testActorName.Trim());
        _ = request.Headers.TryAddWithoutValidation(ArchLucidAuthOptions.TestActorIdHeader, testActorId.Trim());
    }

    private static HttpClient CreateClientWithDefaultScopeHeaders(ArchLucidApiFactory apiFactory)
    {
        HttpClient client = apiFactory.CreateClient();
        WireDefaultSqlIntegrationScopeHeaders(client);

        return client;
    }

    /// <summary>
    ///     Adds <c>x-tenant-id</c> / <c>x-workspace-id</c> / <c>x-project-id</c> for DevelopmentBypass + SQL integration
    ///     hosts.
    /// </summary>
    public static void WireDefaultSqlIntegrationScopeHeaders(HttpClient client)
    {
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-tenant-id", ScopeIds.DefaultTenant.ToString("D"));
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-workspace-id",
            ScopeIds.DefaultWorkspace.ToString("D"));
        _ = client.DefaultRequestHeaders.TryAddWithoutValidation("x-project-id", ScopeIds.DefaultProject.ToString("D"));
    }
}
