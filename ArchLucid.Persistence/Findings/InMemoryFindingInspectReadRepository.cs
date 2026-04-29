using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Persistence.Findings;

/// <summary>
///     In-memory storage mode: resolves the inspector from hydrated <see cref="RunDetailDto" /> (no relational SQL
///     tables).
/// </summary>
[ExcludeFromCodeCoverage(Justification =
    "In-memory composition path; SQL integration tests cover the Dapper implementation.")]
public sealed class InMemoryFindingInspectReadRepository(IAuthorityQueryService authorityQuery)
    : IFindingInspectReadRepository
{
    private static readonly Regex DemoFindingId =
        new(
            "^finding-demo-(?<run>[0-9a-fA-F]{32})-(?<slot>primary|secondary)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    /// <inheritdoc />
    public async Task<FindingInspectResponse?> GetInspectAsync(ScopeContext scope, string findingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrWhiteSpace(findingId))
            throw new ArgumentException("Finding id is required.", nameof(findingId));

        Match m = DemoFindingId.Match(findingId.Trim());

        if (!m.Success)
            return null;

        if (!Guid.TryParseExact(m.Groups["run"].Value, "N", out Guid runId))
            return null;


        RunDetailDto? detail = await _authorityQuery.GetRunDetailAsync(scope, runId, ct);

        if (detail?.FindingsSnapshot?.Findings is not { Count: > 0 } findings)
            return null;


        Finding? match = findings.FirstOrDefault(f =>
            string.Equals(f.FindingId, findingId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return null;


        List<FindingInspectEvidenceItem> evidence = match.RelatedNodeIds
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n =>
                new FindingInspectEvidenceItem { ArtifactId = null, LineRange = null, Excerpt = n.Trim() })
            .ToList();

        string? ruleId = null;
        string? ruleName = null;

        if (detail.AuthorityTrace is RuleAuditTrace ruleAudit)
        {
            RuleAuditTracePayload payload = ruleAudit.RuleAudit;

            if (payload.AppliedRuleIds is { Count: > 0 })
            {
                ruleId = payload.AppliedRuleIds[0];
                ruleName = ruleId;
            }
        }

        if (ruleId is null && match.Trace.RulesApplied is { Count: > 0 })
        {
            ruleId = match.Trace.RulesApplied[0];
            ruleName = ruleId;
        }

        JsonElement? typed = TryPayloadElement(match);

        return new FindingInspectResponse
        {
            FindingId = match.FindingId,
            TypedPayload = typed,
            DecisionRuleId = ruleId,
            DecisionRuleName = ruleName,
            Evidence = evidence,
            AuditRowId = null,
            RunId = runId,
            ManifestVersion = detail.Run.CurrentManifestVersion,
            ModelDeploymentName = match.ModelDeploymentName,
            PromptTemplateVersion = match.PromptTemplateVersion,
            ConfidenceScore = match.ConfidenceScore,
            HumanReviewStatus = match.HumanReviewStatus
        };
    }

    private static JsonElement? TryPayloadElement(Finding finding)
    {
        if (finding.Payload is null)
            return null;

        try
        {
            return JsonSerializer.SerializeToElement(finding.Payload);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
