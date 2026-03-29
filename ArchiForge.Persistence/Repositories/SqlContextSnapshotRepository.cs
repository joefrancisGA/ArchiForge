using System.Data;

using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.ContextSnapshots;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed <see cref="IContextSnapshotRepository"/> with dual-write to legacy JSON columns
/// and relational child tables; reads prefer child rows per collection when any exist, else JSON.
/// </summary>
public sealed class SqlContextSnapshotRepository(ISqlConnectionFactory connectionFactory) : IContextSnapshotRepository
{
    public async Task<ContextSnapshot?> GetLatestAsync(string projectId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        const string sql = """
            SELECT TOP 1
                SnapshotId,
                RunId,
                ProjectId,
                CreatedUtc,
                CanonicalObjectsJson,
                DeltaSummary,
                WarningsJson,
                ErrorsJson,
                SourceHashesJson
            FROM dbo.ContextSnapshots
            WHERE ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        ContextSnapshotRow? row = await connection.QuerySingleOrDefaultAsync<ContextSnapshotRow>(
            new CommandDefinition(sql, new
            {
                ProjectId = projectId
            }, cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        return await HydrateAsync(connection, transaction: null, row, ct).ConfigureAwait(false);
    }

    public async Task<ContextSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                SnapshotId,
                RunId,
                ProjectId,
                CreatedUtc,
                CanonicalObjectsJson,
                DeltaSummary,
                WarningsJson,
                ErrorsJson,
                SourceHashesJson
            FROM dbo.ContextSnapshots
            WHERE SnapshotId = @SnapshotId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        ContextSnapshotRow? row = await connection.QuerySingleOrDefaultAsync<ContextSnapshotRow>(
            new CommandDefinition(sql, new
            {
                SnapshotId = snapshotId
            }, cancellationToken: ct)).ConfigureAwait(false);

        if (row is null)
            return null;

        return await HydrateAsync(connection, transaction: null, row, ct).ConfigureAwait(false);
    }

    public async Task SaveAsync(
        ContextSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (connection is not null)
        {
            await SaveCoreAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
        using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(snapshot, owned, tx, ct).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task SaveCoreAsync(
        ContextSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string headerSql = """
            INSERT INTO dbo.ContextSnapshots
            (
                SnapshotId, RunId, ProjectId, CreatedUtc,
                CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
            )
            VALUES
            (
                @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
            );
            """;

        object headerArgs = new
        {
            snapshot.SnapshotId,
            snapshot.RunId,
            snapshot.ProjectId,
            snapshot.CreatedUtc,
            CanonicalObjectsJson = JsonEntitySerializer.Serialize(snapshot.CanonicalObjects),
            snapshot.DeltaSummary,
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings),
            ErrorsJson = JsonEntitySerializer.Serialize(snapshot.Errors),
            SourceHashesJson = JsonEntitySerializer.Serialize(snapshot.SourceHashes)
        };

        await connection.ExecuteAsync(new CommandDefinition(headerSql, headerArgs, transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        await InsertRelationalChildrenAsync(snapshot, connection, transaction, ct).ConfigureAwait(false);
    }

    private static async Task InsertRelationalChildrenAsync(
        ContextSnapshot snapshot,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string insertObjectSql = """
            INSERT INTO dbo.ContextSnapshotCanonicalObjects
            (
                CanonicalObjectRowId, SnapshotId, SortOrder,
                ObjectId, ObjectType, Name, SourceType, SourceId
            )
            VALUES
            (
                @CanonicalObjectRowId, @SnapshotId, @SortOrder,
                @ObjectId, @ObjectType, @Name, @SourceType, @SourceId
            );
            """;

        const string insertPropertySql = """
            INSERT INTO dbo.ContextSnapshotCanonicalObjectProperties
            (CanonicalObjectRowId, PropertySortOrder, PropertyKey, PropertyValue)
            VALUES (@CanonicalObjectRowId, @PropertySortOrder, @PropertyKey, @PropertyValue);
            """;

        const string insertWarningSql = """
            INSERT INTO dbo.ContextSnapshotWarnings (SnapshotId, SortOrder, WarningText)
            VALUES (@SnapshotId, @SortOrder, @WarningText);
            """;

        const string insertErrorSql = """
            INSERT INTO dbo.ContextSnapshotErrors (SnapshotId, SortOrder, ErrorText)
            VALUES (@SnapshotId, @SortOrder, @ErrorText);
            """;

        const string insertHashSql = """
            INSERT INTO dbo.ContextSnapshotSourceHashes (SnapshotId, SortOrder, SourceKey, HashValue)
            VALUES (@SnapshotId, @SortOrder, @SourceKey, @HashValue);
            """;

        for (int i = 0; i < snapshot.CanonicalObjects.Count; i++)
        {
            CanonicalObject obj = snapshot.CanonicalObjects[i];
            Guid rowId = Guid.NewGuid();

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertObjectSql,
                    new
                    {
                        CanonicalObjectRowId = rowId,
                        snapshot.SnapshotId,
                        SortOrder = i,
                        obj.ObjectId,
                        obj.ObjectType,
                        obj.Name,
                        obj.SourceType,
                        obj.SourceId
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);

            List<KeyValuePair<string, string>> orderedProps = obj.Properties
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            for (int p = 0; p < orderedProps.Count; p++)
            {
                KeyValuePair<string, string> kv = orderedProps[p];

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertPropertySql,
                        new
                        {
                            CanonicalObjectRowId = rowId,
                            PropertySortOrder = p,
                            PropertyKey = kv.Key,
                            PropertyValue = kv.Value
                        },
                        transaction,
                        cancellationToken: ct)).ConfigureAwait(false);
            }
        }

        for (int w = 0; w < snapshot.Warnings.Count; w++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertWarningSql,
                    new
                    {
                        snapshot.SnapshotId,
                        SortOrder = w,
                        WarningText = snapshot.Warnings[w]
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);
        }

        for (int e = 0; e < snapshot.Errors.Count; e++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertErrorSql,
                    new
                    {
                        snapshot.SnapshotId,
                        SortOrder = e,
                        ErrorText = snapshot.Errors[e]
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);
        }

        List<KeyValuePair<string, string>> orderedHashes = snapshot.SourceHashes
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        for (int h = 0; h < orderedHashes.Count; h++)
        {
            KeyValuePair<string, string> kv = orderedHashes[h];

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertHashSql,
                    new
                    {
                        snapshot.SnapshotId,
                        SortOrder = h,
                        SourceKey = kv.Key,
                        HashValue = kv.Value
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);
        }
    }

    private static async Task<ContextSnapshot> HydrateAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        ContextSnapshotRow row,
        CancellationToken ct)
    {
        Guid snapshotId = row.SnapshotId;

        int canonicalCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotCanonicalObjects WHERE SnapshotId = @SnapshotId",
            new
            {
                SnapshotId = snapshotId
            },
            ct).ConfigureAwait(false);

        int warningsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotWarnings WHERE SnapshotId = @SnapshotId",
            new
            {
                SnapshotId = snapshotId
            },
            ct).ConfigureAwait(false);

        int errorsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotErrors WHERE SnapshotId = @SnapshotId",
            new
            {
                SnapshotId = snapshotId
            },
            ct).ConfigureAwait(false);

        int hashesCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ContextSnapshotSourceHashes WHERE SnapshotId = @SnapshotId",
            new
            {
                SnapshotId = snapshotId
            },
            ct).ConfigureAwait(false);

        List<CanonicalObject> canonicalObjects;
        if (canonicalCount > 0)
        {
            canonicalObjects = await LoadCanonicalObjectsRelationalAsync(connection, transaction, snapshotId, ct)
                .ConfigureAwait(false);
        }
        else
        {
            canonicalObjects = ContextSnapshotJsonFallback.DeserializeCanonicalObjects(row.CanonicalObjectsJson);
        }

        List<string> warnings;
        if (warningsCount > 0)
        {
            warnings = await LoadStringColumnRelationalAsync(
                connection,
                transaction,
                """
                SELECT WarningText AS Item
                FROM dbo.ContextSnapshotWarnings
                WHERE SnapshotId = @SnapshotId
                ORDER BY SortOrder;
                """,
                snapshotId,
                ct).ConfigureAwait(false);
        }
        else
        {
            warnings = ContextSnapshotJsonFallback.DeserializeStringList(row.WarningsJson);
        }

        List<string> errors;
        if (errorsCount > 0)
        {
            errors = await LoadStringColumnRelationalAsync(
                connection,
                transaction,
                """
                SELECT ErrorText AS Item
                FROM dbo.ContextSnapshotErrors
                WHERE SnapshotId = @SnapshotId
                ORDER BY SortOrder;
                """,
                snapshotId,
                ct).ConfigureAwait(false);
        }
        else
        {
            errors = ContextSnapshotJsonFallback.DeserializeStringList(row.ErrorsJson);
        }

        Dictionary<string, string> sourceHashes;
        if (hashesCount > 0)
        {
            IEnumerable<SourceHashRow> hashRows = await connection.QueryAsync<SourceHashRow>(
                new CommandDefinition(
                    """
                    SELECT SourceKey, HashValue
                    FROM dbo.ContextSnapshotSourceHashes
                    WHERE SnapshotId = @SnapshotId
                    ORDER BY SortOrder;
                    """,
                    new
                    {
                        SnapshotId = snapshotId
                    },
                    transaction,
                    cancellationToken: ct)).ConfigureAwait(false);

            sourceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (SourceHashRow hr in hashRows)
                sourceHashes[hr.SourceKey] = hr.HashValue;
        }
        else
        {
            sourceHashes = ContextSnapshotJsonFallback.DeserializeStringDictionary(row.SourceHashesJson);
        }

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

    private static async Task<int> ScalarCountAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct))
            .ConfigureAwait(false);
        return count;
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
                new
                {
                    SnapshotId = snapshotId
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false);

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
                new
                {
                    SnapshotId = snapshotId
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

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
                new
                {
                    RowIds = rowIds
                },
                transaction,
                cancellationToken: ct)).ConfigureAwait(false)).ToList();

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

    private sealed class ContextSnapshotRow
    {
        public Guid SnapshotId
        {
            get; init;
        }

        public Guid RunId
        {
            get; init;
        }

        public string ProjectId { get; init; } = null!;

        public DateTime CreatedUtc
        {
            get; init;
        }

        public string CanonicalObjectsJson { get; init; } = null!;

        public string? DeltaSummary
        {
            get; init;
        }

        public string WarningsJson { get; init; } = null!;

        public string ErrorsJson { get; init; } = null!;

        public string SourceHashesJson { get; init; } = null!;
    }

    private sealed class CanonicalObjectRow
    {
        public Guid CanonicalObjectRowId
        {
            get; init;
        }

        public int SortOrder
        {
            get; init;
        }

        public string ObjectId { get; init; } = null!;

        public string ObjectType { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string SourceType { get; init; } = null!;

        public string SourceId { get; init; } = null!;
    }

    private sealed class PropertyRow
    {
        public Guid CanonicalObjectRowId
        {
            get; init;
        }

        public int PropertySortOrder
        {
            get; init;
        }

        public string PropertyKey { get; init; } = null!;

        public string PropertyValue { get; init; } = null!;
    }

    private sealed class SourceHashRow
    {
        public string SourceKey { get; init; } = null!;

        public string HashValue { get; init; } = null!;
    }
}
