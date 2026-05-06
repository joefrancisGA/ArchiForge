namespace ArchLucid.Application.DataConsistency;
public sealed record DataConsistencyFinding(string CheckName, DataConsistencyFindingSeverity Severity, string Description, IReadOnlyList<string> AffectedEntityIds)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(CheckName, Description, AffectedEntityIds);
    private static byte __ValidatePrimaryConstructorArguments(System.String CheckName, System.String Description, System.Collections.Generic.IReadOnlyList<System.String> AffectedEntityIds)
    {
        ArgumentNullException.ThrowIfNull(CheckName);
        ArgumentNullException.ThrowIfNull(Description);
        ArgumentNullException.ThrowIfNull(AffectedEntityIds);
        return (byte)0;
    }
}