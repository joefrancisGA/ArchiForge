using System.Data;

using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.RelationalRead;

using Dapper;

namespace ArchLucid.Persistence.ContextSnapshots;

/// <summary>
///     Hydrates <see cref="ContextSnapshot" /> from relational child tables when rows exist; otherwise legacy JSON
///     columns.
/// </summary>
internal static class ContextSnapshotRelationalRead
{
    public static async Task<ContextSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        ContextSnapshotStorageRow row,
        CancellationToken ct)
    {
        Guid snapshotId = row.SnapshotId;

        int canonicalCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotCanonicalObjects WHERE SnapshotId = @SnapshotId",
            new { SnapshotId = snapshotId },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotWarnings WHERE SnapshotId = @SnapshotId",
            new { SnapshotId = snapshotId },
            ct);

        int errorsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotErrors WHERE SnapshotId = @SnapshotId",
            new { SnapshotId = snapshotId },
            ct);

        int hashesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotSourceHashes WHERE SnapshotId = @SnapshotId",
            new { SnapshotId = snapshotId },
            ct);

        List<CanonicalObject> canonicalObjects = canonicalCount > 0
            ? await LoadCanonicalObjectsRelationalAsync(connection, transaction, snapshotId, ct)
            : ContextSnapshotLegacyJsonReader.DeserializeCanonicalObjects(row.CanonicalObjectsJson);

        List<string> warnings = warningsCount > 0
            ? await LoadStringColumnRelationalAsync(
                connection,
                transaction,
                """
                SELECT WarningText AS Item
                FROM dbo.ContextSnapshotWarnings
                WHERE SnapshotId = @SnapshotId
                ORDER BY SortOrder;
                """,
                snapshotId,
                ct)
            : ContextSnapshotLegacyJsonReader.DeserializeStringList(row.WarningsJson);

        List<string> errors = errorsCount > 0
            ? await LoadStringColumnRelationalAsync(
                connection,
                transaction,
                """
                SELECT ErrorText AS Item
                FROM dbo.ContextSnapshotErrors
                WHERE SnapshotId = @SnapshotId
                ORDER BY SortOrder;
                """,
                snapshotId,
                ct)
            : ContextSnapshotLegacyJsonReader.DeserializeStringList(row.ErrorsJson);

        Dictionary<string, string> sourceHashes = hashesCount > 0
            ? await LoadSourceHashesRelationalAsync(connection, transaction, snapshotId, ct)
            : ContextSnapshotLegacyJsonReader.DeserializeSourceHashes(row.SourceHashesJson);

        return new ContextSnapshot
        {
            SnapshotId = row.SnapshotId,
            RunId = row.RunId,
            ProjectId = row.ProjectId,
            CreatedUtc = row.CreatedUtc,
            CanonicalObjects = canonicalObjects,
            DeltaSummary = row.DeltaSummary,
            Warnings = warnings,
            Errors = errors,
            SourceHashes = sourceHashes
        };
    }

    private static async Task<Dictionary<string, string>> LoadSourceHashesRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid snapshotId,
        CancellationToken ct)
    {
        IEnumerable<SourceHashRow> hashRows = await connection.QueryAsync<SourceHashRow>(
            new CommandDefinition(
                """
                SELECT SourceKey, HashValue
                FROM dbo.ContextSnapshotSourceHashes
                WHERE SnapshotId = @SnapshotId
                ORDER BY SortOrder;
                """,
                new { SnapshotId = snapshotId },
                transaction,
                cancellationToken: ct));

        Dictionary<string, string> sourceHashes = new(StringComparer.Ordinal);

        foreach (SourceHashRow hr in hashRows)
            sourceHashes[hr.SourceKey] = hr.HashValue;

        return sourceHashes;
    }

    private static async Task<List<string>> LoadStringColumnRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        Guid snapshotId,
        CancellationToken ct)
    {
        IEnumerable<string> rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new { SnapshotId = snapshotId },
                transaction,
                cancellationToken: ct));

        return rows.ToList();
    }

    private static async Task<List<CanonicalObject>> LoadCanonicalObjectsRelationalAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid snapshotId,
        CancellationToken ct)
    {
        const string objectsSql = """
                                  SELECT CanonicalObjectRowId, SortOrder, ObjectId, ObjectType, Name, SourceType, SourceId
                                  FROM dbo.ContextSnapshotCanonicalObjects
                                  WHERE SnapshotId = @SnapshotId
                                  ORDER BY SortOrder;
                                  """;

        List<CanonicalObjectRow> objectRows = (await connection.QueryAsync<CanonicalObjectRow>(
            new CommandDefinition(
                objectsSql,
                new { SnapshotId = snapshotId },
                transaction,
                cancellationToken: ct))).ToList();

        if (objectRows.Count == 0)
            return [];

        List<Guid> rowIds = objectRows.Select(r => r.CanonicalObjectRowId).ToList();

        const string propsSql = """
                                SELECT CanonicalObjectRowId, PropertySortOrder, PropertyKey, PropertyValue
                                FROM dbo.ContextSnapshotCanonicalObjectProperties
                                WHERE CanonicalObjectRowId IN @RowIds
                                ORDER BY CanonicalObjectRowId, PropertySortOrder;
                                """;

        List<PropertyRow> propertyRows = (await connection.QueryAsync<PropertyRow>(
            new CommandDefinition(
                propsSql,
                new { RowIds = rowIds },
                transaction,
                cancellationToken: ct))).ToList();

        Dictionary<Guid, Dictionary<string, string>> propsByObject = new();
        foreach (PropertyRow pr in propertyRows)
        {
            if (!propsByObject.TryGetValue(pr.CanonicalObjectRowId, out Dictionary<string, string>? dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                propsByObject[pr.CanonicalObjectRowId] = dict;
            }

            dict[pr.PropertyKey] = pr.PropertyValue;
        }

        List<CanonicalObject> result = [];
        foreach (CanonicalObjectRow r in objectRows)
        {
            propsByObject.TryGetValue(r.CanonicalObjectRowId, out Dictionary<string, string>? props);
            props ??= new Dictionary<string, string>(StringComparer.Ordinal);

            result.Add(
                new CanonicalObject
                {
                    ObjectId = r.ObjectId,
                    ObjectType = r.ObjectType,
                    Name = r.Name,
                    SourceType = r.SourceType,
                    SourceId = r.SourceId,
                    Properties = props
                });
        }

        return result;
    }

    private sealed class CanonicalObjectRow
    {
        public Guid CanonicalObjectRowId
        {
            get;
            init;
        }

        public int SortOrder
        {
            get;
            init;
        }

        public string ObjectId
        {
            get;
            init;
        } = null!;

        public string ObjectType
        {
            get;
            init;
        } = null!;

        public string Name
        {
            get;
            init;
        } = null!;

        public string SourceType
        {
            get;
            init;
        } = null!;

        public string SourceId
        {
            get;
            init;
        } = null!;
    }

    private sealed class PropertyRow
    {
        public Guid CanonicalObjectRowId
        {
            get;
            init;
        }

        public int PropertySortOrder
        {
            get;
            init;
        }

        public string PropertyKey
        {
            get;
            init;
        } = null!;

        public string PropertyValue
        {
            get;
            init;
        } = null!;
    }

    private sealed class SourceHashRow
    {
        public string SourceKey
        {
            get;
            init;
        } = null!;

        public string HashValue
        {
            get;
            init;
        } = null!;
    }
}
