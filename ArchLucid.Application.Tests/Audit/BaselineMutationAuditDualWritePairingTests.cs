using System.Text.RegularExpressions;

using Xunit;

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
    ///     Escalation only: add filenames here when a file cannot carry an in-file <c>LogAsync</c> sibling (rare —
    ///     coordinator orchestrators normally route durable echoes through <see cref="ArchLucid.Application.Common.BaselineMutationAuditService" />).
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedBaselineOnlyFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Implementation / interface files — not call sites pairing against governance workflow.
    /// </summary>
    private static readonly IReadOnlySet<string> IgnoredFilenames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BaselineMutationAuditService.cs",
            "IBaselineMutationAuditService.cs",
        };

    [Fact]
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
        foreach (FileInfo file in root.EnumerateFiles("*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredUnderObjOrBin(file))
                continue;

            if (IgnoredFilenames.Contains(file.Name))
                continue;

            string text = File.ReadAllText(file.FullName);

            if (!BaselineMutationRecordAsync.IsMatch(text))
                continue;

            if (AllowedBaselineOnlyFiles.Contains(file.Name))
                continue;

            if (ContainsDurableAuditEvidence(text))
                continue;

            yield return $"{file.FullName}: IBaselineMutationAuditService.RecordAsync without LogAsync/TryLogAsync sibling (fix or list in {nameof(AllowedBaselineOnlyFiles)})";
        }
    }

    private static bool ContainsDurableAuditEvidence(string fileText)
    {
        if (fileText.Contains("LogAsync(", StringComparison.Ordinal) || fileText.Contains("TryLogAsync(", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static DirectoryInfo LocateArchLucidApplicationProjectDirectory()
    {
        string? assemblyLoc = typeof(ArchLucid.Application.Common.IBaselineMutationAuditService).Assembly.Location;

        string? assemblyDir = Path.GetDirectoryName(assemblyLoc);
        if (string.IsNullOrEmpty(assemblyDir))
            throw new InvalidOperationException("Could not resolve assembly directory.");

        DirectoryInfo? walk = new(assemblyDir);

        while (walk is not null)
        {
            string csproj = Path.Combine(walk.FullName, "ArchLucid.Application.csproj");

            if (File.Exists(csproj))
                return walk;

            walk = walk.Parent;
        }

        throw new InvalidOperationException(
            "Locate ArchLucid.Application project: walk upward from assembly path until ArchLucid.Application.csproj.");
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
