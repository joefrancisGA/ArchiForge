namespace ArchLucid.Core.Tenancy;

/// <summary>Reason a tenant trial write was rejected by <see cref="TrialLimitGate" /> or persistence.</summary>
public enum TrialLimitReason
{
    Expired,

    RunsExceeded,

    SeatsExceeded,

    /// <summary>Post-trial lifecycle states where mutating writes are blocked (<c>Expired</c> may still allow deletes).</summary>
    LifecycleWritesFrozen,

    /// <summary><c>ReadOnly</c> / <c>ExportOnly</c> / <c>Deleted</c> — HTTP deletes blocked per trial DPA workflow.</summary>
    LifecycleDeletesFrozen
}
