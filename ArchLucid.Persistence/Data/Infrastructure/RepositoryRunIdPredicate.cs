using System.Globalization;

using Dapper;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     Matches persisted authority run identifiers when callers supply GUID-equivalent string formats (for example N vs D).
///     Always includes literal <c>{column} = @RunId</c> so rows still match when
///     <c>TRY_CONVERT(UNIQUEIDENTIFIER, {column})</c> yields NULL for the stored string shape.
/// </summary>
internal static class RepositoryRunIdPredicate
{
    internal static string WhereClauseMatching(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name is required.", nameof(columnName));

        return string.Format(
            CultureInfo.InvariantCulture,
            """
            (
                {0} = @RunId
                OR (@RunIdGuid IS NOT NULL AND TRY_CONVERT(UNIQUEIDENTIFIER, {0}) = @RunIdGuid)
            )
            """,
            columnName);
    }

    internal static void AddRunIdMatchParameters(DynamicParameters parameters, string runId)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        parameters.Add("RunId", runId);

        Guid? runGuid = Guid.TryParse(runId, out Guid parsed) ? parsed : null;

        parameters.Add("RunIdGuid", runGuid);
    }
}
