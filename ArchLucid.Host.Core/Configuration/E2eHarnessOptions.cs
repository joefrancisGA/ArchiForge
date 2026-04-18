namespace ArchLucid.Host.Core.Configuration;

/// <summary>Configuration for <c>POST /v1/e2e/*</c> harness routes (CI + local only; never for production tenants).</summary>
public sealed class E2eHarnessOptions
{
    /// <summary>Configuration path <c>ArchLucid:E2eHarness</c>.</summary>
    public const string SectionName = "ArchLucid:E2eHarness";

    /// <summary>When true (with <see cref="SharedSecret"/>), harness routes respond outside strict Development classification (used by CI).</summary>
    public bool Enabled { get; init; }

    /// <summary>Shared secret sent as <c>X-ArchLucid-E2e-Harness-Secret</c> on harness requests.</summary>
    public string? SharedSecret { get; init; }
}
