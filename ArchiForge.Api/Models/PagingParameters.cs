namespace ArchiForge.Api.Models;

public sealed class PagingParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public const int MaxPageSize = 200;

    public (int Skip, int Take) Normalize()
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = 1;
        if (PageSize > MaxPageSize) PageSize = MaxPageSize;

        var skip = (PageNumber - 1) * PageSize;
        return (skip, PageSize);
    }
}

