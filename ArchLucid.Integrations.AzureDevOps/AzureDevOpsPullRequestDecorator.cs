using System.Net.Http.Headers;
using System.Text;

using ArchLucid.Contracts.Abstractions.Integrations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>REST 7.1 client: PR thread comment + optional PR status (best-effort).</summary>
public sealed class AzureDevOpsPullRequestDecorator(
    HttpClient httpClient,
    IOptions<AzureDevOpsIntegrationOptions> options,
    ILogger<AzureDevOpsPullRequestDecorator> logger) : IAzureDevOpsPullRequestDecorator
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IOptions<AzureDevOpsIntegrationOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<AzureDevOpsPullRequestDecorator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task PostManifestDeltaAsync(
        Guid goldenManifestId,
        Guid runId,
        AzureDevOpsPullRequestTarget target,
        CancellationToken cancellationToken)
    {
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

        string markdown =
            $"## ArchLucid — manifest committed{Environment.NewLine}"
            + $"- **Run:** `{runId:D}`{Environment.NewLine}"
            + $"- **Golden manifest id:** `{goldenManifestId:D}`{Environment.NewLine}";

        await PostStatusAsync(pat, basePath, markdown, o.StatusTargetUrl, cancellationToken).ConfigureAwait(false);
        await PostThreadAsync(pat, basePath, markdown, cancellationToken).ConfigureAwait(false);
    }

    private async Task PostStatusAsync(
        string pat,
        string pullRequestBasePath,
        string description,
        string targetUrl,
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
