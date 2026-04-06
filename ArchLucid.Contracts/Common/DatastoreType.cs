namespace ArchiForge.Contracts.Common;

/// <summary>Classifies the storage technology used by a datastore in the architecture manifest.</summary>
public enum DatastoreType
{
    /// <summary>Relational (SQL) database.</summary>
    Sql = 1,
    /// <summary>Document or key-value store (NoSQL).</summary>
    NoSql = 2,
    /// <summary>Object / blob storage.</summary>
    Object = 3,
    /// <summary>In-memory or distributed cache.</summary>
    Cache = 4,
    /// <summary>Full-text or vector search index.</summary>
    Search = 5
}
