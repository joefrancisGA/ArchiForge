using ArchLucid.Core.Configuration;

using Dapper;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Persistence.Configuration;

/// <summary>
///     Applies optional Dapper-wide command timeout from <see cref="ArchLucidPersistenceOptions" /> (SQL hosts only).
/// </summary>
public static class DapperGlobalCommandTimeoutBootstrap
{
    /// <summary>
    ///     When <see cref="ArchLucidPersistenceOptions.DefaultSqlCommandTimeoutSeconds" /> is positive, assigns
    ///     <see cref="SqlMapper.Settings.CommandTimeout" /> so Dapper commands do not use SqlClient's 30s default only.
    /// </summary>
    public static void ApplyIfConfigured(IConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        int? seconds = configuration.GetValue<int?>(
            $"{ArchLucidPersistenceOptions.SectionPath}:DefaultSqlCommandTimeoutSeconds");

        if (seconds is null || seconds <= 0)
            return;

        SqlMapper.Settings.CommandTimeout = seconds.Value;
    }
}
