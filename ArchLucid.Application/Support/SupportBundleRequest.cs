namespace ArchLucid.Application.Support;
/// <summary>
///     Per-request context handed to <see cref = "ISupportBundleAssembler"/>.
/// </summary>
/// <remarks>
///     Both fields are display-only and end up in <c>README.txt</c> / <c>manifest.json</c>
///     so the support engineer reading the bundle knows who downloaded it. Do NOT pass
///     secrets — the assembler's redaction pipeline assumes these strings are safe to write.
/// </remarks>
/// <param name = "RequesterDisplayId">Operator identity (e.g. UPN or email) — written to the manifest.</param>
/// <param name = "TenantDisplayName">Tenant name (or null when anonymous / cross-tenant operator).</param>
public sealed record SupportBundleRequest(string? RequesterDisplayId, string? TenantDisplayName)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(RequesterDisplayId, TenantDisplayName);
    private static byte __ValidatePrimaryConstructorArguments(System.String? RequesterDisplayId, System.String? TenantDisplayName)
    {
        return (byte)0;
    }
}