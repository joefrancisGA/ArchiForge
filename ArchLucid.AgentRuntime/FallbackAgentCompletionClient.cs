using System.ClientModel;
using System.Net;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Decorator that delegates to <paramref name="primary"/> and falls back to <paramref name="secondary"/>
/// when the primary throws an <see cref="HttpRequestException"/> or <see cref="ClientResultException"/> with
/// status 429 or 5xx (matches Azure OpenAI SDK surface).
/// </summary>
public sealed class FallbackAgentCompletionClient(
    IAgentCompletionClient primary,
    IAgentCompletionClient secondary,
    ILogger<FallbackAgentCompletionClient> logger) : IAgentCompletionClient, IDisposable
{
    private readonly IAgentCompletionClient _primary =
        primary ?? throw new ArgumentNullException(nameof(primary));

    private readonly IAgentCompletionClient _secondary =
        secondary ?? throw new ArgumentNullException(nameof(secondary));

    private readonly ILogger<FallbackAgentCompletionClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _primary.Descriptor;

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _primary.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (IsFallbackTrigger(ex))
        {
            return await InvokeFallbackAsync(ex);
        }
        catch (ClientResultException ex) when (IsClientResultFallbackTrigger(ex))
        {
            return await InvokeFallbackAsync(ex);
        }

        async Task<string> InvokeFallbackAsync(Exception primaryFailure)
        {
            _logger.LogWarning(
                primaryFailure,
                "Primary LLM completion failed with a fallback-eligible HTTP status; using fallback completion client.");

            return await _secondary.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
        }
    }

    /// <summary>True when <paramref name="ex"/> carries status 429 or a 5xx server error.</summary>
    private static bool IsFallbackTrigger(HttpRequestException ex)
    {
        if (ex.StatusCode is not HttpStatusCode statusCode)
        {
            return false;
        }

        int code = (int)statusCode;

        return code == 429 || (code >= 500 && code < 600);
    }

    /// <summary>Azure OpenAI SDK path: <see cref="ClientResultException"/> carries the HTTP status.</summary>
    private static bool IsClientResultFallbackTrigger(ClientResultException ex) =>
        IsFallbackEligibleStatus(ex.Status);

    private static bool IsFallbackEligibleStatus(int statusCode) =>
        statusCode == 429 || (statusCode >= 500 && statusCode < 600);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_primary is IDisposable primaryDisposable)
        {
            primaryDisposable.Dispose();
        }

        if (_secondary is IDisposable secondaryDisposable)
        {
            secondaryDisposable.Dispose();
        }
    }
}
