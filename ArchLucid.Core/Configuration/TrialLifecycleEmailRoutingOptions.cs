namespace ArchLucid.Core.Configuration;

/// <summary>Controls whether scheduled trial lifecycle email scans run in-process or are owned by an external Logic App.</summary>
public sealed class TrialLifecycleEmailRoutingOptions
{
    /// <summary>Configuration path: <c>ArchLucid:Notifications:TrialLifecycle</c> (see <see cref="Owner" />).</summary>
    public const string SectionName = "ArchLucid:Notifications:TrialLifecycle";

    /// <summary>
    ///     Flat configuration key for <see cref="Owner" /> (bind with <see cref="SectionName" /> or read via
    ///     <c>configuration[key]</c>).
    /// </summary>
    public const string OwnerConfigurationKey = $"{SectionName}:Owner";

    /// <summary>
    ///     <see cref="OwnerModes.Hosted" /> keeps <c>TrialLifecycleEmailScanHostedService</c> / jobs;
    ///     <see cref="OwnerModes.LogicApp" /> skips scan enqueue (external recurrence).
    /// </summary>
    public string Owner
    {
        get;
        set;
    } = OwnerModes.Hosted;

    /// <summary>Parses a raw <c>Owner</c> string (e.g. from <see cref="OwnerConfigurationKey" />) without binding options.</summary>
    public static bool IsLogicAppOwnerMode(string? owner)
    {
        return string.Equals(owner?.Trim(), OwnerModes.LogicApp, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsLogicAppOwned()
    {
        return string.Equals(Owner?.Trim(), OwnerModes.LogicApp, StringComparison.OrdinalIgnoreCase);
    }

    public static class OwnerModes
    {
        public const string Hosted = "Hosted";

        public const string LogicApp = "LogicApp";
    }
}
