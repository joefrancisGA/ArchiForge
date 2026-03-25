using System.Text.Json;

using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Governance.Resolution;

/// <summary>
/// Default <see cref="IEffectiveGovernanceResolver"/>: merges applicable pack contents into one <see cref="PolicyPackContentDocument"/>
/// using explicit precedence (project &gt; workspace &gt; tenant, pin boost, then <see cref="PolicyPackAssignment.AssignedUtc"/>).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why:</strong> Enterprise governance is layered; operators need deterministic “effective” state and an explainable trace
/// (<see cref="GovernanceResolutionDecision"/>, <see cref="GovernanceConflictRecord"/>) for audits and the governance-resolution API.
/// </para>
/// <para>
/// <strong>Callers:</strong> <see cref="EffectiveGovernanceLoader"/>, HTTP governance-resolution endpoint (API layer), and
/// <c>EffectiveGovernanceResolverTests</c>.
/// </para>
/// </remarks>
/// <param name="assignmentRepository">Supplies hierarchical assignment rows for the scope.</param>
/// <param name="packRepository">Resolves pack metadata for each assignment.</param>
/// <param name="versionRepository">Loads <c>ContentJson</c> for the assigned version string.</param>
public sealed class EffectiveGovernanceResolver(
    IPolicyPackAssignmentRepository assignmentRepository,
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository) : IEffectiveGovernanceResolver
{
    /// <summary>
    /// One materialized pack contribution: assignment + pack + version + parsed <see cref="PolicyPackContentDocument"/>.
    /// </summary>
    /// <remarks>Internal to <see cref="ResolveAsync"/>; keeps merge helpers strongly typed.</remarks>
    private sealed record ResolvedPackRow(
        PolicyPackAssignment Assignment,
        PolicyPack Pack,
        PolicyPackVersion Version,
        PolicyPackContentDocument Content);

    /// <inheritdoc />
    /// <remarks>
    /// Pipeline: (1) list assignments, (2) filter enabled + <see cref="AppliesToScope"/>, (3) load pack/version and deserialize JSON
    /// (skip bad rows), (4) merge each facet via <see cref="ResolveGuidIdList"/>, <see cref="ResolveStringKeyList"/>, <see cref="ResolveDictionary"/>.
    /// Appends human-readable counts to <see cref="EffectiveGovernanceResolutionResult.Notes"/>.
    /// </remarks>
    public async Task<EffectiveGovernanceResolutionResult> ResolveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        IReadOnlyList<PolicyPackAssignment> assignments = await assignmentRepository
            .ListByScopeAsync(tenantId, workspaceId, projectId, ct)
            .ConfigureAwait(false);

        List<PolicyPackAssignment> applicable = assignments
            .Where(x => x.IsEnabled)
            .Where(x => AppliesToScope(x, tenantId, workspaceId, projectId))
            .ToList();

        List<ResolvedPackRow> resolvedPacks = new();
        List<string> skippedNotes = new();

        foreach (PolicyPackAssignment assignment in applicable)
        {
            PolicyPack? pack = await packRepository.GetByIdAsync(assignment.PolicyPackId, ct).ConfigureAwait(false);
            if (pack is null)
            {
                skippedNotes.Add(
                    $"Skipped assignment for policy pack '{assignment.PolicyPackId}': pack not found.");
                continue;
            }

            PolicyPackVersion? version = await versionRepository
                .GetByPackAndVersionAsync(assignment.PolicyPackId, assignment.PolicyPackVersion, ct)
                .ConfigureAwait(false);

            if (version is null)
            {
                skippedNotes.Add(
                    $"Skipped policy pack '{pack.Name}' ({assignment.PolicyPackId}): " +
                    $"version '{assignment.PolicyPackVersion}' not found.");
                continue;
            }

            PolicyPackContentDocument? content;
            try
            {
                content = JsonSerializer.Deserialize<PolicyPackContentDocument>(
                    version.ContentJson,
                    PolicyPackJsonSerializerOptions.Default);
            }
            catch (JsonException ex)
            {
                skippedNotes.Add(
                    $"Skipped policy pack '{pack.Name}' ({assignment.PolicyPackId}) " +
                    $"version '{assignment.PolicyPackVersion}': content JSON is corrupt ({ex.Message}).");
                continue;
            }

            if (content is null)
            {
                skippedNotes.Add(
                    $"Skipped policy pack '{pack.Name}' ({assignment.PolicyPackId}) " +
                    $"version '{assignment.PolicyPackVersion}': content deserialized to null.");
                continue;
            }

            resolvedPacks.Add(new ResolvedPackRow(assignment, pack, version, content));
        }

        EffectiveGovernanceResolutionResult result = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
        };

        foreach (string note in skippedNotes)
            result.Notes.Add(note);

        ResolveGuidIdList(
            result,
            "ComplianceRule",
            resolvedPacks,
            x => x.Content.ComplianceRuleIds,
            (content, ids) => content.ComplianceRuleIds = ids);

        ResolveStringKeyList(
            result,
            "ComplianceRuleKey",
            resolvedPacks,
            x => x.Content.ComplianceRuleKeys,
            (content, keys) => content.ComplianceRuleKeys = keys);

        ResolveGuidIdList(
            result,
            "AlertRule",
            resolvedPacks,
            x => x.Content.AlertRuleIds,
            (content, ids) => content.AlertRuleIds = ids);

        ResolveGuidIdList(
            result,
            "CompositeAlertRule",
            resolvedPacks,
            x => x.Content.CompositeAlertRuleIds,
            (content, ids) => content.CompositeAlertRuleIds = ids);

        ResolveDictionary(
            result,
            "AdvisoryDefault",
            resolvedPacks,
            x => x.Content.AdvisoryDefaults,
            (content, dict) => content.AdvisoryDefaults = dict ?? []);

        ResolveDictionary(
            result,
            "Metadata",
            resolvedPacks,
            x => x.Content.Metadata,
            (content, dict) => content.Metadata = dict ?? []);

        result.Notes.Add($"Resolved {resolvedPacks.Count} applicable policy pack assignment(s).");
        result.Notes.Add($"Produced {result.Decisions.Count} resolution decision(s).");
        result.Notes.Add($"Detected {result.Conflicts.Count} conflict(s).");

        return result;
    }

    /// <summary>
    /// Determines whether an assignment row applies to the runtime project context, independent of repository SQL details.
    /// </summary>
    /// <remarks>
    /// Called only from <see cref="ResolveAsync"/>. Tenant rows ignore workspace/project columns; workspace rows require workspace match;
    /// project rows require both workspace and project match.
    /// </remarks>
    private static bool AppliesToScope(
        PolicyPackAssignment assignment,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        if (assignment.TenantId != tenantId)
            return false;

        return assignment.ScopeLevel switch
        {
            GovernanceScopeLevel.Tenant => true,
            GovernanceScopeLevel.Workspace => assignment.WorkspaceId == workspaceId,
            GovernanceScopeLevel.Project => assignment.WorkspaceId == workspaceId && assignment.ProjectId == projectId,
            _ => false,
        };
    }

    /// <summary>
    /// Computes a single sortable rank: base tier (tenant 1000, workspace 2000, project 3000) plus 100 when <see cref="PolicyPackAssignment.IsPinned"/>.
    /// </summary>
    /// <remarks>
    /// <strong>Why tier &gt; pin:</strong> an unpinned project assignment (3000) still beats a pinned tenant assignment (1100), so scope always wins over pin.
    /// Exposed as <c>internal</c> for unit tests. Used by <see cref="OrderCandidates"/>.
    /// </remarks>
    internal static int GetPrecedenceRank(PolicyPackAssignment assignment)
    {
        int tier = assignment.ScopeLevel switch
        {
            GovernanceScopeLevel.Tenant => 1000,
            GovernanceScopeLevel.Workspace => 2000,
            GovernanceScopeLevel.Project => 3000,
            _ => 0,
        };

        return assignment.IsPinned ? tier + 100 : tier;
    }

    /// <summary>Projects a <see cref="ResolvedPackRow"/> into a <see cref="GovernanceResolutionCandidate"/> for UI/API.</summary>
    /// <remarks>Called from resolve-* helpers when building candidate lists per item key.</remarks>
    private static GovernanceResolutionCandidate ToCandidate(ResolvedPackRow row, string valueJson)
    {
        PolicyPackAssignment a = row.Assignment;
        return new GovernanceResolutionCandidate
        {
            PolicyPackId = row.Pack.PolicyPackId,
            PolicyPackName = row.Pack.Name,
            Version = row.Version.Version,
            ScopeLevel = a.ScopeLevel,
            PrecedenceRank = GetPrecedenceRank(a),
            ValueJson = valueJson,
            AssignmentId = a.AssignmentId,
            AssignedUtc = a.AssignedUtc,
        };
    }

    /// <summary>Deterministic ordering: higher <see cref="GovernanceResolutionCandidate.PrecedenceRank"/>, then newer <see cref="GovernanceResolutionCandidate.AssignedUtc"/>, then <see cref="GovernanceResolutionCandidate.AssignmentId"/>.</summary>
    /// <remarks>Shared by all merge strategies so ties never depend on enumeration order.</remarks>
    private static List<GovernanceResolutionCandidate> OrderCandidates(IEnumerable<GovernanceResolutionCandidate> candidates) =>
        candidates
            .OrderByDescending(c => c.PrecedenceRank)
            .ThenByDescending(c => c.AssignedUtc)
            .ThenByDescending(c => c.AssignmentId)
            .ToList();

    /// <summary>Builds operator-facing text explaining why the first candidate in an ordered list won.</summary>
    /// <remarks>Called when appending <see cref="GovernanceResolutionDecision.ResolutionReason"/>.</remarks>
    private static string BuildResolutionReason(IReadOnlyList<GovernanceResolutionCandidate> ordered)
    {
        if (ordered.Count == 0)
            return "No candidates.";
        if (ordered.Count == 1)
            return "Only one applicable candidate existed.";

        GovernanceResolutionCandidate winner = ordered[0];
        GovernanceResolutionCandidate second = ordered[1];
        if (winner.PrecedenceRank != second.PrecedenceRank)
            return "Higher governance scope tier (project > workspace > tenant), or pinned assignment within a tier, outranked the other candidate(s).";

        if (winner.AssignedUtc != second.AssignedUtc)
            return "Same scope tier and pin state; the newer assignment (AssignedUtc) won.";

        return "Same scope tier, pin state, and timestamp; winner chosen by deterministic tie-break (AssignmentId).";
    }

    /// <summary>
    /// Merges a list-valued facet keyed by <see cref="Guid"/> (e.g. compliance / alert rule ids): union of distinct ids, winner per id.
    /// </summary>
    /// <remarks>
    /// Emits <see cref="GovernanceConflictRecord"/> with <c>DuplicateDefinition</c> when multiple packs mention the same id.
    /// Invoked from <see cref="ResolveAsync"/> for ComplianceRule, AlertRule, and CompositeAlertRule facets.
    /// </remarks>
    private static void ResolveGuidIdList(
        EffectiveGovernanceResolutionResult result,
        string itemType,
        List<ResolvedPackRow> packs,
        Func<ResolvedPackRow, List<Guid>?> selector,
        Action<PolicyPackContentDocument, List<Guid>> setter)
    {
        List<Guid> allIds = packs
            .SelectMany(x => selector(x) ?? [])
            .Distinct()
            .ToList();

        List<Guid> effective = new();

        foreach (Guid id in allIds)
        {
            string raw = id.ToString("D");
            List<GovernanceResolutionCandidate> candidates = OrderCandidates(
                packs
                    .Where(x => (selector(x) ?? []).Contains(id))
                    .Select(x => ToCandidate(x, raw)));

            if (candidates.Count == 0)
                continue;

            candidates[0].WasSelected = true;
            effective.Add(id);

            result.Decisions.Add(new GovernanceResolutionDecision
            {
                ItemType = itemType,
                ItemKey = raw,
                WinningPolicyPackId = candidates[0].PolicyPackId,
                WinningPolicyPackName = candidates[0].PolicyPackName,
                WinningVersion = candidates[0].Version,
                WinningScopeLevel = candidates[0].ScopeLevel,
                ResolutionReason = BuildResolutionReason(candidates),
                Candidates = candidates,
            });

            if (candidates.Count > 1)
            {
                result.Conflicts.Add(new GovernanceConflictRecord
                {
                    ItemType = itemType,
                    ItemKey = raw,
                    ConflictType = "DuplicateDefinition",
                    Description =
                        $"Multiple policy packs defined the same {itemType} item. The higher-precedence candidate was selected.",
                    Candidates = candidates,
                });
            }
        }

        setter(result.EffectiveContent, effective);
    }

    /// <summary>Merges string list facets (e.g. <see cref="PolicyPackContentDocument.ComplianceRuleKeys"/>) with case-insensitive key equality.</summary>
    /// <remarks>
    /// Stores JSON-encoded <see cref="GovernanceResolutionCandidate.ValueJson"/> for keys so UI can show quoted strings consistently.
    /// <c>DuplicateDefinition</c> conflicts when the same key appears in multiple packs.
    /// </remarks>
    private static void ResolveStringKeyList(
        EffectiveGovernanceResolutionResult result,
        string itemType,
        List<ResolvedPackRow> packs,
        Func<ResolvedPackRow, List<string>?> selector,
        Action<PolicyPackContentDocument, List<string>> setter)
    {
        List<string> allKeys = packs
            .SelectMany(x => selector(x) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> effective = new();

        foreach (string key in allKeys)
        {
            List<GovernanceResolutionCandidate> candidates = OrderCandidates(
                packs
                    .Where(x => (selector(x) ?? []).Contains(key, StringComparer.OrdinalIgnoreCase))
                    .Select(x =>
                    {
                        List<string> list = selector(x) ?? [];
                        string v = list.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                        return ToCandidate(x, JsonSerializer.Serialize(v, PolicyPackJsonSerializerOptions.Default));
                    }));

            if (candidates.Count == 0)
                continue;

            candidates[0].WasSelected = true;
            string canonical = packs
                .SelectMany(x => selector(x) ?? [])
                .First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            effective.Add(canonical);

            result.Decisions.Add(new GovernanceResolutionDecision
            {
                ItemType = itemType,
                ItemKey = canonical,
                WinningPolicyPackId = candidates[0].PolicyPackId,
                WinningPolicyPackName = candidates[0].PolicyPackName,
                WinningVersion = candidates[0].Version,
                WinningScopeLevel = candidates[0].ScopeLevel,
                ResolutionReason = BuildResolutionReason(candidates),
                Candidates = candidates,
            });

            if (candidates.Count > 1)
            {
                result.Conflicts.Add(new GovernanceConflictRecord
                {
                    ItemType = itemType,
                    ItemKey = canonical,
                    ConflictType = "DuplicateDefinition",
                    Description =
                        $"Multiple policy packs defined the same {itemType} key. The higher-precedence candidate was selected.",
                    Candidates = candidates,
                });
            }
        }

        setter(result.EffectiveContent, effective);
    }

    /// <summary>
    /// Merges dictionary facets (<see cref="PolicyPackContentDocument.AdvisoryDefaults"/>, <see cref="PolicyPackContentDocument.Metadata"/>):
    /// last-winner per key by precedence; <c>ValueConflict</c> when values differ across packs.
    /// </summary>
    /// <remarks>
    /// Unlike id lists, duplicate keys with identical values do not produce a value conflict—only <see cref="GovernanceResolutionDecision"/> entries.
    /// </remarks>
    private static void ResolveDictionary(
        EffectiveGovernanceResolutionResult result,
        string itemType,
        List<ResolvedPackRow> packs,
        Func<ResolvedPackRow, Dictionary<string, string>?> selector,
        Action<PolicyPackContentDocument, Dictionary<string, string>> setter)
    {
        List<string> keys = packs
            .SelectMany(x => (selector(x) ?? []).Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Dictionary<string, string> effective = new(StringComparer.OrdinalIgnoreCase);

        foreach (string key in keys)
        {
            List<GovernanceResolutionCandidate> candidates = OrderCandidates(
                packs
                    .Where(x =>
                        (selector(x) ?? []).Keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
                    .Select(x =>
                    {
                        Dictionary<string, string> dict = selector(x) ?? [];
                        string actualKey = dict.Keys.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                        string val = dict[actualKey];
                        return ToCandidate(x, val);
                    }));

            if (candidates.Count == 0)
                continue;

            candidates[0].WasSelected = true;
            string canonicalKey = packs
                .SelectMany(x => (selector(x) ?? []).Keys)
                .First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            effective[canonicalKey] = candidates[0].ValueJson;

            result.Decisions.Add(new GovernanceResolutionDecision
            {
                ItemType = itemType,
                ItemKey = canonicalKey,
                WinningPolicyPackId = candidates[0].PolicyPackId,
                WinningPolicyPackName = candidates[0].PolicyPackName,
                WinningVersion = candidates[0].Version,
                WinningScopeLevel = candidates[0].ScopeLevel,
                ResolutionReason = BuildResolutionReason(candidates),
                Candidates = candidates,
            });

            if (candidates.Count > 1)
            {
                int distinctValues = candidates
                    .Select(x => x.ValueJson)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                if (distinctValues > 1)
                {
                    result.Conflicts.Add(new GovernanceConflictRecord
                    {
                        ItemType = itemType,
                        ItemKey = canonicalKey,
                        ConflictType = "ValueConflict",
                        Description =
                            $"Multiple policy packs defined different values for {itemType} '{canonicalKey}'. The higher-precedence value was selected.",
                        Candidates = candidates,
                    });
                }
            }
        }

        setter(result.EffectiveContent, effective);
    }
}
