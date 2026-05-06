namespace ArchLucid.Application.DataConsistency;
public sealed record DataConsistencyReport(DateTime CheckedAtUtc, IReadOnlyList<DataConsistencyFinding> Findings, bool IsHealthy)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Findings);
    private static byte __ValidatePrimaryConstructorArguments(System.Collections.Generic.IReadOnlyList<ArchLucid.Application.DataConsistency.DataConsistencyFinding> Findings)
    {
        ArgumentNullException.ThrowIfNull(Findings);
        return (byte)0;
    }
}