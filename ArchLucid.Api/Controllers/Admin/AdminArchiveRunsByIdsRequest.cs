namespace ArchLucid.Api.Controllers.Admin;

/// <summary>Body for <c>POST /v1/admin/runs/archive-by-ids</c>.</summary>
public sealed class AdminArchiveRunsByIdsRequest
{
    /// <summary>Run primary keys to archive (max 100, duplicates ignored).</summary>
    public IReadOnlyList<Guid> RunIds
    {
        get;
        set;
    } = [];
}
