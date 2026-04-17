using System.Text.RegularExpressions;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Regression guard: versioned API controllers should not return bare MVC <c>NotFound()</c> / <c>Conflict</c>,
/// bare numeric <c>StatusCode(404)</c>, or <c>StatusCode(StatusCodes.Status404NotFound)</c> without RFC 9457 Problem Details (see <c>docs/API_ERROR_CONTRACT.md</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiControllerProblemDetailsSourceGuardTests
{
    private static string ControllersDirectory()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            string sln = Path.Combine(dir.FullName, "ArchLucid.sln");
            if (File.Exists(sln))
            {
                string controllers = Path.Combine(dir.FullName, "ArchLucid.Api", "Controllers");
                if (Directory.Exists(controllers))
                {
                    return controllers;
                }
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate ArchLucid.sln / ArchLucid.Api/Controllers from test base directory.");
    }

    [Fact]
    public void Controller_sources_must_not_use_bare_NotFound_or_Conflict_results()
    {
        string root = ControllersDirectory();
        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
        files.Length.Should().BeGreaterThan(0);

        // Any `return NotFound(` / `Conflict(` / `BadRequest(` is a violation: use ProblemDetailsExtensions
        // (`NotFoundProblem`, `ConflictProblem`, `BadRequestProblem`). `this.NotFoundProblem` does not match
        // `return NotFound(` because the token after `return` is `this`.
        Regex returnNotFound = new(@"\breturn\s+NotFound\s*\(", RegexOptions.CultureInvariant);
        Regex returnConflict = new(@"\breturn\s+Conflict\s*\(", RegexOptions.CultureInvariant);
        Regex returnBadRequest = new(@"\breturn\s+BadRequest\s*\(", RegexOptions.CultureInvariant);
        Regex bareStatusCode = new(@"\breturn\s+StatusCode\s*\(\s*\d+\s*\)\s*;", RegexOptions.CultureInvariant);
        Regex statusCodeNamed404 = new(
            @"\breturn\s+StatusCode\s*\(\s*StatusCodes\.Status404NotFound\s*\)\s*;",
            RegexOptions.CultureInvariant);
        Regex objectResultWithStatus = new(@"new\s+ObjectResult\s*\([^)]*\)\s*\{[^}]*StatusCode\s*=", RegexOptions.CultureInvariant);
        List<string> violations = [];

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);

            if (returnNotFound.IsMatch(text))
            {
                violations.Add($"{file}: return NotFound(...) — use NotFoundProblem per RFC 9457");
            }

            if (returnConflict.IsMatch(text))
            {
                violations.Add($"{file}: return Conflict(...) — use ConflictProblem per RFC 9457");
            }

            if (returnBadRequest.IsMatch(text))
            {
                violations.Add($"{file}: return BadRequest(...) — use BadRequestProblem per RFC 9457");
            }

            if (bareStatusCode.IsMatch(text))
            {
                violations.Add($"{file}: bare StatusCode(nnn) — use Problem/IActionResult factory per docs/API_ERROR_CONTRACT.md");
            }

            if (statusCodeNamed404.IsMatch(text))
            {
                violations.Add(
                    $"{file}: return StatusCode(StatusCodes.Status404NotFound) — use NotFoundProblem per RFC 9457");
            }

            if (objectResultWithStatus.IsMatch(text))
            {
                violations.Add($"{file}: ObjectResult with StatusCode property — prefer typed Problem() helpers");
            }
        }

        violations.Should().BeEmpty(
            "use ProblemDetailsExtensions (e.g. NotFoundProblem, ConflictProblem) per docs/API_ERROR_CONTRACT.md: " +
            string.Join("; ", violations));
    }
}
