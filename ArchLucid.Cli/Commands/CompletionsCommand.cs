using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Emits shell completion scripts for bash, zsh, or PowerShell (static word lists — no runtime API calls).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Console output; exercised via CompletionsCommandTests.")]
public static class CompletionsCommand
{
    private static readonly string[] TopLevelCommands =
    [
        "new",
        "dev",
        "pilot",
        "try",
        "second-run",
        "trial",
        "roi-bulletin",
        "security-trust",
        "run",
        "status",
        "trace",
        "submit",
        "commit",
        "seed",
        "artifacts",
        "first-value-report",
        "sponsor-one-pager",
        "reference-evidence",
        "comparisons",
        "health",
        "marketplace",
        "golden-cohort",
        "doctor",
        "check",
        "support-bundle",
        "completions",
    ];

    /// <summary>
    /// Usage: <c>completions bash|zsh|powershell</c>.
    /// </summary>
    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: archlucid completions bash|zsh|powershell");

            return Task.FromResult(CliExitCode.UsageError);
        }

        string shell = args[0].Trim().ToLowerInvariant();

        switch (shell)
        {
            case "bash":
                Console.Write(GenerateBash());
                break;
            case "zsh":
                Console.Write(GenerateZsh());
                break;
            case "powershell":
            case "pwsh":
                Console.Write(GeneratePowerShell());
                break;
            default:
                Console.Error.WriteLine($"Unknown shell: {args[0]}. Use bash, zsh, or powershell.");

                return Task.FromResult(CliExitCode.UsageError);
        }

        return Task.FromResult(CliExitCode.Success);
    }

    private static string GenerateBash()
    {
        string words = string.Join(" ", TopLevelCommands);

        // Plain lines — avoid $""" so bash ${...} and "$(...)" are not parsed as C# interpolation.
        return string.Join(
            Environment.NewLine,
            new[]
            {
                "_archlucid_completion() {",
                "  local cur",
                "  COMPREPLY=()",
                "  cur=\"${COMP_WORDS[COMP_CWORD]}\"",
                "  if [ \"$COMP_CWORD\" -eq 1 ]; then",
                $"    COMPREPLY=( $(compgen -W \"{words}\" -- \"$cur\") )",
                "  fi",
                "  return 0",
                "}",
                "complete -F _archlucid_completion archlucid",
                string.Empty,
            });
    }

    private static string GenerateZsh()
    {
        string[] commandLines = TopLevelCommands
            .Select(static c => $"    '{c}'")
            .ToArray();

        IEnumerable<string> body = new[]
        {
            "#compdef archlucid",
            "_archlucid() {",
            "  local -a commands",
            "  commands = (",
        }.Concat(commandLines).Concat([
            "  )",
            "  _describe 'command' commands",
            "}",
            "compdef _archlucid archlucid",
            string.Empty
        ]);

        return string.Join(Environment.NewLine, body);
    }

    private static string GeneratePowerShell()
    {
        string words = string.Join(",", TopLevelCommands.Select(c => $"'{c}'"));

        return string.Join(
            Environment.NewLine,
            new[]
            {
                "Register-ArgumentCompleter -Native -CommandName archlucid -ScriptBlock {",
                "  param($wordToComplete, $commandAst, $cursorPosition)",
                $"  $commands = @({words})",
                "  $commands | Where-Object { $_ -like \"$wordToComplete*\" } | ForEach-Object {",
                "    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)",
                "  }",
                "}",
                string.Empty,
            });
    }
}
