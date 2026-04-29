using ArchLucid.Core.Safety;

namespace ArchLucid.AgentRuntime.Safety;

/// <summary>Pass-through guard used when content safety is disabled.</summary>
public sealed class NullContentSafetyGuard : IContentSafetyGuard
{
    private static readonly ContentSafetyResult Allowed = new(true, null, null, null);

    /// <inheritdoc />
    public Task<ContentSafetyResult> CheckInputAsync(string text, CancellationToken cancellationToken)
    {
        _ = text;
        _ = cancellationToken;

        return Task.FromResult(Allowed);
    }

    public Task<ContentSafetyResult> CheckOutputAsync(string text, CancellationToken cancellationToken)
    {
        _ = text;
        _ = cancellationToken;

        return Task.FromResult(Allowed);
    }
}
