namespace ArchiForge.Api.Http;

/// <summary>
/// Well-known custom HTTP response header names written by ArchiForge API endpoints.
/// Centralised here to prevent typos and to make it easy to keep server code, tests,
/// and client docs in sync.
/// </summary>
public static class ArchiForgeHttpHeaders
{
    /// <summary>
    /// Set on comparison summary responses when the result was persisted.
    /// Value is the <c>ComparisonRecordId</c> of the newly created record.
    /// </summary>
    public const string ComparisonRecordId = "X-ArchiForge-ComparisonRecordId";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string ComparisonType = "X-ArchiForge-ComparisonType";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string ReplayMode = "X-ArchiForge-ReplayMode";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string VerificationPassed = "X-ArchiForge-VerificationPassed";

    /// <summary>Set on comparison replay artifact responses when a verification message is present.</summary>
    public const string VerificationMessage = "X-ArchiForge-VerificationMessage";

    /// <summary>Optional — left run identifier attached to replay responses.</summary>
    public const string LeftRunId = "X-ArchiForge-LeftRunId";

    /// <summary>Optional — right run identifier attached to replay responses.</summary>
    public const string RightRunId = "X-ArchiForge-RightRunId";

    /// <summary>Optional — left export record identifier attached to replay responses.</summary>
    public const string LeftExportRecordId = "X-ArchiForge-LeftExportRecordId";

    /// <summary>Optional — right export record identifier attached to replay responses.</summary>
    public const string RightExportRecordId = "X-ArchiForge-RightExportRecordId";

    /// <summary>Optional — UTC timestamp of the comparison record attached to replay responses.</summary>
    public const string CreatedUtc = "X-ArchiForge-CreatedUtc";

    /// <summary>Optional — format profile of the replay attached to replay responses.</summary>
    public const string FormatProfile = "X-ArchiForge-Format-Profile";

    /// <summary>Optional — persisted replay record identifier attached to replay responses.</summary>
    public const string PersistedReplayRecordId = "X-ArchiForge-PersistedReplayRecordId";

    /// <summary>
    /// Set on batch comparison replay ZIP responses when at least one ID failed and at least one succeeded.
    /// Value is <c>true</c>.
    /// </summary>
    public const string BatchReplayPartial = "X-ArchiForge-Batch-Partial";
}
