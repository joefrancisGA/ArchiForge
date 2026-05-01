using System.Collections.Immutable;
using System.Text.RegularExpressions;

using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Core.Llm.Redaction;

/// <summary>
///     Default deny-list redactor: compiled invariant-culture regexes with bounded work via
///     <see cref="Regex.Replace(string, string, RegexOptions, TimeSpan)" />.
/// </summary>
public sealed class PromptRedactor(IOptionsMonitor<LlmPromptRedactionOptions> options, ILogger<PromptRedactor> logger)
    : IPromptRedactor
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly (string Category, Regex Pattern)[] BuiltInRules =
    [
        ("email", new Regex(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout)),
        ("ssn", new Regex(
            @"\b\d{3}-\d{2}-\d{4}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout)),
        ("credit_card", new Regex(
            @"\b(?:\d[ -]*?){13,19}\d\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout)),
        ("jwt", new Regex(
            @"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout)),
        ("api_key", new Regex(
            @"\b[A-Za-z0-9]{32,}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout))
    ];

    private readonly ILogger<PromptRedactor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IOptionsMonitor<LlmPromptRedactionOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public PromptRedactionOutcome Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return new PromptRedactionOutcome(input ?? string.Empty, ImmutableDictionary<string, int>.Empty);

        LlmPromptRedactionOptions opts = _options.CurrentValue;

        if (!opts.Enabled)
            return new PromptRedactionOutcome(input, ImmutableDictionary<string, int>.Empty);

        string replacement = string.IsNullOrWhiteSpace(opts.ReplacementToken)
            ? "[REDACTED]"
            : opts.ReplacementToken.Trim();
        Dictionary<string, int> counts = [];

        string working = input;

        foreach ((string category, Regex pattern) in BuiltInRules)
        {
            int matchCount;

            try
            {
                matchCount = pattern.Matches(working).Count;
            }
            catch (RegexMatchTimeoutException ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(ex, "Prompt redaction timed out for category {Category}", category);

                continue;
            }

            if (matchCount <= 0)
                continue;

            try
            {
                working = pattern.Replace(working, replacement);
            }
            catch (RegexMatchTimeoutException ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(ex, "Prompt redaction replace timed out for category {Category}", category);

                continue;
            }

            counts[category] = counts.GetValueOrDefault(category, 0) + matchCount;
        }

        IReadOnlyList<string> extras = opts.DenyListRegexes;

        for (int i = 0; i < extras.Count; i++)
        {
            string expr = extras[i];

            if (string.IsNullOrWhiteSpace(expr))
                continue;

            try
            {
                Regex rx = new(
                    expr.Trim(),
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    MatchTimeout);

                int matchCount = rx.Matches(working).Count;

                if (matchCount <= 0)
                    continue;

                working = rx.Replace(working, replacement);
                counts["custom"] = counts.GetValueOrDefault("custom", 0) + matchCount;
            }
            catch (ArgumentException ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(ex, "Invalid custom prompt redaction regex at index {Index}", i);
            }
            catch (RegexMatchTimeoutException ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(ex, "Custom prompt redaction timed out at index {Index}", i);
            }
        }

        return new PromptRedactionOutcome(working, counts);
    }
}
