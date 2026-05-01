namespace ArchLucid.Core.Hosting;

/// <summary>
/// Stable <c>rule_name</c> labels for <see cref="ArchLucid.Core.Diagnostics.ArchLucidInstrumentation.RecordStartupConfigWarning"/> (TB-002).
/// </summary>
public static class ProductionLikeHostingMisconfigurationAdvisorRuleNames
{
    /// <summary>API host lacks CORS origins on staging/production-like.</summary>
    public const string CorsAllowedOriginsEmptyProductionLikeHost = "cors_allowed_origins_empty_production_like_host";

    /// <summary>JwtBearer configured without Authority or PEM path.</summary>
    public const string JwtBearerMissingAuthorityAndPem = "jwt_bearer_missing_authority_and_pem";

    /// <summary>ArchLucidAuth Mode is ApiKey but API keys disabled.</summary>
    public const string ApiKeyModeDisabledWhenConfigured = "api_key_mode_disabled_when_api_key_auth_configured";
}
