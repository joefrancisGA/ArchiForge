namespace ArchLucid.Contracts.Pilots;

/// <summary>HTTP headers used by <c>archlucid try --real</c> to attribute execute calls to the pilot path.</summary>
public static class PilotTryRealModeHeaders
{
    /// <summary>When set to <c>1</c>, execute is attributed to the first-real-value pilot flow (telemetry + audit).</summary>
    public const string PilotTryRealMode = "X-ArchLucid-Pilot-Try-Real-Mode";
}
