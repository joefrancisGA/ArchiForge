namespace ArchLucid.Host.Core.Configuration;

/// <summary>Graded operator response for orphan probes (SQL in <c>DataConsistencyEnforcementSql</c>).</summary>
public sealed class DataConsistencyEnforcementOptions
{
    /// <summary>Configuration subsection bound to <c>DataConsistency:Enforcement</c>.</summary>
    public const string SectionName = "DataConsistency:Enforcement";

    /// <summary>Default <see cref="DataConsistencyEnforcementMode.Warn"/> — logs without quarantine.</summary>
    public DataConsistencyEnforcementMode Mode
    {
        get; set;
    }
        = DataConsistencyEnforcementMode.Warn;

    /// <summary>Maximum golden-manifest orphan rows inserted per probe pass in <see cref="DataConsistencyEnforcementMode.Quarantine"/>.</summary>
    public int MaxRowsPerBatch
    {
        get; set;
    }
        = 500;

    /// <summary>Minimum orphan count before <c>archlucid_data_consistency_alerts_total</c> increments in Alert/Quarantine modes.</summary>
    public int AlertThreshold
    {
        get; set;
    }
        = 1;

    /// <summary>
    ///     When <see langword="true"/>, orphan golden-manifest rows are inserted into <c>dbo.DataConsistencyQuarantine</c>
    ///     whenever the probe runs and orphans exist, even if <see cref="Mode"/> is <see cref="DataConsistencyEnforcementMode.Alert"/>
    ///     or <see cref="DataConsistencyEnforcementMode.Warn"/> (still suppressed when <see cref="Mode"/> is
    ///     <see cref="DataConsistencyEnforcementMode.Off"/>).
    /// </summary>
    public bool AutoQuarantine
    {
        get; set;
    }
        = false;
}
