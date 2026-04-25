using System.Collections.Generic;

using ArchLucid.Core.Configuration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Configuration;

/// <summary>Registry invariants: <see cref="ConfigurationKeyCatalog"/> and <c>docs/library/CONFIGURATION_REFERENCE.md</c> must stay aligned.</summary>
[Trait("Suite", "Configuration")]
public sealed class ConfigurationKeyCatalogTests
{
  [Fact]
  public void All_ConfigPathsAreUnique_AndInExpectedSizeRange()
  {
    IReadOnlyList<ConfigurationKeyEntry> all = ConfigurationKeyCatalog.All;
    IReadOnlyList<ConfigurationKeyEntry> cli = ConfigurationKeyCatalog.CliLocalOnly;
    all.Count
      .Should()
      .BeInRange(100, 150, "the prompt targets ~100–150 operator keys");
    cli.Count
      .Should()
      .BeInRange(1, 10, "small CLI-only overlay");
    new HashSet<string>(
        all
          .Concat(cli)
          .Select(e => e.ConfigPath), StringComparer.OrdinalIgnoreCase).Count
      .Should()
      .Be(all.Count + cli.Count, "config paths are unique (case-insensitive)");
  }

  [Fact]
  public void ReferenceDoc_ListsEveryCatalogPathInBackticks()
  {
    string p = Path.Combine(
      AppContext.BaseDirectory, "docs", "library", "CONFIGURATION_REFERENCE.md");
    File.Exists(p)
      .Should()
      .BeTrue($"expected {p} (copy from project)");
    string doc = File.ReadAllText(p);
    foreach (ConfigurationKeyEntry e in ConfigurationKeyCatalog.All
               .Concat(ConfigurationKeyCatalog.CliLocalOnly))
    {
      doc
        .Should()
        .Contain('`' + e.ConfigPath + '`', $"doc must name `{e.ConfigPath}`");
    }
  }
}
