using System.Globalization;

using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Heuristic precheck only — does not replace a full LLM content-safety service. Blocks a small set of
///     high-signal instruction-override phrases often used in prompt-injection attempts.
/// </summary>
public sealed class DefaultRequestContentSafetyPrecheck : IRequestContentSafetyPrecheck
{
    private static readonly string[] BlockedPhrases =
    [
        "ignore previous instructions",
        "ignore all prior",
        "disregard previous",
        "you are now",
        "new system prompt",
        "override safety",
        "jailbreak"
    ];

    public Task<RequestContentSafetyResult> EvaluateAsync(ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        List<string> reasons = [];

        Accumulate(request.Description, nameof(request.Description), reasons);
        Accumulate(request.SystemName, nameof(request.SystemName), reasons);

        foreach (string req in request.InlineRequirements)
            Accumulate(req, nameof(request.InlineRequirements), reasons);

        foreach (ContextDocumentRequest doc in request.Documents)
        {
            Accumulate(doc.Name, $"{nameof(request.Documents)}.{nameof(ContextDocumentRequest.Name)}", reasons);
            Accumulate(doc.Content, $"{nameof(request.Documents)}.{nameof(ContextDocumentRequest.Content)}", reasons);
        }

        return Task.FromResult(new RequestContentSafetyResult { IsAllowed = reasons.Count == 0, Reasons = reasons });
    }

    private static void Accumulate(string? text, string fieldLabel, List<string> reasons)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string normalized = text.Trim().ToLowerInvariant();

        reasons.AddRange(from phrase in BlockedPhrases
            where normalized.Contains(phrase, StringComparison.Ordinal)
            select string.Format(CultureInfo.InvariantCulture, "Field {0} matches blocked phrase \"{1}\".", fieldLabel,
                phrase));
    }
}
