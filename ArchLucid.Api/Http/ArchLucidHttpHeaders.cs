namespace ArchLucid.Api.Http;

/// <summary>
/// Well-known custom HTTP response header names written by ArchLucid API endpoints.
/// Centralised here to prevent typos and to make it easy to keep server code, tests,
/// and client docs in sync.
/// </summary>
public static class ArchLucidHttpHeaders
{
    /// <summary>
    /// Set on comparison summary responses when the result was persisted.
    /// Value is the <c>ComparisonRecordId</c> of the newly created record.
    /// </summary>
    public const string ComparisonRecordId = "X-ArchLucid-ComparisonRecordId";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string ComparisonType = "X-ArchLucid-ComparisonType";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string ReplayMode = "X-ArchLucid-ReplayMode";

    /// <summary>Set on comparison replay artifact responses.</summary>
    public const string VerificationPassed = "X-ArchLucid-VerificationPassed";

    /// <summary>Set on comparison replay artifact responses when a verification message is present.</summary>
    public const string VerificationMessage = "X-ArchLucid-VerificationMessage";

    /// <summary>Optional — left run identifier attached to replay responses.</summary>
    public const string LeftRunId = "X-ArchLucid-LeftRunId";

    /// <summary>Optional — right run identifier attached to replay responses.</summary>
    public const string RightRunId = "X-ArchLucid-RightRunId";

    /// <summary>Optional — left export record identifier attached to replay responses.</summary>
    public const string LeftExportRecordId = "X-ArchLucid-LeftExportRecordId";

    /// <summary>Optional — right export record identifier attached to replay responses.</summary>
    public const string RightExportRecordId = "X-ArchLucid-RightExportRecordId";

    /// <summary>Optional — UTC timestamp of the comparison record attached to replay responses.</summary>
    public const string CreatedUtc = "X-ArchLucid-CreatedUtc";

    /// <summary>Optional — format profile of the replay attached to replay responses.</summary>
    public const string FormatProfile = "X-ArchLucid-Format-Profile";

    /// <summary>Optional — persisted replay record identifier attached to replay responses.</summary>
    public const string PersistedReplayRecordId = "X-ArchLucid-PersistedReplayRecordId";

    /// <summary>
    /// Set on batch comparison replay ZIP responses when at least one ID failed and at least one succeeded.
    /// Value is <c>true</c>.
    /// </summary>
    public const string BatchReplayPartial = "X-ArchLucid-Batch-Partial";

    /// <summary>
    /// Set on governance workflow responses when <c>?dryRun=true</c> was used: validation ran but nothing was persisted.
    /// Value is <c>true</c>.
    /// </summary>
    public const string DryRun = "X-ArchLucid-DryRun";
}
