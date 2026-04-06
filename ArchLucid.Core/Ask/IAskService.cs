using ArchiForge.Core.Scoping;

namespace ArchiForge.Core.Ask;

/// <summary>
/// Grounded natural-language Q&amp;A over a scoped run’s golden manifest, provenance, optional comparison, and retrieval hits.
/// </summary>
/// <remarks>Implementation: <c>ArchiForge.Api.Services.Ask.AskService</c> (registered scoped in API).</remarks>
public interface IAskService
{
    /// <summary>
    /// Resolves or creates a conversation thread, loads authority context, calls the LLM with structured JSON instructions, persists assistant turn, and optionally indexes the turn for retrieval.
    /// </summary>
    /// <param name="request">Thread/run anchors and question text.</param>
    /// <param name="scope">Tenant/workspace/project from the HTTP scope provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Answer plus reference lists and the effective <see cref="AskResponse.ThreadId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <see cref="AskRequest.Question"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no run can be resolved (missing <see cref="AskRequest.RunId"/> on a new thread, or run/manifest missing in scope).</exception>
    Task<AskResponse> AskAsync(AskRequest request, ScopeContext scope, CancellationToken ct);
}
