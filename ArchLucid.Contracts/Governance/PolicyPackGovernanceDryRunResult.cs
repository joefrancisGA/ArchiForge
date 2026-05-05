namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Result of <c>POST /v1/governance/policy-packs/dry-run</c>: pre-commit style evaluation of a proposed pack JSON
///     against a scoped run (or manifest resolved to a run) without persisting the pack or mutating run state.
/// </summary>
public sealed class PolicyPackGovernanceDryRunResult
{
    /// <summary>Run id used for evaluation (canonical form from persistence).</summary>
    public string ResolvedRunId
    {
        get;
        init;
    } = null!;

    /// <summary>Manifest id from the request when the caller targeted a manifest; otherwise null.</summary>
    public Guid? TargetManifestId
    {
        get;
        init;
    }

    /// <summary>Outcome matching <see cref="PreCommitGovernanceGate" /> semantics.</summary>
    public PreCommitGateResult GateResult
    {
        get;
        init;
    } = null!;

    /// <summary>Named checks that succeeded (human-readable).</summary>
    public IReadOnlyList<string> PassedChecks
    {
        get;
        init;
    } = [];

    /// <summary>Named checks that failed (human-readable).</summary>
    public IReadOnlyList<string> FailedChecks
    {
        get;
        init;
    } = [];

    /// <summary>Warnings copied from <see cref="PreCommitGateResult.Warnings" /> plus any additive notes.</summary>
    public IReadOnlyList<string> Warnings
    {
        get;
        init;
    } = [];
}
