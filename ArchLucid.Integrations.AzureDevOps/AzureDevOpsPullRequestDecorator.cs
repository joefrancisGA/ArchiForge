using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Abstractions.Integrations;
using ArchLucid.Core.Comparison;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>REST 7.1 client: PR thread comment + optional PR status (best-effort).</summary>
public sealed class AzureDevOpsPullRequestDecorator(
    HttpClient httpClient,
    IOptions<AzureDevOpsIntegrationOptions> options,
    ILogger<AzureDevOpsPullRequestDecorator> logger) : IAzureDevOpsPullRequestDecorator
{
    private static readonly JsonSerializerOptions CompareJsonOptions = new() { PropertyNameCaseInsensitive = true, };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private readonly IOptions<AzureDevOpsIntegrationOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<AzureDevOpsPullRequestDecorator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task PostManifestDeltaAsync(
        AzureDevOpsManifestDeltaRequest request,
        AzureDevOpsPullRequestTarget target,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        AzureDevOpsIntegrationOptions o = _options.Value;
        if (string.IsNullOrWhiteSpace(o.Organization)
            || string.IsNullOrWhiteSpace(o.Project)
            || string.IsNullOrWhiteSpace(o.PersonalAccessToken))
        {
            if (_logger.IsEnabled(LogLevel.Debug))

                _logger.LogDebug("Azure DevOps PR decoration skipped: organization, project, or PAT not configured.");

            return;
        }

        string org = o.Organization.Trim();
        string project = o.Project.Trim();
        string basePath =
            $"https://dev.azure.com/{Uri.EscapeDataString(org)}/{Uri.EscapeDataString(project)}/_apis/git/repositories/{target.RepositoryId:D}/pullrequests/{target.PullRequestId}";

        string pat = o.PersonalAccessToken.Trim();
        string? operatorRunDeepLink = BuildOperatorRunDeepLink(o.StatusTargetUrl, request.RunId);
        string markdown = await BuildPrMarkdownAsync(request, o, operatorRunDeepLink, cancellationToken)
            .ConfigureAwait(false);

        await PostStatusAsync(pat, basePath, markdown, operatorRunDeepLink, cancellationToken).ConfigureAwait(false);
        await PostThreadAsync(pat, basePath, markdown, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> BuildPrMarkdownAsync(
        AzureDevOpsManifestDeltaRequest request,
        AzureDevOpsIntegrationOptions o,
        string? operatorRunDeepLink,
        CancellationToken ct)
    {
        if (request.PreviousRunId is null || request.PreviousRunId == Guid.Empty)
            return AzureDevOpsRunSummaryMarkdown.Format(
                request.RunId,
                request.ManifestId,
                request.Findings,
                operatorRunDeepLink);

        if (string.IsNullOrWhiteSpace(o.ArchLucidApiBaseUrl) || string.IsNullOrWhiteSpace(o.ArchLucidApiKey))
        {
            if (_logger.IsEnabled(LogLevel.Debug))

                _logger.LogDebug("Azure DevOps compare skipped: ArchLucidApiBaseUrl or ArchLucidApiKey not configured.");

            return AzureDevOpsRunSummaryMarkdown.Format(
                request.RunId,
                request.ManifestId,
                request.Findings,
                operatorRunDeepLink);
        }

        string? compareMarkdown =
            await TryGetCompareMarkdownAsync(request, o, operatorRunDeepLink, ct).ConfigureAwait(false);

        if (compareMarkdown is not null)
            return compareMarkdown;

        return AzureDevOpsRunSummaryMarkdown.Format(
            request.RunId,
            request.ManifestId,
            request.Findings,
            operatorRunDeepLink);
    }

    private async Task<string?> TryGetCompareMarkdownAsync(
        AzureDevOpsManifestDeltaRequest request,
        AzureDevOpsIntegrationOptions o,
        string? operatorRunDeepLink,
        CancellationToken ct)
    {
        Guid previousRunId = request.PreviousRunId!.Value;
        string apiBase = o.ArchLucidApiBaseUrl.Trim().TrimEnd('/');
        string compareUrl =
            $"{apiBase}/v1/compare?baseRunId={Uri.EscapeDataString(previousRunId.ToString("D"))}&targetRunId={Uri.EscapeDataString(request.RunId.ToString("D"))}";

        using HttpRequestMessage compareRequest = new(HttpMethod.Get, compareUrl);
        compareRequest.Headers.TryAddWithoutValidation("Accept", "application/json");
        compareRequest.Headers.TryAddWithoutValidation("X-Api-Key", o.ArchLucidApiKey.Trim());
        compareRequest.Headers.TryAddWithoutValidation("x-tenant-id", request.TenantId.ToString("D"));
        compareRequest.Headers.TryAddWithoutValidation("x-workspace-id", request.WorkspaceId.ToString("D"));
        compareRequest.Headers.TryAddWithoutValidation("x-project-id", request.ProjectId.ToString("D"));

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(compareRequest, ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                string err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(
                        "ArchLucid GET /v1/compare failed: {StatusCode} {Body}",
                        (int)response.StatusCode,
                        err.Length > 512 ? err[..512] : err);

                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            ComparisonResult? parsed =
                await JsonSerializer.DeserializeAsync<ComparisonResult>(stream, CompareJsonOptions, ct)
                    .ConfigureAwait(false);

            if (parsed is not null)
                return GoldenManifestCompareMarkdownFormatter.Format(parsed, operatorRunDeepLink);

            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning("ArchLucid GET /v1/compare returned JSON that did not deserialize to ComparisonResult.");

            return null;

        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(ex, "ArchLucid GET /v1/compare threw; using run summary.");

            return null;
        }
    }

    private static string? BuildOperatorRunDeepLink(string? statusTargetBase, Guid runId)
    {
        if (string.IsNullOrWhiteSpace(statusTargetBase))
            return null;

        string b = statusTargetBase.Trim().TrimEnd('/');

        return $"{b}/runs/{runId:D}";
    }

    private async Task PostStatusAsync(
        string pat,
        string pullRequestBasePath,
        string description,
        string? targetUrl,
        CancellationToken cancellationToken)
    {
        string url = $"{pullRequestBasePath}/statuses?api-version=7.1";
        string json = AzureDevOpsPullRequestWireFormat.SerializeStatusCreate(description, targetUrl);

        try
        {
            using HttpResponseMessage response = await SendPatPostAsync(pat, url, json, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(
                        "Azure DevOps PR status POST failed: {StatusCode} {Body}",
                        (int)response.StatusCode,
                        err.Length > 512 ? err[..512] : err);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(ex, "Azure DevOps PR status POST threw; continuing.");
        }
    }

    private async Task PostThreadAsync(string pat, string pullRequestBasePath, string markdown, CancellationToken cancellationToken)
    {
        string url = $"{pullRequestBasePath}/threads?api-version=7.1";
        string json = AzureDevOpsPullRequestWireFormat.SerializeThreadCreate(markdown);

        try
        {
            using HttpResponseMessage response = await SendPatPostAsync(pat, url, json, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(
                        "Azure DevOps PR thread POST failed: {StatusCode} {Body}",
                        (int)response.StatusCode,
                        err.Length > 512 ? err[..512] : err);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(ex, "Azure DevOps PR thread POST threw.");
        }
    }

    private async Task<HttpResponseMessage> SendPatPostAsync(
        string pat,
        string requestUrl,
        string json,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(requestUrl, UriKind.Absolute));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
