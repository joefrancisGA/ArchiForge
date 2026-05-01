using ArchLucid.Contracts.Agents;

namespace ArchLucid.Application.Evidence;

/// <summary>
///     Formats an <see cref="AgentEvidencePackage" /> as human-readable text for embedding in
///     reports, LLM prompts, or export documents.
/// </summary>
public interface IEvidenceSummaryFormatter
{
    /// <summary>
    ///     Returns a Markdown representation of <paramref name="evidence" />, covering request context,
    ///     constraints, capabilities, policy hints, service catalog hints, patterns, and prior manifest.
    /// </summary>
    string FormatMarkdown(AgentEvidencePackage evidence);
}
