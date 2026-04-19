using System.Security.Cryptography;
using System.Text;

using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Identity;

/// <summary>Have I Been Pwned k-anonymity range API (SHA-1 prefix).</summary>
public sealed class PwnedPasswordRangeClient(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<TrialAuthOptions> trialOptions)
{
    /// <summary>How long downloaded HIBP range lines stay in <see cref="IMemoryCache"/> (per SHA-1 prefix).</summary>
    public static readonly TimeSpan RangeResponseCacheDuration = TimeSpan.FromHours(24);

    private const string CacheKeyPrefix = "pwned-range:";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly TrialAuthOptions _trial =
        trialOptions?.Value ?? throw new ArgumentNullException(nameof(trialOptions));

    /// <summary>True when the full SHA-1 hash of the password appears in the downloaded range set.</summary>
    public async Task<bool> IsPasswordPwnedAsync(string password, CancellationToken cancellationToken)
    {
        if (!_trial.LocalIdentity.PwnedPasswordRangeCheckEnabled)
            return false;

        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] sha1 = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        string fullHex = Convert.ToHexString(sha1);
        string prefix = fullHex[..5];
        string suffix = fullHex[5..];

        string cacheKey = CacheKeyPrefix + prefix;

        if (_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? suffixes) && suffixes is not null)
            return suffixes.Contains(suffix, StringComparer.OrdinalIgnoreCase);

        using HttpResponseMessage response = await _httpClient.GetAsync(
            new Uri($"https://api.pwnedpasswords.com/range/{prefix}", UriKind.Absolute),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        HashSet<string> set = ParseRangeBody(body);

        _cache.Set(
            cacheKey,
            (IReadOnlySet<string>)set,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = RangeResponseCacheDuration });

        return set.Contains(suffix, StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> ParseRangeBody(string body)
    {
        HashSet<string> suffixes = new(StringComparer.OrdinalIgnoreCase);

        using StringReader reader = new(body);

        while (reader.ReadLine() is { } line)
        {
            int colon = line.IndexOf(':', StringComparison.Ordinal);

            if (colon <= 0)
                continue;

            string suffix = line[..colon].Trim();

            if (suffix.Length > 0)
                suffixes.Add(suffix);
        }

        return suffixes;
    }
}
