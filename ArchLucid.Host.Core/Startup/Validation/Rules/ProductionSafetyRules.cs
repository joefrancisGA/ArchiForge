using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Connections;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class ProductionSafetyRules
{
    /// <summary>Require RLS session context when using SQL in Production (API and Worker).</summary>
    public static void CollectSqlRowLevelSecurity(
        IConfiguration configuration,
        ArchLucidOptions archLucidOptions,
        List<string> errors)
    {
        if (!string.Equals(archLucidOptions.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SqlServerOptions sql =
            configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>() ?? new SqlServerOptions();

        if (sql.RowLevelSecurity.ApplySessionContext)
        {
            return;
        }

        errors.Add(
            "Production with ArchLucid:StorageProvider=Sql requires SqlServer:RowLevelSecurity:ApplySessionContext=true so tenant/workspace/project SESSION_CONTEXT keys are applied (defense in depth with SQL RLS).");
    }

    /// <summary>Fail-fast CORS checks in Production for API-facing hosts only.</summary>
    public static void CollectCors(IConfiguration configuration, List<string> errors)
    {
        string[]? origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origins is null || origins.Length == 0)
        {
            errors.Add("Production requires at least one Cors:AllowedOrigins entry.");
        }
        else
        {
            errors.AddRange(
                from origin in origins
                where !string.IsNullOrWhiteSpace(origin)
                select origin.Trim() into trimmed
                where string.Equals(trimmed, "*", StringComparison.Ordinal)
                select "Cors:AllowedOrigins must not use a wildcard '*' in Production.");
        }
    }

    /// <summary>Outbound webhook HMAC when HTTP delivery is enabled (API and Worker).</summary>
    public static void CollectWebhookSecrets(IConfiguration configuration, List<string> errors)
    {
        WebhookDeliveryOptions webhook =
            configuration.GetSection(WebhookDeliveryOptions.SectionName).Get<WebhookDeliveryOptions>() ??
            new WebhookDeliveryOptions();

        const int minWebhookSecretChars = 32;

        if (!webhook.UseHttpClient)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(webhook.HmacSha256SharedSecret))
        {
            errors.Add(
                "WebhookDelivery:HmacSha256SharedSecret is required in Production when WebhookDelivery:UseHttpClient is true.");

            return;
        }

        if (webhook.HmacSha256SharedSecret.Length < minWebhookSecretChars)
        {
            errors.Add(
                $"WebhookDelivery:HmacSha256SharedSecret must be at least {minWebhookSecretChars} characters in Production when WebhookDelivery:UseHttpClient is true.");
        }
    }
}
