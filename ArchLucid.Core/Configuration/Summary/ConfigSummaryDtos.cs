namespace ArchLucid.Core.Configuration.Summary;

/// <summary>Server response for <c>GET /v1/admin/config-summary</c> — presence only, no secret material.</summary>
public sealed class AdminConfigSummaryResponse
{
  public IReadOnlyList<ConfigSummaryKeyRow>? Keys
  {
    get; set;
  }
}

public sealed class ConfigSummaryKeyRow
{
  public string? ConfigPath
  {
    get; set;
  }

  public bool IsSet
  {
    get; set;
  }
}
