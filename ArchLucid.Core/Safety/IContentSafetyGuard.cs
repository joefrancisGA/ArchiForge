namespace ArchLucid.Core.Safety;

/// <summary>
///     Optional pre/post filter for LLM prompts and completions. Implementations should be fast-fail when unsafe.
/// </summary>
public interface IContentSafetyGuard
{
    /// <summary>Validates operator/user prompt material before it is sent to the model.</summary>
    Task<ContentSafetyResult> CheckInputAsync(string text, CancellationToken cancellationToken);

    /// <summary>Validates model completion text before it is persisted or returned to clients.</summary>
    Task<ContentSafetyResult> CheckOutputAsync(string text, CancellationToken cancellationToken);
}
