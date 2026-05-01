using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class MigrationCatalogMutexScopeTests
{
    private const string SampleConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=ArchLucidMutexTest;Integrated Security=true;TrustServerCertificate=true;";

    [SkippableFact]
    public void Acquire_Throws_WhenConnectionStringNull()
    {
        Action act = () => MigrationCatalogMutexScope.Acquire(null!, TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [SkippableFact]
    public void Dispose_IsIdempotent_AndReleasesMutexForSameCatalog()
    {
        using (MigrationCatalogMutexScope.Acquire(SampleConnectionString, TimeSpan.FromMinutes(1)))
        {
        }

        using (MigrationCatalogMutexScope second = MigrationCatalogMutexScope.Acquire(
                   SampleConnectionString,
                   TimeSpan.FromMinutes(1)))
        {
            second.Should().NotBeNull();
        }
    }

    [SkippableFact]
    public async Task Acquire_ThrowsTimeoutException_WhenSameCatalogAlreadyLocked()
    {
        using ManualResetEventSlim holderReady = new(false);
        using ManualResetEventSlim releaseHolder = new(false);
        Exception? backgroundError = null;

        Task holder = Task.Run(() =>
        {
            try
            {
                using (MigrationCatalogMutexScope.Acquire(SampleConnectionString, TimeSpan.FromMinutes(1)))
                {
                    holderReady.Set();
                    releaseHolder.Wait(TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                backgroundError = ex;
            }
        });

        holderReady.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("background task should acquire mutex");
        backgroundError.Should().BeNull();

        try
        {
            Action act = () => MigrationCatalogMutexScope.Acquire(SampleConnectionString, TimeSpan.FromMilliseconds(200));

            act.Should().Throw<TimeoutException>()
                .WithMessage("*waiting for exclusive database migration lock*");
        }
        finally
        {
            releaseHolder.Set();
        }

        await holder;
        backgroundError.Should().BeNull();
    }
}
