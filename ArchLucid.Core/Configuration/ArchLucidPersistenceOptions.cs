namespace ArchLucid.Core.Configuration;

/// <summary>
///     Cross-cutting persistence security switches (see <c>ArchLucid:Persistence</c>).
/// </summary>
/// <remarks>
///     Do not use <c>public const string</c> for the configuration path: NetArchTest treats <c>':'</c> like <c>'.'</c>
///     when scanning string <em>field constants</em>, so <c>"ArchLucid:Persistence"</c> is parsed as the namespace
///     <c>ArchLucid.Persistence</c> and fails <c>Core_must_not_depend_on_any_solution_project</c> in Release.
/// </remarks>
public sealed class ArchLucidPersistenceOptions
{
    /// <summary>Configuration section path <c>ArchLucid:Persistence</c> (not a <c>const</c>; see remarks on enclosing type).</summary>
    public static string SectionPath => string.Concat("ArchLucid", ":", "Persistence");

    /// <summary>
    ///     When true, allows <see cref="ArchLucid.Core.Scoping.SqlRowLevelSecurityBypassAmbient.Enter" /> together with
    ///     process environment <c>ARCHLUCID_ALLOW_RLS_BYPASS=true</c>. Both are required for break-glass RLS bypass.
    /// </summary>
    public bool AllowRlsBypass
    {
        get;
        set;
    }

    /// <summary>
    ///     When greater than zero, sets Dapper's global <c>SqlMapper.Settings.CommandTimeout</c> (seconds) for SQL-backed
    ///     hosts.
    ///     SqlClient defaults to 30s per command; long authority create paths on cold CI can exceed that without this knob.
    /// </summary>
    public int DefaultSqlCommandTimeoutSeconds
    {
        get;
        set;
    }
}
