using ArchLucid.Core.Safety;

namespace ArchLucid.AgentRuntime.Safety;

/// <summary>
///     Placeholder registered when <see cref="ArchLucid.Core.Configuration.ContentSafetyOptions.Enabled" /> is true but no
///     Azure implementation is present yet.
/// </summary>
public sealed class ContentSafetyEnabledButUnconfiguredGuard : IContentSafetyGuard
{
    private const string Message =
        "ArchLucid:ContentSafety:Enabled is true but no content-safety client is registered. "
        + "Add an Azure AI Content Safety (or compatible) implementation of IContentSafetyGuard, or set Enabled=false.";

    /// <inheritdoc />
    public Task<ContentSafetyResult> CheckInputAsync(string text, CancellationToken cancellationToken)
    {
        _ = text;
        _ = cancellationToken;

        throw new InvalidOperationException(Message);
    }

    public Task<ContentSafetyResult> CheckOutputAsync(string text, CancellationToken cancellationToken)
    {
        _ = text;
        _ = cancellationToken;

        throw new InvalidOperationException(Message);
    }
}
