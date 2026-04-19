namespace ArchLucid.Host.Composition.Configuration;

/// <summary>
/// Registers persistence and authority storage for one <see cref="ArchLucid.Host.Core.Configuration.ArchLucidOptions.StorageProvider"/> mode.
/// Keeps Sql vs InMemory paths separate so shared registrations are not omitted from one branch.
/// </summary>
internal interface IStorageProviderRegistrar
{
    void Register(IServiceCollection services, IConfiguration configuration);
}
