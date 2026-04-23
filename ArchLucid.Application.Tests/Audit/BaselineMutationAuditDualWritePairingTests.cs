using System.Text.RegularExpressions;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Audit;

/// <summary>
/// Static-source assertion that every <c>IBaselineMutationAuditService.RecordAsync</c>
/// call site in <c>ArchLucid.Application/**</c> is paired (in the same source file) with
/// a sibling durable audit call (<c>auditService.LogAsync</c>,
/// <c>DurableAuditLogRetry.TryLogAsync</c>, or <c>CoordinatorRunFailedDurableAudit.TryLogAsync</c>).
/// </summary>
/// <remarks>
/// This pins the dual-write contract documented in
/// <c>docs/library/AUDIT_COVERAGE_MATRIX.md</c> § "Known gaps". A new baseline-only call
/// site without a sibling durable call must either add the durable pair or be
/// explicitly listed here so the next reviewer sees the exception.
/// </remarks>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class BaselineMutationAuditDualWritePairingTests
{
    /// <summary>
    /// Files allowed to contain a baseline-mutation call without a sibling durable call.
    /// Empty by design — additions require a CHANGELOG entry citing why the dual-write
    /// is unnecessary or impossible.
    /// </summary>
    private static readonly HashSet<string> AllowedBaselineOnlyFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Architecture coordinator durable rows are emitted from BaselineMutationAuditService when these
        // orchestrators call RecordAsync (see BaselineMutationAuditArchitectureDurableWriter); sibling LogAsync
        // in-file is intentionally absent to avoid duplicate dbo.AuditEvents rows.
        "ArchitectureRunCreateOrchestrator.cs",
        "ArchitectureRunExecuteOrchestrator.cs",
    };

    [Fact]
    public void EveryBaselineMutationAuditCallSiteHasASiblingDurableAuditCallInTheSameFile()
    {
        string applicationRoot = ResolveApplicationProjectRoot();
        string[] sourceFiles = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories);

        List<string> violations = sourceFiles
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(HasBaselineMutationAuditCall)
            .Where(file => !HasSiblingDurableAuditCall(file))
            .Where(file => !AllowedBaselineOnlyFiles.Contains(Path.GetFileName(file)))
            .Select(file => Path.GetRelativePath(applicationRoot, file))
            .OrderBy(p => p)
            .ToList();

        violations.Should().BeEmpty(
            "every baseline-mutation audit call must dual-write to durable audit (see docs/library/AUDIT_COVERAGE_MATRIX.md). "
            + "Add a sibling auditService.LogAsync / DurableAuditLogRetry / CoordinatorRunFailedDurableAudit call, "
            + "or add the file to AllowedBaselineOnlyFiles with a CHANGELOG entry. Violators: "
            + string.Join(", ", violations));
    }

    private static bool HasBaselineMutationAuditCall(string file)
    {
        string text = File.ReadAllText(file);

        if (text.Contains("BaselineMutationAuditService", StringComparison.Ordinal))
            return false;

        if (text.Contains("IBaselineMutationAuditService", StringComparison.Ordinal))
            return ContainsRecordAsyncOnBaselineSymbol(text);

        return Regex.IsMatch(text, @"\bbaselineMutationAudit(Service)?\s*\.RecordAsync\(", RegexOptions.Compiled);
    }

    private static bool ContainsRecordAsyncOnBaselineSymbol(string text)
        => Regex.IsMatch(text, @"\bbaselineMutationAudit(Service)?\s*\.RecordAsync\(", RegexOptions.Compiled)
            || Regex.IsMatch(text, @"\b_baselineMutationAudit(Service)?\s*\.RecordAsync\(", RegexOptions.Compiled);

    private static bool HasSiblingDurableAuditCall(string file)
    {
        string text = File.ReadAllText(file);

        return Regex.IsMatch(text, @"\b(_?audit(Service)?)\s*\.LogAsync\(", RegexOptions.Compiled)
            || text.Contains("DurableAuditLogRetry.TryLogAsync", StringComparison.Ordinal)
            || text.Contains("CoordinatorRunFailedDurableAudit.TryLogAsync", StringComparison.Ordinal);
    }

    private static string ResolveApplicationProjectRoot()
    {
        string current = AppContext.BaseDirectory;
        DirectoryInfo? dir = new(current);

        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "ArchLucid.Application", "ArchLucid.Application.csproj");

            if (File.Exists(candidate))
                return Path.GetDirectoryName(candidate)!;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not resolve ArchLucid.Application project root from test base directory.");
    }
}
