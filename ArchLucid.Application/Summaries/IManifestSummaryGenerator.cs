using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Summaries;

/// <summary>
///     Generates a Markdown narrative summary for a <see cref="GoldenManifest" />, optionally enriched
///     with evidence context from the run that produced it.
/// </summary>
public interface IManifestSummaryGenerator
{
    /// <summary>
    ///     Generates a Markdown summary of <paramref name="manifest" />.
    ///     When <paramref name="evidence" /> is provided, an evidence context section is appended.
    /// </summary>
    string GenerateMarkdown(
        GoldenManifest manifest,
        AgentEvidencePackage? evidence = null);
}
