namespace ArchLucid.Core.Configuration;

/// <summary>When a configuration leaf becomes mandatory for a healthy production-like deployment.
/// The CLI and <c>GET /v1/admin/config-summary</c> use the same rules for <c>isRequired</c> hints.</summary>
public enum ConfigKeyRequirementKind
{
    /// <summary>Never blocks <c>archlucid config check</c> (may still be validated at API startup in special modes).</summary>
    None,

    /// <summary>When <c>ArchLucid:StorageProvider</c> is <c>Sql</c> or unset (default Sql per product defaults).</summary>
    WhenDefaultSqlStorage,

    /// <summary>When <c>AgentExecution:Mode=Real</c> and <c>AgentExecution:CompletionClient</c> is not <c>Echo</c>.</summary>
    WhenRealLlmNotEcho,

    /// <summary>When <c>Authentication:ApiKey:Enabled</c> is true (non-empty / truthy in configuration).</summary>
    WhenApiKeyEnabled,

    /// <summary>When <c>Observability:Otlp:Enabled</c> is true.</summary>
    WhenOtlpEnabled,

    /// <summary>When <c>ASPNETCORE_ENVIRONMENT</c> (or <c>DOTNET_ENVIRONMENT</c>) is <c>Production</c>.</summary>
    WhenProduction,

    /// <summary>When <c>Email:Provider</c> (or project-specific email key) is Azure Communications — see <c>ProductionSafetyRules</c> for the exact key.</summary>
    WhenAcsEmail,

    /// <summary>When <c>Hosting:Role</c> is <c>Worker</c> for worker-only hosts.</summary>
    WhenWorkerRole
}
