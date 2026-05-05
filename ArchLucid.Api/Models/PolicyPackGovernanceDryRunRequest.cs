using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Api.Models;

/// <summary>
///     Request body for <c>POST /v1/governance/policy-packs/dry-run</c> — proposed
///     <see cref="ArchLucid.Decisioning.Governance.PolicyPacks.PolicyPackContentDocument" /> JSON plus a scoped run or
///     manifest target.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API request DTO; no business logic.")]
public sealed class PolicyPackGovernanceDryRunRequest
{
    /// <summary>JSON object matching <c>PolicyPackContentDocument</c> (same as pack <c>contentJson</c>).</summary>
    public string PolicyPackContentJson
    {
        get;
        set;
    } = null!;

    /// <summary>Architecture run id (with or without hyphens). Mutually exclusive with <see cref="TargetManifestId" />.</summary>
    public string? TargetRunId
    {
        get;
        set;
    }

    /// <summary>Golden manifest id; resolved to a run under scope. Mutually exclusive with <see cref="TargetRunId" />.</summary>
    public Guid? TargetManifestId
    {
        get;
        set;
    }

    /// <summary>Optional override for <c>BlockCommitOnCritical</c> (wins over pack metadata).</summary>
    public bool? BlockCommitOnCritical
    {
        get;
        set;
    }

    /// <summary>Optional override for minimum blocking <see cref="ArchLucid.Contracts.Findings.FindingSeverity" /> ordinal.</summary>
    public int? BlockCommitMinimumSeverity
    {
        get;
        set;
    }

    /// <summary>Optional label for <see cref="ArchLucid.Contracts.Governance.PreCommitGateResult.PolicyPackId" /> in the response.</summary>
    public Guid? ProposedPolicyPackId
    {
        get;
        set;
    }
}
