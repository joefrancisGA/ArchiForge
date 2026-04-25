using System.Reflection;

using Microsoft.Data.SqlClient;

namespace ArchLucid.TestSupport;

/// <summary>Builds <see cref="SqlException" /> instances for tests (no public constructor on the exception type).</summary>
public static class SqlExceptionTestFactory
{
    /// <summary>Creates a <see cref="SqlException" /> with the given SQL Server error <paramref name="number" />.</summary>
    public static SqlException Create(int number)
    {
        SqlErrorCollection errors = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            null,
            null)!;

        SqlError error = (SqlError)Activator.CreateInstance(
            typeof(SqlError),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [
                number,
                (byte)0,
                (byte)0,
                "server",
                "message",
                "procedure",
                0,
                0,
                null
            ],
            null)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errors, [error]);

        SqlException ex = (SqlException)Activator.CreateInstance(
            typeof(SqlException),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [
                "Test SQL exception",
                errors,
                null,
                Guid.Empty
            ],
            null)!;

        return ex;
    }
}
