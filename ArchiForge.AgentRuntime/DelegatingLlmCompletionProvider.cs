namespace ArchiForge.AgentRuntime;

/// <summary>
/// Exposes <see cref="IAgentCompletionClient"/> as <see cref="ILlmCompletionProvider"/> with configured telemetry labels.
/// </summary>
public sealed class DelegatingLlmCompletionProvider(
    IAgentCompletionClient inner,
    string providerId,
    string modelDeploymentLabel) : ILlmCompletionProvider
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public string ProviderId { get; } = string.IsNullOrWhiteSpace(providerId) ? "unknown" : providerId.Trim();

    /// <inheritdoc />
    public string ModelDeploymentLabel { get; } =
        string.IsNullOrWhiteSpace(modelDeploymentLabel) ? "unknown" : modelDeploymentLabel.Trim();

    /// <inheritdoc />
    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        return _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
    }
}
