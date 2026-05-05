using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "Console/report integration; evaluator covered by tests.")]
internal static class ValidateConfigCommand
{
    private static readonly JsonSerializerOptions JsonWriter = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true,
    };

    internal static Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        ArchLucidProjectScaffolder.ArchLucidCliConfig? cli = CliCommandShared.TryLoadConfigFromCwd();

        string contentRoot = Directory.GetCurrentDirectory();

        bool appsettingsExists = ValidateConfigConfigurationFactory.AppsettingsFileExists(contentRoot);

        IConfiguration configuration = ValidateConfigConfigurationFactory.BuildMerged(cli);

        IReadOnlyList<ValidateConfigFinding> findings = ValidateConfigEvaluator.Evaluate(
            configuration,
            contentRoot,
            appsettingsExists);

        int errors = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Error);

        int warnings = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Warning);

        int passed = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Ok);

        bool ok = errors == 0;

        if (CliExecutionContext.JsonOutput)

            WriteJson(findings, ok, errors, warnings, passed);

        else

            WriteConsoleReport(findings, ok, errors, warnings, passed);

        return Task.FromResult(ok ? CliExitCode.Success : CliExitCode.OperationFailed);
    }

    private static void WriteJson(
        IReadOnlyList<ValidateConfigFinding> findings,
        bool ok,
        int errors,
        int warnings,
        int passed)
    {
        var payload = new
        {
            ok,
            summary = new { errors, warnings, passed, info = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Info) },
            findings = findings
                .Select(f => new
                {
                    severity = f.Severity.ToString(), category = f.Category, check = f.Check, detail = f.Detail,
                })
                .ToList(),
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, JsonWriter));
    }

    private static int SeparatorLineLength =>
        Console.IsOutputRedirected ? 120 : GetConsoleWidthSafe();

    private static int GetConsoleWidthSafe()
    {
        try
        {
            int w = Console.WindowWidth;

            return w > 1 ? w - 1 : 120;
        }
        catch (IOException)
        {
            return 120;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return 120;
        }
    }

    private static void WriteConsoleReport(
        IReadOnlyList<ValidateConfigFinding> findings,
        bool ok,
        int errors,
        int warnings,
        int passed)
    {
        ConsoleColor previous = Console.ForegroundColor;

        try
        {
            WriteColored(ok ? ConsoleColor.Green : ConsoleColor.Red, ok ? "[PASS]" : "[FAIL]");
            Console.WriteLine(" archlucid validate-config");

            Console.WriteLine();
            Console.WriteLine($"{"SEV".PadRight(10)} {"CATEGORY".PadRight(20)} {"CHECK".PadRight(40)} DETAIL");

            Console.WriteLine(new string('-', Math.Min(120, SeparatorLineLength)));

            foreach (ValidateConfigFinding f in findings)
            {
                ConsoleColor fg = SeverityToColor(f.Severity);

                WriteColored(fg, f.Severity.ToString().PadRight(10));
                Console.Write($"{f.Category.PadRight(20)} ");

                Console.Write($"{Truncate(f.Check, 40).PadRight(40)} ");
                Console.WriteLine(f.Detail);
            }

            Console.WriteLine(new string('-', Math.Min(120, SeparatorLineLength)));

            WriteLineColored(
                warnings > 0 ? ConsoleColor.Yellow : ConsoleColor.Gray,
                $"Summary: {errors} error(s), {warnings} warning(s), {passed} passed (Ok), "
                + $"{findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Info)} info.");
        }

        finally
        {
            Console.ForegroundColor = previous;
        }
    }

    private static ConsoleColor SeverityToColor(ValidateConfigFindingSeverity severity) =>
        severity switch
        {
            ValidateConfigFindingSeverity.Error => ConsoleColor.Red,
            ValidateConfigFindingSeverity.Warning => ConsoleColor.Yellow,
            ValidateConfigFindingSeverity.Ok => ConsoleColor.Green,
            ValidateConfigFindingSeverity.Info => ConsoleColor.Cyan,
            _ => ConsoleColor.Gray,
        };

    private static void WriteColored(ConsoleColor color, string text)
    {
        ConsoleColor prev = Console.ForegroundColor;

        Console.ForegroundColor = color;

        Console.Write(text);

        Console.ForegroundColor = prev;
    }

    private static void WriteLineColored(ConsoleColor color, string line)
    {
        ConsoleColor before = Console.ForegroundColor;

        Console.ForegroundColor = color;

        Console.WriteLine(line);

        Console.ForegroundColor = before;
    }

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))

            return value;

        if (value.Length <= maxLen)

            return value;

        return string.Concat(value.AsSpan(0, maxLen - 1), "…");
    }
}
