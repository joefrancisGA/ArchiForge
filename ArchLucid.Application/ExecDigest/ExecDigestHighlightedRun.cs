namespace ArchLucid.Application.ExecDigest;
/// <summary>One committed manifest run highlighted in the digest (significance = proxy score from pilot deltas).</summary>
public sealed record ExecDigestHighlightedRun(string RunIdHex, int SignificanceScore, string? Caption)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(RunIdHex, Caption);
    private static byte __ValidatePrimaryConstructorArguments(System.String RunIdHex, System.String? Caption)
    {
        ArgumentNullException.ThrowIfNull(RunIdHex);
        return (byte)0;
    }
}