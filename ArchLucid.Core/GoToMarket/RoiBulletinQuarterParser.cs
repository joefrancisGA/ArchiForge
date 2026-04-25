using System.Globalization;
using System.Text.RegularExpressions;

namespace ArchLucid.Core.GoToMarket;

/// <summary>Parses bulletin quarter labels such as <c>Q1-2026</c> into UTC windows.</summary>
public static partial class RoiBulletinQuarterParser
{
    private static readonly Regex QuarterRegex = QuarterRegexImpl();

    [GeneratedRegex("^Q(?<q>[1-4])-(?<y>\\d{4})$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex QuarterRegexImpl();

    /// <summary>Parses <paramref name="quarter" /> as <c>Q1-2026</c> … <c>Q4-2026</c> (calendar quarters, UTC).</summary>
    public static bool TryParse(string quarter, out RoiBulletinQuarterWindow window, out string? error)
    {
        if (string.IsNullOrWhiteSpace(quarter))
        {
            window = new RoiBulletinQuarterWindow(string.Empty, default, default);
            error = "Quarter is required (format Q1-YYYY … Q4-YYYY).";
            return false;
        }

        string trimmed = quarter.Trim();
        Match m = QuarterRegex.Match(trimmed);

        if (!m.Success)
        {
            window = new RoiBulletinQuarterWindow(string.Empty, default, default);
            error = $"Invalid quarter '{trimmed}'. Expected Q1-YYYY through Q4-YYYY.";
            return false;
        }

        int q = int.Parse(m.Groups["q"].ValueSpan, CultureInfo.InvariantCulture);
        int year = int.Parse(m.Groups["y"].ValueSpan, CultureInfo.InvariantCulture);

        int startMonth = q switch
        {
            1 => 1,
            2 => 4,
            3 => 7,
            4 => 10,
            _ => throw new InvalidOperationException("Quarter regex only allows 1–4.")
        };

        DateTimeOffset start = new(year, startMonth, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMonths(3);

        window = new RoiBulletinQuarterWindow(trimmed, start, end);
        error = null;
        return true;
    }
}
