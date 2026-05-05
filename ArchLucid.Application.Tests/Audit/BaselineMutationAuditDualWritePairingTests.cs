using System.Text.RegularExpressions;

namespace ArchLucid.Application.Tests.Audit;

/// <summary>
///     Static source assertion: every <see cref="ArchLucid.Application.Common.IBaselineMutationAuditService" /> call
///     site (<c>_baselineMutationAudit</c> / primary ctor <c>baselineMutationAudit</c>) must either emit durable audit
///     in-file (<c>LogAsync</c> / <c>TryLogAsync</c>) or be listed under <see cref="AllowedBaselineOnlyFiles" />.
/// </summary>
[Trait("Category", "Architecture")]
public sealed class BaselineMutationAuditDualWritePairingTests
{
    private static readonly Regex BaselineMutationRecordAsync = new(
        @"(?:_baselineMutationAudit|baselineMutationAudit)\s*\.\s*RecordAsync\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    ///     Escalation only: add filenames here when a file cannot carry an in-file <c>LogAsync</c> sibling (rare â€”
    ///     coordinator orchestrators normally route durable echoes through <see cref="ArchLucid.Application.Common.BaselineMutationAuditService" />).
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedBaselineOnlyFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Implementation / interface files â€” not call sites pairing against governance workflow.
    /// </summary>
    private static readonly IReadOnlySet<string> IgnoredFilenames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BaselineMutationAuditService.cs", "IBaselineMutationAuditService.cs", };

    [SkippableFact]
    public void BaselineMutationAudit_RecordAsync_call_sites_have_durable_pair_or_allowlist()
    {
        DirectoryInfo root = LocateArchLucidApplicationProjectDirectory();
        string[] violations = EnumerateViolationFiles(root).ToArray();

        if (violations.Length == 0)
            return;

        Assert.Fail(string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateViolationFiles(DirectoryInfo root)
    {
        return from file in root.EnumerateFiles("*.cs", SearchOption.AllDirectories)
            where !IsIgnoredUnderObjOrBin(file)
            where !IgnoredFilenames.Contains(file.Name)
            let text = File.ReadAllText(file.FullName)
            where BaselineMutationRecordAsync.IsMatch(text)
            where !AllowedBaselineOnlyFiles.Contains(file.Name)
            where !ContainsDurableAuditEvidence(text)
            select
                $"{file.FullName}: IBaselineMutationAuditService.RecordAsync without LogAsync/TryLogAsync sibling (fix or list in {nameof(AllowedBaselineOnlyFiles)})";
    }

    private static bool ContainsDurableAuditEvidence(string fileText)
    {
        return fileText.Contains("LogAsync(", StringComparison.Ordinal) || fileText.Contains("TryLogAsync(", StringComparison.Ordinal);
    }

    private static DirectoryInfo LocateArchLucidApplicationProjectDirectory()
    {
        // Referenced assembly often loads from the test project's output (e.g. Tests\bin\…\ArchLucid.Application.dll).
        // Parents of that path reach the solution root, where the csproj is in a sibling folder
        // (…\ArchLucid.Application\ArchLucid.Application.csproj), not in the ancestor directory itself.
        string assemblyLoc = typeof(Common.IBaselineMutationAuditService).Assembly.Location;

        string? assemblyDir = Path.GetDirectoryName(assemblyLoc);
        if (string.IsNullOrEmpty(assemblyDir))
            assemblyDir = AppContext.BaseDirectory;

        if (string.IsNullOrEmpty(assemblyDir))
            throw new InvalidOperationException("Could not resolve assembly directory.");

        DirectoryInfo? walk = new(assemblyDir);

        while (walk is not null)
        {
            string directCsproj = Path.Combine(walk.FullName, "ArchLucid.Application.csproj");

            if (File.Exists(directCsproj))
                return walk;

            string nestedCsproj =
                Path.Combine(walk.FullName, "ArchLucid.Application", "ArchLucid.Application.csproj");

            if (File.Exists(nestedCsproj))
                return new DirectoryInfo(Path.Combine(walk.FullName, "ArchLucid.Application"));

            walk = walk.Parent;
        }

        throw new InvalidOperationException(
            "Locate ArchLucid.Application project: walk upward from assembly path until ArchLucid.Application.csproj " +
            "(project directory or ArchLucid.Application subfolder).");
    }

    private static bool IsIgnoredUnderObjOrBin(FileInfo file)
    {
        string full = file.FullName;
        bool hasObj =
            full.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

        bool hasBin =
            full.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

        return hasObj || hasBin;
    }
}
