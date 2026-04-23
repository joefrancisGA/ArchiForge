namespace ArchLucid.Core.Configuration;

/// <summary>
///     Optional Azure AI Content Safety (or compatible) integration for LLM prompts and completions.
///     When <c>Enabled</c> is true, host composition registers a stub guard that throws until a real implementation is
///     wired.
/// </summary>
public sealed class ContentSafetyOptions
{
    public const string SectionPath = "ArchLucid:ContentSafety";

    /// <summary>When false, a pass-through <c>NullContentSafetyGuard</c> is used (non-production-like hosts only).</summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>
    ///     When true (default), non-production-like hosts may use <c>NullContentSafetyGuard</c> while <see cref="Enabled" />
    ///     is false.
    ///     Set false to force explicit enablement in development.
    /// </summary>
    public bool AllowNullGuardInDevelopment
    {
        get;
        set;
    } = true;

    /// <summary>Optional endpoint URI when a concrete guard is added (not read by the null/stub guards).</summary>
    public string? Endpoint
    {
        get;
        set;
    }

    /// <summary>Optional API key name in Key Vault / user-secrets (not read by the null/stub guards).</summary>
    public string? ApiKey
    {
        get;
        set;
    }

    /// <summary>
    ///     Minimum Azure Content Safety text severity (four-level scale: 0, 2, 4, 6) that blocks the request.
    ///     Default <c>4</c> blocks high and highest.
    /// </summary>
    public int BlockSeverityThreshold
    {
        get;
        set;
    } = 4;

    /// <summary>
    ///     When true, SDK or network failures during analysis fail closed (block). When false, failures are logged and content
    ///     is allowed.
    /// </summary>
    public bool FailClosedOnSdkError
    {
        get;
        set;
    }
}
