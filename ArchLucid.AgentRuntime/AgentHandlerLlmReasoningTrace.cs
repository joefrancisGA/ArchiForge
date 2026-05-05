using System.Text;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Collects LLM provider reasoning snippets for the active <see cref="RealAgentExecutor" /> handler invocation via
///     <see cref="System.Threading.AsyncLocal{T}" />. Azure OpenAI completions append here; the executor merges the buffer into
///     <see cref="ArchLucid.Contracts.Agents.AgentResult.ReasoningTrace" />.
/// </summary>
public static class AgentHandlerLlmReasoningTrace
{
    private static readonly AsyncLocal<StringBuilder?> Buffer = new();

    /// <summary>Begins trace accumulation for one handler execution; dispose to detach.</summary>
    public static IDisposable BeginHandlerScope()
    {
        Buffer.Value = new StringBuilder();

        return new Scope();
    }

    /// <summary>Appends one completion's reasoning text (several completions in one handler are separated).</summary>
    public static void AppendCompletionSnippet(string? snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet))
            return;

        StringBuilder? sb = Buffer.Value;

        if (sb is null)
            return;

        if (sb.Length > 0)
            sb.Append("\n\n---\n\n");

        sb.Append(snippet.Trim());
    }

    /// <summary>Reads accumulated trace, clears the buffer, and leaves the scope value until dispose.</summary>
    public static string? TryConsumeBuffered()
    {
        StringBuilder? sb = Buffer.Value;

        if (sb is null || sb.Length == 0)
            return null;

        string s = sb.ToString();
        sb.Clear();

        return s;
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            Buffer.Value = null;
        }
    }
}
