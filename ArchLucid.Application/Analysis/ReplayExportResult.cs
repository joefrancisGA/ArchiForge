namespace ArchLucid.Application.Analysis;

public sealed class ReplayExportResult
{
    public string ExportRecordId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     When replay requested <see cref="ReplayExportRequest.RecordReplayExport" />, the id of the newly persisted export
    ///     row; otherwise <see langword="null" />.
    ///     The replayed source id remains in <see cref="ExportRecordId" />.
    /// </summary>
    public string? RecordedReplayExportRecordId
    {
        get;
        set;
    }

    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string ExportType
    {
        get;
        set;
    } = string.Empty;

    public string Format
    {
        get;
        set;
    } = string.Empty;

    public string FileName
    {
        get;
        set;
    } = string.Empty;

    public byte[] Content
    {
        get;
        set;
    } = [];

    public string? TemplateProfile
    {
        get;
        set;
    }

    public string? TemplateProfileDisplayName
    {
        get;
        set;
    }

    public bool WasAutoSelected
    {
        get;
        set;
    }

    public string? ResolutionReason
    {
        get;
        set;
    }
}
