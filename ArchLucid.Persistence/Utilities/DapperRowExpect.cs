namespace ArchLucid.Persistence.Utilities;

/// <summary>
///     Centralizes explicit Dapper "single row" results so nullability is not silently ignored when a row
///     must exist for the call to be coherent.
/// </summary>
public static class DapperRowExpect
{
    public static T Required<T>(T? row, string notFoundMessage) where T : class
    {
        if (row is null)
        {
            throw new InvalidOperationException(notFoundMessage);
        }

        return row;
    }
}
