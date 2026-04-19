using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
/// Exclusive lock so only one process at a time runs <see cref="DatabaseMigrator"/> against a given SQL catalog.
/// CI and local runs can execute <c>dotnet test</c> on multiple assemblies in parallel; each may call
/// <see cref="DatabaseMigrator.Run"/> with the same <c>ARCHLUCID_SQL_TEST</c> connection string — without serialization,
/// <c>GreenfieldBaselineMigrationRunner</c> / DbUp can replay the same DDL and raise duplicate FK / table errors.
/// </summary>
internal sealed class MigrationCatalogMutexScope : IDisposable
{
    private readonly Mutex _mutex;
    private bool _released;

    private MigrationCatalogMutexScope(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>Blocks until this process holds an exclusive lock for the catalog implied by <paramref name="connectionString"/>.</summary>
    public static MigrationCatalogMutexScope Acquire(string connectionString, TimeSpan wait)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        string mutexName = MutexNameFromConnectionString(connectionString);
        Mutex mutex = new(initiallyOwned: false, name: mutexName);

        try
        {
            if (!mutex.WaitOne(wait))
            {
                mutex.Dispose();
                throw new TimeoutException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Timed out after {0} waiting for exclusive database migration lock '{1}'.",
                        wait,
                        mutexName));
            }

            return new MigrationCatalogMutexScope(mutex);
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }

    /// <summary>Stable, filesystem-safe name derived from the full connection string (includes server + catalog).</summary>
    private static string MutexNameFromConnectionString(string connectionString)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(connectionString));
        string suffix = Convert.ToHexString(hash.AsSpan(0, 16));

        return "ArchLucid-DbUp-" + suffix;
    }

    public void Dispose()
    {
        if (_released)
        {
            return;
        }

        _released = true;

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Mutex not owned — ignore so Dispose stays idempotent.
        }

        _mutex.Dispose();
    }
}
