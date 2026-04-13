namespace ArchLucid.AgentRuntime;

/// <summary>
/// Reads token counts from the last <see cref="AzureOpenAiCompletionClient.CompleteJsonAsync"/> on the async flow, if any.
/// </summary>
public static class AgentCompletionTokenUsage
{
    /// <summary>Sets <paramref name="inputTokens"/> and <paramref name="outputTokens"/> to <see langword="null"/> when unavailable.</summary>
    public static void TryConsume(out int? inputTokens, out int? outputTokens)
    {
        if (AzureOpenAiCompletionClient.TryConsumeLastCompletionTokenUsage(out int p, out int c) && (p > 0 || c > 0))
        {
            inputTokens = p;
            outputTokens = c;

            return;
        }

        inputTokens = null;
        outputTokens = null;
    }
}
