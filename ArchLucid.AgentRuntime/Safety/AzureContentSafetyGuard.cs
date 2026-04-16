using ArchLucid.Core.Configuration;
using ArchLucid.Core.Safety;

using Azure;
using Azure.AI.ContentSafety;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Safety;

/// <summary>
/// Azure AI Content Safety text analyzer wired to <see cref="IContentSafetyGuard"/>.
/// </summary>
public sealed class AzureContentSafetyGuard : IContentSafetyGuard
{
    private static readonly ContentSafetyResult Allowed = new(true, null, null, null);

    private readonly ContentSafetyClient _client;
    private readonly IOptionsMonitor<ContentSafetyOptions> _optionsMonitor;
    private readonly ILogger<AzureContentSafetyGuard> _logger;

    public AzureContentSafetyGuard(
        Uri endpoint,
        string apiKey,
        IOptionsMonitor<ContentSafetyOptions> optionsMonitor,
        ILogger<AzureContentSafetyGuard> logger)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _client = new ContentSafetyClient(endpoint, new AzureKeyCredential(apiKey));
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContentSafetyResult> CheckInputAsync(string text, CancellationToken cancellationToken)
    {
        return await AnalyzeAsync(text, "input", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentSafetyResult> CheckOutputAsync(string text, CancellationToken cancellationToken)
    {
        return await AnalyzeAsync(text, "output", cancellationToken);
    }

    private async Task<ContentSafetyResult> AnalyzeAsync(string text, string kind, CancellationToken cancellationToken)
    {
        ContentSafetyOptions options = _optionsMonitor.CurrentValue;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Allowed;
        }

        AnalyzeTextOptions request = new(text)
        {
            OutputType = AnalyzeTextOutputType.FourSeverityLevels,
        };

        try
        {
            Response<AnalyzeTextResult> response = await _client.AnalyzeTextAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return MapResult(response.Value, options.BlockSeverityThreshold);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Content safety {Kind} analysis failed; FailClosedOnSdkError={FailClosed}.", kind, options.FailClosedOnSdkError);
            }

            if (options.FailClosedOnSdkError)
            {
                return new ContentSafetyResult(false, "Content safety service error.", "SdkError", null);
            }

            return Allowed;
        }
    }

    internal static ContentSafetyResult MapResult(AnalyzeTextResult result, int blockSeverityThreshold)
    {
        IReadOnlyList<TextCategoriesAnalysis>? analyses = result.CategoriesAnalysis;

        if (analyses is null || analyses.Count == 0)
        {
            return Allowed;
        }

        foreach (TextCategoriesAnalysis row in analyses)
        {
            int? severity = row.Severity;

            if (!severity.HasValue)
            {
                continue;
            }

            if (severity.Value >= blockSeverityThreshold)
            {
                string category = row.Category.ToString();

                return new ContentSafetyResult(
                    false,
                    $"Blocked at severity {severity.Value} (threshold {blockSeverityThreshold}).",
                    category,
                    severity.Value);
            }
        }

        return Allowed;
    }
}
