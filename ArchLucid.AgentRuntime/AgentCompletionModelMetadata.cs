namespace ArchLucid.AgentRuntime;

/// <summary>
/// Reads model deployment / model id from the last successful <see cref="AzureOpenAiCompletionClient.CompleteJsonAsync"/> on the async flow, if any.
/// </summary>
public static class AgentCompletionModelMetadata
{
    /// <summary>
    /// Sets <paramref name="deploymentName"/> and <paramref name="modelVersion"/> to <see langword="null"/> when unavailable.
    /// </summary>
    public static void TryConsume(out string? deploymentName, out string? modelVersion)
    {
        if (AzureOpenAiCompletionClient.TryConsumeLastModelMetadata(out string d, out string? v)
            && !string.IsNullOrWhiteSpace(d))
        {
            deploymentName = d;
            modelVersion = v;

            return;
        }

        deploymentName = null;
        modelVersion = null;
    }
}
