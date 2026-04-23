namespace ArchLucid.Core.Configuration;

/// <summary>Background trial pre-seed: one simulator architecture run + commit after self-service trial bootstrap.</summary>
public sealed class TrialArchitecturePreseedOptions
{
    public const string SectionName = "TrialArchitecturePreseed";

    /// <summary>When false, <see cref="ArchLucid.Host.Core.Hosted.TrialArchitecturePreseedHostedService" /> does not run.</summary>
    public bool Enabled
    {
        get;
        init;
    } = true;

    /// <summary>Worker poll interval when leader-elected.</summary>
    public int PollIntervalSeconds
    {
        get;
        init;
    } = 15;

    /// <summary>Max tenants to drain per poll iteration.</summary>
    public int BatchSize
    {
        get;
        init;
    } = 5;
}
