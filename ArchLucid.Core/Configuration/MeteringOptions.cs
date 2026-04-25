namespace ArchLucid.Core.Configuration;

/// <summary>Feature switch for persisting <see cref="Metering.UsageEvent" /> rows.</summary>
public sealed class MeteringOptions
{
    public const string SectionName = "Metering";

    public bool Enabled
    {
        get;
        set;
    }
}
