using System.Text.Json;

using ArchLucid.Core.Tenancy;

namespace ArchLucid.Core.Billing.AzureMarketplace;

/// <summary>
///     Extracts Marketplace SaaS webhook fields used for <c>ChangePlan</c> / <c>ChangeQuantity</c> (payload shape
///     varies slightly by action).
/// </summary>
public static class MarketplaceWebhookPayloadParser
{
    /// <summary>
    ///     Maps Azure Marketplace <c>planId</c> text to persisted <see cref="TenantTier" /> storage codes (
    ///     <c>Standard</c> vs <c>Enterprise</c>).
    /// </summary>
    public static string TierStorageCodeFromPlanId(string? planId)
    {
        if (string.IsNullOrWhiteSpace(planId))
            return nameof(TenantTier.Standard);

        string p = planId.Trim();

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (p.Contains("enterprise", StringComparison.OrdinalIgnoreCase))
            return nameof(TenantTier.Enterprise);

        return nameof(TenantTier.Standard);
    }

    /// <summary>Reads <c>planId</c> when present (string).</summary>
    public static bool TryGetPlanId(JsonElement root, out string? planId)
    {
        planId = null;

        if (!root.TryGetProperty("planId", out JsonElement el))
            return false;

        string? s = el.GetString();

        if (string.IsNullOrWhiteSpace(s))
            return false;

        planId = s.Trim();

        return true;
    }

    /// <summary>
    ///     Reads seat <c>quantity</c> from the webhook root (number or numeric string); defaults to
    ///     <paramref name="fallback" /> when absent.
    /// </summary>
    public static int ReadQuantity(JsonElement root, int fallback = 1)
    {
        if (!root.TryGetProperty("quantity", out JsonElement q))
            return Math.Max(1, fallback);

        if (q.ValueKind == JsonValueKind.Number && q.TryGetInt32(out int n))
            return Math.Max(1, n);

        if (q.ValueKind != JsonValueKind.String)
            return Math.Max(1, fallback);

        string? s = q.GetString();

        return Math.Max(1, int.TryParse(s, out int parsed) ? parsed : fallback);
    }
}
