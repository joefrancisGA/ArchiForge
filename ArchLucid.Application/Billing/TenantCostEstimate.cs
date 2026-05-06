using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Billing;
/// <summary>Rough monthly spend band for operator settings (Standard+ only at the HTTP layer).</summary>
public sealed record TenantCostEstimate(string Currency, TenantTier Tier, decimal EstimatedMonthlyUsdLow, decimal EstimatedMonthlyUsdHigh, IReadOnlyList<string> Factors, string MethodologyNote)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Currency, Factors, MethodologyNote);
    private static byte __ValidatePrimaryConstructorArguments(System.String Currency, System.Collections.Generic.IReadOnlyList<System.String> Factors, System.String MethodologyNote)
    {
        ArgumentNullException.ThrowIfNull(Currency);
        ArgumentNullException.ThrowIfNull(Factors);
        ArgumentNullException.ThrowIfNull(MethodologyNote);
        return (byte)0;
    }
}