namespace ArchLucid.Core.Configuration;

/// <summary>Outcome for a single <see cref="ConfigurationKeyEntry"/> during <c>archlucid config check</c>.</summary>
public sealed record ConfigCheckLine(
  string ConfigPath,
  bool IsSet,
  string Source,
  bool IsRequired,
  string Notes);

public sealed class ConfigCheckSummary
{
  public int RequiredSatisfied
  {
    get; init;
  }

  public int RequiredTotal
  {
    get; init;
  }

  public int OptionalSet
  {
    get; init;
  }

  public int OptionalTotal
  {
    get; init;
  }
}
